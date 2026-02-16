using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using Avalonia.Media;
using Avalonia.Threading;
using ReignOS.Core;
using ReignOS.ControlCenter.Desktop;
using System.Text;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Platform;
using System.Threading.Tasks;

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

class GPU
{
    public string card;
    public int number;
}

enum MessageBoxOption
{
    Cancel,
    Option1,
    Option2
}

class AudioSetting
{
    public string name;
    public bool defaultSink;
}

class PowerSetting
{
    public string name;
    public string driver;
    public bool active;
}

class PowerCPUSetting
{
    public string name;
    public int minFreq, maxFreq, minFreqScale, maxFreqScale;
    public bool? boost;
}

class DisplaySetting
{
    public string name;
    public ScreenRotation rotation;
    public int widthOverride, heightOverride;
    public bool enabled;
}

enum KernelSettingsCopy
{
    Chimera,
    Bazzite,
    Cachy
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
    private const string launchFile = "/home/gamer/ReignOS_Launch.sh";
    private const string settingsFile = "/home/gamer/ReignOS_Ext/Settings.txt";
    
    private System.Timers.Timer connectedTimer;
    private List<string> wlanDevices = new();
    private string wlanDevice;
    private bool isConnected;
    
    private List<Drive> drives;
    private List<GPU> gpus;
    private List<string> muxes;
    
    private List<AudioSetting> audioSettings = new();
    private AudioSetting defaultAudioSetting;
    
    private List<PowerSetting> powerSettings = new();
    private List<PowerCPUSetting> powerCPUSettings = new();
    private bool? powerIntelTurboBoostEnabled;
    private string lastPowerManagerOption = "PowerProfiles";
    
    private List<DisplaySetting> displaySettings = new();

    private List<string> allUsernames;

    private string kernelArchConf, kernelArchConf_Options;
    private int kernelIntelMaxCState, kernelAMDMaxCState;

    private bool hibernatePowerButton;
    private bool disabledUIState;
    
    private const string magicModulePowerPath = "/sys/class/firmware-attributes/ayaneo-ec/attributes/controller_power/current_value";
    private const string magicModuleStatePath = "/sys/class/firmware-attributes/ayaneo-ec/attributes/controller_modules/current_value";
    
    public MainView()
    {
        InitializeComponent();
        versionText.Text = "Version: " + VersionInfo.version;
        if (Design.IsDesignMode) return;
        compositorText.Text = "Control-Center Compositor: " + Program.compositorMode.ToString();

        string gitResult = ProcessUtil.Run("git", "branch --show-current", useBash:false);
        gitText.Text = "Branch: " + gitResult.Trim();
        FullSettingsReload();
        #if !DEBUG
        SaveSystemSettings();// apply any system settings in case things get updated
        #endif
        
        connectedTimer = new System.Timers.Timer(1000 * 5);
        connectedTimer.Elapsed += ConnectedTimer;
        connectedTimer.AutoReset = true;
        connectedTimer.Start();
        ConnectedTimer(null, null);
        
        // check if hibernation file exists
        hibernatePowerButton = File.Exists("/swapfile");

        // enable Ayaneo3 options
        ayaneoModulePopGrid.IsVisible = Program.ayaneoModules;
        ayaneoModuleNote.IsVisible = !(File.Exists(magicModulePowerPath) && File.Exists(magicModuleStatePath));
    }

    private void FullSettingsReload()
    {
        ResetUI();
        RefreshGPUs();
        RefreshMUX();
        LoadSettings();
        PostRefreshGPUs();
    }

    private void ResetUI()
    {
        boot_ControlCenter.IsChecked = true;
        rot_Unset.IsChecked = true;
        rot_Touchscreen.IsChecked = false;
        amd_Mesa.IsChecked = true;
        nvidia_Nouveau.IsChecked = true;
        gpuButton0.IsChecked = true;
        muxButton0.IsChecked = false;
        muxButton1.IsChecked = true;
        mangohubCheckbox.IsChecked = false;
        vrrCheckbox.IsChecked = false;
        hdrCheckbox.IsChecked = false;
        disableSteamGPUCheckbox.IsChecked = false;
        disableSteamDeckCheckbox.IsChecked = false;
        reignOSInputCheckbox.IsChecked = true;
        powerProfilesCheckbox.IsChecked = true;
        rgbDisabledCheckbox.IsChecked = true;
        kernelArchCheckbox.IsChecked = true;
        restSleepCheckbox.IsChecked = true;
        reignMonitorCheckbox.IsChecked = false;
    }

    private void RefreshGPUs()
    {
        gpus = new List<GPU>();
        try
        {
            foreach (string gpuFilename in Directory.GetFiles("/dev/dri"))
            {
                string filename = Path.GetFileName(gpuFilename);
                var match = Regex.Match(filename, @"card(\d)");
                if (match.Success)
                {
                    var gpu = new GPU()
                    {
                        card = gpuFilename,
                        number = int.Parse(match.Groups[1].Value)
                    };
                    gpus.Add(gpu);
                }
            }
        }
        catch (Exception ex)
        {
            Log.WriteLine(ex);
        }

        gpuButton2.IsVisible = gpus.Count >= 2;
        gpuButton3.IsVisible = gpus.Count >= 3;
        gpuButton4.IsVisible = gpus.Count >= 4;
    }

    private void PostRefreshGPUs()
    {
        gpuButtonNvidiaPrime.IsVisible = nvidia_Proprietary.IsChecked == true || nvidia_Proprietary_Legacy_580.IsChecked == true || nvidia_Proprietary_Legacy_470.IsChecked == true || nvidia_Proprietary_Legacy_340.IsChecked == true;
        if (gpuButtonNvidiaPrime.IsVisible)
        {
            if (!gpuButton2.IsVisible) gpuButtonNvidiaPrime.Margin = gpuButton2.Margin;
            else if (!gpuButton3.IsVisible) gpuButtonNvidiaPrime.Margin = gpuButton3.Margin;
            else if (!gpuButton4.IsVisible) gpuButtonNvidiaPrime.Margin = gpuButton4.Margin;
        }
    }

    private void RefreshMUX()
    {
        muxes = new List<string>();
        try
        {
            string result = ProcessUtil.Run("supergfxctl", "-s", useBash:false, killAfterSec:2);
            if (!result.Contains("Zbus error"))
            {
                result = result.Replace("[", "").Replace("]", "");
                var lines = result.Split(',');
                foreach (string line in lines)
                {
                    string value = line.Trim();
                    muxes.Add(value);
                }
            }
        }
        catch (Exception ex)
        {
            Log.WriteLine(ex);
        }

        muxButton1.IsVisible = muxes.Count >= 1;
        muxButton2.IsVisible = muxes.Count >= 2;
        muxButton3.IsVisible = muxes.Count >= 3;
        muxButton4.IsVisible = muxes.Count >= 4;

        if (muxes.Count >= 1) muxButton1.Content = muxes[0];
        if (muxes.Count >= 2) muxButton2.Content = muxes[1];
        if (muxes.Count >= 3) muxButton3.Content = muxes[2];
        if (muxes.Count >= 4) muxButton4.Content = muxes[3];
    }
    
