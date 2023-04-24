using HarmonyLib;
using Rimatomics;

namespace SkyMind
{
    public class Building_LaunchPad_Patch
    {
        // Launch pads can be considered Manned if they have a SkyMind Core connection.
        [HarmonyPatch(typeof(Building_LaunchPad), "get_Manned")]
        public class get_Manned_Patch
        {
            [HarmonyPostfix]
            public static void Listener(Building_LaunchPad __instance, ref bool __result)
            {
                // Do nothing if it was legal already.
                if (__result)
                {
                    return;
                }

                // If this building has a SkyMind connection and there is a SkyMind Core, it is manned.
                if (__instance.GetComp<CompSkyMind>().connected && SMN_Utils.gameComp.GetSkyMindCloudCapacity() > 0)
                {
                    __result = true;
                }
            }
        }
    }
}
