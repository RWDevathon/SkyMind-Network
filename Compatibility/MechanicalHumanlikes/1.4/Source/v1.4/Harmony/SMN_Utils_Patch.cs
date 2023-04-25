using System;
using HarmonyLib;
using MechHumanlikes;
using Verse;

namespace SkyMind
{
    public class SMN_Utils_Patch
    {
        // Drones may not be surrogates.
        [HarmonyPatch(typeof(SMN_Utils), "MayEverBeSurrogate")]
        [HarmonyPatch(new Type[] { typeof(ThingDef) }, new ArgumentType[] { ArgumentType.Normal })]
        public class MayEverBeSurrogate_Patch
        {
            [HarmonyPostfix]
            public static void Listener(ThingDef thingDef, ref bool __result)
            {
                __result = __result && !MHC_Utils.IsConsideredMechanicalDrone(thingDef);
            }
        }
    }
}
