using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidSharp;
using Launcher.Core.Hardware;

namespace Launcher.Core
{
    enum ServiceState
    {
        None,
        Starting,
        Running,
        ShuttingDown,
        ShutDown
    }

    public static class Service
    {
        public static HardwareType hardwareType { get; private set; }

        /*private static object threadLock = new object();
        private static Thread? thread;
        private static ServiceState state;*/

        public static void Start()
        {
            // detect system hardware
            try
            {
                string productName = ProcessUtil.Run("dmidecode", "-s system-product-name");
                Console.WriteLine("Product: " + productName);
                if (productName == "Claw A1M") hardwareType = HardwareType.MSI_Claw_A1M;
                else if (productName.StartsWith("Claw ")) hardwareType = HardwareType.MSI_Claw;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get system hardware");
                Console.Write(e);
            }
            Console.WriteLine("Known hardware detection: " + hardwareType.ToString());

            // detect device & configure hardware
            try
            {
                foreach (var device in DeviceList.Local.GetHidDevices())
                {
                    MSI_Claw.Configure(device);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get device hardware");
                Console.Write(e);
            }

            /*state = ServiceState.Starting;
            thread = new Thread(BackgroundThread);
            thread.IsBackground = true;
            thread.Start();*/
        }

        public static void Shutdown()
        {
            //lock (threadLock) state = ServiceState.ShuttingDown;
        }

        /*private static void BackgroundThread(object? arg)
        {
            lock (threadLock) if (state != ServiceState.ShuttingDown) state = ServiceState.Running;
            while (state == ServiceState.Running)
            {

            }

            EXIT: state = ServiceState.ShutDown;
        }*/


        /*private static void OnResume()
        {
            DateTime lastCheck = DateTime.Now;

            while (true)
            {
                // Check uptime or last wake time. This is a simplified example:
                var uptime = TimeSpan.FromSeconds(long.Parse(File.ReadAllText("/proc/uptime").Split(' ')[0]));

                if (DateTime.Now - lastCheck > uptime && DateTime.Now - lastCheck > TimeSpan.FromSeconds(60)) // Arbitrary threshold
                {
                    Console.WriteLine("System has likely resumed from sleep.");
                    lastCheck = DateTime.Now;
                }

                System.Threading.Thread.Sleep(60000); // Check every minute
            }
        }*/
    }
}
