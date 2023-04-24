using Verse;
using HarmonyLib;
using RimWorld;
using Verse.AI;
using System.Collections.Generic;

namespace SkyMind
{
    // Surrogates that have mental states triggered on them will trigger it on their controller. Controllers will disconnect from their surrogates.
    public class MentalStateHandler_Patch
    {
        [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
        public class TryStartMentalState_Patch
        {
            [HarmonyPostfix]
            public static void Listener(ref bool __result, Pawn ___pawn, MentalStateDef stateDef, string reason, bool forceWake, bool causedByMood, Pawn otherPawn, bool transitionSilently, bool causedByDamage, bool causedByPsycast)
            {
                // No need to continue if the mental state was not started for some reason.
                if (!__result)
                {
                    return;
                }

                // Not all pawns are SkyMindLinkable. Skip this method if that is the case.
                CompSkyMindLink compSkyMindLink = ___pawn.GetComp<CompSkyMindLink>();
                if (compSkyMindLink == null)
                {
                    return;
                }

                // This will return true if a surrogate or a controller. No special behavior is necessary if it is false.
                if (!compSkyMindLink.HasSurrogate())
                {
                    return;
                }

                // Pawns that have a surrogate connection are either a controller or a surrogate themselves. Handle cases separately.
                if (SMN_Utils.IsSurrogate(___pawn))
                {
                    // If the controller is in the SkyMind Core, do nothing.
                    Pawn controller = compSkyMindLink.GetSurrogates().FirstOrFallback();

                    // Less than extreme mental states simply apply a mood debuff to their controller and reboots this particular surrogate.
                    if (!stateDef.IsExtreme)
                    {
                        controller.needs.mood?.thoughts?.memories?.TryGainMemoryFast(SMN_ThoughtDefOf.SMN_SurrogateMentalBreak);
                        ___pawn.health.AddHediff(SMN_HediffDefOf.SMN_FeedbackLoop);
                        Find.LetterStack.ReceiveLetter("SMN_SurrogateSufferedMentalState".Translate(), "SMN_SurrogateSufferedMentalStateDesc".Translate(), LetterDefOf.NegativeEvent);

                    }
                    // Extreme mental states are applied to the controller directly.
                    else
                    {
                        // If the controller is not a SkyMind Core intelligence, it will have the mental state applied directly.
                        if (!SMN_Utils.gameComp.GetCloudPawns().Contains(controller))
                        {
                            controller.mindState.mentalStateHandler.TryStartMentalState(stateDef, reason, forceWake, causedByMood, otherPawn, transitionSilently, causedByDamage, causedByPsycast);
                        }
                        // Surrogates of a SkyMind Core intelligence simply reboot upon suffering a mental break, regardless of extremity.
                        else
                        {
                            ___pawn.health.AddHediff(SMN_HediffDefOf.SMN_FeedbackLoop);
                        }
                    }
                }
                // Controllers reboot all surrogates and disconnect them when suffering a mental break.
                else
                {
                    IEnumerable<Pawn> surrogates = compSkyMindLink.GetSurrogates();
                    foreach (Pawn surrogate in surrogates)
                    {
                        surrogate.health.AddHediff(SMN_HediffDefOf.SMN_FeedbackLoop);
                    }
                    compSkyMindLink.DisconnectSurrogates();
                    Find.LetterStack.ReceiveLetter("SMN_ControllerSufferedMentalState".Translate(), "SMN_ControllerSufferedMentalStateDesc".Translate(), LetterDefOf.NegativeEvent);
                }
            }
        }
    }
}