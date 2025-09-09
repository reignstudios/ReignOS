using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using ReignOS.Core;

namespace ReignOS.Installer.Views;

enum MessageBoxOption
{
    Cancel,
    Option1,
    Option2
}

enum InstallerStage
{
    Start,
    Network,
    Drive,
    Install,
    Installing,
    DoneInstalling
}

class SSID
{
    public string name;
    public string desc;
    public bool connected;

    public SSID(string name, string desc, bool connected)
    {
        this.name = name;
        this.desc = desc;
        this.connected = connected;
    }
}

public partial class MainView : UserControl
{
    public static MainView singleton { get; private set; }
    private InstallerStage stage;

    private System.Timers.Timer connectedTimer;
    private List<string> wlanDevices = new List<string>();
    private string wlanDevice;
    private bool isConnected;
    
    private List<Drive> drives;
    private Drive efiDrive, ext4Drive;
    private Partition efiPartition, ext4Partition;
    private const string efiPartitionName = "ReignOS_EFI";
    private const string ext4PartitionName = "ReignOS";

    private DateTime allowRotApplyTime;
    
    public MainView()
    {
        singleton = this;
        InitializeComponent();
        versionText.Text = "Version: " + VersionInfo.version;
        compositorText.Text = "Compositor: " + Program.compositorMode.ToString();
        if (Design.IsDesignMode) return;
        
        InstallUtil.InstallProgress += InstallProgress;
        MainWindow.singleton.Closing += Window_Closing;

        connectedTimer = new System.Timers.Timer(1000 * 5);
        connectedTimer.Elapsed += ConnectedTimer;
        connectedTimer.AutoReset = true;
        connectedTimer.Start();
        ConnectedTimer(null, null);

        // load rotation
        allowRotApplyTime = DateTime.Now;
        const string configPath = "/home/gamer/.config/";
        const string westonSettingsFile = configPath + "weston-settings.txt";
        if (File.Exists(westonSettingsFile))
        {
            try
            {
                using (var readerSettings = new StreamReader(westonSettingsFile))
                {
                    string value = readerSettings.ReadLine();
                    if (value == "rot=default") defaultRotRadioButton.IsChecked = true;
                    else if (value == "rot=left") leftRotRadioButton.IsChecked = true;
                    else if (value == "rot=right") rightRotRadioButton.IsChecked = true;
                    else if (value == "rot=flip") flipRotRadioButton.IsChecked = true;
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(e);
            }
        }
    }

    private void Window_Closing(object sender, WindowClosingEventArgs e)
    {
        lock (this)
        {
            connectedTimer.Stop();
            connectedTimer.Dispose();
            connectedTimer = null;
        }
    }

    private void ConnectedTimer(object sender, ElapsedEventArgs e)
    {
        lock (this) if (connectedTimer == null) return;
        try
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    isConnected = App.IsOnline();
                    if (stage == InstallerStage.Network) nextButton.IsEnabled = isConnected;
                    if (isConnected)
                    {
                        isConnectedText.Text = "Network Connected";
                        isConnectedText.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        isConnectedText.Text = "Network Disconnected";
                        isConnectedText.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex);
                }
            });
        }
        catch (Exception ex)
        {
            Log.WriteLine(ex);
        }
    }

    private void InstallProgress(string task, float progress)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            installText.Text = task;
            installProgressBar.Value = progress;
            if (progress >= 100)
            {
                nextButton.Content = "Restart";
                nextButton.IsEnabled = true;
                exitButton.IsEnabled = true;
                stage = InstallerStage.DoneInstalling;
            }
            else if (task == "Failed")
            {
                exitButton.IsEnabled = true;
            }
        });
    }

    private static StringBuilder installOutputBuilder = new StringBuilder();
    public static void ProcessOutput(string line)
    {
        //Log.WriteLine(line);// NOTE: this is already logged from ProcessUtil
        bool error = InstallUtil.failIfError && line.ToLower().Contains("error:");
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            installOutputBuilder.AppendLine(line);
            const int maxLength = 2048;
            if (installOutputBuilder.Length > maxLength) installOutputBuilder.Remove(0, installOutputBuilder.Length - maxLength);
            singleton.installTerminalText.Text = installOutputBuilder.ToString();
            singleton.installTerminalScroll.ScrollToEnd();

            // allow user to exit installer
            if (error)
            {
                singleton.exitButton.IsEnabled = true;
            }
        });

        // cancel install
        if (error)
        {
            InstallUtil.cancel = true;
        }
    }

    private void RotationToggleButton_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        if ((now - allowRotApplyTime).TotalSeconds < 3) return;

        static void WriteWestonSettings(StreamWriter writer, string rotation, string display)
        {
            writer.WriteLine("[output]");
            writer.WriteLine($"name={display}");
            writer.WriteLine($"transform={rotation}");
        }

        static string GetWestonDisplay()
        {
            try
            {
                string result = ProcessUtil.Run("wayland-info", "", useBash:false);
                var lines = result.Split('\n');
                bool outputMode = false;
                foreach (string line in lines)
                {
                    if (outputMode)
                    {
                        if (line.Contains("name: "))
                        {
                            var match = Regex.Match(line, @"name:\s*(.*)");
                            if (match.Success)
                            {
                                return match.Groups[1].Value.Trim();
                            }
                            break;
                        }
                    }
                    else if (line.Contains("'wl_output'"))
                    {
                        outputMode = true;
                    }
                }
            }
            catch {}
            return "ERROR";
        }

        static string GetWaylandDisplay()
        {
            try
            {
                string result = ProcessUtil.Run("wlr-randr", "", out _);
                var lines = result.Split('\n');
                result = lines[0].Split(" ")[0];
                return result;
            }
            catch {}
            return "ERROR";
        }
        
        if (Program.compositorMode == CompositorMode.Weston)
        {
            try
            {
                const string configPath = "/home/gamer/.config/";
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);
                const string westonConfigFile = configPath + "weston.ini";
                const string westonSettingsFile = configPath + "weston-settings.txt";
                using (var writer = new StreamWriter(westonConfigFile))
                using (var writerSettings = new StreamWriter(westonSettingsFile))
                {
                    if (defaultRotRadioButton.IsChecked == true)
                    {
                        WriteWestonSettings(writer, "normal", GetWestonDisplay());
                        writerSettings.WriteLine("rot=default");
                    }
                    else if (leftRotRadioButton.IsChecked == true)
                    {
                        WriteWestonSettings(writer, "rotate-90", GetWestonDisplay());
                        writerSettings.WriteLine("rot=left");
                    }
                    else if (rightRotRadioButton.IsChecked == true)
                    {
                        WriteWestonSettings(writer, "rotate-270", GetWestonDisplay());
                        writerSettings.WriteLine("rot=right");
                    }
                    else if (flipRotRadioButton.IsChecked == true)
                    {
                        WriteWestonSettings(writer, "rotate-180", GetWestonDisplay());
                        writerSettings.WriteLine("rot=flip");
                    }
                }
                MainWindow.singleton.Close();// exit so rotation takes effect
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex);
            }
        }
        else
        {
            if (defaultRotRadioButton.IsChecked == true)
            {
                ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform normal", out _);
            }
            else if (leftRotRadioButton.IsChecked == true)
            {
                ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 90", out _);// 270, flipped-270 (options)
            }
            else if (rightRotRadioButton.IsChecked == true)
            {
                ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 270", out _);// 90, flipped-90 (options)
            }
            else if (flipRotRadioButton.IsChecked == true)
            {
                ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 180", out _);// 180, flipped, flipped-180 (options)
            }
        }
    }

    private void ShutdownButton_OnClick(object sender, RoutedEventArgs e)
    {
        ProcessUtil.Run("poweroff", "", out _, wait:false);
        MainWindow.singleton.Close();
    }

    private void NextButton_OnClick(object sender, RoutedEventArgs e)
    {
        switch (stage)
        {
            case InstallerStage.Start:
                stage = InstallerStage.Network;
                startPage.IsVisible = false;
                networkSelectPage.IsVisible = true;
                backButton.IsEnabled = true;
                nextButton.IsEnabled = isConnected;
                RefreshNetworkPage();
                break;
            
            case InstallerStage.Network:
                stage = InstallerStage.Drive;
                networkSelectPage.IsVisible = false;
                drivePage.IsVisible = true;
                RefreshDrivePage();
                break;
            
            case InstallerStage.Drive:
                stage = InstallerStage.Install;
                drivePage.IsVisible = false;
                installPage.IsVisible = true;
                nextButton.Content = "Install";
                break;
            
            case InstallerStage.Install:
                stage = InstallerStage.Installing;
                backButton.IsEnabled = false;
                nextButton.IsEnabled = false;
                exitButton.IsEnabled = false;
                defaultRotRadioButton.IsEnabled = false;
                leftRotRadioButton.IsEnabled = false;
                rightRotRadioButton.IsEnabled = false;
                flipRotRadioButton.IsEnabled = false;
                installProgressBar.Value = 0;
                installProgressBar.IsVisible = true;
                installTerminalText.Text = "";
                installTerminalScroll.IsVisible = true;
                refreshIntegrityCheckbox.IsEnabled = false;
                InstallUtil.Install(efiPartition, ext4Partition, refreshIntegrityCheckbox.IsChecked == true);
                break;
            
            case InstallerStage.DoneInstalling:
                ProcessUtil.Run("reboot", "", out _, wait:false);
                MainWindow.singleton.Close();
                break;
        }
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        switch (stage)
        {
            case InstallerStage.Network:
                stage = InstallerStage.Start;
                startPage.IsVisible = true;
                networkSelectPage.IsVisible = false;
                backButton.IsEnabled = false;
                nextButton.IsEnabled = true;
                break;
            
            case InstallerStage.Drive:
                stage = InstallerStage.Network;
                networkSelectPage.IsVisible = true;
                drivePage.IsVisible = false;
                nextButton.IsEnabled = isConnected;
                break;
            
            case InstallerStage.Install:
                stage = InstallerStage.Drive;
                drivePage.IsVisible = true;
                installPage.IsVisible = false;
                nextButton.Content = "Next";
                break;
        }
    }
    
    private void RefreshNetworkButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshNetworkPage();
    }

    private void RefreshNetworkPage()
    {
        // find wlan devices
        void deviceOut(string line)
        {
            if (line.Contains("-------") || line.Contains("Name")) return;
            try
            {
                var match = Regex.Match(line, @"\s*(wlan\d)");
                if (match.Success)
                {
                    wlanDevices.Add(match.Groups[1].Value);
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(e);
            }
        }

        wlanDevices.Clear();
        ProcessUtil.Run("iwctl", "device list", standardOut:deviceOut, useBash: false);
        
        // choose device
        if (wlanDevices.Count == 0) return;
        wlanDevice = wlanDevices[0];
        Log.WriteLine("wlanDevice: " + wlanDevice);
        
        // get SSID
        int networkNameIndex = 0, securityIndex = 0, signalIndex = 0;
        var ssids = new List<SSID>();
        void ssidOut(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Contains("-------") || line.Contains("Available networks")) return;
            try
            {
                string lineClipped;
                
                // get index values
                if (line.Contains("Network name"))
                {
                    lineClipped = line.Replace("[0m[1;90m", "");
                    networkNameIndex = lineClipped.IndexOf("Network name");
                    securityIndex = lineClipped.IndexOf("Security");
                    signalIndex = lineClipped.IndexOf("Signal");
                    return;
                }
                
                // seperate line sections
                lineClipped = line.Replace("[0m", "").Replace("[1;90m", "");
                string networkNameSection = lineClipped.Substring(networkNameIndex, securityIndex - networkNameIndex);
                string securitySection = lineClipped.Substring(securityIndex, signalIndex - securityIndex);
                string signalSection = lineClipped.Substring(signalIndex, lineClipped.Length - signalIndex);
                
                // get network name
                bool isConnected = false;
                string networkName = "!ERROR!";
                var match = Regex.Match(lineClipped, @"\s*>\s*(\S*)");
                if (match.Success)
                {
                    isConnected = true;
                    networkName = match.Groups[1].Value;
                }
                else
                {
                    match = Regex.Match(networkNameSection, @"\s*(\S*)");
                    if (match.Success) networkName = match.Groups[1].Value;
                }
                
                // get network security
                string networkSecurity = securitySection.TrimEnd();
                
                // get network security
                string signal = signalSection.TrimEnd();
                if (signal.Contains("[1;90m")) signal = signal.Substring(0, signal.IndexOf("[1;90m") - 1);
                if (signal.Length == 1) signal = "Weak";
                else if (signal.Length == 2) signal = "Low";
                else if (signal.Length == 3) signal = "Med";
                else if (signal.Length == 4) signal = "High";

                // add entry
                string connectedTag = isConnected ? " (Connected)" : "";
                ssids.Add(new SSID(networkName, $"{networkName} [{networkSecurity}] <Signal {signal}>{connectedTag}", isConnected));
            }
            catch (Exception e)
            {
                Log.WriteLine(e);
            }
        }

        ProcessUtil.Run("iwctl", $"station {wlanDevice} scan", useBash: false);
        ProcessUtil.Run("iwctl", $"station {wlanDevice} get-networks", standardOut:ssidOut, useBash: false);
        connectionListBox.Items.Clear();
        foreach (var ssid in ssids) connectionListBox.Items.Add(new ListBoxItem { Content = ssid.desc, Tag = ssid });
    }
    
    // NOTE: forget network for debugger: iwctl known-networks <SSID> forget
    private void NetworkConnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (connectionListBox.SelectedIndex < 0) return;

        var item = (ListBoxItem)connectionListBox.Items[connectionListBox.SelectedIndex];
        var ssid = (SSID)item.Tag;
        ProcessUtil.KillHard("iwctl", true, out _);// make sure any failed processes are not open
        ProcessUtil.Run("iwctl", $"--passphrase '{networkPasswordText.Text}' station {wlanDevice} connect '{ssid.name}'", useBash: true);
        string result = ProcessUtil.Run("iwctl", $"station {wlanDevice} show", useBash: false);
        Log.WriteLine(result);
    }
    
    private void RefreshDrivesButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshDrivePage();
    }
    
    private void RefreshDrivePage()
    {
        nextButton.IsEnabled = false;
        fixBootButton.IsEnabled = false;
        
        static ulong ParseSizeName(string sizeName)
        {
            if (sizeName.EndsWith("TB"))
            {
                sizeName = sizeName.Replace("TB", "");
                if (ulong.TryParse(sizeName, out ulong sizeUL)) return sizeUL * 1024 * 1024 * 1024 * 1024;
                if (double.TryParse(sizeName, out double sizeD)) return (ulong)(sizeD * 1024 * 1024 * 1024 * 1024);
            }
            else if (sizeName.EndsWith("GB"))
            {
                sizeName = sizeName.Replace("GB", "");
                if (ulong.TryParse(sizeName, out ulong sizeUL)) return sizeUL * 1024 * 1024 * 1024;
                if (double.TryParse(sizeName, out double sizeD)) return (ulong)(sizeD * 1024 * 1024 * 1024);
            }
            else if (sizeName.EndsWith("MB"))
            {
                sizeName = sizeName.Replace("MB", "");
                if (ulong.TryParse(sizeName, out ulong sizeUL)) return sizeUL * 1024 * 1024;
                if (double.TryParse(sizeName, out double sizeD)) return (ulong)(sizeD * 1024 * 1024);
            }
            else if (sizeName.EndsWith("kB"))
            {
                sizeName = sizeName.Replace("kB", "");
                if (ulong.TryParse(sizeName, out ulong sizeUL)) return sizeUL * 1024;
                if (double.TryParse(sizeName, out double sizeD)) return (ulong)(sizeD * 1024);
            }
            else if (sizeName.EndsWith("B"))
            {
                sizeName = sizeName.Replace("B", "");
                if (ulong.TryParse(sizeName, out ulong sizeUL)) return sizeUL;
                if (double.TryParse(sizeName, out double sizeD)) return (ulong)(sizeD);
            }

            return 0;
        }
        
        var drive = new Drive();
        bool partitionMode = false;
        int partitionIndex_Number = 1;
        int partitionIndex_Start = 0;
        int partitionIndex_End = 0;
        int partitionIndex_Size = 0;
        int partitionIndex_FileSystem = 0;
        int partitionIndex_Name = 0;
        int partitionIndex_Flags = 0;
        void standardOutput(string line)
        {
            if (partitionMode)
            {
                if (line.Length == 0)
                {
                    drives.Add(drive);
                    drive = new Drive();
                    partitionMode = false;
                }
                else
                {
                    try
                    {
                        var partition = new Partition(drive);

                        // number
                        string value = line.Substring(partitionIndex_Number, line.Length - partitionIndex_Number);
                        value = value.Split(' ')[0];
                        partition.number = int.Parse(value);
                        
                        // start
                        value = line.Substring(partitionIndex_Start, line.Length - partitionIndex_Start);
                        value = value.Split(' ')[0];
                        partition.start = ParseSizeName(value);
                        
                        // end
                        value = line.Substring(partitionIndex_End, line.Length - partitionIndex_End);
                        value = value.Split(' ')[0];
                        partition.end = ParseSizeName(value);
                        
                        // size
                        value = line.Substring(partitionIndex_Size, line.Length - partitionIndex_Size);
                        value = value.Split(' ')[0];
                        partition.size = ParseSizeName(value);
                        
                        // file-system
                        if (partitionIndex_FileSystem >= 0 && line.Length - partitionIndex_FileSystem > 0)
                        {
                            value = line.Substring(partitionIndex_FileSystem, line.Length - partitionIndex_FileSystem);
                            if (value.Length != 0 && value[0] != ' ') partition.fileSystem = value.Split(' ')[0].Trim();
                        }

                        // name
                        if (partitionIndex_Name >= 0 && line.Length - partitionIndex_Name > 0)
                        {
                            value = line.Substring(partitionIndex_Name, line.Length - partitionIndex_Name);
                            if (value.Length != 0 && value[0] != ' ') partition.name = value.Split(' ')[0].Trim();
                        }

                        // flags
                        if (partitionIndex_Flags >= 0 && line.Length - partitionIndex_Flags > 0)
                        {
                            value = line.Substring(partitionIndex_Flags, line.Length - partitionIndex_Flags);
                            if (value.Length != 0 && value[0] != ' ') partition.flags = value.Trim();
                        }

                        drive.partitions.Add(partition);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(e);
                    }
                }
            }
            else if (line.StartsWith("Model:"))
            {
                if (drive.model != null)
                {
                    drives.Add(drive);
                    drive = new Drive();
                    partitionMode = false;
                }
                
                var values = line.Split(':');
                drive.model = values[1].Trim();
            }
            else if (line.StartsWith("Disk ") && !line.StartsWith("Disk Flags:"))
            {
                try
                {
                    var match = Regex.Match(line, @"Disk (\S*): (\S*)");
                    if (match.Success)
                    {
                        drive.disk = match.Groups[1].Value.Trim();
                        drive.size = ParseSizeName(match.Groups[2].Value);
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLine(e);
                }
            }
            else if (line.StartsWith("Number "))
            {
                partitionMode = true;
                partitionIndex_Start = line.IndexOf("Start");
                partitionIndex_End = line.IndexOf("End");
                partitionIndex_Size = line.IndexOf("Size");
                partitionIndex_FileSystem = line.IndexOf("File system");
                partitionIndex_Name = line.IndexOf("Name");
                partitionIndex_Flags = line.IndexOf("Flags");
            }
        }
        
        drives = new List<Drive>();
        driveListBox.Items.Clear();
        ProcessUtil.Run("parted", "-l", asAdmin:true, standardOut:standardOutput);
        foreach (var d in drives)
        {
            var item = new ListBoxItem();
            item.Content = $"{d.model}\nSize: {d.size / 1024 / 1024 / 1024}GB\nPath: {d.disk}";
            item.Tag = d;
            driveListBox.Items.Add(item);
        }
        
        // refresh found drives
        DriveListBox_OnSelectionChanged(null, null);
    }
    
    private void DriveListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool IsValidDrive(ListBoxItem item, bool efiCheck, bool ext4Check)
        {
            var drive = (Drive)item.Tag;
        
            // validate & find partitions
            if (drive.partitions == null || drive.partitions.Count == 0)
            {
                return false;
            }

            Partition partitionEFI = null;
            Partition partitionEXT4 = null;
            foreach (var parition in drive.partitions)
            {
                if (parition.name == efiPartitionName) partitionEFI = parition;
                else if (parition.name == ext4PartitionName) partitionEXT4 = parition;
            }

            if (efiCheck)
            {
                if (partitionEFI == null) return false;
                
                const ulong size1024MB = 1024ul * 1024 * 1024;
                bool validNameEFI = partitionEFI.name == efiPartitionName;
                bool validSizeEFI = partitionEFI.size >= size1024MB;
                bool validFormatEFI = partitionEFI.fileSystem == "fat32";
                bool validFlagsEFI = partitionEFI.flags.Contains("boot") && partitionEFI.flags.Contains("esp");
                if (!validNameEFI || !validSizeEFI || !validFormatEFI || !validFlagsEFI) return false;
            }

            if (ext4Check)
            {
                if (partitionEXT4 == null) return false;
                
                const ulong size48GB = 48ul * 1024 * 1024 * 1024;
                bool validNameEXT4 = partitionEXT4.name == ext4PartitionName;
                bool validSizeEXT4 = partitionEXT4.size >= size48GB;
                bool validFormatExt4 = partitionEXT4.fileSystem == "ext4";
                if (!validNameEXT4 || !validSizeEXT4 || !validFormatExt4) return false;
            }

            return true;
        }
        
        efiDrive = ext4Drive = null;
        efiPartition = ext4Partition = null;
        if (dualBootInstallRadioButton.IsChecked == true && useMultipleDrivesCheckBox.IsChecked == true)
        {
            foreach (ListBoxItem item in driveListBox.Items)
            {
                if (IsValidDrive(item, true, false)) efiDrive = (Drive)item.Tag;
                if (IsValidDrive(item, false, true)) ext4Drive = (Drive)item.Tag;
            }
            
            if (efiDrive != null) efiPartition = efiDrive.partitions.FirstOrDefault(x => x.name == efiPartitionName);
            if (ext4Drive != null) ext4Partition = ext4Drive.partitions.FirstOrDefault(x => x.name == ext4PartitionName);
        }
        else
        {
            if (driveListBox.SelectedIndex < 0)
            {
                nextButton.IsEnabled = false;
                fixBootButton.IsEnabled = false;
                return;
            }
        
            var item = (ListBoxItem)driveListBox.Items[driveListBox.SelectedIndex];
            if (!IsValidDrive(item, true, true))
            {
                nextButton.IsEnabled = false;
                fixBootButton.IsEnabled = false;
                return;
            }

            efiDrive = ext4Drive = (Drive)item.Tag;// always select drive in clean install even if not currently valid
            if (efiDrive.partitions != null) efiPartition = efiDrive.partitions.FirstOrDefault(x => x.name == efiPartitionName);
            if (ext4Drive.partitions != null) ext4Partition = ext4Drive.partitions.FirstOrDefault(x => x.name == ext4PartitionName);
        }

        nextButton.IsEnabled = efiDrive != null && ext4Drive != null && efiPartition != null && ext4Partition != null;
        if (cleanInstallRadioButton.IsChecked != true && dualBootInstallRadioButton.IsChecked != true) nextButton.IsEnabled = false;
        fixBootButton.IsEnabled = nextButton.IsEnabled;
    }
    
    private void FormatDriveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (driveListBox.SelectedIndex < 0) return;
        var item = (ListBoxItem)driveListBox.Items[driveListBox.SelectedIndex];
        var drive = (Drive)item.Tag;

        // delete old partitions
        foreach (var parition in drive.partitions)
        {
            ProcessUtil.Run("parted", $"-s {drive.disk} rm {parition.number}", asAdmin:true, useBash:false);
        }
        
        // make sure gpt partition scheme
        ProcessUtil.Run("parted", $"-s {drive.disk} mklabel gpt", asAdmin:true, useBash:false);
        
        // make new partitions
        ProcessUtil.Run("parted", $"-s -a optimal {drive.disk} mkpart ESP fat32 1MiB 1025MiB", asAdmin:true, useBash:false);
        ProcessUtil.Run("parted", $"-s -a optimal {drive.disk} mkpart primary ext4 1025MiB 100%", asAdmin:true, useBash:false);

        // configure partition
        ProcessUtil.Run("parted", $"-s {drive.disk} set 1 boot on", asAdmin:true, useBash:false);
        ProcessUtil.Run("parted", $"-s {drive.disk} set 1 esp on", asAdmin:true, useBash:false);
        ProcessUtil.Run("parted", $"-s {drive.disk} name 1 \"{efiPartitionName}\"", asAdmin:true, useBash:false);
        ProcessUtil.Run("parted", $"-s {drive.disk} name 2 \"{ext4PartitionName}\"", asAdmin:true, useBash:false);
        
        // format partitions
        if (drive.PartitionsUseP()) ProcessUtil.Run("mkfs.fat", $"-F32 {drive.disk}p1", asAdmin:true, useBash:false);
        else ProcessUtil.Run("mkfs.fat", $"-F32 {drive.disk}1", asAdmin:true, useBash:false);

        if (drive.PartitionsUseP()) ProcessUtil.Run("mkfs.ext4", $"{drive.disk}p2", asAdmin: true, useBash:false);
        else ProcessUtil.Run("mkfs.ext4", $"{drive.disk}2", asAdmin:true, useBash:false);
        
        // finish
        RefreshDrivePage();
    }

    public static void FormatExistingPartitions(Partition efiPartition, Partition ext4Partition)
    {
        ProcessUtil.Run("mkfs.fat", $"-F32 {efiPartition.path}", asAdmin:true, useBash:false);
        ProcessUtil.Run("mkfs.ext4", $"{ext4Partition.path}", asAdmin:true, useBash:false);
    }

    private void OpenGPartedButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Program.compositorMode == CompositorMode.Weston || Program.compositorMode == CompositorMode.Labwc || Program.compositorMode == CompositorMode.X11)
        {
            ProcessUtil.Run("gparted", "", wait:true, asAdmin:true);
            driveListBox.Items.Clear();
        }
        else
        {
            Log.WriteLine("Relaunch with 'install.sh -labwc'");
            MainWindow.singleton.Close();// exit so user can use labwc
        }
    }

    private void FixBootButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (driveListBox.SelectedIndex < 0 || efiDrive == null || ext4Drive == null) return;

        static void Unmount()
        {
            ProcessUtil.Run("umount", "-R /mnt/boot", useBash:false, asAdmin:true);
            ProcessUtil.Run("umount", "-R /mnt", useBash: false, asAdmin: true);
        }

        Unmount();
        ProcessUtil.Run("mount", $"{ext4Partition.path} /mnt", useBash: false, asAdmin: true);
        ProcessUtil.Run("mount", $"{efiPartition.path} /mnt/boot", useBash: false, asAdmin: true);
        try
        {
            InstallUtil.WriteSystemdBootKernelConf(ext4Partition);
            InstallUtil.WriteSystemdBootLoader();
            FixBootButton_RemoveHibernation();
        }
        catch (Exception ex)
        {
            Log.WriteLine(ex);
        }
        Unmount();
        
        // poweroff
        ShutdownButton_OnClick(null, null);
    }

    private void FixBootButton_RemoveHibernation()
    {
        // disable swap file
        string filename = "/mnt/etc/fstab";
        string text = File.ReadAllText(filename);
        if (text.Contains("/swapfile none swap defaults 0 0"))
        {
            text = text.Replace("/swapfile none swap defaults 0 0", "").TrimEnd();
            ProcessUtil.WriteAllTextAdmin(filename, text);
        }

        // delete swap file
        ProcessUtil.DeleteFileAdmin("/mnt/swapfile");

        // remove "resume" hook
        filename = "/mnt/etc/mkinitcpio.conf";
        text = File.ReadAllText(filename);
        var lines = text.Split('\n');
        foreach (string line in lines)
        {
            if (!line.StartsWith("HOOKS=")) continue;
            var match = Regex.Match(line, @"(HOOKS=\(.*\))");
            if (match.Success)
            {
                string segment = match.Groups[1].Value;
                if (segment.Contains("filesystems resume"))
                {
                    string newLine = segment.Replace("filesystems resume", "filesystems");
                    text = text.Replace(segment, newLine);
                    ProcessUtil.WriteAllTextAdmin(filename, text);
                    break;
                }
            }
        }

        // refresh mkinitcpio
        static void standardOut_Callback(string line) {/* do nothing: just used to keep output read */}
        ProcessUtil.Run("arch-chroot", $"/mnt bash -c \\\"mkinitcpio -P\\\"", asAdmin: true, standardOut: standardOut_Callback, verboseLog: true);
    }

    private void CleanInstallButton_OnClick(object sender, RoutedEventArgs e)
    {
        cleanDriveGrid.IsVisible = true;
        keepOSDriveGrid.IsVisible = false;
        RefreshDrivePage();
    }
    
    private void DualInstallButton_OnClick(object sender, RoutedEventArgs e)
    {
        cleanDriveGrid.IsVisible = false;
        keepOSDriveGrid.IsVisible = true;
        RefreshDrivePage();
    }

    private void UseMultipleDrivesCheckBox_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
        RefreshDrivePage();
    }

    private void ExitButton_OnClick(object sender, RoutedEventArgs e)
    {
        MainWindow.singleton.Close();
    }

    private void EmailLogsButton_Click(object sender, RoutedEventArgs e)
    {
        emailGrid.IsVisible = true;
        emailFromNameText.IsEnabled = true;
        emailToAddressText.IsEnabled = true;
        emailSendButton.IsEnabled = true;
        emailCancelButton.IsEnabled = true;
        emailSendingText.IsVisible = false;
    }

    private void EmailSendButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(emailFromNameText.Text) || string.IsNullOrWhiteSpace(emailToAddressText.Text)) return;

        void EmailSendCallback(bool success, string message)
        {
            Log.WriteLine($"Email Send Finished: Success:{success} Message:'{message}'");
            emailGrid.IsVisible = false;
            if (success) MessageBoxShow("Success!", "Ok", null, false, null);
            else MessageBoxShow("Failed: " + message, "Ok", null, false, null);
        }

        string from = emailFromNameText.Text.Trim();
        string toAddress = emailToAddressText.Text.Trim();

        string unameID = ProcessUtil.Run("uname", "-n", useBash: false).Trim();
        string vendorName = ProcessUtil.Run("dmidecode", "-s system-manufacturer", asAdmin: true).Trim();
        string productName = ProcessUtil.Run("dmidecode", "-s system-product-name", asAdmin: true).Trim();
        string body = $"Sent Date UTC: {DateTime.UtcNow}\nDevice: Ven='{vendorName}' Prod='{productName}'\nUserName='{unameID}'";

        var attachments = new List<string>();
        if (File.Exists("/home/gamer/ReignOS_Ext/ReignOS.Installer.log")) attachments.Add("/home/gamer/ReignOS_Ext/ReignOS.Installer.log");

        emailFromNameText.IsEnabled = false;
        emailToAddressText.IsEnabled = false;
        emailSendButton.IsEnabled = false;
        emailCancelButton.IsEnabled = false;
        emailSendingText.IsVisible = true;
        const string pass = "mssp" + ".skomTDc" + ".x2p034" + "7w19ylzdrn" + ".PZKSda7";
        EmailUtils.SendEmail(from, "MS_Qb1G8I@test-dnvo4d90k76g5r86.mlsender.net", pass, "ReignOS", toAddress, $"ReignOS: {from}", body, EmailSendCallback, attachments.ToArray());
    }

    private void EmailCancelButton_Click(object sender, RoutedEventArgs e)
    {
        emailGrid.IsVisible = false;
    }

    private delegate void MessageBoxDelegate(MessageBoxOption option);
    private MessageBoxDelegate messageBoxCallback;
    private void MessageBoxShow(string message, string optionText1, string optionText2, bool cancel, MessageBoxDelegate callback)
    {
        msgBoxText.Text = message;
        msgBoxOption1.Content = optionText1;
        msgBoxOption2.Content = optionText2;
        msgBoxOption2.IsVisible = optionText2 != null;
        msgBoxCancel.IsVisible = cancel;
        messageBoxCallback = callback;
        messageBoxGrid.IsVisible = true;
    }

    private void MessageBoxButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender == msgBoxOption1) messageBoxCallback?.Invoke(MessageBoxOption.Option1);
        else if (sender == msgBoxOption2) messageBoxCallback?.Invoke(MessageBoxOption.Option2);
        else if (sender == msgBoxCancel) messageBoxCallback?.Invoke(MessageBoxOption.Cancel);
        messageBoxGrid.IsVisible = false;
    }
}
