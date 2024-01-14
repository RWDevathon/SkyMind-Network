using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.Text;

namespace SkyMind
{
    public class CompTemperatureSensitive : ThingComp
    {
        private int tempLevel;

        private int temperatureAverage = 0;

        public CompProperties_TemperatureSensitive Props
        {
            get
            {
                return (CompProperties_TemperatureSensitive)props;
            }
        }

        public int TemperatureLevel
        {
            get
            {
                return tempLevel;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref tempLevel, "SMN_tempLevel", 0, false);
            Scribe_Values.Look(ref temperatureAverage, "SMN_temperatureAverage", 0, false);
        }

        public override void PostDraw()
        {
            Material iconMat = null;

            if (tempLevel == 0)
            {
                return;
            }

            if (Math.Abs(tempLevel) == 1)
                iconMat = SMN_Textures.WarningHeat;
            else if (Math.Abs(tempLevel) == 2)
                iconMat = SMN_Textures.DangerHeat;
            else if (Math.Abs(tempLevel) == 3)
                iconMat = SMN_Textures.CriticalHeat;

            Vector3 vector = parent.TrueCenter();
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) + 0.28125f;
            vector.x += parent.def.size.x / 4;

            vector.z -= 1;

            var num = (Time.realtimeSinceStartup + 397f * (parent.thingIDNumber % 571)) * 4f;
            var num2 = ((float)Math.Sin((double)num) + 1f) * 0.5f;
            num2 = 0.3f + num2 * 0.7f;
            var material = FadedMaterialPool.FadedVersionOf(iconMat, num2);
            Graphics.DrawMesh(MeshPool.plane05, vector, Quaternion.identity, material, 0);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            SMN_Utils.gameComp.temperatureSensitiveDevices.Add(parent);
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            SMN_Utils.gameComp.temperatureSensitiveDevices.Remove(parent);
        }

        // Checker for heat on normal Ticks.
        public override void CompTick()
        {
            base.CompTick();

            CheckTemperature(1);
        }

        // Checker for heat on Rare Ticks (250 Ticks).
        public override void CompTickRare()
        {
            base.CompTickRare();

            CheckTemperature(GenTicks.TickRareInterval);
        }

        // Checker for heat on Long Ticks (2000 Ticks).
        public override void CompTickLong()
        {
            base.CompTickLong();

            CheckTemperature(GenTicks.TickLongInterval);
        }

        private void CheckTemperature(int ticks)
        {
            float ambientTemperature = parent.AmbientTemperature;

            // Ensure the device is sitting in the correct heat level index. -3: critical cold, -2: danger cold, -1: warning cold, 0: safe, 1: warning heat, 2: danger heat, 3: critical heat
            if (ambientTemperature < Props.coldDangerLimit)
            {
                tempLevel = -3;
            }
            else if (ambientTemperature < Props.coldWarningLimit)
            {
                tempLevel = -2;
            }
            else if (ambientTemperature < Props.coldSafeLimit)
            {
                tempLevel = -1;
            }
            else if (ambientTemperature > Props.heatDangerLimit)
            {
                tempLevel = 3;
            }
            else if (ambientTemperature > Props.heatWarningLimit)
            {
                tempLevel = 2;
            }
            else if (ambientTemperature > Props.heatSafeLimit)
            {
                tempLevel = 1;
            }
            else
            {
                tempLevel = 0;
                Mathf.MoveTowards(temperatureAverage, 0, ticks);
            }

            if (tempLevel != 0)
            {
                temperatureAverage += ticks * tempLevel * Mathf.Abs(tempLevel);
            }

            if (ticks > 1 || parent.IsHashIntervalTick(GenTicks.TickRareInterval))
            {
                // If the structure is outside, take damage.
                if (parent.GetRoom().UsesOutdoorTemperature)
                {
                    SufferDamage();
                }

                // If the device has been overheating or underheating for too long, take damage.
                if (Mathf.Abs(temperatureAverage) >= GenDate.TicksPerDay)
                {
                    SufferDamage();
                }

                // Can only occur after ~ 1.5 days and will certainly happen at the 6 day mark.
                if (Mathf.Abs(temperatureAverage) >= (GenDate.TicksPerDay * 2) + Rand.RangeInclusive(-GenDate.TicksPerDay / 2, GenDate.TicksPerDay * 4))
                {
                    temperatureAverage /= 8;
                    
                    // Only allow online structures to actually suffer failures to avoid letter spam and issues.
                    if (parent.GetComp<CompPowerTrader>()?.PowerOn != false)
                    {
                        SufferFailure();
                        Find.LetterStack.ReceiveLetter("SMN_DeviceFailure".Translate(), "SMN_DeviceFailureDesc".Translate(), LetterDefOf.NegativeEvent, new TargetInfo(parent.Position, parent.Map, false), null, null);
                    }
                }
            }
        }

        public void SufferFailure()
        {
            parent.GetComp<CompBreakdownable>()?.DoBreakdown();

            // Significantly overheating structures that have power flowing through them spark and short circuit.
            if (tempLevel > 1)
            {
                GenExplosion.DoExplosion(parent.Position, parent.Map, 3, DamageDefOf.Flame, null);
            }
        }

        public void SufferDamage()
        {
            Building building = (Building)parent;
            DamageInfo damageInfo = new DamageInfo(DamageDefOf.Deterioration, building.MaxHitPoints * (0.01f * Mathf.Max(Mathf.Abs(tempLevel), 1)));
            building.TakeDamage(damageInfo);
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (parent.GetRoom().UsesOutdoorTemperature)
            {
                stringBuilder.AppendLine("SMN_CompTempSensitiveStructureOutside".Translate());
            }

            if (Mathf.Abs(tempLevel) == 3)
                stringBuilder.Append("SMN_CompTempSensitiveCriticalText".Translate());
            else if (Mathf.Abs(tempLevel) == 2)
                stringBuilder.Append("SMN_CompTempSensitiveDangerText".Translate());
            else if (Mathf.Abs(tempLevel) == 1)
                stringBuilder.Append("SMN_CompTempSensitiveWarningText".Translate());
            else
                stringBuilder.Append("SMN_CompTempSensitiveSafeText".Translate());

            return stringBuilder.ToString();
        }
    }
}
