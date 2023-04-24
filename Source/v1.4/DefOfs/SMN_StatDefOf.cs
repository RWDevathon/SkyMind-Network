using RimWorld;

namespace SkyMind
{
    [DefOf]
    public static class SMN_StatDefOf
    {
        static SMN_StatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SMN_StatDefOf));
        }

        public static StatDef SMN_SurrogateLimitBonus;
    }
}