using RimWorld;

namespace SkyMind
{
    [DefOf]
    public static class SMN_ThoughtDefOf
    {
        static SMN_ThoughtDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SMN_ThoughtDefOf));
        }

        public static ThoughtDef SMN_ConnectedSkyMindAttacked;

        public static ThoughtDef SMN_AttackedViaSkyMind;

        public static ThoughtDef SMN_TrolledViaSkyMind;

        public static ThoughtDef SMN_SurrogateMentalBreak;

    }
}
