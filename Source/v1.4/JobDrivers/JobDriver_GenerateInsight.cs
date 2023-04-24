using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;

namespace SkyMind
{
    public class JobDriver_GenerateInsight : JobDriver
    {
        private const int JobEndInterval = 4000;

        private Building_ResearchBench ResearchBench => (Building_ResearchBench)TargetThingA;
        private CompInsightBench CompInsightBench => ResearchBench.GetComp<CompInsightBench>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(ResearchBench, job, 1, -1, null, errorOnFailed))
            {
                if (ResearchBench.def.hasInteractionCell)
                {
                    return pawn.ReserveSittableOrSpot(ResearchBench.InteractionCell, job, errorOnFailed);
                }
                return true;
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil generateInsight = ToilMaker.MakeToil("MakeNewToils");
            generateInsight.tickAction = delegate
            {
                Pawn actor = generateInsight.actor;
                float pointsGenerated = 0.008f;
                pointsGenerated *= actor.GetStatValue(StatDefOf.ResearchSpeed);
                pointsGenerated *= TargetThingA.GetStatValue(StatDefOf.ResearchSpeedFactor);
                SMN_Utils.gameComp.ChangeServerPoints(pointsGenerated, CompInsightBench.SMN_ServerType);
                actor.skills.Learn(SkillDefOf.Intellectual, 0.1f);
                actor.GainComfortFromCellIfPossible(chairsOnly: true);
            };
            generateInsight.FailOn(() => ResearchBench.GetComp<CompSkyMind>()?.connected != true);
            generateInsight.FailOn(() => CompInsightBench == null);
            generateInsight.FailOn(() => CompInsightBench.SMN_ServerType == SMN_ServerType.None);
            generateInsight.FailOn(() => SMN_Utils.gameComp.GetPointCapacity(CompInsightBench.SMN_ServerType) <= SMN_Utils.gameComp.GetPoints(CompInsightBench.SMN_ServerType));
            generateInsight.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            generateInsight.WithEffect(EffecterDefOf.Research, TargetIndex.A);
            generateInsight.WithProgressBar(TargetIndex.A, () => SMN_Utils.gameComp.GetPoints(CompInsightBench.SMN_ServerType) / SMN_Utils.gameComp.GetPointCapacity(CompInsightBench.SMN_ServerType));
            generateInsight.defaultCompleteMode = ToilCompleteMode.Delay;
            generateInsight.defaultDuration = JobEndInterval;
            generateInsight.activeSkill = () => SkillDefOf.Intellectual;
            yield return generateInsight;
            yield return Toils_General.Wait(2);
        }
    }
}