    private void LoadSettings()
    {
        if (!File.Exists(settingsFile)) return;

        bool needsReset = false;
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
                        else if (parts[1] == "Weston") boot_Weston.IsChecked = true;
                        else if (parts[1] == "Cage") boot_Cage.IsChecked = true;
                        else if (parts[1] == "X11") boot_X11.IsChecked = true;
                        else if (parts[1] == "KDE-G") boot_KDEG.IsChecked = true;
                    }
                    else if (parts[0] == "ScreenRotation")
                    {
                        if (parts[1] == "Default") rot_Default.IsChecked = true;
                        else if (parts[1] == "Left") rot_Left.IsChecked = true;
                        else if (parts[1] == "Right") rot_Right.IsChecked = true;
                        else if (parts[1] == "Flip") rot_Flip.IsChecked = true;
                    }
                    else if (parts[0] == "TouchscreenRotation")
                    {
                        if (parts[1] == "Enabled") rot_Touchscreen.IsChecked = true;
                    }
                    else if (parts[0] == "AMDDrivers")
                    {
                        if (parts[1] == "Mesa") amd_Mesa.IsChecked = true;
                        else if (parts[1] == "AMDVLK") amd_VLK.IsChecked = true;
                        else if (parts[1] == "Proprietary") amd_Proprietary.IsChecked = true;
                    }
                    else if (parts[0] == "NvidiaDrivers")
                    {
                        if (parts[1] == "Nouveau")
                        {
                            nvidia_Nouveau.IsChecked = true;
                            nvidiaSettingsButton.IsEnabled = false;
                        }
                        else if (parts[1] == "Proprietary")
                        {
                            nvidia_Proprietary.IsChecked = true;
                            nvidiaSettingsButton.IsEnabled = true;
                        }
                        else if (parts[1] == "Proprietary_580")
                        {
                            nvidia_Proprietary_Legacy_580.IsChecked = true;
                            nvidiaSettingsButton.IsEnabled = true;
                        }
                        else if (parts[1] == "Proprietary_470")
                        {
                            nvidia_Proprietary_Legacy_470.IsChecked = true;
                            nvidiaSettingsButton.IsEnabled = true;
                        }
                        else if (parts[1] == "Proprietary_340")
                        {
                            nvidia_Proprietary_Legacy_340.IsChecked = true;
                            nvidiaSettingsButton.IsEnabled = true;
                        }
                    }
                    else if (parts[0] == "GPU")
                    {
                        if (parts[1] == "1")
                        {
                            gpuButton1.IsChecked = true;
                        }
                        else if (parts[1] == "2")
                        {
                            if (gpus.Count >= 2) gpuButton2.IsChecked = true;
                            else needsReset = true;
                        }
                        else if (parts[1] == "3")
                        {
                            if (gpus.Count >= 3) gpuButton3.IsChecked = true;
                            else needsReset = true;
                        }
                        else if (parts[1] == "4")
                        {
                            if (gpus.Count >= 4) gpuButton4.IsChecked = true;
                            else needsReset = true;
                        }
                        else
                        {
                            gpuButton0.IsChecked = true;
                            if (parts[1] != "0") needsReset = true;
                        }
                    }
                    else if (parts[0] == "GPU_NVIDIA_PRIME")
                    {
                        if (parts[1] == "100")
                        {
                            if (nvidia_Proprietary.IsChecked == true || nvidia_Proprietary_Legacy_580.IsChecked == true || nvidia_Proprietary_Legacy_470.IsChecked == true || nvidia_Proprietary_Legacy_340.IsChecked == true)
                            {
                                gpuButtonNvidiaPrime.IsChecked = true;
                            }
                            else
                            {
                                needsReset = true;
                            }
                        }
                    }
                    else if (parts[0] == "MUX_ENABLED")
                    {
                        muxButton0.IsChecked = parts[1] == "On";
                    }
                    else if (parts[0] == "MUX")
                    {
                        if (parts[1] == muxButton1.Content as string)
                        {
                            if (muxes.Count >= 1) muxButton1.IsChecked = true;
                        }
                        else if (parts[1] == muxButton2.Content as string)
                        {
                            if (muxes.Count >= 2) muxButton2.IsChecked = true;
                        }
                        else if (parts[1] == muxButton3.Content as string)
                        {
                            if (muxes.Count >= 3) muxButton3.IsChecked = true;
                        }
                        else if (parts[1] == muxButton4.Content as string)
                        {
                            if (muxes.Count >= 4) muxButton4.IsChecked = true;
                        }
                    }
                    else if (parts[0] == "MangoHub")
                    {
                        mangohubCheckbox.IsChecked = parts[1] == "On";
                    }
                    else if (parts[0] == "VRR")
                    {
                        vrrCheckbox.IsChecked = parts[1] == "On";
                    }
                    else if (parts[0] == "HDR")
                    {
                        hdrCheckbox.IsChecked = parts[1] == "On";
                    }
                    else if (parts[0] == "DisableSteamGPU")
                    {
                        disableSteamGPUCheckbox.IsChecked = parts[1] == "On";
                    }
                    else if (parts[0] == "DisableSteamDeck")
                    {
                        disableSteamDeckCheckbox.IsChecked = parts[1] == "On";
                    }
                    else if (parts[0] == "Input")
                    {
                        if (parts[1] == "ReignOS") reignOSInputCheckbox.IsChecked = true;
                        else if (parts[1] == "InputPlumber") inputPlumberInputCheckbox.IsChecked = true;
                        else if (parts[1] == "HHD")
                        {
                            hhdInputCheckbox.IsChecked = true;

                            // adjustor will always be used with HHD
                            hhdAdjustorCheckbox.IsChecked = true;
                            powerButton.IsVisible = false;
                        }
                        else if (parts[1] == "Disable") disableInputCheckbox.IsChecked = true;
                    }
                    else if (parts[0] == "PowerManager")
                    {
                        lastPowerManagerOption = parts[1];
                        if (parts[1] == "PowerProfiles")
                        {
                            powerProfilesCheckbox.IsChecked = true;
                            powerButton.IsVisible = true;
                        }
                        /*else if (parts[1] == "PowerStation")
                        {
                            powerStationCheckbox.IsChecked = true;
                            powerButton.IsVisible = false;
                        }*/
                        else if (parts[1] == "DeckyTDP")
                        {
                            powerDeckyTDPCheckbox.IsChecked = true;
                            powerButton.IsVisible = false;
                        }
                        /*else if (parts[1] == "Adjustor")
                        {
                            hhdAdjustorCheckbox.IsChecked = true;
                            powerButton.IsVisible = false;
                        }*/
                        /*else if (parts[1] == "SteamOSManager")
                        {
                            steamOSManagerCheckbox.IsChecked = true;
                            powerButton.IsVisible = false;
                        }*/
                        else if (parts[1] == "Disabled")
                        {
                            powerManagementDisabledCheckbox.IsChecked = true;
                            powerButton.IsVisible = false;
                        }
                    }
                    else if (parts[0] == "RGBManager")
                    {
                        if (parts[1] == "HueSync") rgbHueSyncCheckbox.IsChecked = true;
                    }
                    else if (parts[0] == "PowerPercentage")
                    {
                        if (int.TryParse(parts[1], out int value)) powerSlider.Value = value;
                    }
                    else if (parts[0] == "Kernel")
                    {
                        if (parts[1] == "Arch") kernelArchCheckbox.IsChecked = true;
                        else if (parts[1] == "Chimera") kernelChimeraCheckbox.IsChecked = true;
                        else if (parts[1] == "Bazzite") kernelBazziteCheckbox.IsChecked = true;
                        else if (parts[1] == "Cachy") kernelCachyCheckbox.IsChecked = true;
                    }
                    else if (parts[0] == "Rest")
                    {
                        if (parts[1] == "Sleep") restSleepCheckbox.IsChecked = true;
                        else if (parts[1] == "Hibernate") restHibernateCheckbox.IsChecked = true;
                    }
                    else if (parts[0] == "ReignOS.Monitor")
                    {
                        reignMonitorCheckbox.IsChecked = parts[1] == "Enabled";
                    }
                    else if (parts[0].StartsWith("AudioDefault:"))
                    {
                        var audioParts = parts[1].Split(':');
                        if (audioParts.Length != 0)
                        {
                            defaultAudioSetting = new AudioSetting();
                            defaultAudioSetting.name = audioParts[1];
                            defaultAudioSetting.defaultSink = true;
                        }
                    }
                    else if (parts[0].StartsWith("Display_"))
                    {
                        var displayParts = parts[1].Split(' ');
                        if (displayParts.Length != 0)
                        {
                            var setting = new DisplaySetting();
                            foreach (var displayPart in displayParts)
                            {
                                var elementParts = displayPart.Split(':');
                                if (elementParts.Length == 2)
                                {
                                    switch (elementParts[0])
                                    {
                                        case "Name": setting.name = elementParts[1]; break;
                                        
                                        case "Rotation":
                                            if (elementParts[1] == "Default") setting.rotation = ScreenRotation.Default;
                                            else if (elementParts[1] == "Left") setting.rotation = ScreenRotation.Left;
                                            else if (elementParts[1] == "Right") setting.rotation = ScreenRotation.Right;
                                            else if (elementParts[1] == "Flip") setting.rotation = ScreenRotation.Flip;
                                            break;
                                        
                                        case "WidthOverride": int.TryParse(elementParts[1], out setting.widthOverride); break;
                                        case "HeightOverride": int.TryParse(elementParts[1], out setting.heightOverride); break;
                                        case "Enabled": setting.enabled = elementParts[1] == "True"; break;
                                    }
                                }
                            }
                            displaySettings.Add(setting);
                        }
                    }
                } while (!reader.EndOfStream);
            }
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }

        if (needsReset)
        {
            SaveSettings();
            App.exitCode = 0;
            MainWindow.singleton.Close();
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
                else if (boot_Weston.IsChecked == true) writer.WriteLine("Boot=Weston");
                else if (boot_Cage.IsChecked == true) writer.WriteLine("Boot=Cage");
                else if (boot_X11.IsChecked == true) writer.WriteLine("Boot=X11");
                else if (boot_KDEG.IsChecked == true) writer.WriteLine("Boot=KDE-G");
                else writer.WriteLine("Boot=ControlCenter");

                if (rot_Default.IsChecked == true) writer.WriteLine("ScreenRotation=Default");
                else if (rot_Left.IsChecked == true) writer.WriteLine("ScreenRotation=Left");
                else if (rot_Right.IsChecked == true) writer.WriteLine("ScreenRotation=Right");
                else if (rot_Flip.IsChecked == true) writer.WriteLine("ScreenRotation=Flip");
                else writer.WriteLine("ScreenRotation=Unset");

                if (rot_Touchscreen.IsChecked == true) writer.WriteLine("TouchscreenRotation=Enabled");
                else writer.WriteLine("TouchscreenRotation=Disabled");

                if (amd_Mesa.IsChecked == true) writer.WriteLine("AMDDrivers=Mesa");
                else if (amd_VLK.IsChecked == true) writer.WriteLine("AMDDrivers=AMDVLK");
                else if (amd_Proprietary.IsChecked == true) writer.WriteLine("AMDDrivers=Proprietary");
                else writer.WriteLine("AMDDrivers=Mesa");

                if (nvidia_Nouveau.IsChecked == true) writer.WriteLine("NvidiaDrivers=Nouveau");
                else if (nvidia_Proprietary.IsChecked == true) writer.WriteLine("NvidiaDrivers=Proprietary");
                else if (nvidia_Proprietary_Legacy_580.IsChecked == true) writer.WriteLine("NvidiaDrivers=Proprietary_580");
                else if (nvidia_Proprietary_Legacy_470.IsChecked == true) writer.WriteLine("NvidiaDrivers=Proprietary_470");
                else if (nvidia_Proprietary_Legacy_340.IsChecked == true) writer.WriteLine("NvidiaDrivers=Proprietary_340");
                else writer.WriteLine("NvidiaDrivers=Nouveau");

                if (gpuButton1.IsChecked == true) writer.WriteLine("GPU=1");
                else if (gpuButton2.IsChecked == true) writer.WriteLine("GPU=2");
                else if (gpuButton3.IsChecked == true) writer.WriteLine("GPU=3");
                else if (gpuButton4.IsChecked == true) writer.WriteLine("GPU=4");
                else writer.WriteLine("GPU=0");
                
                if (gpuButtonNvidiaPrime.IsChecked == true) writer.WriteLine("GPU_NVIDIA_PRIME=100");

                if (muxButton0.IsChecked == true)
                {
                    writer.WriteLine("MUX_ENABLED=On");
                    if (muxButton1.IsChecked == true) writer.WriteLine($"MUX={muxButton1.Content as string}");
                    else if (muxButton2.IsChecked == true) writer.WriteLine($"MUX={muxButton2.Content as string}");
                    else if (muxButton3.IsChecked == true) writer.WriteLine($"MUX={muxButton3.Content as string}");
                    else if (muxButton4.IsChecked == true) writer.WriteLine($"MUX={muxButton4.Content as string}");
                }
                else
                {
                    writer.WriteLine("MUX_ENABLED=Off");
                }

                if (mangohubCheckbox.IsChecked == true) writer.WriteLine("MangoHub=On");
                else writer.WriteLine("MangoHub=Off");

                if (vrrCheckbox.IsChecked == true) writer.WriteLine("VRR=On");
                else writer.WriteLine("VRR=Off");

                if (hdrCheckbox.IsChecked == true) writer.WriteLine("HDR=On");
                else writer.WriteLine("HDR=Off");

                if (disableSteamGPUCheckbox.IsChecked == true) writer.WriteLine("DisableSteamGPU=On");
                else writer.WriteLine("DisableSteamGPU=Off");

                if (disableSteamDeckCheckbox.IsChecked == true) writer.WriteLine("DisableSteamDeck=On");
                else writer.WriteLine("DisableSteamDeck=Off");

                if (disableInputCheckbox.IsChecked == true) writer.WriteLine("Input=Disable");
                else if (inputPlumberInputCheckbox.IsChecked == true) writer.WriteLine("Input=InputPlumber");
                else if (hhdInputCheckbox.IsChecked == true) writer.WriteLine("Input=HHD");
                else writer.WriteLine("Input=ReignOS");

                if (powerProfilesCheckbox.IsChecked == true) writer.WriteLine("PowerManager=PowerProfiles");
                //else if (powerStationCheckbox.IsChecked == true) writer.WriteLine("PowerManager=PowerStation");
                else if (powerDeckyTDPCheckbox.IsChecked == true) writer.WriteLine("PowerManager=DeckyTDP");
                else if (hhdAdjustorCheckbox.IsChecked == true) writer.WriteLine("PowerManager=Adjustor");
                //else if (steamOSManagerCheckbox.IsChecked == true) writer.WriteLine("PowerManager=SteamOSManager");
                else if (powerManagementDisabledCheckbox.IsChecked == true) writer.WriteLine("PowerManager=Disabled");

                if (rgbHueSyncCheckbox.IsChecked == true) writer.WriteLine("RGBManager=HueSync");
                else writer.WriteLine("RGBManager=Disabled");

                writer.WriteLine($"PowerPercentage={(int)powerSlider.Value}");
                if (powerIntelTurboBoostCheckbox.IsChecked == true) writer.WriteLine("PowerIntelTurboBoost=True");
                if (powerBoostCheckBox.IsChecked == true) writer.WriteLine("PowerBoost=True");

                if (kernelArchCheckbox.IsChecked == true) writer.WriteLine("Kernel=Arch");
                else if (kernelChimeraCheckbox.IsChecked == true) writer.WriteLine("Kernel=Chimera");
                else if (kernelBazziteCheckbox.IsChecked == true) writer.WriteLine("Kernel=Bazzite");
                else if (kernelCachyCheckbox.IsChecked == true) writer.WriteLine("Kernel=Cachy");

                if (restSleepCheckbox.IsChecked == true) writer.WriteLine("Rest=Sleep");
                else if (restHibernateCheckbox.IsChecked == true) writer.WriteLine("Rest=Hibernate");
                
                if (reignMonitorCheckbox.IsChecked == true) writer.WriteLine("ReignOS.Monitor=Enabled");
                else writer.WriteLine("ReignOS.Monitor=Disabled");

                foreach (var setting in audioSettings)
                {
                    if (setting.defaultSink)
                    {
                        writer.WriteLine($"AudioDefault:{setting.name}");
                        break;
                    }
                }
                
                int d = 0;
                foreach (var setting in displaySettings)
                {
                    writer.WriteLine($"Display_{d}=Name:{setting.name} Rotation:{setting.rotation} WidthOverride:{setting.widthOverride} HeightOverride:{setting.heightOverride} Enabled:{setting.enabled}");
                    d++;
                }
            }
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }

        SaveSystemSettings();
    }

    private void SaveSystemSettings()
    {
        void WriteX11Settings(StreamWriter writer, string rotation)
        {
            if (displaySettings.Count == 0 || !displaySettings.Exists(x => x.enabled))
            {
                writer.WriteLine("display=$(xrandr --query | awk '/ connected/ {print $1; exit}')");
                if (!string.IsNullOrEmpty(rotation)) rotation = $" --rotate {rotation}";
                writer.WriteLine($"xrandr --output $display{rotation}");
            }
            else
            {
                foreach (var setting in displaySettings)
                {
                    if (!setting.enabled)
                    {
                        writer.WriteLine($"xrandr --output {setting.name} --off");
                        continue;
                    }

                    switch (setting.rotation)
                    {
                        case ScreenRotation.Unset: rotation = ""; break;
                        case ScreenRotation.Default: rotation = "normal"; break;
                        case ScreenRotation.Left: rotation = "left"; break;
                        case ScreenRotation.Right: rotation = "right"; break;
                        case ScreenRotation.Flip: rotation = "inverted"; break;
                    }

                    string mode = "";
                    if (setting.widthOverride > 0 && setting.heightOverride > 0) mode = $" --mode {setting.widthOverride}x{setting.heightOverride}";
                    if (!string.IsNullOrEmpty(rotation)) rotation = $" --rotate {rotation}";
                    writer.WriteLine($"xrandr --output {setting.name}{rotation}{mode}");
                }
            }
        }

        static string WaylandTouchscreenRule(ScreenRotation screenRotation)
        {
            string devicesText = ProcessUtil.Run("libinput", "list-devices", asAdmin:true, useBash:false);
            var deviceParts = devicesText.Split('\n');
            string deviceName = null;
            bool touchFound = false;
            foreach (var devicePart in deviceParts)
            {
                if (devicePart.StartsWith("Device:"))
                {
                    var parts = devicePart.Split(':');
                    if (parts.Length == 2) deviceName = parts[1].Trim();
                }
                else if (deviceName != null && devicePart.StartsWith("Capabilities:"))
                {
                    var parts = devicePart.Split(':');
                    if (parts.Length == 2 && parts[1].Contains("touch"))
                    {
                        touchFound = true;
                        break;
                    }
                }
            }

            if (touchFound)
            {
                string display = GetX11Display();
                string rule = "SUBSYSTEM==\"input\", KERNEL==\"event[0-9]*\", ATTRS{name}==\"" + deviceName + "\", ENV{WL_OUTPUT}=\"" + display + "\"";
                switch (screenRotation)
                {
                    case ScreenRotation.Unset: return "";
                    case ScreenRotation.Default: return rule + ", ENV{LIBINPUT_CALIBRATION_MATRIX}=\"1 0 0 0 1 0\"";
                    case ScreenRotation.Left: return rule + ", ENV{LIBINPUT_CALIBRATION_MATRIX}=\"0 -1 1 1 0 0\"";
                    case ScreenRotation.Right: return rule + ", ENV{LIBINPUT_CALIBRATION_MATRIX}=\"0 1 0 -1 0 1\"";
                    case ScreenRotation.Flip: return rule + ", ENV{LIBINPUT_CALIBRATION_MATRIX}=\"-1 0 1 0 -1 1\"";
                }
            }

            return "";
        }
        
        void WriteWaylandSettings(StreamWriter writer, string rotation, ScreenRotation screenRotation)
        {
            string rule = "";
            string vrrArg = vrrCheckbox.IsChecked == true ? " --adaptive-sync enabled" : ""; //--vrr on
            if (displaySettings.Count == 0 || !displaySettings.Exists(x => x.enabled))
            {
                writer.WriteLine("display=$(wlr-randr | awk '/^[^ ]+/{print $1; exit}')");
                if (!string.IsNullOrEmpty(rotation))
                {
                    rotation = $" --transform {rotation}";
                    rule = WaylandTouchscreenRule(screenRotation);
                }
                writer.WriteLine($"wlr-randr --output $display{rotation}{vrrArg}");
            }
            else
            {
                foreach (var setting in displaySettings)
                {
                    if (!setting.enabled)
                    {
                        writer.WriteLine($"wlr-randr --output {setting.name} --off");
                        continue;
                    }

                    rule = WaylandTouchscreenRule(setting.rotation);
                    switch (setting.rotation)
                    {
                        case ScreenRotation.Unset: rotation = ""; break;
                        case ScreenRotation.Default: rotation = "normal"; break;
                        case ScreenRotation.Left: rotation = "90";  break;
                        case ScreenRotation.Right: rotation = "270"; break;
                        case ScreenRotation.Flip: rotation = "180";  break;
                    }

                    string mode = "";
                    if (setting.widthOverride > 0 && setting.heightOverride > 0) mode = $" --mode {setting.widthOverride}x{setting.heightOverride}";
                    if (!string.IsNullOrEmpty(rotation)) rotation = $" --transform {rotation}";
                    writer.WriteLine($"wlr-randr --output {setting.name}{rotation}{vrrArg}{mode}");
                }
            }

            // write touchscreen rotation rule
            if (rot_Touchscreen.IsChecked != true) rule = "";// allow allow rule if wanted
            ProcessUtil.WriteAllTextAdmin("/etc/udev/rules.d/99-touchscreen.rules", rule);
        }
        
        void WriteWestonSettings(StreamWriter writer, string rotation, string display)
        {
            /*if (hdrCheckbox.IsChecked == true)
            {
                writer.WriteLine("[core]");
                writer.WriteLine("color-management=true");// HDR color managment
                writer.WriteLine();
            }*/

            if (displaySettings.Count == 0 || !displaySettings.Exists(x => x.enabled))
            {
                writer.WriteLine("[output]");
                writer.WriteLine($"name={display}");
                if (!string.IsNullOrEmpty(rotation)) writer.WriteLine($"transform={rotation}");

                if (vrrCheckbox.IsChecked == true)
                {
                    writer.WriteLine("enable_vrr=true");
                    writer.WriteLine("vrr-mode=game");
                }

                /*if (hdrCheckbox.IsChecked == true)
                {
                    writer.WriteLine("eotf-mode=st2084"); // HDR PQ curve
                    writer.WriteLine("colorimetry-mode=bt2020rgb"); // HDR wide‑gamut space
                }*/
            }
            else
            {
                foreach (var setting in displaySettings)
                {
                    switch (setting.rotation)
                    {
                        case ScreenRotation.Unset: rotation = ""; break;
                        case ScreenRotation.Default: rotation = "normal"; break;
                        case ScreenRotation.Left: rotation = "rotate-90"; break;
                        case ScreenRotation.Right: rotation = "rotate-270"; break;
                        case ScreenRotation.Flip: rotation = "rotate-180"; break;
                    }
                    
                    if (!setting.enabled)
                    {
                        writer.WriteLine("[output]");
                        writer.WriteLine($"name={setting.name}");
                        writer.WriteLine("mode=off");
                        writer.WriteLine();
                        continue;
                    }

                    writer.WriteLine("[output]");
                    writer.WriteLine($"name={setting.name}");
                    if (!string.IsNullOrEmpty(rotation)) writer.WriteLine($"transform={rotation}");
                    if (setting.widthOverride > 0 && setting.heightOverride > 0)
                    {
                        writer.WriteLine($"mode={setting.widthOverride}x{setting.heightOverride}");
                    }

                    if (vrrCheckbox.IsChecked == true)
                    {
                        writer.WriteLine("enable_vrr=true");
                        writer.WriteLine("vrr-mode=game");
                    }

                    /*if (hdrCheckbox.IsChecked == true)
                    {
                        writer.WriteLine("eotf-mode=st2084"); // HDR PQ curve
                        writer.WriteLine("colorimetry-mode=bt2020rgb"); // HDR wide‑gamut space
                    }*/
                }
            }
        }

        void WriteBootloaderArgSetting(string rotation)
        {
            string displayArg = "";
            var displaySetting = displaySettings.FirstOrDefault(x => x.enabled);
            if (displaySetting != null)
            {
                displayArg = $" --display-name={displaySetting.name}";
                if (displaySetting.widthOverride > 0 && displaySetting.heightOverride > 0) displayArg += $" --resolution={displaySetting.widthOverride}x{displaySetting.heightOverride}";
            }

            string text = File.ReadAllText(launchFile);
            text = text.Replace(" --rotation-default", "");
            text = text.Replace(" --rotation-left", "");
            text = text.Replace(" --rotation-right", "");
            text = text.Replace(" --rotation-flip", "");
            var match = Regex.Match(text, @" --display-name=(\S*)");
            if (match.Success) text = text.Replace($" --display-name={match.Groups[1].Value}", "");
            while (true)
            {
                match = Regex.Match(text, @"( --resolution=\d*x\d*)");
                if (!match.Success) break;
                text = text.Replace(match.Groups[1].Value, "");
            }

            if (!string.IsNullOrEmpty(rotation)) rotation = $" --rotation-{rotation}";
            text = text.Replace("--use-controlcenter", $"--use-controlcenter{rotation}{displayArg}");
            File.WriteAllText(launchFile, text);
        }
        
        static List<string> GetWaylandDisplays()
        {
            var results = new List<string>();
            try
            {
                ProcessUtil.KillHard("wlr-randr", true, out _);
                string result = ProcessUtil.Run("wlr-randr", "", useBash:false, killAfterSec:2);
                var lines = result.Split('\n');
                foreach (string line in lines)
                {
                    var parts = line.Split(' ');
                    if (parts.Length >= 2 && !parts[0].StartsWith(' ') && parts[1].StartsWith('"'))
                    {
                        results.Add(parts[0].Trim());
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(e);
            }
            return results;
        }
        
        static string GetWaylandDisplay()
        {
            var results = GetWaylandDisplays();
            if (results.Count >= 1) return results[0];
            return "ERROR";
        }

        static List<string> GetWestonDisplays()
        {
            var results = new List<string>();
            try
            {
                ProcessUtil.KillHard("wayland-info", true, out _);
                string result = ProcessUtil.Run("wayland-info", "", useBash:false, killAfterSec:2, log:false);
                var lines = result.Split('\n');
                bool outputMode = false;
                foreach (string line in lines)
                {
                    if (outputMode)
                    {
                        if (line.Contains("name: "))
                        {
                            var match = Regex.Match(line, @"name:\s*(.*)");
                            if (match.Success) results.Add(match.Groups[1].Value.Trim());
                        }
                    }
                    else if (line.Contains("'wl_output'"))
                    {
                        outputMode = true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(e);
            }
            return results;
        }

        static string GetWestonDisplay()
        {
            var results = GetWestonDisplays();
            if (results.Count >= 1) return results[0];
            return "ERROR";
        }

        static List<string> GetX11Displays()
        {
            var results = new List<string>();
            try
            {
                ProcessUtil.KillHard("xrandr", true, out _);
                string result = ProcessUtil.Run("xrandr", "", useBash:false);
                var lines = result.Split('\n');
                bool screenMode = false;
                foreach (string line in lines)
                {
                    if (screenMode)
                    {
                        var parts = line.Split(' ');
                        if (parts.Length >= 1) results.Add(parts[0].Trim());
                        screenMode = false;
                    }
                    else if (line.StartsWith("Screen "))
                    {
                        screenMode = true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(e);
            }
            return results;
        }
        
        static string GetX11Display()
        {
            var results = GetX11Displays();
            if (results.Count >= 1) return results[0];
            return "ERROR";
        }
        
        const string folder = "/home/gamer/ReignOS_Ext";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        if (!Directory.Exists("/home/gamer/.config")) Directory.CreateDirectory("/home/gamer/.config");
        string x11SettingsFile = Path.Combine(folder, "X11_Settings.sh");
        string waylandSettingsFile = Path.Combine(folder, "Wayland_Settings.sh");
        const string westonConfigFile = "/home/gamer/.config/weston.ini";
        try
        {
            //ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform normal", useBash:false);// KEEP: used to set wlroots rot at runtime
            if (rot_Unset.IsChecked == true)
            {
                using (var writer = new StreamWriter(x11SettingsFile)) WriteX11Settings(writer, "");
                using (var writer = new StreamWriter(waylandSettingsFile)) WriteWaylandSettings(writer, "", ScreenRotation.Unset);
                using (var writer = new StreamWriter(westonConfigFile))  WriteWestonSettings(writer, "", GetWestonDisplay());
                WriteBootloaderArgSetting("");
            }
            else if (rot_Default.IsChecked == true)
            {
                using (var writer = new StreamWriter(x11SettingsFile)) WriteX11Settings(writer, "normal");
                using (var writer = new StreamWriter(waylandSettingsFile)) WriteWaylandSettings(writer, "normal", ScreenRotation.Default);
                using (var writer = new StreamWriter(westonConfigFile))  WriteWestonSettings(writer, "normal", GetWestonDisplay());
                WriteBootloaderArgSetting("default");
            }
            else if (rot_Left.IsChecked == true)
            {
                using (var writer = new StreamWriter(x11SettingsFile)) WriteX11Settings(writer, "left");
                using (var writer = new StreamWriter(waylandSettingsFile)) WriteWaylandSettings(writer, "90", ScreenRotation.Left);
                using (var writer = new StreamWriter(westonConfigFile)) WriteWestonSettings(writer, "rotate-90", GetWestonDisplay());
                WriteBootloaderArgSetting("left");
            }
            else if (rot_Right.IsChecked == true)
            {
                using (var writer = new StreamWriter(x11SettingsFile)) WriteX11Settings(writer, "right");
                using (var writer = new StreamWriter(waylandSettingsFile)) WriteWaylandSettings(writer, "270", ScreenRotation.Right);
                using (var writer = new StreamWriter(westonConfigFile)) WriteWestonSettings(writer, "rotate-270", GetWestonDisplay());
                WriteBootloaderArgSetting("right");
            }
            else if (rot_Flip.IsChecked == true)
            {
                using (var writer = new StreamWriter(x11SettingsFile)) WriteX11Settings(writer, "inverted");
                using (var writer = new StreamWriter(waylandSettingsFile)) WriteWaylandSettings(writer, "180", ScreenRotation.Flip);
                using (var writer = new StreamWriter(westonConfigFile)) WriteWestonSettings(writer, "rotate-180", GetWestonDisplay());
                WriteBootloaderArgSetting("flip");
            }
        }
        catch (Exception ex)
        {
            Log.WriteLine(ex);
        }

        // default gpu settings
        try
        {
            const string bashrc = "/home/gamer/.bashrc";
            const string gpuSettings = folder + "/PrimeGPU";
            const string gpuInc = ". ~/ReignOS_Ext/PrimeGPU";

            // add to bashrc
            string text = File.ReadAllText(bashrc);
            StringBuilder builder;
            if (!text.Contains(gpuInc))
            {
                builder = new StringBuilder(text);
                builder.AppendLine();
                builder.AppendLine(gpuInc);
                File.WriteAllText(bashrc, builder.ToString());
            }

            // update prime gpu and mux
            builder = new StringBuilder();

            int gpu = 0;
            if (gpuButton1.IsChecked == true) gpu = 1;
            else if (gpuButton2.IsChecked == true) gpu = 2;
            else if (gpuButton3.IsChecked == true) gpu = 3;
            else if (gpuButton4.IsChecked == true) gpu = 4;

            if (gpu >= 1)
            {
                gpu--;
                builder.AppendLine($"export DRI_PRIME={gpu}");
                builder.AppendLine($"export NESA_VK_DEVICE_SELECT={gpu}");
                builder.AppendLine($"export VK_DEVICE_SELECT={gpu}");
            }

            // force AMDVLK
            if (amd_VLK.IsChecked == true) builder.AppendLine("export VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/amd_icd64.json:/usr/share/vulkan/icd.d/amd_icd32.json");

            // force AMDGPU-Pro
            if (amd_Proprietary.IsChecked == true) builder.AppendLine("export VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/amd_pro_icd64.json:/usr/share/vulkan/icd.d/amd_pro_icd32.json");

            File.WriteAllText(gpuSettings, builder.ToString());
        }
        catch (Exception ex)
        {
            Log.WriteLine(ex);
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
                        // update network status
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

                        // allow UI features
                        bool enabled = isConnected && !disabledUIState;
                        checkUpdatesButton.IsEnabled = enabled;
                        fixUpdatesButton.IsEnabled = enabled;
                        reinstallSteamButton.IsEnabled = enabled;
                        amdDriversApplyButton.IsEnabled = isConnected;
                        nvidiaDriversApplyButton.IsEnabled = isConnected;
                        gpuMuxApplyButton.IsEnabled = isConnected;
                        inputControlApplyButton.IsEnabled = isConnected;
                        powerManagerApplyButton.IsEnabled = isConnected;
                        rgbManagerApplyButton.IsEnabled = isConnected;
                        kernelApplyButton.IsEnabled = isConnected;
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
    
    private void GamescopeButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 1;// open Steam in Gamescope
        MainWindow.singleton.Close();
    }
    
    private void WestonButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 2;// open Steam in Weston
        MainWindow.singleton.Close();
    }

    private void WestonWindowedButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 3;// open Steam in Weston-Windowed
        MainWindow.singleton.Close();
    }
    
    private void CageButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 4;// open Steam in Cage
        MainWindow.singleton.Close();
    }
    
    private void LabwcButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 5;// open Steam in Labwc
        MainWindow.singleton.Close();
    }
    
    private void X11Button_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 6;// open Steam in X11
        MainWindow.singleton.Close();
    }

    private bool ValidateKDE()
    {
        void MsgBoxCallback(MessageBoxOption option)
        {
            if (option == MessageBoxOption.Option1)
            {
                App.exitCode = 10;// install KDE minimal
                MainWindow.singleton.Close();
            }
            else if (option == MessageBoxOption.Option2)
            {
                App.exitCode = 11;// install KDE full
                MainWindow.singleton.Close();
            }
        }

        if (!PackageExits("konsole"))
        {
            MessageBoxShow("This will install KDE.\nYou have two options, full or minimal\n\n* Full installs all KDE apps\n* Minimal installs only whats needed.", "Minimal", "Full", true, MsgBoxCallback);
            return false;
        }
        
        return true;
    }

    private void KDEButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateKDE()) return;
        App.exitCode = 7;// open KDE
        MainWindow.singleton.Close();
    }
    
    private void KDEX11Button_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateKDE()) return;
        App.exitCode = 8;// open KDE-X11
        MainWindow.singleton.Close();
    }

    private void KDEGButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateKDE()) return;
        App.exitCode = 9;// open KDE-G
        MainWindow.singleton.Close();
    }

    private static bool PackageExits(string package)
    {
        string result = ProcessUtil.Run("pacman", $"-Q {package}");
        return result != null && !result.StartsWith("error:");
    }
    
    private void SleepButton_Click(object sender, RoutedEventArgs e)
    {
        if (hibernatePowerButton) ProcessUtil.Run("systemctl", "hibernate", wait:false, useBash:false);
        else ProcessUtil.Run("systemctl", "suspend", wait:false, useBash:false);
    }
    
    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 15;
        MainWindow.singleton.Close();
    }
    
    private void ShutdownButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 16;
        MainWindow.singleton.Close();
    }
    
    private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is int value) App.exitCode = value;
        else App.exitCode = 17;// close Managment and check updates
        MainWindow.singleton.Close();
    }

    private void FixUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 14;// close Managment and fix updates
        MainWindow.singleton.Close();
    }

    private void ReInstallSteamButton_Click(object sender, RoutedEventArgs e)
    {
        void callback(MessageBoxOption option)
        {
            if (option != MessageBoxOption.Option1) return;
            App.exitCode = 80;
            MainWindow.singleton.Close();
        }

        MessageBoxShow("Are you sure?\nNOTE: This may remove game data", "Ok", null, true, callback);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 20;// close Managment and go to virtual terminal
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
        
        string unameID = ProcessUtil.Run("uname", "-n", useBash:false).Trim();
        string vendorName = ProcessUtil.Run("dmidecode", "-s system-manufacturer", asAdmin:true).Trim();
        string productName = ProcessUtil.Run("dmidecode", "-s system-product-name", asAdmin:true).Trim();
        string body = $"Sent Date UTC: {DateTime.UtcNow}\nDevice: Ven='{vendorName}' Prod='{productName}'\nUserName='{unameID}'";

        var attachments = new List<string>();
        if (File.Exists("/home/gamer/ReignOS_Ext/ReignOS.Bootloader.log")) attachments.Add("/home/gamer/ReignOS_Ext/ReignOS.Bootloader.log");
        if (File.Exists("/home/gamer/ReignOS_Ext/ReignOS.Service.log")) attachments.Add("/home/gamer/ReignOS_Ext/ReignOS.Service.log");
        if (File.Exists("/home/gamer/ReignOS_Ext/ReignOS.ControlCenter.log")) attachments.Add("/home/gamer/ReignOS_Ext/ReignOS.ControlCenter.log");

        emailFromNameText.IsEnabled = false;
        emailToAddressText.IsEnabled = false;
        emailSendButton.IsEnabled = false;
        emailCancelButton.IsEnabled = false;
        emailSendingText.IsVisible = true;
        EmailUtils.SendEmail(from, "MS_Qb1G8I@test-dnvo4d90k76g5r86.mlsender.net", Private.Keys.emailKey, "ReignOS", toAddress, $"ReignOS: {from}", body, EmailSendCallback, attachments.ToArray());
    }
    
    private void EmailCancelButton_Click(object sender, RoutedEventArgs e)
    {
        emailGrid.IsVisible = false;
    }

    private void BootApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        string text = File.ReadAllText(launchFile);

        text = text.Replace(" --gamescope", "");
        text = text.Replace(" --weston", "");
        text = text.Replace(" --cage", "");
        text = text.Replace(" --x11", "");
        text = text.Replace(" --kde-g", "");

        if (boot_Gamescope.IsChecked == true) text = text.Replace("--use-controlcenter", "--use-controlcenter --gamescope");
        else if (boot_Weston.IsChecked == true) text = text.Replace("--use-controlcenter", "--use-controlcenter --weston");
        else if (boot_Cage.IsChecked == true) text = text.Replace("--use-controlcenter", "--use-controlcenter --cage");
        else if (boot_X11.IsChecked == true) text = text.Replace("--use-controlcenter", "--use-controlcenter --x11");
        else if (boot_KDEG.IsChecked == true) text = text.Replace("--use-controlcenter", "--use-controlcenter --kde-g");
        File.WriteAllText(launchFile, text);
        SaveSettings();
    }

    private void RotApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        ProcessUtil.Run("udevadm", "control --reload-rules", asAdmin:true, useBash:false);
        ProcessUtil.Run("udevadm", "trigger", asAdmin: true, useBash: false);
        App.exitCode = 0;// user reboot so gamescope & weston rotation works
        MainWindow.singleton.Close();
    }
    
    private void AMDApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        // invoke AMD driver install script
        SaveSettings();
        if (amd_Mesa.IsChecked == true) App.exitCode = 40;
        else if (amd_VLK.IsChecked == true) App.exitCode = 41;
        else if (amd_Proprietary.IsChecked == true) App.exitCode = 42;
        MainWindow.singleton.Close();
    }

    private void NvidiaApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        // invoke Nvidia driver install script
        SaveSettings();
        if (nvidia_Nouveau.IsChecked == true) App.exitCode = 30;
        else if (nvidia_Proprietary.IsChecked == true) App.exitCode = 31;
        else if (nvidia_Proprietary_Legacy_580.IsChecked == true) App.exitCode = 32;
        else if (nvidia_Proprietary_Legacy_470.IsChecked == true) App.exitCode = 33;
        else if (nvidia_Proprietary_Legacy_340.IsChecked == true) App.exitCode = 34;
        MainWindow.singleton.Close();
    }

    private void PrimeGPUApplyButton_Click(object sender, RoutedEventArgs e)
    {
        string text = File.ReadAllText(launchFile);
        foreach (string line in text.Split('\n'))
        {
            if (line.Contains("--use-controlcenter"))
            {
                string newLine = line;

                // remove existing options
                newLine = newLine.Replace(" --gpu-100", "");// remove this before remove of 0
                newLine = newLine.Replace(" --gpu-0", "");
                newLine = newLine.Replace(" --gpu-1", "");
                newLine = newLine.Replace(" --gpu-2", "");
                newLine = newLine.Replace(" --gpu-3", "");
                newLine = newLine.Replace(" --gpu-4", "");

                // gather new options
                int gpu = 0;
                if (gpuButton1.IsChecked == true) gpu = 1;
                else if (gpuButton2.IsChecked == true) gpu = 2;
                else if (gpuButton3.IsChecked == true) gpu = 3;
                else if (gpuButton4.IsChecked == true) gpu = 4;
                else if (gpuButtonNvidiaPrime.IsChecked == true) gpu = 100;

                // apply options
                if (gpu >= 1) text = text.Replace(line, newLine + $" --gpu-{gpu}");
                else text = text.Replace(line, newLine);

                break;
            }
        }
        File.WriteAllText(launchFile, text);
        SaveSettings();

        App.exitCode = 0;// reopen with full logout so env vars reset
        MainWindow.singleton.Close();
    }

    private void GPUMUXApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (muxButton0.IsChecked == true)
        {
            ProcessUtil.Run("systemctl", "enable supergfxd.service", asAdmin:true, useBash:false);
            ProcessUtil.Run("systemctl", "start supergfxd.service", asAdmin:true, useBash:false);
            
            if (muxButton1.IsChecked == true) ProcessUtil.Run("supergfxctl", $"-m {muxButton1.Content as string}", asAdmin:true, useBash:false);
            else if (muxButton2.IsChecked == true) ProcessUtil.Run("supergfxctl", $"-m {muxButton2.Content as string}", asAdmin:true, useBash:false);
            else if (muxButton3.IsChecked == true) ProcessUtil.Run("supergfxctl", $"-m {muxButton3.Content as string}", asAdmin:true, useBash:false);
            else if (muxButton4.IsChecked == true) ProcessUtil.Run("supergfxctl", $"-m {muxButton4.Content as string}", asAdmin:true, useBash:false);
        }
        else
        {
            ProcessUtil.Run("systemctl", "stop supergfxd.service", asAdmin:true, useBash:false);
            ProcessUtil.Run("systemctl", "disable supergfxd.service", asAdmin:true, useBash:false);
        }

        SaveSettings();

        App.exitCode = 15;// reboot
        MainWindow.singleton.Close();
    }

    private void OtherSettingsApplyButton_Click(object sender, RoutedEventArgs e)
    {
        string text = File.ReadAllText(launchFile);
        foreach (string line in text.Split('\n'))
        {
            if (line.Contains("--use-controlcenter"))
            {
                string newLine = line;

                // remove existing options
                newLine = newLine.Replace(" --use-mangohub", "");
                newLine = newLine.Replace(" --vrr", "");
                newLine = newLine.Replace(" --hdr", "");
                newLine = newLine.Replace(" --disable-steam-gpu", "");
                newLine = newLine.Replace(" --disable-steam-deck", "");

                // gather new options
                string args = "";
                if (mangohubCheckbox.IsChecked == true) args += " --use-mangohub";
                if (vrrCheckbox.IsChecked == true) args += " --vrr";
                if (hdrCheckbox.IsChecked == true) args += " --hdr";
                if (disableSteamGPUCheckbox.IsChecked == true) args += " --disable-steam-gpu";
                if (disableSteamDeckCheckbox.IsChecked == true) args += " --disable-steam-deck";

                // apply options
                text = text.Replace(line, newLine + args);

                break;
            }
        }
        File.WriteAllText(launchFile, text);
        SaveSettings();

        App.exitCode = 0;// reopen with full logout so reloads env
        MainWindow.singleton.Close();
    }

    private void MenuInputApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // apply settings
        string text = File.ReadAllText(launchFile);
        foreach (string line in text.Split('\n'))
        {
            if (line.Contains("--use-controlcenter"))
            {
                string newLine = line;

                // remove existing options
                newLine = newLine.Replace(" --input-reignos", "");
                newLine = newLine.Replace(" --input-inputplumber", "");
                newLine = newLine.Replace(" --input-disable", "");

                // gather new options
                string args = "";
                if (reignOSInputCheckbox.IsChecked == true) args += " --input-reignos";
                else if (inputPlumberInputCheckbox.IsChecked == true) args += " --input-inputplumber";
                else if (hhdInputCheckbox.IsChecked == true) args += " --input-hhd";
                else if (disableInputCheckbox.IsChecked == true) args += " --input-disable";

                // apply options
                text = text.Replace(line, newLine + args);

                break;
            }
        }
        File.WriteAllText(launchFile, text);
        SaveSettings();

        if (inputPlumberInputCheckbox.IsChecked == true) App.exitCode = 51;
        else if (hhdInputCheckbox.IsChecked == true) App.exitCode = 52;
        else App.exitCode = 50;
        MainWindow.singleton.Close();
    }

    private void PowerManagerApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // apply settings
        SaveSettings();
        if (powerProfilesCheckbox.IsChecked == true) App.exitCode = 60;
        //else if (powerStationCheckbox.IsChecked == true) App.exitCode = 61;
        else if (powerDeckyTDPCheckbox.IsChecked == true) App.exitCode = 62;
        //else if (hhdAdjustorCheckbox.IsChecked == true) App.exitCode = 63;
        //else if (steamOSManagerCheckbox.IsChecked == true) App.exitCode = 64;
        else if (powerManagementDisabledCheckbox.IsChecked == true) App.exitCode = 65;
        MainWindow.singleton.Close();
    }

    private void RBGManagerApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // apply settings
        SaveSettings();
        if (rgbDisabledCheckbox.IsChecked == true) App.exitCode = 70;
        else if (rgbHueSyncCheckbox.IsChecked == true) App.exitCode = 71;
        MainWindow.singleton.Close();
    }

    private void KernelApplyButton_Click(object sender, RoutedEventArgs e)
    {
        static void RemoveEntries(ref string loader)
        {
            while (true)
            {
                var match = Regex.Match(loader, @"(default [^\n]*)");
                if (match.Success) loader = loader.Replace(match.Groups[1].Value, "");
                else break;
            }
            loader = loader.TrimEnd('\n').TrimEnd();
        }
        
        // remove lts kernel (TODO: remove later)
        ProcessUtil.DeleteFileAdmin("/boot/initramfs-linux-lts.img");
        ProcessUtil.DeleteFileAdmin("/boot/initramfs-linux-lts-fallback.img");
        
        // apply settings
        string loader = File.ReadAllText("/boot/loader/loader.conf");
        if (kernelArchCheckbox.IsChecked == true)
        {
            RemoveEntries(ref loader);
            loader += "\ndefault arch.conf";
            ProcessUtil.WriteAllTextAdmin("/boot/ReignOS_Kernel.txt", "Arch");
        }
        else if (kernelChimeraCheckbox.IsChecked == true)
        {
            RemoveEntries(ref loader);
            loader += "\ndefault chimera.conf";
            ProcessUtil.WriteAllTextAdmin("/boot/ReignOS_Kernel.txt", "Chimera");
            CopyKernelConf(KernelSettingsCopy.Chimera);
        }
        else if (kernelBazziteCheckbox.IsChecked == true)
        {
            RemoveEntries(ref loader);
            loader += "\ndefault bazzite.conf";
            ProcessUtil.WriteAllTextAdmin("/boot/ReignOS_Kernel.txt", "Bazzite");
            CopyKernelConf(KernelSettingsCopy.Bazzite);
        }
        else if (kernelCachyCheckbox.IsChecked == true)
        {
            RemoveEntries(ref loader);
            loader += "\ndefault cachy.conf";
            ProcessUtil.WriteAllTextAdmin("/boot/ReignOS_Kernel.txt", "Cachy");
            CopyKernelConf(KernelSettingsCopy.Cachy);
        }

        ProcessUtil.WriteAllTextAdmin("/boot/loader/loader.conf", loader);
        SaveSettings();
        CheckUpdatesButton_Click(18, null);
    }

    private void RestApplyButton_Click(object sender, RoutedEventArgs e)
    {
        Log.WriteLine("Rest Apply...");
        
        // disable existing swap if any
        ProcessUtil.Run("swapoff", "-a", asAdmin: true, useBash: false);
        
        // apply settings
        if (restSleepCheckbox.IsChecked == true) RestApply_Sleep();
        else if (restHibernateCheckbox.IsChecked == true) RestApply_Hibernate();
        
        // finish
        SaveSettings();
        App.exitCode = 19;// reboot after mkinitcpio
        MainWindow.singleton.Close();
    }

    private void RestApply_Sleep()
    {
        // disable swap file
        string filename = "/etc/fstab";
        string text = File.ReadAllText(filename);
        if (text.Contains("/swapfile none swap defaults 0 0"))
        {
            text = text.Replace("/swapfile none swap defaults 0 0", "").TrimEnd();
            ProcessUtil.WriteAllTextAdmin(filename, text);
        }
        
        // delete swap file
        ProcessUtil.DeleteFileAdmin("/swapfile");
        
        // remove "resume" hook
        filename = "/etc/mkinitcpio.conf";
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

        // update kernel config
        {
            filename = "/boot/loader/entries/arch.conf";
            text = File.ReadAllText(filename);
            lines = text.Split('\n');
            string segment = lines.FirstOrDefault(x => x.StartsWith("options "));
            if (segment != null)
            {
                var match = Regex.Match(segment, @"( resume=\S* resume_offset=.\S*)");
                if (match.Success)
                {
                    segment = match.Groups[1].Value;
                    text = text.Replace(segment, "");
                    ProcessUtil.WriteAllTextAdmin(filename, text);
                    CopyKernelConf(KernelSettingsCopy.Chimera);
                    CopyKernelConf(KernelSettingsCopy.Bazzite);
                    CopyKernelConf(KernelSettingsCopy.Cachy);
                }
            }
        }
    }

    private void RestApply_Hibernate()
    {
        // get system total ram size in KB
        string result = ProcessUtil.Run("awk", "'/MemTotal/ {print $2}' /proc/meminfo", asAdmin: false, useBash: true);
        if (!int.TryParse(result, out int ramSize))
        {
            Log.WriteLine("Failed to get total ram");
            return;
        }
        ramSize /= 1024;// get ram in MB
        
        // create swap file the size of ram
        ProcessUtil.Run("dd", $"if=/dev/zero of=/swapfile bs=1M count={ramSize} status=progress", asAdmin: true, useBash: true);
        ProcessUtil.Run("chmod", "600 /swapfile", asAdmin: true, useBash: false);
        ProcessUtil.Run("mkswap", "/swapfile", asAdmin: true, useBash: false);
        
        // enable swap file
        ProcessUtil.Run("swapon /swapfile", "/swapfile", asAdmin: true, useBash: false);
        string filename = "/etc/fstab";
        string text = File.ReadAllText(filename);
        if (!text.Contains("/swapfile none swap defaults 0 0"))
        {
            text += "\n/swapfile none swap defaults 0 0";
            ProcessUtil.WriteAllTextAdmin(filename, text);
        }
        
        // add "resume" hook
        filename = "/etc/mkinitcpio.conf";
        text = File.ReadAllText(filename);
        var lines = text.Split('\n');
        foreach (string line in lines)
        {
            if (!line.StartsWith("HOOKS=")) continue;
            var match = Regex.Match(line, @"(HOOKS=\(.*\))");
            if (!match.Success)
            {
                Log.WriteLine("Failed to get mkinitcpio.conf hooks");
                continue;
            }

            string segment = match.Groups[1].Value;
            if (!segment.Contains("filesystems"))
            {
                Log.WriteLine("Failed to get mkinitcpio.conf HOOKS filesystems");
                continue;
            }

            if (!segment.Contains("filesystems resume"))
            {
                string newSegment = segment.Replace("filesystems", "filesystems resume");
                text = text.Replace(segment, newSegment);
                ProcessUtil.WriteAllTextAdmin(filename, text);
                break;
            }
        }
        
        // get resume partition offset
        string resumeOffset = ProcessUtil.Run("filefrag", "-v /swapfile | awk '/ 0:/ {print $4}' | cut -d'.' -f1", asAdmin: true, useBash: true);
        resumeOffset = resumeOffset.TrimEnd();
        
        // update kernel config
        {
            filename = "/boot/loader/entries/arch.conf";
            text = File.ReadAllText(filename);
            var match = Regex.Match(text, @"options root=(\S*)");
            if (!match.Success)
            {
                Log.WriteLine("Failed to get arch.conf options root path");
                return;
            }
            
            string segment = match.Groups[1].Value;
            if (!segment.Contains("resume=") && !segment.Contains("resume_offset="))
            {
                text = text.Replace("rootwait", $"rootwait resume={segment} resume_offset={resumeOffset}");
                ProcessUtil.WriteAllTextAdmin(filename, text);
                CopyKernelConf(KernelSettingsCopy.Chimera);
                CopyKernelConf(KernelSettingsCopy.Bazzite);
                CopyKernelConf(KernelSettingsCopy.Cachy);
            }
        }

        // configure GPU (probably not needed)
        //ProcessUtil.Run("systemctl", "enable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service", asAdmin: true, useBash: false);
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
                        /*const int maxLength = 20;
                        if (name.StartsWith("Windows Boot Manager")) name = "Windows Boot Manager";
                        else if (name.StartsWith("Linux Boot Manager")) name = "Linux Boot Manager";
                        else if (name.Length > maxLength) name = name.Substring(0, maxLength) + "...";*/
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
                Log.WriteLine(e);
            }
        }

        wlanDevices.Clear();
        ProcessUtil.Run("iwctl", "device list", standardOut:deviceOut, useBash:false);
        
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

                // exit if invalid index
                if (networkNameIndex == 0 && securityIndex == 0 && signalIndex == 0) return;
                
                // seperate line sections
                lineClipped = line.Replace("[0m", "").Replace("[1;90m", "");
                string networkNameSection = lineClipped.Substring(networkNameIndex, securityIndex - networkNameIndex).Trim();
                string securitySection = lineClipped.Substring(securityIndex, signalIndex - securityIndex).Trim();
                string signalSection = lineClipped.Substring(signalIndex, lineClipped.Length - signalIndex).Trim();
                
                // get network name
                bool isConnected = Regex.IsMatch(lineClipped, @"\s*>\s*\S*");
                
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
                ssids.Add(new SSID(networkNameSection, $"{networkNameSection} [{networkSecurity}] <Signal {signal}>{connectedTag}", isConnected));
            }
            catch (Exception e)
            {
                Log.WriteLine(e);
            }
        }

        ProcessUtil.Run("iwctl", $"station {wlanDevice} scan", useBash:false);
        ProcessUtil.Run("iwctl", $"station {wlanDevice} get-networks", standardOut:ssidOut, useBash:false);
        connectionListBox.Items.Clear();
        foreach (var ssid in ssids) connectionListBox.Items.Add(new ListBoxItem { Content = ssid.desc, Tag = ssid });
    }
    
    // NOTE: forget network for debugger: iwctl known-networks <SSID> forget
    private async void NetworkConnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (connectionListBox.SelectedIndex < 0 || wlanDevice == null) return;
        networkConnectButton.IsEnabled = false;

        var item = (ListBoxItem)connectionListBox.Items[connectionListBox.SelectedIndex];
        var ssid = (SSID)item.Tag;
        ProcessUtil.KillHard("iwctl", true, out _);// make sure any failed processes are not open
        ProcessUtil.Run("iwctl", $"--passphrase '{networkPasswordText.Text.Trim()}' station {wlanDevice} connect '{ssid.name}'", useBash:true);
        string result = ProcessUtil.Run("iwctl", $"station {wlanDevice} show", useBash:false);
        Log.WriteLine(result);

        await Task.Delay(2000);
        networkConnectButton.IsEnabled = true;
    }

    private void NetworkDisconnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (wlanDevice == null) return;
        ProcessUtil.Run("iwctl", $"station {wlanDevice} disconnect");
        connectionListBox.Items.Clear();
    }

    private void NetworkClearSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        NetworkDisconnectButton_OnClick(null, null);

        if (wlanDevice == null) return;
        ProcessUtil.Run("systemctl", "stop iwd", asAdmin:true, useBash:false);
        ProcessUtil.Run("rm", "-r /var/lib/iwd/*", asAdmin:true);
        ProcessUtil.Run("systemctl", "start iwd", asAdmin:true, useBash:false);
        connectionListBox.Items.Clear();
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

        void MessageBoxCallback(MessageBoxOption option)
        {
            if (option == MessageBoxOption.Cancel) return;

            // unmount partitions and kill auto mount
            ProcessUtil.Run("udiskie-umount", "-a", out _, useBash: false);
            ProcessUtil.Run("systemctl", "stop udisks2", asAdmin: true, useBash: false);
            Thread.Sleep(2000);
            ProcessUtil.KillHard("udiskie", true, out _);

            // prep mounting
            ProcessUtil.Run("umount", "-R /mnt/sdcard/", asAdmin: true, useBash: false);
            ProcessUtil.CreateDirectoryAdmin("/mnt/sdcard/");

            // delete old partitions
            foreach (var parition in drive.partitions)
            {
                ProcessUtil.Run("parted", $"-s {drive.disk} rm {parition.number}", asAdmin: true, useBash: false, verboseLog: true);
            }

            // make sure gpt partition scheme
            ProcessUtil.Run("parted", $"-s -a optimal {drive.disk} mklabel gpt", asAdmin: true, useBash: false, verboseLog: true);// this will destroy existing partitions

            // make new partitions
            ProcessUtil.Run("parted", $"-s -a optimal {drive.disk} mkpart primary ext4 4MiB 100%", asAdmin: true, useBash: false, verboseLog: true);

            // format partitions
            string partitionPath;
            if (drive.PartitionsUseP()) partitionPath = $"{drive.disk}p1";
            else partitionPath = $"{drive.disk}1";
            ProcessUtil.Run("mkfs.ext4", partitionPath, asAdmin: true, useBash: false, verboseLog: true);
            Thread.Sleep(1000);
            ProcessUtil.Run("fsck", partitionPath, asAdmin: true, useBash: false, verboseLog: true);
            Thread.Sleep(1000);
            ProcessUtil.Run("mount", $"{partitionPath} /mnt/sdcard/", asAdmin: true, useBash: false, verboseLog: true);
            Thread.Sleep(1000);
            ProcessUtil.Run("chown", "-R gamer:gamer /mnt/sdcard/", asAdmin: true, useBash: false, verboseLog: true);
            ProcessUtil.Run("chmod", "-R u+rwX /mnt/sdcard/", asAdmin: true, useBash: false, verboseLog: true);
            Thread.Sleep(1000);
            ProcessUtil.Run("umount", "-R /mnt/sdcard/", asAdmin: true, useBash: false, verboseLog: true);
            Thread.Sleep(1000);

            // shutdown to fully power cycle drive
            ShutdownButton_Click(null, null);
        }

        MessageBoxShow("Are you sure?", "Format", null, true, MessageBoxCallback);
    }

    private void FixDriveIssuesButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (driveListBox.SelectedIndex < 0) return;
        var item = (ListBoxItem)driveListBox.Items[driveListBox.SelectedIndex];
        var drive = (Drive)item.Tag;

        // fix permissions on uDisk mount points
        string result = ProcessUtil.Run("udisksctl", "dump | grep -i mountpoints", useBash:true, verboseLog: true);
        var lines = result.Split('\n');
        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"\s*MountPoints:\s*(/run/media/gamer/.*)");
            if (match.Success)
            {
                string path = match.Groups[1].Value;
                ProcessUtil.Run("chown", $"-R gamer:gamer {path}", asAdmin: true, useBash: false, verboseLog: true);
                ProcessUtil.Run("chmod", $"-R u+rwX {path}", asAdmin: true, useBash: false, verboseLog: true);
            }
        }

        // unmount partitions and kill auto mount
        ProcessUtil.Run("udiskie-umount", "-a", out _, useBash: false);
        ProcessUtil.Run("systemctl", "stop udisks2", asAdmin: true, useBash: false);
        Thread.Sleep(2000);
        ProcessUtil.KillHard("udiskie", true, out _);

        // fix partitions
        foreach (var partition in drive.partitions)
        {
            ProcessUtil.Run("fsck", partition.path, asAdmin: true, useBash: false, verboseLog: true);
            Thread.Sleep(1000);
            ProcessUtil.Run("mount", $"{partition.path} /mnt/sdcard/", asAdmin: true, useBash: false, verboseLog: true);
            Thread.Sleep(1000);
            ProcessUtil.Run("chown", "-R gamer:gamer /mnt/sdcard/", asAdmin: true, useBash: false, verboseLog: true);
            ProcessUtil.Run("chmod", "-R u+rwX /mnt/sdcard/", asAdmin: true, useBash: false, verboseLog: true);
            Thread.Sleep(1000);
            ProcessUtil.Run("umount", "-R /mnt/sdcard/", asAdmin: true, useBash: false, verboseLog: true);
            Thread.Sleep(1000);
        }

        // shutdown to fully power cycle drive
        ShutdownButton_Click(null, null);
    }

    private void GPUUtilsButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = false;
        gpuUtilsGrid.IsVisible = true;

        // add gpus
        gpusListBox.Items.Clear();
        foreach (var gpu in gpus)
        {
            var item = new ListBoxItem()
            {
                Content = gpu.card
            };
            gpusListBox.Items.Add(item);
        }

        // add gpu names
        gpuNamesListBox.Items.Clear();
        string result = ProcessUtil.Run("lspci", "| grep -E 'VGA|3D'", useBash:true);
        foreach (var name in result.Split('\n'))
        {
            var item = new ListBoxItem()
            {
                Content = name
            };
            gpuNamesListBox.Items.Add(item);
        }

        // add gpu drivers
        gpuDriversListBox.Items.Clear();
        result = ProcessUtil.Run("ls", "-l /sys/class/drm/card*/device/driver", useBash:true);
        foreach (var driver in result.Split('\n'))
        {
            var item = new ListBoxItem()
            {
                Content = driver
            };
            gpuDriversListBox.Items.Add(item);
        }
    }
    
    private void GPUUtilsBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = true;
        gpuUtilsGrid.IsVisible = false;
    }

    private void NvidiaSettingsButton_Click(object sender, RoutedEventArgs e)
	{
        ProcessUtil.Run("nvidia-settings", "", wait:true);
	}
    
    private void AudioManagerButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = false;
        audioManagerGrid.IsVisible = true;
        RefreshAudioPage();
    }
    
    private void AudioManagerBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = true;
        audioManagerGrid.IsVisible = false;
    }
    
    private void RefreshAudioButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshAudioPage();
    }

    private void RefreshAudioPage()
    {
        audioDefaultCheckbox.IsChecked = false;
        audioDefaultCheckbox.IsEnabled = true;

        // default
        var devicesInfoText = ProcessUtil.Run("pactl", "info", useBash:false);
        var deviceInfos = devicesInfoText.Split('\n');
        foreach (var info in deviceInfos)
        {
            if (info.StartsWith("Default Sink:"))
            {
                var parts = info.Split(':');
                if (defaultAudioSetting == null) defaultAudioSetting = new AudioSetting();
                defaultAudioSetting.name = parts[1].Trim();
                defaultAudioSetting.defaultSink = true;
                break;
            }
        }
        
        // all
        var devicesText = ProcessUtil.Run("pactl", "list sinks short", useBash:false);
        var devices = devicesText.Split('\n');
        audioListBox.Items.Clear();
        foreach (var device in devices)
        {
            // get name
            string name = null;
            string driver = null;
            string channels = null;
            string freq = null;
            var match = Regex.Match(device, @"(\d+)\s+(.*)\s+(.*)\s+(.*)\s+(.*)?\s+(.*)\s");
            if (match.Success)
            {
                name = match.Groups[2].Value;
                driver = match.Groups[3].Value;
                channels = match.Groups[5].Value;
                freq = match.Groups[6].Value;
            }
            if (name == null) continue;
            
            // add setting
            var setting = audioSettings.FirstOrDefault(x => x.name == name);
            if (defaultAudioSetting != null && name == defaultAudioSetting.name)
            {
                setting = defaultAudioSetting;
            }
            else if (setting == null)
            {
                setting = new AudioSetting();
                setting.name = name;
            }
            
            // add
            var item = new ListBoxItem();
            item.Tag = setting;
            string enabled = setting.defaultSink ? "* " : "";
            item.Content = $"{enabled}{name}\n{channels} {freq}\n{driver}";
            audioListBox.Items.Add(item);
        }
    }
    
    private void AudioListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (audioListBox.SelectedIndex < 0) return;

        var item = (ListBoxItem)audioListBox.Items[audioListBox.SelectedIndex];
        var setting = (AudioSetting)item.Tag;
        audioDefaultCheckbox.IsChecked = setting.defaultSink;
        audioDefaultCheckbox.IsEnabled = !setting.defaultSink;
    }
    
    private void AudioDefaultCheckbox_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (audioListBox.SelectedIndex < 0) return;
        
        // change active value
        var item = (ListBoxItem)audioListBox.Items[audioListBox.SelectedIndex];
        var setting = (AudioSetting)item.Tag;
        setting.defaultSink = audioDefaultCheckbox.IsChecked == true;
        if (setting.defaultSink) defaultAudioSetting = setting;

        // disable others
        if (setting.defaultSink)
        {
            foreach (ListBoxItem i in audioListBox.Items)
            {
                if (i == item) continue;
                var s = (AudioSetting)i.Tag;
                s.defaultSink = false;
            }
        }

        audioDefaultCheckbox.IsEnabled = false;
    }
    
    private void AudioManagerApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        audioSettings = new List<AudioSetting>();
        foreach (ListBoxItem item in audioListBox.Items)
        {
            var setting = (AudioSetting)item.Tag;
            audioSettings.Add(setting);
            if (setting.defaultSink) defaultAudioSetting = setting;
        }
        
        SaveSettings();
        if (defaultAudioSetting != null) ProcessUtil.Run("pactl", $"set-default-sink {defaultAudioSetting.name}", useBash:false);

        Thread.Sleep(1000);
        RefreshAudioPage();
    }

    private void AudioManagerTestButton_OnClick(object sender, RoutedEventArgs e)
    {
        ProcessUtil.Run("speaker-test", "-c 2 -D pulse -l 1", useBash:false, wait:true);
    }
    
    private void KernelManagerButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = false;
        kernelManagerGrid.IsVisible = true;
        RefreshKernelPage();
    }
    
    private void KernelManagerBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = true;
        kernelManagerGrid.IsVisible = false;
    }

    private void KernelManagerApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        // write snd_hda_intel settings
        if (kernel_snd_hda_intel_DisableSleep_Checkbox.IsChecked == true)
        {
            ProcessUtil.WriteAllTextAdmin("/etc/modprobe.d/snd_hda_intel_sleep.conf", "options snd_hda_intel power_save=0 power_save_controller=N");
        }
        else
        {
            ProcessUtil.DeleteFileAdmin("/etc/modprobe.d/snd_hda_intel_sleep.conf");
        }
            
        if (kernel_snd_hda_intel_DisableHDMI_Checkbox.IsChecked == true)
        {
            // reset audio system so 'aplay' can grab non-disabled cards
            void standardOut(string line) {}
            ProcessUtil.Run("systemctl", "--user stop pipewire.socket pipewire.service pipewire-pulse.socket pipewire-pulse.service", useBash:false, standardOut:standardOut);
            ProcessUtil.Run("modprobe", "-r snd_hda_intel", asAdmin:true, useBash:false);
            ProcessUtil.DeleteFileAdmin("/etc/modprobe.d/snd_hda_intel_disable.conf");
            Thread.Sleep(1000);
            ProcessUtil.Run("modprobe", "snd_hda_intel", asAdmin:true, useBash:false);
            ProcessUtil.Run("systemctl", "--user start pipewire.socket pipewire.service pipewire-pulse.socket pipewire-pulse.service", useBash:false, standardOut:standardOut);
            Thread.Sleep(1000);
            
            // get current audio device layout
            string result = ProcessUtil.Run("aplay", "-l", useBash:false);
            var lines = result.Split('\n');
            var cards = new Dictionary<string, string>();
            foreach (string line in lines)
            {
                var match = Regex.Match(line, @"card (\d):\s");
                if (match.Success)
                {
                    if (line.Contains("HDMI")) cards[match.Groups[1].Value] = "0";
                    else cards[match.Groups[1].Value] = "1";
                }
            }

            string enableLine = "";
            foreach (var card in cards)
            {
                if (enableLine == "") enableLine += "enable=" + card.Value;
                else enableLine += "," + card.Value;
            }
                
            ProcessUtil.WriteAllTextAdmin("/etc/modprobe.d/snd_hda_intel_disable.conf", $"options snd_hda_intel {enableLine}");
        }
        else
        {
            ProcessUtil.DeleteFileAdmin("/etc/modprobe.d/snd_hda_intel_disable.conf");
        }
        
        // write kernel settings
        string archConf = kernelArchConf;
        archConf = archConf.Replace(kernelArchConf_Options, kernelArchConfigTextBox.Text);
        ProcessUtil.WriteAllTextAdmin("/boot/loader/entries/arch.conf", archConf);
        CopyKernelConf(KernelSettingsCopy.Chimera);
        CopyKernelConf(KernelSettingsCopy.Bazzite);
        CopyKernelConf(KernelSettingsCopy.Cachy);

        // reboot
        CheckUpdatesButton_Click(19, null);// run "sudo mkinitcpio -P" before reboot
    }
    
    private void CopyKernelConf(KernelSettingsCopy target)
    {
        try
        {
            const string archKernelConfig = "/boot/loader/entries/arch.conf";
            if (target == KernelSettingsCopy.Chimera)
            {
                const string targetKernelConfig = "/boot/loader/entries/chimera.conf";
                string copyConf = File.ReadAllText(archKernelConfig);
                copyConf = copyConf.Replace("linux /vmlinuz-linux", "linux vmlinuz-linux-chimeraos");
                copyConf = copyConf.Replace("initrd /initramfs-linux.img", "initrd /initramfs-linux-chimeraos.img");
                ProcessUtil.WriteAllTextAdmin(targetKernelConfig, copyConf);
            }
            else if (target == KernelSettingsCopy.Bazzite)
            {
                const string targetKernelConfig = "/boot/loader/entries/bazzite.conf";
                string copyConf = File.ReadAllText(archKernelConfig);
                copyConf = copyConf.Replace("linux /vmlinuz-linux", "linux vmlinuz-linux-bazzite");
                copyConf = copyConf.Replace("initrd /initramfs-linux.img", "initrd /initramfs-linux-bazzite.img");
                ProcessUtil.WriteAllTextAdmin(targetKernelConfig, copyConf);
            }
            else if (target == KernelSettingsCopy.Cachy)
            {
                const string targetKernelConfig = "/boot/loader/entries/cachy.conf";
                string copyConf = File.ReadAllText(archKernelConfig);
                copyConf = copyConf.Replace("linux /vmlinuz-linux", "linux vmlinuz-linux-cachyos");
                copyConf = copyConf.Replace("initrd /initramfs-linux.img", "initrd /initramfs-linux-cachyos.img");
                ProcessUtil.WriteAllTextAdmin(targetKernelConfig, copyConf);
            }
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
    }

    private void KernelValue_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(kernelArchConf_Options)) return;
        
        // validate conflicting options
        if (kernel_acpi_strict_Checkbox.IsChecked == true && kernel_acpi_force_Checkbox.IsChecked == true)
        {
            kernel_acpi_strict_Checkbox.IsChecked = false;
            kernel_acpi_force_Checkbox.IsChecked = false;
        }

        if (kernel_mem_sleep_default_deep_Checkbox.IsChecked == true && kernel_mem_sleep_default_s2idle_Checkbox.IsChecked == true)
        {
            kernel_mem_sleep_default_deep_Checkbox.IsChecked = false;
            kernel_mem_sleep_default_s2idle_Checkbox.IsChecked = false;
        }

        if (!int.TryParse(kernel_intel_idle_max_cstate_TextBox.Text, out int kernel_intel_idle_max_cstate_Value)) kernel_intel_idle_max_cstate_Value = 1;
        if (kernel_intel_idle_max_cstate_Value < 1) kernel_intel_idle_max_cstate_Value = 1;
        if (kernel_intel_idle_max_cstate_Value > kernelIntelMaxCState) kernel_intel_idle_max_cstate_Value = kernelIntelMaxCState;

        if (!int.TryParse(kernel_amd_pstate_processor_max_cstate_TextBox.Text, out int kernel_amd_pstate_processor_max_cstate_Value)) kernel_amd_pstate_processor_max_cstate_Value = 1;
        if (kernel_amd_pstate_processor_max_cstate_Value < 1) kernel_amd_pstate_processor_max_cstate_Value = 1;
        if (kernel_amd_pstate_processor_max_cstate_Value > kernelAMDMaxCState) kernel_amd_pstate_processor_max_cstate_Value = kernelAMDMaxCState;

        // update UI
        var match = Regex.Match(kernelArchConf_Options, @"(options root=[^\n|^\s]+)");
        if (match.Success)
        {
            // add required options
            var builder = new StringBuilder($"{match.Groups[1].Value} rw rootwait");
            
            // add known options
            if (kernel_acpi_strict_Checkbox.IsChecked == true) builder.Append(" acpi=strict");
            if (kernel_acpi_force_Checkbox.IsChecked == true) builder.Append(" acpi=force");
            if (kernel_pci_realloc_Checkbox.IsChecked == true) builder.Append(" pci=realloc");

            if (kernel_intel_idle_max_cstate_Checkbox.IsChecked == true) builder.Append($" intel_idle.max_cstate={kernel_intel_idle_max_cstate_Value}");
            if (kernel_amd_pstate_processor_max_cstate_Checkbox.IsChecked == true) builder.Append($" amd_pstate=disable processor.max_cstate={kernel_amd_pstate_processor_max_cstate_Value}");

            if (kernel_amd_vulkan_Checkbox.IsChecked == true) builder.Append(" radeon.cik_support=0 amdgpu.cik_support=1");

            if (kernel_mem_sleep_default_deep_Checkbox.IsChecked == true) builder.Append(" mem_sleep_default=deep");
            if (kernel_mem_sleep_default_s2idle_Checkbox.IsChecked == true) builder.Append(" mem_sleep_default=s2idle");

            if (kernel_i915_audio_Checkbox.IsChecked == true) builder.Append(" i915.audio=0");
            if (kernel_amdgpu_audio_Checkbox.IsChecked == true) builder.Append(" amdgpu.audio=0");
            if (kernel_radeon_audio_Checkbox.IsChecked == true) builder.Append(" radeon.audio=0");
            if (kernel_nouveau_audio_Checkbox.IsChecked == true) builder.Append(" nouveau.audio=0");

            // add custom options
            var parts = kernelCustomTextuBox.Text.Replace(",", "").Replace(";", "").Split(" ");
            foreach (var part in parts) builder.Append(" " + part);
            
            // finish
            kernelArchConfigTextBox.Text = builder.ToString();
        }
    }
    
    private void RefreshKernelPage()
    {
        // get kernel version
        kernelVersionTextBox.Text = ProcessUtil.Run("uname", "-r", useBash:false);

        // read disable hdmi audio conf
        kernel_snd_hda_intel_DisableSleep_Checkbox.IsChecked = File.Exists("/etc/modprobe.d/snd_hda_intel_sleep.conf");
        kernel_snd_hda_intel_DisableHDMI_Checkbox.IsChecked = File.Exists("/etc/modprobe.d/snd_hda_intel_disable.conf");

        // read cstates
        string cstatePath = "/sys/module/intel_idle/parameters/max_cstate";
        if (File.Exists(cstatePath))
        {
            string cstate = ProcessUtil.ReadAllTextAdmin(cstatePath);
            if (int.TryParse(cstate, out kernelIntelMaxCState)) kernel_intel_idle_max_cstate_MaxCState.Text = $"[cstate: Min=1 Max={kernelIntelMaxCState}]";
        }

        cstatePath = "/sys/module/acpi_idle/parameters/max_cstate";
        if (!File.Exists(cstatePath)) cstatePath = "/sys/module/processor/parameters/max_cstate";
        if (File.Exists(cstatePath))
        {
            string cstate = ProcessUtil.ReadAllTextAdmin(cstatePath);
            if (int.TryParse(cstate, out kernelAMDMaxCState)) kernel_amd_pstate_processor_max_cstate_MaxCState.Text = $"[cstate: Min=1 Max={kernelAMDMaxCState}]";
        }

        // read config and get options
        kernelArchConf = File.ReadAllText("/boot/loader/entries/arch.conf");
        var match = Regex.Match(kernelArchConf, @"(options root=[^\n]+)");
        if (match.Success)
        {
            kernelArchConf_Options = match.Groups[1].Value;
        }
        else
        {
            KernelManagerBackButton_OnClick(null, null);// exit page if there is an error
            return;
        }
        kernelArchConfigTextBox.Text = kernelArchConf_Options;
        
        // parse custom options
        var builder = new StringBuilder();
        match = Regex.Match(kernelArchConf_Options, @"options root=.*? ([^\n]*)");
        if (match.Success)
        {
            var parts = match.Groups[1].Value.Split(' ');
            foreach (var part in parts)
            {
                if (part.Length != 0 &&
                    part != "rw" &&
                    part != "rootwait" &&
                    part != "acpi=strict" && part != "acpi=force" &&
                    part != "pci=realloc" &&

                    !part.Contains("intel_idle.max_cstate=") &&
                    part != "amd_pstate=disable" && !part.Contains("processor.max_cstate=") &&

                    part != "radeon.cik_support=0" && part != "amdgpu.cik_support=1" &&

                    part != "mem_sleep_default=deep" && part != "mem_sleep_default=s2idle" &&
                    part != "i915.audio=0" && part != "amdgpu.audio=0" && part != "radeon.audio=0" && part != "nouveau.audio=0" &&
                    
                    !part.Contains("resume=") && !part.Contains("resume_offset="))
                {
                    builder.Append(part + " ");
                }

                if (part.Contains("intel_idle.max_cstate="))
                {
                    var subParts = part.Split('=');
                    if (subParts.Length == 2) kernel_intel_idle_max_cstate_TextBox.Text = subParts[1];
                }

                if (part.Contains("processor.max_cstate="))
                {
                    var subParts = part.Split('=');
                    if (subParts.Length == 2) kernel_amd_pstate_processor_max_cstate_TextBox.Text = subParts[1];
                }
            }
        }
        kernelCustomTextuBox.Text += builder.ToString();
        
        // setup known options
        kernel_acpi_strict_Checkbox.IsChecked = kernelArchConf_Options.Contains(" acpi=strict");
        kernel_acpi_force_Checkbox.IsChecked = kernelArchConf_Options.Contains(" acpi=force");
        kernel_pci_realloc_Checkbox.IsChecked = kernelArchConf_Options.Contains(" pci=realloc");

        kernel_intel_idle_max_cstate_Checkbox.IsChecked = kernelArchConf_Options.Contains(" intel_idle.max_cstate=");
        kernel_amd_pstate_processor_max_cstate_Checkbox.IsChecked = kernelArchConf_Options.Contains(" amd_pstate=disable processor.max_cstate=");

        kernel_amd_vulkan_Checkbox.IsChecked = kernelArchConf_Options.Contains(" radeon.cik_support=0") && kernelArchConf_Options.Contains(" amdgpu.cik_support=1");

        kernel_mem_sleep_default_deep_Checkbox.IsChecked = kernelArchConf_Options.Contains(" mem_sleep_default=deep");
        kernel_mem_sleep_default_s2idle_Checkbox.IsChecked = kernelArchConf_Options.Contains(" mem_sleep_default=s2idle");

        kernel_i915_audio_Checkbox.IsChecked = kernelArchConf_Options.Contains(" i915.audio=0");
        kernel_amdgpu_audio_Checkbox.IsChecked = kernelArchConf_Options.Contains(" amdgpu.audio=0");
        kernel_radeon_audio_Checkbox.IsChecked = kernelArchConf_Options.Contains(" radeon.audio=0");
        kernel_nouveau_audio_Checkbox.IsChecked = kernelArchConf_Options.Contains(" nouveau.audio=0");
    }

    private void DisplayManagerButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = false;
        displayManagerGrid.IsVisible = true;
        RefreshDisplaysPage();
    }
    
    private void DisplayManagerBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = true;
        displayManagerGrid.IsVisible = false;
    }
    
    private void RefreshDisplaysButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshDisplaysPage();
    }

    private void RefreshDisplaysPage()
    {
        //var screens = MainWindow.singleton.Screens.All;
        var screensText = ProcessUtil.Run("ls", "/sys/class/drm/", useBash:false);
        var screens = screensText.Split('\n');
        displayListBox.Items.Clear();
        foreach (var screen in screens)
        {
            //string connectedText = ProcessUtil.Run("cat", $"/sys/class/drm/{screen}/status", useBash:false);
            //bool connected = connectedText.Trim() == "connected";
            //if (!connected) continue;

            // get name
            string name = null;
            var match = Regex.Match(screen, @"card\d-(.*)");
            if (match.Success) name = match.Groups[1].Value;
            if (name == null) continue;
            
            // get resolutions
            string resolutionsText = ProcessUtil.Run("cat", $"/sys/class/drm/{screen}/modes", useBash:false);
            var resolutions = resolutionsText.Split('\n');
            string resolution = resolutions.FirstOrDefault();
            if (string.IsNullOrEmpty(resolution)) continue;
            
            // add setting
            var setting = displaySettings.FirstOrDefault(x => x.name == name);
            if (setting == null)
            {
                setting = new DisplaySetting();
                setting.name = name;
            }
            
            /*var resolutionParts = resolution.Split('x');
            if (resolutionParts.Length == 2)
            {
                if (!int.TryParse(resolutionParts[0], out setting.width)) setting.width = 0;
                if (!int.TryParse(resolutionParts[1], out setting.height)) setting.height = 0;
            }
            else
            {
                setting.width = 0;
                setting.height = 0;
            }*/
            
            var item = new ListBoxItem();
            item.Tag = setting;
            string enabled = setting.enabled ? " *" : "";
            item.Content = $"Name:{name} Rez:{resolution}{enabled}";
            displayListBox.Items.Add(item);
        }

        // refresh brightness slider
        try
        {
            int brightness = 0, count = 0;
            foreach (string dir in Directory.GetDirectories("/sys/class/backlight"))
            {
                try
                {
                    var brightnessText = File.ReadAllText(Path.Combine(dir, "brightness")).Trim();
                    var maxBrightnessText = File.ReadAllText(Path.Combine(dir, "max_brightness")).Trim();
                    if (int.TryParse(brightnessText, out int b) && int.TryParse(maxBrightnessText, out int maxB))
                    {
                        brightness += (int)((b / (double)maxB) * 100);
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex);
                }
            }

            if (count > 0) brightness /= count;
            displayBrightnessSlider.Value = Math.Clamp(brightness, 0, 100);
        }
        catch (Exception ex)
        {
            Log.WriteLine(ex);
        }
    }

    private void DisplayManagerApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        displaySettings = new List<DisplaySetting>();
        foreach (ListBoxItem item in displayListBox.Items)
        {
            var setting = (DisplaySetting)item.Tag;
            displaySettings.Add(setting);
        }
        
        SaveSettings();
        App.exitCode = 0;
        MainWindow.singleton.Close();
    }

    private void DisplayListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (displayListBox.SelectedIndex < 0) return;

        var item = (ListBoxItem)displayListBox.Items[displayListBox.SelectedIndex];
        var setting = (DisplaySetting)item.Tag;
        
        displayEnabledCheckbox.IsChecked = setting.enabled;
        displayWidthText.Text = setting.widthOverride.ToString();
        displayHeightText.Text = setting.heightOverride.ToString();
        
        if (setting.rotation == ScreenRotation.Unset) displayRot_Unset.IsChecked = true;
        else if (setting.rotation == ScreenRotation.Default) displayRot_Default.IsChecked = true;
        else if (setting.rotation == ScreenRotation.Left) displayRot_Left.IsChecked = true;
        else if (setting.rotation == ScreenRotation.Right) displayRot_Right.IsChecked = true;
        else if (setting.rotation == ScreenRotation.Flip) displayRot_Flip.IsChecked = true;
    }

    private void DisplayEnabledCheckbox_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (displayListBox.SelectedIndex < 0) return;
        
        // change active value
        var item = (ListBoxItem)displayListBox.Items[displayListBox.SelectedIndex];
        var setting = (DisplaySetting)item.Tag;
        setting.enabled = displayEnabledCheckbox.IsChecked == true;
        
        // disable others
        if (setting.enabled)
        {
            foreach (ListBoxItem i in displayListBox.Items)
            {
                if (i == item) continue;
                var s = (DisplaySetting)i.Tag;
                s.enabled = false;
            }
        }
    }

    private void DisplayText_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (displayListBox.SelectedIndex < 0) return;
        
        // change active value
        var item = (ListBoxItem)displayListBox.Items[displayListBox.SelectedIndex];
        var setting = (DisplaySetting)item.Tag;
        if (!int.TryParse(displayWidthText.Text, out setting.widthOverride)) setting.widthOverride = 0;
        if (!int.TryParse(displayHeightText.Text, out setting.heightOverride)) setting.heightOverride = 0;
    }

    private void DisplayRotButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (displayListBox.SelectedIndex < 0) return;
        
        // change active value
        var item = (ListBoxItem)displayListBox.Items[displayListBox.SelectedIndex];
        var setting = (DisplaySetting)item.Tag;
        if (displayRot_Unset.IsChecked == true) setting.rotation = ScreenRotation.Unset;
        else if (displayRot_Default.IsChecked == true) setting.rotation = ScreenRotation.Default;
        else if (displayRot_Left.IsChecked == true) setting.rotation = ScreenRotation.Left;
        else if (displayRot_Right.IsChecked == true) setting.rotation = ScreenRotation.Right;
        else if (displayRot_Flip.IsChecked == true) setting.rotation = ScreenRotation.Flip;
    }

    private void DisplayApplyBrightnessButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            double brightness = Math.Clamp(displayBrightnessSlider.Value / 100, 0.1, 1.0);
            foreach (string dir in Directory.GetDirectories("/sys/class/backlight"))
            {
                var maxBrightnessText = File.ReadAllText(Path.Combine(dir, "max_brightness")).Trim();
                if (int.TryParse(maxBrightnessText, out int maxBrightness))
                {
                    maxBrightness = (int)(maxBrightness * brightness);
                    ProcessUtil.WriteAllTextAdmin(Path.Combine(dir, "brightness"), maxBrightness.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            Log.WriteLine(ex);
        }
    }

    private void DisplayApplyGamescopeConfButton_OnClick(object sender, RoutedEventArgs e)
    {
        const string gamescopePath = "/usr/share/gamescope/scripts/00-gamescope/displays/";
        const string displayPath = "/sys/class/drm/";
        if (!Directory.Exists(gamescopePath) || !Directory.Exists(displayPath)) return;
        
        // for each display with edid
        foreach (string directory in Directory.GetDirectories(displayPath))
        {
            string edidPath = Path.Combine(directory, "edid");
            if (!File.Exists(edidPath)) continue;

            // parse edid desc
            string eotf = "gamescope.eotf.gamma22";
            string max_content_light_level = null;
            string max_frame_average_luminance = null;
            string min_content_light_level = null;
            string vendor = null;
            string model = null;
            var timings = new List<string>();
            string edidDesc = ProcessUtil.Run("cat", $"{edidPath} | edid-decode", useBash: true);
            if (string.IsNullOrEmpty(edidDesc) || edidDesc.Length < 100) continue;
            var lines = edidDesc.Split('\n');
            bool eotfMode = false;
            bool timingsMode = false;
            foreach (string line in lines)
            {
                if (eotfMode)
                {
                    if (line.Contains("Supported static metadata descriptors:"))
                    {
                        eotfMode = false;
                    }
                    else
                    {
                        var match = Regex.Match(line, @"SMPTE (.*)");
                        if (match.Success) eotf = $"gamescope.eotf.{match.Groups[1].Value}";
                    }
                }
                else if (timingsMode)
                {
                    if (line.Contains("Checksum:") || line.Contains("Display Range Limits:"))
                    {
                        timingsMode = false;
                    }
                    else
                    {
                        var match = Regex.Match(line, @"DTD\s+\d+:\s+\d+x\d+\s+(\d+(\.\d*))\s+Hz");
                        if (match.Success) timings.Add(match.Groups[1].Value);
                    }
                }
                else if (line.Contains("Electro optical transfer functions:"))
                {
                    eotfMode = true;
                }
                else if (line.Contains("Detailed Timing Descriptors:"))
                {
                    timingsMode = true;
                }
                else if (line.Contains("Desired content max luminance:"))
                {
                    var match = Regex.Match(line, @"Desired content max luminance: \d* \((\d+(\.\d*)).*\)");
                    if (match.Success) max_content_light_level =  match.Groups[1].Value;
                }
                else if (line.Contains("Desired content max frame-average luminance:"))
                {
                    var match = Regex.Match(line, @"Desired content max frame-average luminance: \d* \((\d+(\.\d*)).*\)");
                    if (match.Success) max_frame_average_luminance =  match.Groups[1].Value;
                }
                else if (line.Contains("Desired content min luminance:"))
                {
                    var match = Regex.Match(line, @"Desired content min luminance: \d* \((\d+(\.\d*)).*\)");
                    if (match.Success) min_content_light_level =  match.Groups[1].Value;
                }
                else if (line.Contains("Manufacturer: "))
                {
                    var match = Regex.Match(line, @"Manufacturer: (.*)");
                    if (match.Success) vendor =  match.Groups[1].Value;
                }
                else if (line.Contains("Display Product Name: "))
                {
                    var match = Regex.Match(line, @"Display Product Name: '(.*)'");
                    if (match.Success) model =  match.Groups[1].Value;
                }
            }
            
            // generate timeing hashset
            var timingsHashset = new HashSet<int>();
            foreach (var timing in timings)
            {
                if (double.TryParse(timing, out double value))
                {
                    value = Math.Round(value);
                    timingsHashset.Add((int)value);
                }
            }
            var timingsSorted = timingsHashset.ToList();
            timingsSorted.Sort();
            
            // format timings
            string timingsFormated = "";
            int timingIndex = 0;
            foreach (var timing in timingsSorted)
            {
                timingsFormated += timing;
                timingIndex++;
                if (timingIndex != timingsSorted.Count) timingsFormated += ", ";
            }
            
            // save gamescope conf
            if (vendor != null && model != null)
            {
                string confPath;
                #if DEBUG
                confPath = "/home/gamer/Dev/ReignOS/Managment/ReignOS.Service/bin/Debug/net8.0/Gamescope/ReignOS.Custom.lua";
                #else
                confPath = "/home/gamer/ReignOS/Managment/ReignOS.Service/bin/Debug/net8.0/linux-x64/publish/Gamescope/ReignOS.Custom.lua";
                #endif
                string conf = File.ReadAllText(confPath);
                conf = conf.Replace("-- ReignOS Custom Settings for: ", $"-- ReignOS Custom Settings for: Manufacturer='{vendor}' Display Product Name='{model}'");
                conf = conf.Replace("local panel_id = \"?\"", $"local panel_id = \"S{vendor}_{model}_display\"");
                conf = conf.Replace("local panel_name = \"?\"", $"local panel_name = \"{vendor} {model} Display\"");
                conf = conf.Replace("  { vendor = \"?\", model = \"?\" },", $"  {{ vendor = \"{vendor}\", model = \"{model}\" }},");
                conf = conf.Replace("local panel_refresh_rates = { ? }", $"local panel_refresh_rates = {{ {timingsFormated} }}");
                conf = conf.Replace("   eotf = ?,", $"   eotf = {eotf},");
                conf = conf.Replace("   max_content_light_level = ?,", $"   max_content_light_level = {max_content_light_level},");
                conf = conf.Replace("   max_frame_average_luminance = ?,", $"   max_frame_average_luminance = {max_frame_average_luminance},");
                conf = conf.Replace("   min_content_light_level = ?", $"   min_content_light_level = {min_content_light_level}");
                ProcessUtil.WriteAllTextAdmin(Path.Combine(gamescopePath, $"ReignOS.{vendor}_{model}.lua"), conf);
            }
        }
    }
    
    private void DisplayDeleteGamescopeConfButton_OnClick(object sender, RoutedEventArgs e)
    {
        const string gamescopePath = "/usr/share/gamescope/scripts/00-gamescope/displays/";
        if (!Directory.Exists(gamescopePath)) return;
        foreach (string file in Directory.GetFiles(gamescopePath))
        {
            string name = Path.GetFileName(file);
            if (name.StartsWith("ReignOS."))
            {
                ProcessUtil.DeleteFileAdmin(file);
            }
        }
    }
    
    private void PowerManagerButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = false;
        powerManagerGrid.IsVisible = true;
        RefreshPowerPage();
    }
    
    private void PowerManagerBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = true;
        powerManagerGrid.IsVisible = false;
    }
    
    private void RefreshPowerButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshPowerPage();
    }
    
    private void PowerListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (powerListBox.SelectedIndex < 0) return;

        var item = (ListBoxItem)powerListBox.Items[powerListBox.SelectedIndex];
        var setting = (PowerSetting)item.Tag;
        powerActiveCheckbox.IsChecked = setting.active;
        powerActiveCheckbox.IsEnabled = !setting.active;
    }
    
    private void PowerActiveCheckbox_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
        if (powerListBox.SelectedIndex < 0) return;

        if (powerActiveCheckbox.IsChecked == true)
        {
            powerActiveCheckbox.IsEnabled = false;
            var item = (ListBoxItem)powerListBox.Items[powerListBox.SelectedIndex];
            var selectedSetting = (PowerSetting)item.Tag;
            selectedSetting.active = true;
            foreach (var setting in powerSettings)
            {
                if (setting != selectedSetting)
                {
                    setting.active = false;
                }
            }
        }
    }
    
    private void PowerSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        powerFreq.Text = $"Freq: {(int)powerSlider.Value}%";
    }
    
    private void PowerManagerApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        var builder = new StringBuilder();
        
        // active profile
        var profile = powerSettings.FirstOrDefault(x => x.active);
        if (profile != null) builder.AppendLine("Profile=" + profile.name);
        else builder.AppendLine("Profile=NONE");
        
        // intel turbo boost
        if (powerIntelTurboBoostEnabled != null) builder.AppendLine("IntelTurboBoost=" + (powerIntelTurboBoostCheckbox.IsChecked == true ? "True" : "False"));

        // core settings
        bool enableBoost = powerBoostCheckBox.IsChecked == true;
        double percentage = powerSlider.Value / 100.0;
        foreach (var s in powerCPUSettings)
        {
            string boost = "";
            if (s.boost.HasValue) boost = " Boost=" + (enableBoost ? "True" : "False");
            int maxFreq = (int)(s.minFreq + ((s.maxFreq - s.minFreq) * percentage));
            builder.AppendLine($"CPU={s.name} MinFreq={s.minFreq} MaxFreq={maxFreq}{boost}");
        }

        // finish
        File.WriteAllText("/home/gamer/ReignOS_Ext/PowerProfileSettings.txt", builder.ToString());
        SaveSettings();
        PowerProfiles.Apply(true);
        Thread.Sleep(1000);
        RefreshPowerPage();
    }

    private void RefreshPowerPage()
    {
        // get power profiles
        try
        {
            var powerProfilesText = ProcessUtil.Run("powerprofilesctl", "list", useBash: false);
            powerSettings.Clear();
            PowerSetting setting = null;
            foreach (string line in powerProfilesText.Split('\n'))
            {
                if (setting == null)
                {
                    if (line.StartsWith("* "))
                    {
                        var match = Regex.Match(line, @"\* (.*):");
                        if ((match.Success))
                        {
                            setting = new PowerSetting()
                            {
                                name = match.Groups[1].Value,
                                active = true
                            };
                            powerSettings.Add(setting);
                        }
                    }
                    else
                    {
                        var match = Regex.Match(line, @"  (.*):");
                        if ((match.Success && !line.StartsWith("    ")))
                        {
                            setting = new PowerSetting()
                            {
                                name = match.Groups[1].Value
                            };
                            powerSettings.Add(setting);
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    var match = Regex.Match(line, @"    CpuDriver:\s*(.*)");
                    if (match.Success)
                    {
                        setting.driver = match.Groups[1].Value;
                        setting = null;
                    }
                }
                else
                {
                    setting = null;
                }
            }
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
        
        // update UI
        powerListBox.Items.Clear();
        powerActiveCheckbox.IsEnabled = false;
        powerActiveCheckbox.IsChecked = false;
        foreach (var setting in powerSettings)
        {
            var item = new ListBoxItem();
            string active = setting.active ? " (Active)" : "";
            item.Content = $"Name: {setting.name}{active}\nDriver: {setting.driver}";
            item.Tag = setting;
            powerListBox.Items.Add(item);
        }
        
        // get cpu info
        bool hasBoostCore = false;
        bool isBoostCoreEnabled = false;
        try
        {
            powerIntelTurboBoostEnabled = null;
            const string turboBoostPath = "/sys/devices/system/cpu/intel_pstate/no_turbo";
            if (File.Exists(turboBoostPath)) powerIntelTurboBoostEnabled = File.ReadAllText(turboBoostPath).Trim() != "1";
            powerIntelTurboBoostCheckbox.IsChecked = powerIntelTurboBoostEnabled;
            
            powerCPUSettings.Clear();
            foreach (var path in Directory.GetDirectories("/sys/devices/system/cpu"))
            {
                string cpu = Path.GetFileName(path);
                var match = Regex.Match(cpu, @"(cpu\d*)");
                if (match.Success && match.Groups[1].Value == cpu)
                {
                    string freqPath = Path.Combine(path, "cpufreq");
                    string minFreqPath = Path.Combine(freqPath, "cpuinfo_min_freq");
                    string maxFreqPath = Path.Combine(freqPath, "cpuinfo_max_freq");
                    string minFreqScalePath = Path.Combine(freqPath, "scaling_min_freq");
                    string maxFreqScalePath = Path.Combine(freqPath, "scaling_max_freq");
                    var setting = new PowerCPUSetting()
                    {
                        name = cpu,
                        minFreq = int.Parse(File.ReadAllText(minFreqPath).Trim()),
                        maxFreq = int.Parse(File.ReadAllText(maxFreqPath).Trim()),
                        minFreqScale = int.Parse(File.ReadAllText(minFreqScalePath).Trim()),
                        maxFreqScale = int.Parse(File.ReadAllText(maxFreqScalePath).Trim())
                    };
                    
                    string boostPath = Path.Combine(freqPath, "boost");
                    if (File.Exists(boostPath))
                    {
                        hasBoostCore = true;
                        setting.boost = File.ReadAllText(boostPath).Trim() == "1";
                        if (setting.boost == true) isBoostCoreEnabled = true;
                    }
                    
                    powerCPUSettings.Add(setting);
                }
            }
            
            powerCPUSettings.Sort((x, y) =>
            {
                string xNumber = x.name.Substring(3);
                string yNumber = y.name.Substring(3);
                return int.Parse(xNumber).CompareTo(int.Parse(yNumber));
            });
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
        
        // update UI
        if (powerIntelTurboBoostEnabled != null)
        {
            powerIntelTurboBoostCheckbox.IsEnabled = true;
            powerIntelTurboBoostCheckbox.IsChecked = powerIntelTurboBoostEnabled;
        }
        else
        {
            powerIntelTurboBoostCheckbox.IsEnabled = false;
            powerIntelTurboBoostCheckbox.IsChecked = false;
        }
        
        if (hasBoostCore)
        {
            powerBoostCheckBox.IsEnabled = true;
            powerBoostCheckBox.IsChecked = isBoostCoreEnabled;
        }
        else
        {
            powerBoostCheckBox.IsEnabled = false;
            powerBoostCheckBox.IsChecked = false;
        }
        
        powerCPUListBox.Items.Clear();
        foreach (var setting in powerCPUSettings)
        {
            var item = new ListBoxItem();
            string boost = setting.boost != null ? $" - Boost: {setting.boost}" : "";
            item.Content = $"{setting.name} (MinFreq: {setting.minFreq} MaxFreq: {setting.maxFreq}) - (MinFreqScale: {setting.minFreqScale} MaxFreqScale: {setting.maxFreqScale}){boost}";
            item.Tag = setting;
            powerCPUListBox.Items.Add(item);
        }
    }

    private void AyaneoModules_PopOut_Button_OnClick(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("ayaneo-popout-module");
    }

    private void AyaneoModules_PoppedIn_Button_OnClick(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("ayaneo-poppedin-module");
    }

    private void ReignMonitorApplyButton_Click(object sender, RoutedEventArgs e)
    {
        string text = File.ReadAllText(launchFile);
        text = text.Replace(" --reign-monitor", "");
        if (reignMonitorCheckbox.IsChecked == true) text = text.Replace("--use-controlcenter", "--use-controlcenter --reign-monitor");
        File.WriteAllText(launchFile, text);
        
        SaveSettings();
        App.exitCode = 0;
        MainWindow.singleton.Close();
    }

    private void UserManagerButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = false;
        userManagerGrid.IsVisible = true;
        RefreshUserPage();
    }
    
    private void UserManagerBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = true;
        userManagerGrid.IsVisible = false;
    }
    
    private void RefreshUserButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshUserPage();
    }

    private void RefreshUserPage()
    {
        string result = ProcessUtil.Run("awk", "-F: '$3 >= 1000 {print $1}' /etc/passwd", useBash: true);
        var lines = result.Split('\n');
        usersListBox.Items.Clear();
        allUsernames = new List<string>();
        foreach (string line in lines)
        {
            string lineTrim = line.Trim();
            if (string.IsNullOrEmpty(lineTrim)) continue;
            allUsernames.Add(lineTrim);
            if (lineTrim == "nobody" || lineTrim == "gamer") continue;
            
            var item = new ListBoxItem();
            item.Content = lineTrim;
            usersListBox.Items.Add(item);
        }
    }
    
    private void UserListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (usersListBox.SelectedIndex < 0) return;
        
        var item = (ListBoxItem)usersListBox.Items[usersListBox.SelectedIndex];
        var username = (string)item.Content;
        customUserTextBox.Text = username;
        customPassTextBox.Text = "";
        customPassConfirmTextBox.Text = "";
    }

    private void UserManagerCreateButton_OnClick(object sender, RoutedEventArgs e)
    {
        string passTrim = customPassTextBox.Text.Trim();
        string passConfirmTrim = customPassConfirmTextBox.Text.Trim();
        if (passTrim != passConfirmTrim) return;
        
        string usernameTrim = customUserTextBox.Text.Trim();
        
        // make sure username doesn't already exist
        foreach (var username in allUsernames)
        {
            if (usernameTrim == username) return;
        }

        // create user
        ProcessUtil.Run("useradd", $"-m -G wheel -s /bin/bash {usernameTrim}", useBash: true, asAdmin: true);// create user
        void getStandardInput(StreamWriter writer)
        {
            writer.WriteLine(passTrim);// password enter
            writer.Flush();
            writer.WriteLine(passTrim);// password re-enter
            writer.Flush();
            writer.Close();
        }
        ProcessUtil.Run("passwd", usernameTrim, useBash: true, asAdmin: true, getStandardInput:getStandardInput);// set user pass
        ProcessUtil.Run("usermod", $"-aG wheel,audio,video,storage,input,games,gamemode {usernameTrim}", useBash: true, asAdmin: true);// set user permissions
        
        // finish
        RefreshUserPage();
    }
    
    private void UserManagerDeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (usersListBox.SelectedIndex < 0) return;
        
        var item = (ListBoxItem)usersListBox.Items[usersListBox.SelectedIndex];
        var username = (string)item.Content;
        ProcessUtil.Run("userdel", $"-r {username}", useBash: true, asAdmin: true);// delete user and user folder
        RefreshUserPage();
    }

    /*private void UserManagerStartUserButton_OnClick(object sender, RoutedEventArgs e)
    {
        ProcessUtil.Run("openvt", "-f -s -w -c 4 -- bash -c 'labwc'", useBash: true, asAdmin: true);// this just doesn't seem to work to launch compositor in a good way
    }*/

    private void DisableMainUI(object modified)
    {
        disabledUIState = true;
        revertButton.IsVisible = true;

        if (modified != ayaneoModulePopGrid) ayaneoModulePopGrid.IsEnabled = false;
        if (modified != defaultBootGrid) defaultBootGrid.IsEnabled = false;
        if (modified != screenRotationGrid) screenRotationGrid.IsEnabled = false;
        if (modified != amdDriversGrid) amdDriversGrid.IsEnabled = false;
        if (modified != nvidiaDriversGrid) nvidiaDriversGrid.IsEnabled = false;
        if (modified != primeGPUGrid) primeGPUGrid.IsEnabled = false;
        if (modified != hybridGPUGrid) hybridGPUGrid.IsEnabled = false;
        if (modified != otherSettingsGrid) otherSettingsGrid.IsEnabled = false;
        if (modified != inputControlGrid) inputControlGrid.IsEnabled = false;
        if (modified != powerManagerSwitchGrid) powerManagerSwitchGrid.IsEnabled = false;
        if (modified != rgbManagerGrid) rgbManagerGrid.IsEnabled = false;
        if (modified != kernelGrid) kernelGrid.IsEnabled = false;
        if (modified != restModeGrid) restModeGrid.IsEnabled = false;
        if (modified != reignOSMonitorGrid) reignOSMonitorGrid.IsEnabled = false;

        checkUpdatesButton.IsEnabled = false;
        fixUpdatesButton.IsEnabled = false;
        reinstallSteamButton.IsEnabled = false;

        usersPageButton.IsEnabled = false;
        kernelPageButton.IsEnabled = false;
        gpuPageButton.IsEnabled = false;
        bootPageButton.IsEnabled = false;
        diskPageButton.IsEnabled = false;
        networkPageButton.IsEnabled = false;
        displayPageButton.IsEnabled = false;
        audioPageButton.IsEnabled = false;
        powerButton.IsEnabled = false;
    }

    private void EnableMainUI()
    {
        disabledUIState = false;
        revertButton.IsVisible = false;

        ayaneoModulePopGrid.IsEnabled = true;
        defaultBootGrid.IsEnabled = true;
        screenRotationGrid.IsEnabled = true;
        amdDriversGrid.IsEnabled = true;
        nvidiaDriversGrid.IsEnabled = true;
        primeGPUGrid.IsEnabled = true;
        hybridGPUGrid.IsEnabled = true;
        otherSettingsGrid.IsEnabled = true;
        inputControlGrid.IsEnabled = true;
        powerManagerSwitchGrid.IsEnabled = true;
        rgbManagerGrid.IsEnabled = true;
        kernelGrid.IsEnabled = true;
        restModeGrid.IsEnabled = true;
        reignOSMonitorGrid.IsEnabled = true;

        checkUpdatesButton.IsEnabled = isConnected;
        fixUpdatesButton.IsEnabled = isConnected;
        reinstallSteamButton.IsEnabled = isConnected;

        usersPageButton.IsEnabled = true;
        kernelPageButton.IsEnabled = true;
        gpuPageButton.IsEnabled = true;
        bootPageButton.IsEnabled = true;
        diskPageButton.IsEnabled = true;
        networkPageButton.IsEnabled = true;
        displayPageButton.IsEnabled = true;
        audioPageButton.IsEnabled = true;
        powerButton.IsEnabled = true;

        FullSettingsReload();
    }

    private void RevertButton_Click(object sender, RoutedEventArgs e)
    {
        EnableMainUI();
    }

    private void DefaultBootOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(defaultBootGrid);
    }

    private void ScreenRotationOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(screenRotationGrid);
    }

    private void AMDDriversOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(amdDriversGrid);
    }

    private void NvidiaDriversOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(nvidiaDriversGrid);
    }

    private void PrimeGPUOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(primeGPUGrid);
    }

    private void HybridGPUOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(hybridGPUGrid);
    }

    private void OtherSettingsOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(otherSettingsGrid);
    }

    private void InputControlOption_Click(object sender, RoutedEventArgs e)
    {
        if (hhdInputCheckbox.IsChecked == true)
        {
            hhdAdjustorCheckbox.IsChecked = true;
            
            // disable other options
            powerProfilesCheckbox.IsEnabled = false;
            powerDeckyTDPCheckbox.IsEnabled = false;
            powerManagementDisabledCheckbox.IsEnabled = false;
        }
        else
        {
            switch (lastPowerManagerOption)
            {
                case "PowerProfiles": powerProfilesCheckbox.IsChecked = true; break;
                case "DeckyTDP": powerDeckyTDPCheckbox.IsChecked = true; break;
                default: powerManagementDisabledCheckbox.IsChecked = true; break;
            }
            
            // enable other options
            powerProfilesCheckbox.IsEnabled = true;
            powerDeckyTDPCheckbox.IsEnabled = true;
            powerManagementDisabledCheckbox.IsEnabled = true;
        }
        DisableMainUI(inputControlGrid);
    }

    private void PowerManagerOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(powerManagerSwitchGrid);
    }

    private void RGBManagerOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(rgbManagerGrid);
    }

    private void KernelOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(kernelGrid);
    }

    private void RestModeOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(restModeGrid);
    }

    private void ReignOSMonitorOption_Click(object sender, RoutedEventArgs e)
    {
        DisableMainUI(reignOSMonitorGrid);
    }
}
