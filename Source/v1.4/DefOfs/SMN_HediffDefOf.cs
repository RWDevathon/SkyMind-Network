using Verse;
using RimWorld;

namespace SkyMind
{
    [DefOf]
    public static class SMN_HediffDefOf
    {
        static SMN_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SMN_HediffDefOf));
        }

        // SkyMind related Hediffs

        public static HediffDef SMN_DDOSRecovery;

        public static HediffDef SMN_MemoryCorruption;

        public static HediffDef SMN_SplitConsciousness;

        public static HediffDef SMN_ForeignConsciousness;

        public static HediffDef SMN_MindOperation;

        public static HediffDef SMN_FeedbackLoop;

        public static HediffDef SMN_Unconscious;

        public static HediffDef SMN_NoController;

        public static HediffDef SMN_SkyMindReceiver;

    }
}
