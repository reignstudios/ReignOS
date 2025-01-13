using HidSharp;
using Launcher.Core.Hardware;

namespace Launcher.Core
{
    public enum HardwareType
    {
        Unknown,
        MSI_Claw_A1M,
        MSI_Claw
    }

    public enum Compositor
    {
        Cage,
        Gamescope
    }

    public static class Bootloader
    {
        public static void Start(Compositor compositor)
        {
            // start compositor
            switch (compositor)
            {
                case Compositor.Cage: StartCompositor_Cage(); break;
                case Compositor.Gamescope: StartCompositor_Gamescope(); break;
                default: throw new NotImplementedException();
            }
        }

        private static void StartCompositor_Cage()
        {

        }

        private static void StartCompositor_Gamescope()
        {

        }
    }
}
