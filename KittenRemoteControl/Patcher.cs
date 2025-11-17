using HarmonyLib;

namespace KittenRemoteControl
{
    [HarmonyPatch]
    internal static class Patcher
    {
        private static Harmony? _harmony = new Harmony("KittenRemoteControl");

        public static void Patch()
        {
            _harmony?.PatchAll();
        }

        public static void Unload()
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _harmony = null;
        }
    }
}