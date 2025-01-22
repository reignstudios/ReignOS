namespace ReignOS.Service;
using ReignOS.Core;
using ReignOS.Service.Hardware;
using ReignOS.Service.OS;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

enum HardwareType
{
    Unknown,
    MSI_Claw_A1M,
    MSI_Claw
}

internal class Program
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SignalHandler(int signal);

    [DllImport("libc.so", SetLastError = true)]
    private static extern int signal(int sig, SignalHandler handler);
    private const int SIGINT = 2;

    private static bool exit;
    public static HardwareType hardwareType { get; private set; }

    public static KeyboardInput keyboardInput;

    private static void BindSignalEvents()
    {
        Console.CancelKeyPress += ExitEvent;
        var signalHandler = new SignalHandler(SignalCloseEvent);
        signal(SIGINT, SignalCloseEvent);
    }
    
    static void Main(string[] args)
    {
        Log.prefix = "ReignOS.Service: ";
        Log.WriteLine("Service started: " + VersionInfo.version);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        LibraryResolver.Init(Assembly.GetExecutingAssembly());
        BindSignalEvents();

        // install Systemd services
        string processPath = Path.GetDirectoryName(Environment.ProcessPath);
        string srcPath = Path.Combine(processPath, "Systemd");
        string dstPath = "/etc/systemd/system/";
        InstallService(Path.Combine(srcPath, "reignos-shutdown.service"), Path.Combine(dstPath, "reignos-shutdown.service"));
        ProcessUtil.Run("systemctl", "daemon-reload", out _, wait:true);// reload installed services
        ProcessUtil.Run("systemctl", "enable reignos-shutdown.service", out _, wait:true);

        // install SteamOS3 scripts
        srcPath = Path.Combine(processPath, "SteamOS3/steamos-polkit-helpers/");
        dstPath = "/usr/bin/steamos-polkit-helpers/";
        InstallScript(Path.Combine(srcPath, "jupiter-biosupdate"), Path.Combine(dstPath, "jupiter-biosupdate"));
        InstallScript(Path.Combine(srcPath, "steamos-select-branch"), Path.Combine(dstPath, "steamos-select-branch"));
        InstallScript(Path.Combine(srcPath, "steamos-update"), Path.Combine(dstPath, "steamos-update"));

        srcPath = Path.Combine(processPath, "SteamOS3/");
        dstPath = "/usr/bin/";
        InstallScript(Path.Combine(srcPath, "jupiter-biosupdate"), Path.Combine(dstPath, "jupiter-biosupdate"));
        InstallScript(Path.Combine(srcPath, "steam-http-loader"), Path.Combine(dstPath, "steam-http-loader"));
        InstallScript(Path.Combine(srcPath, "steamos-select-branch"), Path.Combine(dstPath, "steamos-select-branch"));
        InstallScript(Path.Combine(srcPath, "steamos-session-select"), Path.Combine(dstPath, "steamos-session-select"));
        InstallScript(Path.Combine(srcPath, "steamos-update"), Path.Combine(dstPath, "steamos-update"));

        // init virtual gamepad
        VirtualGamepad.Init();
        
        // detect system hardware
        try
        {
            string productName = ProcessUtil.Run("dmidecode", "-s system-product-name", out _);
            Log.WriteLine("Product: " + productName.TrimEnd());
            if (productName == "Claw A1M") hardwareType = HardwareType.MSI_Claw_A1M;
            else if (productName.StartsWith("Claw ")) hardwareType = HardwareType.MSI_Claw;
        }
        catch (Exception e)
        {
            Log.WriteLine("Failed to get system hardware");
            Log.WriteLine(e);
        }
        Log.WriteLine("Known hardware detection: " + hardwareType.ToString());

        // detect device & configure hardware
        try
        {
            MSI_Claw.Configure();
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
            keyboardInput.Init(null, false, 0, 0);// TODO: update to just load default keyboard
        }

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
            keyboardInput.ReadNextKey(out ushort key, out bool keyPressed);

            // update devices
            if (MSI_Claw.isEnabled) MSI_Claw.Update(resumeFromSleep, key, keyPressed);

            // update volume
            if (keyPressed)
            {
                // send signal to bootloader
                if (key == input.KEY_VOLUMEDOWN) Console.WriteLine("SET_VOLUME_DOWN");
                else if (key == input.KEY_VOLUMEUP) Console.WriteLine("SET_VOLUME_UP");
            }

            // handle special close steam events
            // TODO: invoke "steam -shutdown" if you hold Alt+F4 or Guide+B for more than 4 seconds

            // sleep thread
            Thread.Sleep(1000 / 30);
        }
        
        // shutdown
        Log.WriteLine("Shutting down...");
        MSI_Claw.Dispose();
        VirtualGamepad.Dispose();
        keyboardInput.Dispose();
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

    private static void InstallService(string srcPath, string dstPath)
    {
        try
        {
            File.Copy(srcPath, dstPath, true);
        }
        catch (Exception e)
        {
            Log.WriteLine(e.Message);
        }
    }

    private static void InstallScript(string srcPath, string dstPath)
    {
        try
        {
            File.Copy(srcPath, dstPath, true);
        }
        catch (Exception e)
        {
            Log.WriteLine(e.Message);
        }

        ProcessUtil.Run("chmod", $"+x \"{dstPath}\"", out _, wait:true);
    }
}