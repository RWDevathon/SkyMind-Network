using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Text;

namespace SkyMind
{
    public class CompSkyMind : ThingComp
    {
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref integrityBreach, "SMN_integrityBreach", -1);
            Scribe_Values.Look(ref connected, "SMN_connected", false);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            SMN_Utils.gameComp.PopVirusedThing(parent);
            SMN_Utils.gameComp.DisconnectFromSkyMind(parent);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (connected)
            {
                if (!SMN_Utils.gameComp.AttemptSkyMindConnection(parent))
                {
                    connected = false;
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Infected don't get buttons to interact with the SkyMind.
            if (integrityBreach != -1)
                yield break;

            // If this pawn can't use the SkyMind or is not a colonist or prisoner, then it doesn't get any buttons to interact with it.
            if (parent is Pawn pawn)
            {
                if (!SMN_Utils.HasNetworkCapableImplant(pawn) || (pawn.Faction != Faction.OfPlayer && !pawn.IsPrisonerOfColony))
                {
                    yield break;
                }
            }
            // Non-pawns (buildings) must belong to the player to connect to the SkyMind network.
            else
            {
                if (parent.Faction != Faction.OfPlayer)
                {
                    yield break;
                }
            }

            // If there is no SkyMind capacity, then it doesn't get any buttons to interact with it.
            if (SMN_Utils.gameComp.GetSkyMindNetworkSlots() <= 0)
            {
                yield break;
            }

            // Connect/Disconnect to SkyMind
            yield return new Command_Toggle
            {
                icon = SMN_Textures.ConnectSkyMindIcon,
                defaultLabel = "SMN_ConnectSkyMind".Translate(),
                defaultDesc = "SMN_ConnectSkyMindDesc".Translate(),
                isActive = () => connected,
                toggleAction = delegate ()
                {
                    if (!connected)
                    { // Attempt to connect to SkyMind
                        if (!SMN_Utils.gameComp.AttemptSkyMindConnection(parent))
                        { // If trying to connect but it is unable to, inform the player.
                            if (SMN_Utils.gameComp.GetSkyMindNetworkSlots() == 0)
                                Messages.Message("SMN_SkyMindConnectionFailedNoNetwork".Translate(), parent, MessageTypeDefOf.NegativeEvent);
                            else
                                Messages.Message("SMN_SkyMindConnectionFailed".Translate(), parent, MessageTypeDefOf.NegativeEvent);
                            return;
                        }
                    }
                    else
                    { // Disconnect from SkyMind
                        SMN_Utils.gameComp.DisconnectFromSkyMind(parent);
                    }
                }
            };
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);

            switch (signal)
            {
                case "SkyMindNetworkUserConnected":
                    connected = true;
                    break;
                case "SkyMindNetworkUserDisconnected":
                    connected = false;
                    break;
            }
        }

        // Controller for the state of viruses in the parent. -1 = clean, 1 = sleeper, 2 = cryptolocker, 3 = breaker. Ticker handled by the GC to avoid calculating when clean.
        public int Breached
        {
            get
            {
                return integrityBreach;
            }

            set
            {
                int status = integrityBreach;
                integrityBreach = value;
                // Device is no longer breached. Release restrictions and remove from the virus list.
                if (integrityBreach == -1 && status != -1)
                {
                    // Release hacked pawn. Remove Mind Operation hediff and reboot.
                    if (parent is Pawn pawn)
                    {
                        Hediff hediff = HediffMaker.MakeHediff(SMN_HediffDefOf.SMN_FeedbackLoop, pawn, null);
                        hediff.Severity = 0.75f;
                        pawn.health.AddHediff(hediff, null, null);
                        hediff = pawn.health.hediffSet.GetFirstHediffOfDef(SMN_HediffDefOf.SMN_MindOperation);
                        if (hediff != null)
                        {
                            pawn.health.RemoveHediff(hediff);
                        }
                    }
                    // Handle buildings that lost power.
                    else
                    {
                        CompFlickable cf = parent.TryGetComp<CompFlickable>();
                        if (cf != null)
                        {
                            cf.SwitchIsOn = true;
                            parent.SetFaction(Faction.OfPlayer);
                        }
                    }
                    SMN_Utils.gameComp.PopVirusedThing(parent);
                }
                else
                {
                    // Breached building. Hacking effect is that it gets turned off and is applied to a hostile faction until released.
                    if (parent is Building)
                    {
                        CompFlickable cf = parent.TryGetComp<CompFlickable>();
                        if (cf != null)
                        {
                            cf.SwitchIsOn = false;
                            parent.SetFaction(Faction.OfAncientsHostile);
                        }
                    }
                    // Breached pawn (surrogates). Hacking effect is that it is put offline via a forced Mind Operation hediff.
                    else if (parent is Pawn pawn)
                    {
                        pawn.health.AddHediff(HediffMaker.MakeHediff(SMN_HediffDefOf.SMN_MindOperation, pawn));
                    }
                }
            }
        }

        // Buildings must always disconnect when despawned.
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            // Servers can not be connected to the network when despawned.
            if (parent is Building && connected)
            {
                SMN_Utils.gameComp.DisconnectFromSkyMind(parent);
            }
        }

        public override void Notify_MapRemoved()
        {
            base.Notify_MapRemoved();

            // Pawns may stay connected to the SkyMind network if despawned.
            if (parent is Pawn)
            {
                return;
            }

            // No building can be connected to the network when their map is removed.
            if (connected)
            {
                SMN_Utils.gameComp.DisconnectFromSkyMind(parent);
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder ret = new StringBuilder();

            if (parent.Map == null)
                return base.CompInspectStringExtra();

            // Add a special line for devices hacked into a shut-down state.
            if ((integrityBreach == 1 || integrityBreach == 3) && SMN_Utils.gameComp.GetAllVirusedDevices().ContainsKey(parent))
            {
                ret.Append("SMN_HackedWithTimer".Translate((SMN_Utils.gameComp.GetVirusedDevice(parent) - Find.TickManager.TicksGame).ToStringTicksToPeriodVerbose()));
            }
            // Add a special line for cryptolocked devices.
            else if (integrityBreach == 2)
            {
                ret.Append("SMN_CryptoLocked".Translate());
            }
            else if (connected)
            {
                ret.Append("SMN_SkyMindDetected".Translate());
            }

            return ret.Append(base.CompInspectStringExtra()).ToString();
        }

        private int integrityBreach = -1; // -1 : Not integrityBreach. 1: Sleeper Virus. 2: Cryptolocked. 3: Breaker Virus.
        public bool connected;
    }
}