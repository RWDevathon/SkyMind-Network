using Verse;
using RimWorld;

namespace SkyMind
{
    [DefOf]
    public static class SMN_JobDefOf
    {
        static SMN_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SMN_JobDefOf));
        }

        public static JobDef SMN_GenerateInsight;
    }
}