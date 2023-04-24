using HarmonyLib;
using System.Reflection;
using Verse;
using UnityEngine;
using RimWorld;

namespace SkyMind
{
    public class SkyMindNetwork : Mod
    {
        public static SkyMindNetwork_Settings settings;
        public static SkyMindNetwork ModSingleton { get; private set; }

        public SkyMindNetwork(ModContentPack content) : base(content)
        {
            ModSingleton = this;
            new Harmony("SkyMindNetwork").PatchAll(Assembly.GetExecutingAssembly());
        }

        // Handles the localization for the mod's name in the list of mods in the mod settings page.
        public override string SettingsCategory()
        {
            return "SkyMindNetwork_ModTitle".Translate();
        }

        // Handles actually displaying this mod's settings.
        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }

    [StaticConstructorOnStartup]
    public static class SkyMindNetwork_PostInit
    {
        static SkyMindNetwork_PostInit()
        {
            SkyMindNetwork.settings = SkyMindNetwork.ModSingleton.GetSettings<SkyMindNetwork_Settings>();

            // Must dynamically modify some ThingDefs based on certain qualifications.
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                // Check race to see if the thingDef is for a Pawn.
                if (thingDef.race != null)
                {
                    // Humanlikes get specific comps for SkyMind related things.
                    if (thingDef.race.intelligence == Intelligence.Humanlike)
                    {
                        CompProperties cp;
                        cp = new CompProperties
                        {
                            compClass = typeof(CompSkyMind)
                        };
                        thingDef.comps.Add(cp);

                        cp = new CompProperties
                        {
                            compClass = typeof(CompSkyMindLink)
                        };
                        thingDef.comps.Add(cp);
                    }
                }
                // Handle SkyMind-connectable buildings.
                else if (typeof(Building).IsAssignableFrom(thingDef.thingClass) && thingDef.comps != null)
                {
                    foreach (CompProperties compProp in thingDef.comps)
                    {
                        // Add CompSkyMind if it can be powered.
                        if (compProp.compClass?.IsAssignableFrom(typeof(CompPowerTrader)) == true)
                        {
                            CompProperties cp = new CompProperties
                            {
                                compClass = typeof(CompSkyMind)
                            };
                            thingDef.comps.Add(cp);

                            // Autodoors get a special comp to allow them to be opened/closed remotely.
                            if (thingDef.IsDoor)
                            {
                                cp = new CompProperties
                                {
                                    compClass = typeof(CompAutoDoor)
                                };
                                thingDef.comps.Add(cp);
                            }

                            // Research benches get a special comp to control what server type it can be used to generate points for.
                            if (typeof(Building_ResearchBench).IsAssignableFrom(thingDef.thingClass))
                            {
                                cp = new CompProperties
                                {
                                    compClass = typeof(CompInsightBench)
                                };
                                thingDef.comps.Add(cp);
                            }
                            break;
                        }
                    }
                    // Explosive traps may be connected to the SkyMind network for remote triggering
                    if (typeof(Building_TrapExplosive).IsAssignableFrom(thingDef.thingClass))
                    {
                        CompProperties cp;

                        // If it didn't get a CompSkyMind from the previous step because it has no CompPowerTrader, add one now.
                        if (!thingDef.HasComp(typeof(CompPowerTrader)))
                        {
                            cp = new CompProperties
                            {
                                compClass = typeof(CompSkyMind)
                            };
                            thingDef.comps.Add(cp);
                        }

                        cp = new CompProperties
                        {
                            compClass = typeof(CompRemotelyTriggered)
                        };
                        thingDef.comps.Add(cp);
                    }
                }
            }
        }
    }
}