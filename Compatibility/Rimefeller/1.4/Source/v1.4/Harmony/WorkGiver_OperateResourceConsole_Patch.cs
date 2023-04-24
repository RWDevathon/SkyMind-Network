using HarmonyLib;
using Rimefeller;
using Verse;

namespace SkyMind
{
    public class WorkGiver_OperateResourceConsole_Patch
    {
        // Don't bother giving a pawn a job on a resource console if it is being "manned" remotely via SkyMind Core.
        [HarmonyPatch(typeof(WorkGiver_OperateResourceConsole), "HasJobOnThing")]
        public class IsConsideredNonHumanlike_Patch
        {
            [HarmonyPostfix]
            public static void Listener(Pawn pawn, Thing t, bool forced, ref bool __result)
            {
                // Do nothing if it wasn't legal already.
                if (!__result)
                {
                    return;
                }

                // If the thing (guaranteed to be a console by the target method) is connected to a SkyMind Core, there is no need to work here.
                if (t.TryGetComp<CompSkyMind>()?.connected ?? false && SMN_Utils.gameComp.GetSkyMindCloudCapacity() > 0)
                {
                    __result = false;
                }
            }
        }
    }
}
