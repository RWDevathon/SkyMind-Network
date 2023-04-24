using Verse;
using RimWorld;


namespace SkyMind
{
    [DefOf]
    public static class SMN_ThingDefOf
    {
        static SMN_ThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SMN_ThingDefOf));
        }

        public static ThingDef SMN_MindOperationAttachedMote;
    }

}