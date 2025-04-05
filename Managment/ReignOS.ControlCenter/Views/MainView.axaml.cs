using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using Avalonia.Media;
using Avalonia.Threading;
using ReignOS.Core;

namespace ReignOS.ControlCenter.Views;

class Partition
{
    public Drive drive;
    public int number;
    public ulong start, end, size;
    public string fileSystem;
    public string name;
    public string flags;

    public string path
    {
        get
        {
            if (drive.PartitionsUseP()) return drive.disk + "p" + number.ToString();
            return drive.disk + number.ToString();
        }
    }

    public Partition(Drive drive)
    {
        this.drive = drive;
    }

	private void CheckBox_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
	}
}

class Drive
{
    public string model;
    public string disk;
    public ulong size;
    public List<Partition> partitions = new List<Partition>();

    public bool PartitionsUseP()
    {
        return disk.StartsWith("/dev/nvme") || disk.StartsWith("/dev/mmcblk");
    }
}

public partial class MainView : UserControl
{
    private const string settingsFile = "/home/gamer/ReignOS_Ext/Settings.txt";
    
    private System.Timers.Timer connectedTimer;
    private List<string> wlanDevices = new List<string>();
    private string wlanDevice;
    private bool isConnected;
    
    private List<Drive> drives;
    
    public MainView()
    {
        InitializeComponent();
        versionText.Text = "Version: " + VersionInfo.version;
        if (Design.IsDesignMode) return;

        LoadSettings();
        
        connectedTimer = new System.Timers.Timer(1000 * 5);
        connectedTimer.Elapsed += ConnectedTimer;
        connectedTimer.AutoReset = true;
        connectedTimer.Start();
        ConnectedTimer(null, null);
    }
    
