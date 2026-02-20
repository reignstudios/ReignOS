namespace ReignOS.Service;
using ReignOS.Core;
using ReignOS.Service.Hardware;
using ReignOS.Service.HardwarePatches;
using ReignOS.Core.OS;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

enum HardwareType
{
    Unknown,

    // MSI
    MSI_Claw,

    // Asus
    RogAlly,
    
    // Lenovo
    Lenovo_LegionGo,
    Lenovo_LegionGo2,
    Lenovo_LegionGoS,

    // Ayaneo
    Ayaneo,
    Ayaneo1,
    Ayaneo2,
    Ayaneo3,
    AyaneoPro,
    AyaneoPlus,
    AyaneoFlipDS,
    AyaneoFlipDS_1S,
    AyaneoSlide,
    AyaneoNextLite,
    AyaneoKun,

    // One-Netbook
    OneXPlayer_Gen1,
    OneXPlayer_Gen2,

    // GPD
    GPD_Win3,
    GPD_Win4,
    GPD_Win5,

    // Zotac
    ZotacZone,

    // AOKZOE
    AOKZOE,

    // AYN
    LokiZero,

    // Anbernic
    Win600,

    // TJD
    TJD
}

internal class Program
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SignalHandler(int signal);

    [DllImport("libc.so", SetLastError = true)]
    private static extern int signal(int sig, SignalHandler handler);
    private const int SIGINT = 2;

    public static bool exit;
    public static HardwareType hardwareType { get; private set; } = HardwareType.Unknown;

    public static KeyboardDevice keyboardInput;
    public static InputMode inputMode = InputMode.ReignOS;
    private static bool hibernatePowerButton = false, disablePowerButton = false;

    public static bool? isRebootMode;
    private static string brightnessSettingsPath;

    private static SignalHandler signalHandler;
    private static void BindSignalEvents()
    {
        Console.CancelKeyPress += ExitEvent;
        signalHandler = new SignalHandler(SignalCloseEvent);
        signal(SIGINT, signalHandler);
    }
    
    static void Main(string[] args)
    {
        Log.Init("ReignOS.Service");
        Log.WriteLine("Service started: " + VersionInfo.version);
        
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        LibraryResolver.Init(Assembly.GetExecutingAssembly());
        BindSignalEvents();

        // process args
        foreach (string arg in args)
        {
            if (arg == "--input-inputplumber") inputMode = InputMode.InputPlumber;
            else if (arg == "--input-hhd") inputMode = InputMode.HHD;
            else if (arg == "--input-disable") inputMode = InputMode.Disabled;
        }
        
        // check if hibernation file exists
        hibernatePowerButton = File.Exists("/swapfile");

		// detect system hardware
		try
        {
            string vendorName = ProcessUtil.Run("dmidecode", "-s system-manufacturer").Trim();
            Log.WriteLine("Hardware Vendor: " + vendorName);
            if (vendorName.StartsWith("AYANEO") || vendorName.StartsWith("AYADEVICE")) hardwareType = HardwareType.Ayaneo;
            else if (vendorName.StartsWith("AOKZOE")) hardwareType = HardwareType.AOKZOE;

            string productName = ProcessUtil.Run("dmidecode", "-s system-product-name").Trim();
            Log.WriteLine("Hardware Product: " + productName);
            if (productName.StartsWith("Claw ")) hardwareType = HardwareType.MSI_Claw;
            else if (productName.StartsWith("ROG Ally")) hardwareType = HardwareType.RogAlly;
            else if (vendorName == "LENOVO" && productName == "83E1") hardwareType = HardwareType.Lenovo_LegionGo;
            else if (vendorName == "LENOVO" && productName == "83N1") hardwareType = HardwareType.Lenovo_LegionGo2;
            else if (vendorName == "LENOVO" && productName == "83L3") hardwareType = HardwareType.Lenovo_LegionGoS;
            else if (productName.StartsWith("AIR Pro")) hardwareType = HardwareType.AyaneoPro;
            else if (productName.StartsWith("AIR Plus")) hardwareType = HardwareType.AyaneoPlus;
            else if (productName.StartsWith("FLIP DS")) hardwareType = HardwareType.AyaneoFlipDS;
            else if (productName.StartsWith("FLIP 1S DS")) hardwareType = HardwareType.AyaneoFlipDS_1S;
            else if (productName.StartsWith("SLIDE")) hardwareType = HardwareType.AyaneoSlide;
            else if (productName.StartsWith("NEXT Lite")) hardwareType = HardwareType.AyaneoNextLite;
            else if (productName.StartsWith("AYA NEO FOUNDER")) hardwareType = HardwareType.Ayaneo1;
            else if (productName.StartsWith("AYANEO 2")) hardwareType = HardwareType.Ayaneo2;
            else if (productName.StartsWith("AYANEO 3")) hardwareType = HardwareType.Ayaneo3;
			else if (vendorName == "AYANEO" && productName == "KUN") hardwareType = HardwareType.AyaneoKun;
			else if (productName.StartsWith("ONE XPLAYER")) hardwareType = HardwareType.OneXPlayer_Gen1;
            else if (productName.StartsWith("ONEXPLAYER")) hardwareType = HardwareType.OneXPlayer_Gen2;
            else if (vendorName == "GPD" && productName == "G1618-03") hardwareType = HardwareType.GPD_Win3;
            else if (vendorName == "GPD" && productName == "G1618-04") hardwareType = HardwareType.GPD_Win4;
            else if (vendorName == "GPD" && productName == "G1618-05") hardwareType = HardwareType.GPD_Win5;
            else if (productName.StartsWith("ZOTAC GAMING ZONE")) hardwareType = HardwareType.ZotacZone;
            else if (productName.StartsWith("Loki Zero")) hardwareType = HardwareType.LokiZero;
            else if (productName.StartsWith("Win600")) hardwareType = HardwareType.Win600;
            else if (productName == "G1") hardwareType = HardwareType.TJD;
        }
        catch (Exception e)
        {
            Log.WriteLine("Failed to get system hardware");
            Log.WriteLine(e);
        }
        Log.WriteLine("Known hardware detection: " + hardwareType.ToString());

        // install SteamOS3 scripts
        string processPath = Path.GetDirectoryName(Environment.ProcessPath);
        string srcPath = Path.Combine(processPath, "SteamOS3/steamos-polkit-helpers/");
        string dstPath = "/usr/bin/steamos-polkit-helpers/";
        FileUtils.InstallScript(Path.Combine(srcPath, "jupiter-biosupdate"), Path.Combine(dstPath, "jupiter-biosupdate"));
        FileUtils.InstallScript(Path.Combine(srcPath, "steamos-select-branch"), Path.Combine(dstPath, "steamos-select-branch"));
        FileUtils.InstallScript(Path.Combine(srcPath, "steamos-update"), Path.Combine(dstPath, "steamos-update"));

        srcPath = Path.Combine(processPath, "SteamOS3/");
        dstPath = "/usr/bin/";
        FileUtils.InstallScript(Path.Combine(srcPath, "jupiter-biosupdate"), Path.Combine(dstPath, "jupiter-biosupdate"));
        FileUtils.InstallScript(Path.Combine(srcPath, "steam-http-loader"), Path.Combine(dstPath, "steam-http-loader"));
        FileUtils.InstallScript(Path.Combine(srcPath, "steamos-select-branch"), Path.Combine(dstPath, "steamos-select-branch"));
        FileUtils.InstallScript(Path.Combine(srcPath, "steamos-session-select"), Path.Combine(dstPath, "steamos-session-select"));
        FileUtils.InstallScript(Path.Combine(srcPath, "steamos-update"), Path.Combine(dstPath, "steamos-update"));
        FileUtils.InstallScript(Path.Combine(srcPath, "steamos-update-user"), Path.Combine(dstPath, "steamos-update-user"));

        srcPath = Path.Combine(processPath, "SteamOS3/polkit-1/");
        dstPath = "/usr/share/polkit-1/actions";
        FileUtils.InstallScript(Path.Combine(srcPath, "org.reignos.update.policy"), Path.Combine(dstPath, "org.reignos.update.policy"));

        // install extra gamescope display configs
        dstPath = "/usr/share/gamescope/scripts/00-gamescope/displays/";
        srcPath = Path.Combine(processPath, "Gamescope");// custom
        FileUtils.SafeCopy(Path.Combine(srcPath, "YHB-YHB02P25.lua"), Path.Combine(dstPath, "YHB-YHB02P25.lua"));
        FileUtils.SafeCopy(Path.Combine(srcPath, "ZDZ-ZDZ0501.lua"), Path.Combine(dstPath, "ZDZ-ZDZ0501.lua"));
        FileUtils.SafeCopy(Path.Combine(srcPath, "AYA-AYAOLED_FHD.lua"), Path.Combine(dstPath, "AYA-AYAOLED_FHD.lua"));
		FileUtils.SafeCopy(Path.Combine(srcPath, "SDC-AMS881KB01-0.lua"), Path.Combine(dstPath, "SDC-AMS881KB01-0.lua"));

		// install missing firmware
		if (AudioPatches.InstallFirmware_aw87559())
        {
            ProcessUtil.Run("reboot", "-f", useBash: false);
            return;
        }

        if (AudioPatches.InstallFirmware_Bazzite())
        {
            ProcessUtil.Run("reboot", "-f", useBash: false);
            return;
        }

        // configure power button for sleep
        dstPath = "/etc/systemd/logind.conf.d/";
        if (!Directory.Exists(dstPath)) Directory.CreateDirectory(dstPath);
        dstPath = Path.Combine(dstPath, "reignos.conf");
        var builder = new StringBuilder();
        builder.AppendLine("[Login]");
        builder.AppendLine("HandlePowerKey=ignore");
        File.WriteAllText(dstPath, builder.ToString());
        ProcessUtil.Run("systemctl", "restart systemd-logind");
        
        // force sleep events to hibernate events
        dstPath = "/etc/systemd/system/systemd-suspend.service.d";
        if (!Directory.Exists(dstPath)) Directory.CreateDirectory(dstPath);
        dstPath = Path.Combine(dstPath, "reignos.conf");
        if (hibernatePowerButton && !File.Exists(dstPath))// old suspend systemd settings
        {
            builder = new StringBuilder();
            builder.AppendLine("[Service]");
            builder.AppendLine("# ignore event");
            builder.AppendLine("ExecStart=");
            builder.AppendLine("# hibernate instead");
            builder.AppendLine("ExecStart=/usr/lib/systemd/systemd-sleep hibernate");
            File.WriteAllText(dstPath, builder.ToString());
            ProcessUtil.Run("systemctl", "daemon-reload");
        }
        else if (File.Exists(dstPath))
        {
            File.Delete(dstPath);
            ProcessUtil.Run("systemctl", "daemon-reload");
        }

		// save display brightness settings before power off
		srcPath = Path.Combine(processPath, "SystemD/");
		ProcessUtil.Run("chmod", $"+x {Path.Combine(srcPath, "reignos-save-brightness.sh")}", out _, wait:true, asAdmin:false);

		brightnessSettingsPath = "/home/gamer/ReignOS_Ext/DisplayBrightness/";
		if (!Directory.Exists(brightnessSettingsPath)) Directory.CreateDirectory(brightnessSettingsPath);

		dstPath = "/etc/systemd/system/";
		if (!Directory.Exists(dstPath)) Directory.CreateDirectory(dstPath);
		dstPath = Path.Combine(dstPath, "reignos-save-brightness.service");
        if (!File.Exists(dstPath))// configure service
        {
			builder = new StringBuilder();
		    builder.AppendLine("[Unit]");
		    builder.AppendLine("Description=Save backlight brightness");
		    builder.AppendLine("DefaultDependencies=no");
		    builder.AppendLine("Before=shutdown.target reboot.target poweroff.target halt.target");
		    builder.AppendLine();
		    builder.AppendLine("[Service]");
		    builder.AppendLine("Type=oneshot");
		    builder.AppendLine($"ExecStart={Path.Combine(srcPath, "reignos-save-brightness.sh")}");
		    builder.AppendLine();
		    builder.AppendLine("[Install]");
		    builder.AppendLine("WantedBy=shutdown.target reboot.target poweroff.target halt.target");
		    File.WriteAllText(dstPath, builder.ToString());
		    ProcessUtil.Run("systemctl", "daemon-reload");
		    ProcessUtil.Run("systemctl", "enable reignos-save-brightness.service");
		    ProcessUtil.Run("systemctl", "start reignos-save-brightness.service");
        }
        else// restore brightness values
        {
            RestoreBrightness();
        }

        // init virtual gamepad
        if (inputMode == InputMode.ReignOS) VirtualGamepad.Init();

        // detect device & configure hardware
        try
        {
            MSI_Claw.Configure();
            RogAlly.Configure();
            Lenovo.Configure();
            Ayaneo.Configure();
            OneXPlayer.Configure();
            GPD.Configure();
            ZotacZone.Configure();
            AOKZOE.Configure();
            LokiZero.Configure();
            Win600.Configure();
            TJD.Configure();
        }
        catch (Exception e)
        {
            Log.WriteLine("Failed to get device hardware");
            Log.WriteLine(e);
        }

        if (isRebootMode == true)
        {
            ProcessUtil.Run("reboot", "-f", useBash:false);
            return;
        }

        // if no hardware has known keyboard find generic one
        if (keyboardInput == null)
        {
            keyboardInput = new KeyboardDevice();
            keyboardInput.Init(null, false, 0, 0);
        }

        // start Dbus monitor
        DbusMonitor.Init();

        // start bootloader command listener
        var bootloaderListenThread = new Thread(ReadBootloaderCommands);
        bootloaderListenThread.IsBackground = true;
        bootloaderListenThread.Start();
        
        // apply PowerProfiles
        PowerProfiles.Apply(false);

        // run events
        Log.WriteLine("Running events...");
        var time = DateTime.Now;
        float wakeFromSleepTimeSec = 0;
        while (!exit)
        {
            // update time
            var lastTime = time;
            time = DateTime.Now;
            var timeSpan = time - lastTime;

            // detect possible resume from sleep
            bool resumeFromSleep = false;
            if (timeSpan.TotalSeconds >= 3)
            {
                resumeFromSleep = true;
                wakeFromSleepTimeSec = 0;
                keyboardInput.ClearKeys();
                PowerProfiles.Apply(false);
                RestoreBrightness();
            }

            // update keyboard
            keyboardInput.ReadNextKeys(out var keys, 256);// wait 256ms sec to build input

            // update devices
            if (MSI_Claw.isEnabled) MSI_Claw.Update(ref time, resumeFromSleep, keys);
            else if (RogAlly.isEnabled) RogAlly.Update(keys);
            else if (Lenovo.isEnabled) Lenovo.Update(ref time, resumeFromSleep);
            else if (Ayaneo.isEnabled) Ayaneo.Update(keys);
            else if (OneXPlayer.isEnabled) OneXPlayer.Update(keys);
            else if (GPD.isEnabled) GPD.Update(keys);
            else if (ZotacZone.isEnabled) ZotacZone.Update(keys);
            else if (AOKZOE.isEnabled) AOKZOE.Update(keys);
            else if (LokiZero.isEnabled) LokiZero.Update(keys);
            else if (Win600.isEnabled) Win600.Update(keys);
            else if (TJD.isEnabled) TJD.Update(keys);

            // update volume (send signal to bootloader)
            if (KeyEvent.Pressed(keys, input.KEY_VOLUMEDOWN, includeHeld:true)) Console.WriteLine("SET_VOLUME_DOWN");
            else if (KeyEvent.Pressed(keys, input.KEY_VOLUMEUP, includeHeld:true)) Console.WriteLine("SET_VOLUME_UP");
            
            // handle rest state
            if (!disablePowerButton && !resumeFromSleep && wakeFromSleepTimeSec >= 10 && KeyEvent.Pressed(keys, input.KEY_POWER))
            {
                Log.WriteLine("PowerButton Pressed");
                if (hibernatePowerButton) ProcessUtil.Run("systemctl", "hibernate", useBash: false);
                else ProcessUtil.Run("systemctl", "suspend", useBash: false);
            }

            // handle special close steam events
            // TODO: invoke "steam -shutdown" if you hold Alt+F4 or Guide+B for more than 4 seconds

            // sleep thread
            const int sleepMS = 1;//1000 / 30;
            Thread.Sleep(sleepMS);
            wakeFromSleepTimeSec += sleepMS / 1000f;
        }
        
        // shutdown
        Log.WriteLine("Shutting down...");
        try
        {
            DbusMonitor.Shutdown();
            MSI_Claw.Dispose();
            Lenovo.Dispose();
            if (inputMode == InputMode.ReignOS) VirtualGamepad.Dispose();
            if (keyboardInput != null) keyboardInput.Dispose();
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
        Log.Close();
        Environment.ExitCode = isRebootMode == null ? 0 : (isRebootMode == true ? 15 : 16);
    }

    private static void ReadBootloaderCommands()
    {
        while (true)
        {
            string line = Console.ReadLine();
            if (line == "stop-inhibit")
            {
                disablePowerButton = true;
                DbusMonitor.Shutdown();
            }
            else if (line == "start-inhibit")
            {
                disablePowerButton = false;
                DbusMonitor.Init();
            }
            else if (line == "ayaneo-popout-module")
            {
                Ayaneo.MagicModule_PopOut();
            }
            else if (line == "ayaneo-poppedin-module")
            {
                Ayaneo.MagicModule_PoppedIn();
            }
        }
    }

    private static void SignalCloseEvent(int signal)
    {
        if (signal == SIGINT)
        {
            exit = true;
            Log.WriteLine("Exit event");
        }
    }

    private static void ExitEvent(object sender, ConsoleCancelEventArgs e)
    {
        Log.WriteLine("Console Exit event");
        e.Cancel = true;
        exit = true;
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e != null) Log.WriteLine($"Unhandled exception: {e.ExceptionObject}");
        else Log.WriteLine("Unhandled exception: Unknown");
    }

    public static void RunUserCmd(string cmd)
    {
        Console.WriteLine("ReignOS.Service.COMMAND: " + cmd);
    }
    
    private static void RestoreBrightness()
    {
        Log.WriteLine("Restoring brighness settings...");
        try
        {
            if (!Directory.Exists(brightnessSettingsPath))
            {
                Log.WriteLine("ERROR: Brightness settings path not found");
                return;
            }
            
            foreach (string settingsFile in Directory.GetFiles(brightnessSettingsPath))
            {
                // read brightness setting
                string name = Path.GetFileNameWithoutExtension(settingsFile);
                string brightnessValue = File.ReadAllText(settingsFile).Trim();
                if (!ulong.TryParse(brightnessValue, out _)) continue;

                // apply brightness setting
                foreach (string dir in Directory.GetDirectories("/sys/class/backlight"))
                {
                    string dirName = Path.GetFileName(dir);
                    if (dirName != name) continue;
                    try
                    {
                        Log.WriteLine($"Restoring display brightness for: '{name}' with value {brightnessValue}");
                        File.WriteAllText(Path.Combine(dir, "brightness"), brightnessValue);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
    }
}