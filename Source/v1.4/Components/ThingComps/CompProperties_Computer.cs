using Verse;

namespace SkyMind
{
    public class CompProperties_Computer : CompProperties
    {
        public CompProperties_Computer()
        {
            compClass = typeof(CompComputer);
        }

        public SMN_ServerType serverMode;
        public int passivePointGeneration = 0;
        public int percentageWorkBoost = 0;
        public int pointStorage = 0;
    }
}