    private void LoadSettings()
    {
        if (!File.Exists(settingsFile)) return;
        try
        {
            using (var reader = new StreamReader(settingsFile))
            {
                string line;
                do
                {
                    line = reader.ReadLine();
                    if (line == null) break;
                
                    var parts = line.Split('=');
                    if (parts.Length < 2) continue;
                
                    if (parts[0] == "Boot")
                    {
                        if (parts[1] == "ControlCenter") boot_ControlCenter.IsChecked = true;
                        else if (parts[1] == "Gamescope") boot_Gamescope.IsChecked = true;
                        else if (parts[1] == "Cage") boot_Cage.IsChecked = true;
                    }
                    else if (parts[0] == "ScreenRotation")
                    {
                        if (parts[1] == "Default") rot_Default.IsChecked = true;
                        else if (parts[1] == "Left") rot_Left.IsChecked = true;
                        else if (parts[1] == "Right") rot_Right.IsChecked = true;
                        else if (parts[1] == "Flip") rot_Flip.IsChecked = true;
                    }
                    else if (parts[0] == "NvidiaDrivers")
                    {
                        if (parts[1] == "Nouveau") nvidia_Nouveau.IsChecked = true;
                        else if (parts[1] == "Proprietary") nvidia_Proprietary.IsChecked = true;
                    }
                    else if (parts[0] == "MangoHub")
                    {
                        if (parts[1] == "On") useMangohubCheckbox.IsChecked = true;
                        else useMangohubCheckbox.IsChecked = false;
                    }
                } while (!reader.EndOfStream);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    private void SaveSettings()
    {
        try
        {
            string path = Path.GetDirectoryName(settingsFile);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            using (var writer = new StreamWriter(settingsFile))
            {
                if (boot_ControlCenter.IsChecked == true) writer.WriteLine("Boot=ControlCenter");
                else if (boot_Gamescope.IsChecked == true) writer.WriteLine("Boot=Gamescope");
                else if (boot_Cage.IsChecked == true) writer.WriteLine("Boot=Cage");
                else writer.WriteLine("Boot=ControlCenter");
            
                if (rot_Default.IsChecked == true) writer.WriteLine("ScreenRotation=Default");
                else if (rot_Left.IsChecked == true) writer.WriteLine("ScreenRotation=Left");
                else if (rot_Right.IsChecked == true) writer.WriteLine("ScreenRotation=Right");
                else if (rot_Flip.IsChecked == true) writer.WriteLine("ScreenRotation=Flip");
                else writer.WriteLine("ScreenRotation=Default");
            
                if (nvidia_Nouveau.IsChecked == true) writer.WriteLine("NvidiaDrivers=Nouveau");
                else if (nvidia_Proprietary.IsChecked == true) writer.WriteLine("NvidiaDrivers=Proprietary");
                else writer.WriteLine("NvidiaDrivers=Nouveau");

                if (useMangohubCheckbox.IsChecked == true) writer.WriteLine("MangoHub=On");
                else writer.WriteLine("MangoHub=Off");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    private void ConnectedTimer(object sender, ElapsedEventArgs e)
    {
        lock (this)
        {
            if (connectedTimer == null) return;
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    try
                    {
                        isConnected = App.IsOnline();
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
                        Console.WriteLine(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
    
    private void GamescopeButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 1;// open Steam in Gamescope
        MainWindow.singleton.Close();
    }
    
    private void CageButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 2;// open Steam in Cage
        MainWindow.singleton.Close();
    }
    
    private void LabwcButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 3;// open Steam in Labwc
        MainWindow.singleton.Close();
    }
    
    private void X11Button_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 4;// open Steam in X11
        MainWindow.singleton.Close();
    }
    
    private void SleepButton_Click(object sender, RoutedEventArgs e)
    {
        ProcessUtil.Run("systemctl", "suspend", out _, wait:false, useBash:false);
    }
    
    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 10;
        MainWindow.singleton.Close();
    }
    
    private void ShutdownButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 11;
        MainWindow.singleton.Close();
    }
    
    private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 12;// close Managment and launch CheckUpdates.sh
        MainWindow.singleton.Close();
    }
    
    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 20;// close Managment and go to virtual terminal
        MainWindow.singleton.Close();
    }

    private void BootApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        const string profileFile = "/home/gamer/.bash_profile";
        string text = File.ReadAllText(profileFile);
        text = text.Replace(" --gamescope", "");
        text = text.Replace(" --cage", "");
        if (boot_Gamescope.IsChecked == true) text = text.Replace("--use-controlcenter", "--use-controlcenter --gamescope");
        else if (boot_Cage.IsChecked == true) text = text.Replace("--use-controlcenter", "--use-controlcenter --cage");
        File.WriteAllText(profileFile, text);
        SaveSettings();
    }

    private void UseMangoHubCheckBox_Click(object sender, RoutedEventArgs e)
    {
        const string profileFile = "/home/gamer/.bash_profile";
        string text = File.ReadAllText(profileFile);
        foreach (string line in text.Split('\n'))
        {
            if (line.Contains("--use-controlcenter"))
            {
                if (useMangohubCheckbox.IsChecked == true) text = text.Replace(line, line + " --use-mangohub");
                else text = text.Replace(" --use-mangohub", "");
                break;
            }
        }
        File.WriteAllText(profileFile, text);
        SaveSettings();

        App.exitCode = 0;// reopen
        MainWindow.singleton.Close();
    }
    
    private void RotApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        static void WriteX11Settings(StreamWriter writer, string rotation)
        {
            writer.WriteLine("display=$(xrandr --query | awk '/ connected/ {print $1; exit}')");
            writer.WriteLine($"xrandr --output $display --rotate {rotation}");
        }
        
        static void WriteWaylandSettings(StreamWriter writer, string rotation)
        {
            writer.WriteLine("display=$(wlr-randr | awk '/^[^ ]+/{print $1; exit}')");
            writer.WriteLine($"wlr-randr --output $display --transform {rotation}");// --adaptive-sync enabled (this can break rotation)
        }
        
        static string GetWaylandDisplay()
        {
            try
            {
                string result = ProcessUtil.Run("wlr-randr", "", useBash:false);
                var lines = result.Split('\n');
                result = lines[0].Split(" ")[0];
                return result;
            }
            catch {}
            return "ERROR";
        }
        
        const string folder = "/home/gamer/ReignOS_Ext";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        string x11File = Path.Combine(folder, "X11_Settings.sh");
        string waylandFile = Path.Combine(folder, "Wayland_Settings.sh");
        if (rot_Default.IsChecked == true)
        {
            using (var writer = new StreamWriter(x11File)) WriteX11Settings(writer, "normal");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "normal");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform normal", useBash:false);
        }
        else if (rot_Left.IsChecked == true)
        {
            using (var writer = new StreamWriter(x11File)) WriteX11Settings(writer, "left");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "270");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 270", useBash:false);// 270, flipped-270 (options)
        }
        else if (rot_Right.IsChecked == true)
        {
            using (var writer = new StreamWriter(x11File)) WriteX11Settings(writer, "right");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "90");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()}--transform 90", useBash:false);// 90, flipped-90 (options)
        }
        else if (rot_Flip.IsChecked == true)
        {
            using (var writer = new StreamWriter(x11File)) WriteX11Settings(writer, "inverted");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "180");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 180", useBash:false);// 180, flipped, flipped-180 (options)
        }
        
        SaveSettings();
    }
    
    private void NvidiaApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        // invoke Nvidia driver install script
        if (nvidia_Nouveau.IsChecked == true) App.exitCode = 30;
        else if (nvidia_Proprietary.IsChecked == true) App.exitCode = 31;
        MainWindow.singleton.Close();
        SaveSettings();
    }

    private void BootManagerButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = false;
        bootManagerGrid.IsVisible = true;

        bootOptionsListBox.Items.Clear();
        string result = ProcessUtil.Run("efibootmgr", "", asAdmin:true, useBash:false);
        var values = result.Split('\n');
        foreach (string value in values)
        {
            if (value.StartsWith("Boot"))
            {
                if (value.StartsWith("BootCurrent:"))
                {
                    var match = Regex.Match(value, @"BootCurrent:\s*(.*)");
                    if (match.Success) bootCurrentText.Text = "Current Boot: " + match.Groups[1].Value;
                    else bootCurrentText.Text = "Current Boot: N/A";
                }
                else if (value.StartsWith("BootOrder:"))
                {
                    var match = Regex.Match(value, @"BootOrder:\s*(.*)");
                    if (match.Success) bootOrderText.Text = "Boot Order: " + match.Groups[1].Value;
                    else bootOrderText.Text = "Boot Order: N/A";
                }
                else
                {
                    var match = Regex.Match(value, @"Boot(\w*)(\*)?\s*(.*)");
                    if (match.Success)
                    {
                        string name = match.Groups[3].Value;
                        const int maxLength = 20;
                        if (name.StartsWith("Windows Boot Manager")) name = "Windows Boot Manager";
                        else if (name.StartsWith("Linux Boot Manager")) name = "Linux Boot Manager";
                        else if (name.Length > maxLength) name = name.Substring(0, maxLength) + "...";
                        var item = new ListBoxItem();
                        item.Content = $"({match.Groups[1].Value}{match.Groups[2].Value}): {name}";
                        item.Tag = match.Groups[1].Value;
                        bootOptionsListBox.Items.Add(item);
                    }
                }
            }
        }
    }

    private void BootManagerRebootButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (bootOptionsListBox.SelectedIndex <= -1) return;
        var item = (ListBoxItem)bootOptionsListBox.Items[bootOptionsListBox.SelectedIndex];
        string boot = (string)item.Tag;
        ProcessUtil.Run("efibootmgr", $"-n {boot}", asAdmin:true, useBash:false);

        Thread.Sleep(1000);
        RestartButton_Click(null, null);
    }
    
    private void BootManagerBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = true;
        bootManagerGrid.IsVisible = false;
    }

    private void NetworkManagerButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = false;
        networkManagerGrid.IsVisible = true;
        RefreshNetworkPage();
    }
    
    private void NetworkManagerBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = true;
        networkManagerGrid.IsVisible = false;
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
                Console.WriteLine(e);
            }
        }

        wlanDevices.Clear();
        ProcessUtil.Run("iwctl", "device list", standardOut:deviceOut);
        
        // choose device
        if (wlanDevices.Count == 0) return;
        wlanDevice = wlanDevices[0];
        Console.WriteLine("wlanDevice: " + wlanDevice);
        
        // get SSID
        var ssids = new List<string>();
        void ssidOut(string line)
        {
            lock (this) Console.WriteLine("LINE: " + line);
            if (line.Contains("-------") || line.Contains("Network name")) return;
            try
            {
                var match = Regex.Match(line, @"\s*(\S*)\s*psk");
                if (match.Success)
                {
                    string value = match.Groups[1].Value;
                    lock (this)
                    {
                        if (line.Contains(">")) ssids.Add(value + " (Connected)");
                        else ssids.Add(value);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        ProcessUtil.Run("iwctl", $"station {wlanDevice} scan");
        ProcessUtil.Run("iwctl", $"station {wlanDevice} get-networks", standardOut:ssidOut);
        connectionListBox.Items.Clear();
        foreach (var ssid in ssids) connectionListBox.Items.Add(new ListBoxItem { Content = ssid });
    }
    
    // NOTE: forget network for debugger: iwctl known-networks <SSID> forget
    private void NetworkConnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (connectionListBox.SelectedIndex < 0) return;

        var item = (ListBoxItem)connectionListBox.Items[connectionListBox.SelectedIndex];
        var ssid = (string)item.Content;
        ProcessUtil.KillHard("iwctl", true, out _);// make sure any failed processes are not open
        ProcessUtil.Run("iwctl", $"--passphrase {networkPasswordText.Text} station {wlanDevice} connect {ssid}", asAdmin:true);
        string result = ProcessUtil.Run("iwctl", $"station {wlanDevice} show");
        Console.WriteLine(result);
    }
    
    private void DriveManagerButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = false;
        driveManagerGrid.IsVisible = true;
        RefreshDrivePage();
    }
    
    private void DriveManagerBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = true;
        driveManagerGrid.IsVisible = false;
    }
    
    private void RefreshDrivesButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshDrivePage();
    }
    
    private void RefreshDrivePage()
    {
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
                        Console.WriteLine(e);
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
                    Console.WriteLine(e);
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
        ProcessUtil.Run("parted", "-l", asAdmin:true, useBash:false, standardOut:standardOutput);
        foreach (var d in drives)
        {
            var item = new ListBoxItem();
            item.Content = $"{d.model}\nSize: {d.size / 1024 / 1024 / 1024}GB\nPath: {d.disk}";
            item.Tag = d;
            driveListBox.Items.Add(item);
        }
    }
    
    private void FormatDriveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (driveListBox.SelectedIndex < 0) return;
        var item = (ListBoxItem)driveListBox.Items[driveListBox.SelectedIndex];
        var drive = (Drive)item.Tag;

        // unmount partitions and kill auto mount
        ProcessUtil.Run("udiskie-umount", "-a", out _, useBash:false);
        ProcessUtil.KillHard("udiskie", true, out _);
        
        // delete old partitions
        foreach (var parition in drive.partitions)
        {
            ProcessUtil.Run("parted", $"-s {drive.disk} rm {parition.number}", asAdmin:true, useBash:false);
        }
        
        // make sure gpt partition scheme
        ProcessUtil.Run("parted", $"-s {drive.disk} mklabel gpt", asAdmin:true, useBash:false);
        
        // make new partitions
        ProcessUtil.Run("parted", $"-s {drive.disk} mkpart primary ext4 1MiB 100%", asAdmin:true, useBash:false);
        
        // format partitions
        if (drive.PartitionsUseP())
        {
            ProcessUtil.Run("chown", $"gamer {drive.disk}p1", asAdmin:true, useBash:false);
            ProcessUtil.Run("mkfs.ext4", $"{drive.disk}p1", asAdmin:false, useBash:false);
            //ProcessUtil.Run("mkfs.ext4", $"{drive.disk}p1", asAdmin:true, useBash:false);
        }
        else
        {
            ProcessUtil.Run("chown", $"gamer {drive.disk}1", asAdmin:true, useBash:false);
            ProcessUtil.Run("mkfs.ext4", $"{drive.disk}1", asAdmin:false, useBash:false);
            //ProcessUtil.Run("mkfs.ext4", $"{drive.disk}1", asAdmin:true, useBash:false);
        }

        // start auto mount back up
        ProcessUtil.Run("udiskie", "--no-tray", out _, wait:false, useBash:false);
        
        // finish
        RefreshDrivePage();
    }
}
