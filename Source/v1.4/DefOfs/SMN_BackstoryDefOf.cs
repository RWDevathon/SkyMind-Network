using RimWorld;

namespace SkyMind
{
    [DefOf]
    public static class SMN_BackstoryDefOf
    {
        static SMN_BackstoryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SMN_BackstoryDefOf));
        }

        public static BackstoryDef SMN_BlankChildhood;
        public static BackstoryDef SMN_BlankAdulthood;
    }
}