using Launcher.Core;

namespace Launcher
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Launcher started");

            // start device service
            Service.Start();

            // start bootloader
            var compositor = Compositor.Cage;
            foreach (string arg in args)
            {
                if (arg == "--cage")
                {
                    compositor = Compositor.Cage;
                }
                else if (arg == "--gamescope")
                {
                    compositor = Compositor.Gamescope;
                }
            }

            Bootloader.Start(compositor);

            // shutdown
            Service.Shutdown();
        }
    }
}
