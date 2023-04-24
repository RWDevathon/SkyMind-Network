using RimWorld;

namespace SkyMind
{
    [DefOf]
    public static class SMN_IncidentDefOf
    {
        static SMN_IncidentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SMN_IncidentDefOf));
        }
        public static IncidentDef SMN_HackingIncident;

        public static IncidentDef ResourcePodCrash;

        public static IncidentDef RefugeePodCrash;

        public static IncidentDef PsychicEmanatorShipPartCrash;

        public static IncidentDef DefoliatorShipPartCrash;
    }
}
