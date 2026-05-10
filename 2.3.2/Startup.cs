using UnityModManagerNet;

    public static class Startup
    {
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            MainClass.Setup(modEntry);

            return true;
        }
    }
