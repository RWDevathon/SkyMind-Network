using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace SkyMind
{
    public class Alert_TemperatureSensitiveDevicesAtRisk : Alert
    {
        public Alert_TemperatureSensitiveDevicesAtRisk()
        {
            defaultLabel = "SMN_AlertTemperatureSensitiveDevicesAtRisk".Translate();
            defaultExplanation = "SMN_AlertTemperatureSensitiveDevicesAtRiskDesc".Translate();
            defaultPriority = AlertPriority.High;
        }


        public override AlertReport GetReport()
        {
            defaultPriority = AlertPriority.High;
            if (!SMN_Utils.gameComp.temperatureSensitiveDevices.Any())
                return false;

            List<Thing> culprits = new List<Thing>();

            foreach (ThingWithComps sensitiveDevice in SMN_Utils.gameComp.temperatureSensitiveDevices)
            {
                int absoluteLevel = Mathf.Abs(sensitiveDevice.GetComp<CompTemperatureSensitive>().TemperatureLevel);
                if (absoluteLevel >= 2)
                {
                    defaultPriority = AlertPriority.Critical;
                }

                if (absoluteLevel >= 1)
                {
                    culprits.Add(sensitiveDevice);
                }

                if (sensitiveDevice.GetRoom().UsesOutdoorTemperature)
                {
                    culprits.Add(sensitiveDevice);
                }
            }

            return AlertReport.CulpritsAre(culprits);
        }
    }
}
