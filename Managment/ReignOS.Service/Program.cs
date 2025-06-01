namespace ReignOS.Service;
using ReignOS.Core;
using ReignOS.Service.Hardware;
using ReignOS.Service.OS;

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

    // Ayaneo
    Ayaneo,

    // OneXPlayer
    OneXPlayer,

    // Zotac
    ZotacZone,

    // AYN
    LokiZero,

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
    public static HardwareType hardwareType { get; private set; }

    public static KeyboardInput keyboardInput;
    public static bool useInputPlumber;

    private static void BindSignalEvents()
    {
        Console.CancelKeyPress += ExitEvent;
        var signalHandler = new SignalHandler(SignalCloseEvent);
        signal(SIGINT, SignalCloseEvent);
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
            if (arg == "--input-inputplumber") useInputPlumber = true;
        }

        // detect system hardware
        try
        {
            string productName = ProcessUtil.Run("dmidecode", "-s system-product-name", out _).Trim();
            Log.WriteLine("Product: " + productName);
            if (productName.StartsWith("Claw ")) hardwareType = HardwareType.MSI_Claw;
            else if (productName.StartsWith("AIR Pro")) hardwareType = HardwareType.Ayaneo;
            else if (productName.StartsWith("ONEXPLAYER")) hardwareType = HardwareType.OneXPlayer;
            else if (productName.StartsWith("ZOTAC GAMING ZONE")) hardwareType = HardwareType.ZotacZone;
            else if (productName.StartsWith("Loki Zero")) hardwareType = HardwareType.LokiZero;
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

        srcPath = Path.Combine(processPath, "SteamOS3/polkit-1/");
        dstPath = "/usr/share/polkit-1/actions";
        FileUtils.InstallScript(Path.Combine(srcPath, "org.reignos.update.policy"), Path.Combine(dstPath, "org.reignos.update.policy"));

        // install extra gamescope display configs
        srcPath = Path.Combine(processPath, "Gamescope");
        dstPath = "/usr/share/gamescope/scripts/00-gamescope/displays/";
        FileUtils.SafeCopy(Path.Combine(srcPath, "YHB-YHB02P25.lua"), Path.Combine(dstPath, "YHB-YHB02P25.lua"));
        FileUtils.SafeCopy(Path.Combine(srcPath, "ZDZ-ZDZ0501.lua"), Path.Combine(dstPath, "ZDZ-ZDZ0501.lua"));

        // configure pwr button for sleep
        dstPath = "/etc/systemd/logind.conf.d/";
        if (!Directory.Exists(dstPath)) Directory.CreateDirectory(dstPath);
        dstPath = Path.Combine(dstPath, "reignos.conf");
        if (!File.Exists(dstPath))
        {
            var builder = new StringBuilder();
            builder.AppendLine("[Login]");
            builder.AppendLine("HandlePowerKey=suspend");
            File.WriteAllText(dstPath, builder.ToString());
            ProcessUtil.Run("systemctl", "restart systemd-logind");
        }

        // init virtual gamepad
        if (!useInputPlumber) VirtualGamepad.Init();

        // detect device & configure hardware
        try
        {
            MSI_Claw.Configure();
            OneXPlayer.Configure();
            ZotacZone.Configure();
            LokiZero.Configure();
            TJD.Configure();
            Ayaneo.Configure();
        }
        catch (Exception e)
        {
            Log.WriteLine("Failed to get device hardware");
            Log.WriteLine(e);
        }

        // if no hardware has known keyboard find generic one
        if (keyboardInput == null)
        {
            keyboardInput = new KeyboardInput();
            keyboardInput.Init(null, false, 0, 0);
        }

        // start Dbus monitor
        DbusMonitor.Init();

        // start bootloader command listener
        var bootloaderListenThread = new Thread(ReadBootloaderCommands);
        bootloaderListenThread.IsBackground = true;
        bootloaderListenThread.Start();

        // run events
        Log.WriteLine("Running events...");
        var time = DateTime.Now;
        while (!exit)
        {
            // update time
            var lastTime = time;
            time = DateTime.Now;
            var timeSpan = time - lastTime;

            // detect possible resume from sleep
            bool resumeFromSleep = false;
            if (timeSpan.TotalSeconds >= 3) resumeFromSleep = true;

            // update keyboard
            keyboardInput.ReadNextKeys(out var keys);

            // update devices
            if (MSI_Claw.isEnabled) MSI_Claw.Update(ref time, resumeFromSleep, keys);
            if (Ayaneo.isEnabled) Ayaneo.Update(keys);
            if (OneXPlayer.isEnabled) OneXPlayer.Update(keys);
            if (ZotacZone.isEnabled) ZotacZone.Update(keys);
            if (LokiZero.isEnabled) LokiZero.Update(keys);
            if (TJD.isEnabled) TJD.Update(keys);

            // update volume
            if (KeyEvent.Pressed(keys))
            {
                // send signal to bootloader
                if (KeyEvent.Pressed(keys, input.KEY_VOLUMEDOWN)) Console.WriteLine("SET_VOLUME_DOWN");
                else if (KeyEvent.Pressed(keys, input.KEY_VOLUMEUP)) Console.WriteLine("SET_VOLUME_UP");
            }

            // handle special close steam events
            // TODO: invoke "steam -shutdown" if you hold Alt+F4 or Guide+B for more than 4 seconds

            // sleep thread
            Thread.Sleep(1000 / 15);
        }
        
        // shutdown
        Log.WriteLine("Shutting down...");
        DbusMonitor.Shutdown();
        MSI_Claw.Dispose();
        if (!useInputPlumber) VirtualGamepad.Dispose();
        keyboardInput.Dispose();
        Environment.ExitCode = DbusMonitor.isRebootMode == null ? 0 : (DbusMonitor.isRebootMode == true ? 15 : 16);
    }

    private static void ReadBootloaderCommands()
    {
        while (true)
        {
            string line = Console.ReadLine();
            if (line == "stop-inhibit") DbusMonitor.Shutdown();
            else if (line == "start-inhibit") DbusMonitor.Init();
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
        if (e != null) Log.WriteLine($"Unhandled exception: {e}");
        else Log.WriteLine("Unhandled exception: Unknown");
    }

    public static void RunUserCmd(string cmd)
    {
        Console.WriteLine("ReignOS.Service.COMMAND: " + cmd);
    }
}