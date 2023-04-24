using Verse;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SkyMind
{
    public class PawnGroupMakerUtility_Patch
    {
        // Handle generation of groups of pawns so that foreign factions may use surrogates. Random selected pawns of the group will be controlled surrogates.
        [HarmonyPatch(typeof(PawnGroupMakerUtility), "GeneratePawns")]
        public class GeneratePawns_Patch
        {
            [HarmonyPostfix]
            public static void Listener(PawnGroupMakerParms parms, bool warnOnZeroResults, ref IEnumerable<Pawn> __result)
            {
                List<Pawn> modifiedResults = __result.ToList();

                // If settings disable this faction from using surrogates (or all surrogates are banned entirely), then there is no work to do here. Allow default generation to proceed.
                if (!SkyMindNetwork_Settings.surrogatesAllowed || !SkyMindNetwork_Settings.otherFactionsAllowedSurrogates || parms.faction == null)
                {
                    return;
                }

                // Only factions with the faction extension set to true and which roll the chance to have surrogates should continue.
                SMN_FactionSkyMindExtension factionExtension = parms.faction.def.GetModExtension<SMN_FactionSkyMindExtension>();
                if (factionExtension == null || !factionExtension.canUseSurrogates || !Rand.Chance(factionExtension.percentChanceForGroupToHaveSurrogates))
                {
                    return;
                }

                try
                {
                    List<Pawn> surrogateCandidates = new List<Pawn>();

                    foreach (Pawn pawn in modifiedResults)
                    {
                        // Count all non-trader pawns with humanlike intelligence that are organics with the proper setting or androids with the proper setting. Don't take pawns that have relations or that are too weak.
                        if (pawn.def.race != null && pawn.def.race.Humanlike && pawn.trader == null && pawn.TraderKind == null && !pawn.relations.RelatedToAnyoneOrAnyoneRelatedToMe)
                        {
                            if (factionExtension.canUseSurrogates && SMN_Utils.MayEverBeSurrogate(pawn) && factionExtension.minStrengthForSurrogates <= pawn.kindDef.combatPower)
                            {
                                surrogateCandidates.Add(pawn);
                            }
                        }
                    }

                    // Skip groups that are too small
                    if (surrogateCandidates.Count < factionExtension.minLegalPawnsForSurrogates)
                    {
                        return;
                    }

                    // Determine how many surrogates are taking the place of candidates
                    int surCount = (int)(surrogateCandidates.Count * Rand.Range(factionExtension.percentOfGroupToBeSurrogatesMin, factionExtension.percentOfGroupToBeSurrogatesMax));

                    IEnumerable<Pawn> selectedPawns = surrogateCandidates.TakeRandom(surCount);

                    // Set the selected pawn to control itself, as foreign surrogates do not actually have separate pawns to control them.
                    foreach (Pawn selectedPawn in selectedPawns)
                    {
                        SMN_Utils.TurnIntoSurrogate(selectedPawn);

                        // Connect the chosenCandidate to the surrogate as the controller. It is an external controller.
                        selectedPawn.GetComp<CompSkyMindLink>().ConnectSurrogate(selectedPawn, true);
                    }
                    __result = modifiedResults;
                }
                catch (Exception ex)
                {
                    Log.Warning("[SMN] Failed to apply surrogates when creating a Pawn Group. Unknown consequences may occur." + ex.Message + " " + ex.StackTrace);
                }
            }
        }
    }
}