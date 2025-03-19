using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using ReignOS.Core;

namespace ReignOS.Installer.Views;

enum InstallerStage
{
    Start,
    Network,
    Drive,
    Install,
    Installing,
    DoneInstalling
}

public partial class MainView : UserControl
{
    private InstallerStage stage;

    private System.Timers.Timer connectedTimer;
    private List<string> wlanDevices = new List<string>();
    private string wlanDevice;
    private bool isConnected;
    
    private List<Drive> drives;
    private Partition efiPartition, ext4Partition;
    private const string efiPartitionName = "ReignOS EFI";
    private const string ext4PartitionName = "ReignOS";
    
    public MainView()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;
        
        InstallUtil.InstallProgress += InstallProgress;
        MainWindow.singleton.Closing += Window_Closing;

        ConnectedTimer(null, null);
        connectedTimer = new System.Timers.Timer(1000 * 5);
        connectedTimer.Elapsed += ConnectedTimer;
        connectedTimer.AutoReset = true;
        connectedTimer.Start();
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
        lock (this)
        {
            if (connectedTimer == null) return;
            Dispatcher.UIThread.Invoke(() =>
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
            });
        }
    }

    private void InstallProgress(string task, float progress)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            installText.Text = task;
            installProgressBar.Value = progress;
            if (progress >= 100)
            {
                nextButton.Content = "Restart";
                stage = InstallerStage.DoneInstalling;
            }
        });
    }

    private void RotationToggleButton_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
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
        
        if (defaultRotRadioButton.IsChecked == true)
        {
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform normal", out _);
        }
        else if (leftRotRadioButton.IsChecked == true)
        {
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 270", out _);
        }
        else if (rightRotRadioButton.IsChecked == true)
        {
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()}--transform 90", out _);
        }
        else if (flipRotRadioButton.IsChecked == true)
        {
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 180", out _);
        }
    }

    private void ShutdownButton_OnClick(object sender, RoutedEventArgs e)
    {
        //ProcessUtil.Run("poweroff", "", out _, wait:false);
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
                installProgressBar.Value = 0;
                installProgressBar.IsVisible = true;
                InstallUtil.Install(efiPartition, ext4Partition);
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
    
    private void NetworkConnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (connectionListBox.SelectedIndex < 0) return;

        StreamWriter inputWriter = null;
        void getStandardInput(StreamWriter writer)
        {
            inputWriter = writer;
        }

        string pass = networkPasswordText.Text;
        void standardOut(string line)
        {
            Console.WriteLine(line);
            if (line.Contains("Passphrase:"))
            {
                Console.WriteLine("Enter WiFi password: " + pass);
                inputWriter.WriteLine(pass);
                inputWriter.Flush();
            }
        }
        
        var item = (ListBoxItem)connectionListBox.Items[connectionListBox.SelectedIndex];
        var ssid = (string)item.Content;
        ProcessUtil.Run("iwctl", $"station {wlanDevice} connect {ssid}", asAdmin:true, getStandardInput:getStandardInput, standardOut:standardOut);
        string result = ProcessUtil.Run("iwctl", $"station {wlanDevice} show");
        Console.WriteLine(result);
    }
    
    private void RefreshDrivesButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshDrivePage();
    }
    
    private void RefreshDrivePage()
    {
        nextButton.IsEnabled = false;
        
        static ulong ParseSizeName(string sizeName)
        {
            if (sizeName.EndsWith("GB"))
            {
                return ulong.Parse(sizeName.Replace("GB", "")) * 1024 * 1024 * 1024;
            }
            else if (sizeName.EndsWith("MB"))
            {
                return ulong.Parse(sizeName.Replace("MB", "")) * 1024 * 1024;
            }
            else if (sizeName.EndsWith("kB"))
            {
                return ulong.Parse(sizeName.Replace("kB", "")) * 1024;
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
                        if (line.Length - partitionIndex_FileSystem > 0)
                        {
                            value = line.Substring(partitionIndex_FileSystem, line.Length - partitionIndex_FileSystem);
                            if (value.Length != 0 && value[0] != ' ') partition.fileSystem = value.Split(' ')[0];
                        }

                        // name
                        if (line.Length - partitionIndex_Name > 0)
                        {
                            value = line.Substring(partitionIndex_Name, line.Length - partitionIndex_Name);
                            if (value.Length != 0 && value[0] != ' ') partition.name = value.Split(' ')[0];
                        }

                        // flags
                        if (line.Length - partitionIndex_Flags > 0)
                        {
                            value = line.Substring(partitionIndex_Flags, line.Length - partitionIndex_Flags);
                            if (value.Length != 0 && value[0] != ' ') partition.flags = value;
                        }

                        if (drive.partitions == null) drive.partitions = new List<Partition>();
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
        ProcessUtil.Run("parted", "-l", asAdmin:true, standardOut:standardOutput);
        foreach (var d in drives)
        {
            var item = new ListBoxItem();
            item.Content = $"{d.model}\nSize: {d.size / 1024 / 1024 / 1024}GB\nPath: {d.disk}";
            item.Tag = d;
            driveListBox.Items.Add(item);
        }
    }
    
    private void DriveListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool IsValidDrive(ListBoxItem item, bool efiCheck, bool ext4Check)
        {
            var drive = (Drive)item.Tag;
        
            // validate & find partitions
            if (drive.partitions == null || drive.partitions.Count < 2)
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
                
                const ulong size512MB = 512ul * 1024 * 1024;
                bool validSizeEFI = drive.size >= size512MB;
                bool validFormatEFI = partitionEFI.fileSystem == "fat32";
                bool validFlagsEFI = partitionEFI.flags.Contains("boot") && partitionEFI.flags.Contains("esp");
                if (!validSizeEFI || !validFormatEFI || !validFlagsEFI) return false;
            }

            if (ext4Check)
            {
                if (partitionEXT4 == null) return false;
                
                const ulong size32GB = 32ul * 1024 * 1024 * 1024;
                bool validSizeEXT4 = drive.size >= size32GB;
                bool validFormatExt4 = partitionEXT4.fileSystem == "ext4";
                if (!validSizeEXT4 || !validFormatExt4) return false;
            }

            return true;
        }
        
        if (useMultipleDrivesCheckBox.IsChecked == true)
        {
            Drive efiDrive = null;
            Drive ext4Drive = null;
            foreach (ListBoxItem item in driveListBox.Items)
            {
                if (IsValidDrive(item, true, false)) efiDrive = (Drive)item.Tag;
                if (IsValidDrive(item, false, true)) ext4Drive = (Drive)item.Tag;
            }
            
            if (efiDrive != null) efiPartition = efiDrive.partitions.First(x => x.name == efiPartitionName);
            if (ext4Drive != null) ext4Partition = ext4Drive.partitions.First(x => x.name == ext4PartitionName);
            nextButton.IsEnabled = efiDrive != null && ext4Drive != null;
        }
        else
        {
            if (driveListBox.SelectedIndex < 0)
            {
                nextButton.IsEnabled = false;
                return;
            }
        
            var item = (ListBoxItem)driveListBox.Items[driveListBox.SelectedIndex];
            nextButton.IsEnabled = IsValidDrive(item, true, true);
        }
    }
    
    private void FormatDriveButton_OnClick(object sender, RoutedEventArgs e)
    {
        ProcessUtil.Run("mkfs.fat", $"-F32 {efiPartition.path}", asAdmin:true);
        ProcessUtil.Run("mkfs.ext4", ext4Partition.path, asAdmin:true);
        RefreshDrivePage();
    }

    private void OpenGPartedButton_OnClick(object sender, RoutedEventArgs e)
    {
        ProcessUtil.Run("gparted", "", wait:false, asAdmin:true);
    }

    private void CleanInstallButton_OnClick(object sender, RoutedEventArgs e)
    {
        cleanDriveGrid.IsVisible = true;
        keepOSDriveGrid.IsVisible = false;
    }
    
    private void DualInstallButton_OnClick(object sender, RoutedEventArgs e)
    {
        cleanDriveGrid.IsVisible = false;
        keepOSDriveGrid.IsVisible = true;
    }
}
