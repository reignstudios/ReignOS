namespace Launcher.Core
{
    public enum Compositor
    {
        Cage,
        Gamescope
    }

    public static class Bootloader
    {
        public static void Start(Compositor compositor)
        {
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
