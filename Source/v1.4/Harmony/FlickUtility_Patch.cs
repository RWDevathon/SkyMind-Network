using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace SkyMind
{
    public static class FlickUtility_Patch
    {
        // Ensure the SkyMind auto-flicks things when requested and connected.
        [HarmonyPatch(typeof(FlickUtility), "UpdateFlickDesignation")]
        public class UpdateFlickDesignation_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Thing t)
            {
                if (t.TryGetComp<CompSkyMind>()?.connected == true && (SMN_Utils.gameComp.HasSkyMindCore() || SMN_Utils.gameComp.HasNetworkedPawn()))
                {
                    CompFlickable compFlick = t.TryGetComp<CompFlickable>();
                    if (compFlick != null)
                    {
                        string txt;
                        if (compFlick.SwitchIsOn)
                        {
                            txt = "SMN_FlickDisable".Translate();
                        }
                        else
                        {
                            txt = "SMN_FlickEnable".Translate();
                        }

                        MoteMaker.ThrowText(t.TrueCenter() + new Vector3(0.5f, 0f, 0.5f), t.Map, txt, Color.white, -1f);

                        compFlick.DoFlick();
                    }
                    return false;
                }
                return true;
            }
        }
    }
}