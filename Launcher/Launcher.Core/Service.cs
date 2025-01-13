using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
