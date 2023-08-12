using System.Collections.Generic;
using Verse;
using RimWorld;

namespace SkyMind
{
    public class Recipe_InstallCranialSkyMindInterface : Recipe_InstallImplant
    {
        // If the pawn has any implant that allows SkyMind connection already, then we can not install another one. SkyMind implants are mutually exclusive.
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (thing is Pawn pawn)
            {
                HediffSet hediffSet = pawn.health.hediffSet;
                for (int i = hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    if (hediffSet.hediffs[i].def.GetModExtension<SMN_HediffSkyMindExtension>()?.allowsConnection == true)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }


        // This recipe is specifically targetting organic brains, so we only need to check if the brain is available (a slight optimization over checking fixed body parts).
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            HediffSet hediffSet = pawn.health.hediffSet;
            BodyPartRecord targetBodyPart = hediffSet.GetBrain();
            if (targetBodyPart != null)
            {
                yield return targetBodyPart;
            }
            yield break;
        }

        // Install the part as normal, and then handle which type of chip was installed if it was successful (which can be measured by seeing if it actually got the hediff or not).
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null)
            {
                if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
            }

            if (recipe.addsHediff.GetModExtension<SMN_HediffSkyMindExtension>()?.isReceiver == true)
            {
                Hediff receiverHediff = HediffMaker.MakeHediff(recipe.addsHediff, pawn, part);
                SMN_Utils.TurnIntoSurrogate(pawn, receiverHediff, part, true);
                pawn.health.AddHediff(SMN_HediffDefOf.SMN_NoController);
            }
            else
            {
                pawn.health.AddHediff(recipe.addsHediff, part);
            }
        }
    }
}

