using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidSharp;

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
        private static object threadLock = new object();
        private static Thread? thread;
        private static ServiceState state;

        public static void Start()
        {
            string result = ProcessUtil.Run("dmidecode", "-s system-product-name");
            Console.WriteLine("Product: " + result);

            // detect MSI-Claw gamepad
            var device = DeviceList.Local.GetHidDeviceOrNull(0x0DB0, 0x1901);//MSI Claw
			if (device != null)
			{
				Console.WriteLine("MSI-Claw gamepad found");
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

        private static void BackgroundThread(object? arg)
        {
            lock (threadLock) if (state != ServiceState.ShuttingDown) state = ServiceState.Running;
            while (state == ServiceState.Running)
            {

            }

            EXIT: state = ServiceState.ShutDown;
        }


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
