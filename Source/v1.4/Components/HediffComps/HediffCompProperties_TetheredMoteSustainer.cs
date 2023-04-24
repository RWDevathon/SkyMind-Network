using Verse;

namespace SkyMind
{
    public class HediffCompProperties_TetheredMoteSustainer : HediffCompProperties
    {
        public HediffCompProperties_TetheredMoteSustainer()
        {
            compClass = typeof(HediffComp_TetheredMoteSustainer);
        }

        public ThingDef moteDef;

        public float scale = 1f;
    }
}
