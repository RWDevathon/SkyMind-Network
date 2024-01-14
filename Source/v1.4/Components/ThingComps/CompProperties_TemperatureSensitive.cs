using System.Collections.Generic;
using Verse;


namespace SkyMind
{
    public class CompProperties_TemperatureSensitive : CompProperties
    {
        public CompProperties_TemperatureSensitive()
        {
            compClass = typeof(CompTemperatureSensitive);
        }

        public float coldDangerLimit = 0;
        public float coldWarningLimit = 4;
        public float coldSafeLimit = 12;
        public float heatSafeLimit = 32;
        public float heatWarningLimit = 40;
        public float heatDangerLimit = 44;

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            if (coldDangerLimit > coldWarningLimit)
            {
                yield return "Temperature sensitive device " + parentDef.label + " has a higher cold danger limit than its warning limit!";
            }
            if (coldWarningLimit > coldSafeLimit)
            {
                yield return "Temperature sensitive device " + parentDef.label + " has a higher cold warning limit than its safe limit!";
            }
            if (coldSafeLimit > heatSafeLimit)
            {
                yield return "Temperature sensitive device " + parentDef.label + " has its safe heat threshold lower than its safe cold threshold!";
            }

            if (heatSafeLimit > heatWarningLimit)
            {
                yield return "Temperature sensitive device " + parentDef.label + " has a higher heat safe limit than its warning limit!";
            }
            if (heatWarningLimit > heatDangerLimit)
            {
                yield return "Temperature sensitive device " + parentDef.label + " has a higher heat warning limit than its danger limit!";
            }

            foreach (string configError in base.ConfigErrors(parentDef))
                {
                yield return configError;
            }
        }
    }
}
