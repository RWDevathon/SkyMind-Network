using HarmonyLib;
using Rimatomics;

namespace SkyMind
{
    public class Building_EnergyWeapon_Patch
    {
        // Weapons can be considered Manned if they have a SkyMind Core connection.
        [HarmonyPatch(typeof(Building_EnergyWeapon), "get_IsConsoleManned")]
        public class get_IsConsoleManned_Patch
        {
            [HarmonyPostfix]
            public static void Listener(Building_EnergyWeapon __instance, ref bool __result)
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
