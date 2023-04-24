using Verse;
using HarmonyLib;
using RimWorld;

namespace SkyMind
{
    public class Pawn_HealthTracker_Patch
    {
        // Upon a pawn being downed, some pawns should die automatically, and enemy surrogates brains explode.
        [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
        public static class MakeDowned_Patch
        {
            [HarmonyPrefix]
            public static bool Listener(Pawn_HealthTracker __instance, DamageInfo? dinfo, Hediff hediff, ref Pawn ___pawn)
            {
                if (SMN_Utils.IsSurrogate(___pawn))
                {
                    // Surrogates from other factions are fail-deadly, and will self-immolate to prevent capture.
                    if (___pawn.Faction != null && ___pawn.Faction != Faction.OfPlayer)
                    {
                        ___pawn.TakeDamage(new DamageInfo(DamageDefOf.Bomb, 99999f, 999f, -1f, null, ___pawn.health.hediffSet.GetBrain()));
                        return false;
                    }
                }
                return true;
            }
        }

        // Handle dead pawns in terms of the SkyMind network and surrogates.
        [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDead")]
        public static class ShouldBeDead_Patch
        {
            [HarmonyPostfix]
            public static void Listener(ref bool __result, Pawn ___pawn)
            {
                if (!__result || !___pawn.RaceProps.Humanlike)
                    return;

                // Dead pawns always try to disconnect from the network. This only actually affects player pawns, as they are the only things actually in the network.
                SMN_Utils.gameComp.DisconnectFromSkyMind(___pawn);

                // Non-player surrogates must disconnect directly.
                if (SMN_Utils.IsSurrogate(___pawn) && ___pawn.Faction != Faction.OfPlayer)
                    ___pawn.GetComp<CompSkyMindLink>().DisconnectController();
            }
        }
    }
}