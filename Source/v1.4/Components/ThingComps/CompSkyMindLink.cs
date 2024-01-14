using System;
using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace SkyMind
{
    public class CompSkyMindLink : ThingComp, ITargetingSource
    {
        private Pawn ThisPawn => (Pawn)parent;

        public bool CasterIsPawn => true;

        public bool IsMeleeAttack => false;

        public bool Targetable => true;

        public bool MultiSelect => true;

        public bool HidePawnTooltips => true;

        public Thing Caster => parent;

        public Pawn CasterPawn => (Pawn)parent;

        public Verb GetVerb => null;

        private TargetingParameters cachedTargetParameters;

        public Texture2D UIIcon => SMN_Textures.ConnectIcon;

        public TargetingParameters targetParams
        {
            get
            {
                if (cachedTargetParameters == null)
                {
                    cachedTargetParameters = new TargetingParameters()
                    {
                        canTargetPawns = true,
                        canTargetBuildings = false,
                        canTargetAnimals = false,
                        canTargetMechs = false,
                        mapObjectTargetsMustBeAutoAttackable = false,
                        onlyTargetIncapacitatedPawns = true,
                    };
                }
                return cachedTargetParameters;
            }
        }

        public ITargetingSource DestinationSelector => null;

        public bool CanHitTarget(LocalTargetInfo target)
        {
            return ValidateTarget(target, showMessages: false);
        }

        public bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!(target.Thing is Pawn pawn))
            {
                return false;
            }

            if ((pawn.Faction != null && !pawn.Faction.IsPlayer) || !SMN_Utils.IsSurrogate(pawn))
            {
                if (showMessages)
                {
                    Messages.Message("SMN_NotUncontrolledSurrogate".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }

            if (pawn.GetComp<CompSkyMind>().Breached != -1)
            {
                if (showMessages)
                {
                    Messages.Message("SMN_LockedSurrogate".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return true;
        }

        public void DrawHighlight(LocalTargetInfo target)
        {
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }

        public void OrderForceTarget(LocalTargetInfo target)
        {
            ConnectSurrogate((Pawn)target.Thing);
        }

        public void OnGUI(LocalTargetInfo target)
        {
            Widgets.MouseAttachedLabel("SMN_IdentifySurrogate".Translate());
            if (ValidateTarget(target, showMessages: false))
            {
                GenUI.DrawMouseAttachment(UIIcon);
            }
            else
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref networkOperationInProgress, "SMN_networkOperationInProgress", -1);
            Scribe_Values.Look(ref controlMode, "SMN_controlMode", false);
            Scribe_Values.Look(ref isForeign, "SMN_isForeign", false);

            // Only save and load recipient data if this comp is registered as being in a mind operation.
            if (networkOperationInProgress > -1)
            {
                Scribe_References.Look(ref recipientPawn, "SMN_recipientPawn");
            }
            // Only save and load surrogate data if this comp is attached to a player pawn and has or is a surrogate.
            if (!isForeign && Linked == -2)
            {
                Scribe_Collections.Look(ref surrogatePawns, "SMN_surrogatePawns", lookMode: LookMode.Reference);
            }

            // Reduplicate the controller skills into tethered surrogates as it seems to desync after loading.
            if (Scribe.mode == LoadSaveMode.PostLoadInit && controlMode == true && HasSurrogate())
            {
                foreach (Pawn surrogate in surrogatePawns)
                {
                    SMN_Utils.DuplicateSkills(ThisPawn, surrogate, true);
                    SMN_Utils.DuplicateRelations(ThisPawn, surrogate, true);
                }
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (controlMode)
            {
                ToggleControlMode();
            }
            if (Linked > -1)
            {
                Log.Warning("[SMN] Destroyed a pawn mid-mind operation. This may not be a clean interrupt, issues may arise.");
                HandleInterrupt();
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();

            if (HasSurrogate() && !isForeign)
            {
                foreach (Pawn surrogate in surrogatePawns)
                {
                    if (ThisPawn.Spawned && surrogate.Map == parent.Map)
                    {
                        GenDraw.DrawLineBetween(parent.TrueCenter(), surrogate.TrueCenter(), SimpleColor.Blue);
                    }
                }
            }
        }

        // Toggle whether this pawn is designated as a surrogate controller or not. Surrogate controllers are downed.
        public void ToggleControlMode()
        {
            controlMode = !controlMode;
            // Set to controller mode
            if (controlMode && Linked == -1)
            {
                Linked = -2;
            }
            // Return to default mode
            else if (!controlMode && Linked == -2)
            {
                Linked = -1;
            }
            if (!controlMode && HasSurrogate())
            {
                DisconnectSurrogates();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // No point in showing SkyMind operations on a pawn that isn't connected to it or is not a valid target for mind operations.
            if (!SMN_Utils.IsValidMindTransferTarget(ThisPawn))
                yield break;

            // Only pawns belonging explicitly to the the player may have these actions used.
            if (ThisPawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            // Surrogate only operations.
            if (SMN_Utils.IsSurrogate(ThisPawn))
            {
                // Cloud pawn controllers that aren't busy controlling other surrogates or that are in a mind operation already are eligible for downloading from.
                Pawn controller = surrogatePawns.FirstOrFallback();
                if (SMN_Utils.gameComp.GetCloudPawns().Contains(controller) && controller.health.hediffSet.GetFirstHediffOfDef(SMN_HediffDefOf.SMN_MindOperation) == null && controller.GetComp<CompSkyMindLink>().surrogatePawns.Count == 1 && MayDownloadTo(controller, ThisPawn))
                {
                    yield return new Command_Action
                    {
                        icon = SMN_Textures.DownloadFromSkyCloud,
                        defaultLabel = "SMN_DownloadCloudPawn".Translate(),
                        defaultDesc = "SMN_DownloadCloudPawnDesc".Translate(),
                        action = delegate ()
                        {
                            Find.WindowStack.Add(new Dialog_MessageBox("SMN_DownloadCloudPawnConfirm".Translate() + "\n" + "SMN_SkyMindDisconnectionRisk".Translate(), "Confirm".Translate(), buttonBText: "Cancel".Translate(), title: "SMN_DownloadCloudPawn".Translate(), buttonAAction: delegate
                            {
                                InitiateConnection(4, controller);
                            }));
                        }
                    };
                }

                // Surrogates may disconnect freely from their host.
                yield return new Command_Action
                {
                    icon = SMN_Textures.DisconnectIcon,
                    defaultLabel = "SMN_DisconnectSurrogate".Translate(),
                    defaultDesc = "SMN_DisconnectSurrogateDesc".Translate(),
                    action = delegate ()
                    {
                        SMN_Utils.gameComp.DisconnectFromSkyMind(ThisPawn);
                    }
                };

                // Skip all other operations. They are illegal on surrogates.
                yield break;
            }

            // Show surrogate control mode as long as they are enabled via settings, as any non-surrogate SkyMind connected pawn may use them.
            if (SkyMindNetwork_Settings.surrogatesAllowed)
            {
                yield return new Command_Toggle
                {
                    icon = SMN_Textures.ControlModeIcon,
                    defaultLabel = "SMN_ToggleControlMode".Translate(),
                    defaultDesc = "SMN_ToggleControlModeDesc".Translate(),
                    isActive = () => controlMode,
                    toggleAction = delegate ()
                    {
                        ToggleControlMode();
                    }
                };
            }

            // Always show Skill Up menu option, as any non-surrogate SkyMind connected pawn may use them.
            yield return new Command_Action
            {
                icon = SMN_Textures.SkillWorkshopIcon,
                defaultLabel = "SMN_Skills".Translate(),
                defaultDesc = "SMN_SkillsDesc".Translate(),
                action = delegate ()
                {
                    Find.WindowStack.Add(new Dialog_SkillUp((Pawn)parent));
                }
            };

            // If in surrogate control mode, allow selecting a surrogate to control and allow updating surrogate subconsciousness. No other operations are legal while in control mode.
            if (controlMode)
            {
                // Allow connecting to new surrogates.
                yield return new Command_Action
                {
                    icon = SMN_Textures.ConnectIcon,
                    defaultLabel = "SMN_ControlSurrogate".Translate(),
                    defaultDesc = "SMN_ControlSurrogateDesc".Translate(),
                    action = delegate ()
                    {
                        List<FloatMenuOption> opts = new List<FloatMenuOption>();
                        foreach (Map map in Find.Maps)
                        {
                            opts.Add(new FloatMenuOption(map.Parent.Label, delegate
                            {
                                Current.Game.CurrentMap = map;
                                Find.Targeter.BeginTargeting(this, null, true, null, null);
                            }));
                        }
                        if (opts.Count == 1)
                        {
                            Find.Targeter.BeginTargeting(this, null, true, null, null);
                        }
                        else if (opts.Count > 1)
                        {
                            FloatMenu floatMenuMap = new FloatMenu(opts);
                            Find.WindowStack.Add(floatMenuMap);
                        }
                    }
                };

                // Allow connecting a host to hostless surrogates in caravans.
                IEnumerable<Pawn> hostlessSurrogatesInCaravans = SMN_Utils.GetHostlessCaravanSurrogates();

                if (hostlessSurrogatesInCaravans != null)
                {
                    yield return new Command_Action
                    {
                        icon = SMN_Textures.RecoveryIcon,
                        defaultLabel = "SMN_ControlCaravanSurrogate".Translate(),
                        defaultDesc = "SMN_ControlCaravanSurrogateDesc".Translate(),
                        action = delegate ()
                        {
                            List<FloatMenuOption> opts = new List<FloatMenuOption>();
                            foreach (Pawn surrogate in hostlessSurrogatesInCaravans)
                            {
                                opts.Add(new FloatMenuOption(surrogate.LabelShortCap, delegate ()
                                {
                                    if (!SMN_Utils.gameComp.AttemptSkyMindConnection(surrogate))
                                        return;
                                    ConnectSurrogate(surrogate);
                                }));
                            }
                            opts.SortBy((x) => x.Label);
                            Find.WindowStack.Add(new FloatMenu(opts, ""));
                        }
                    };
                }

                // Allow interactions with surrogates of this pawn.
                if (HasSurrogate())
                {
                    // Always allow controllers to disconnect from all pawns.
                    yield return new Command_Action
                    {
                        icon = SMN_Textures.DisconnectIcon,
                        defaultLabel = "SMN_DisconnectSurrogate".Translate(),
                        defaultDesc = "SMN_DisconnectSurrogateDesc".Translate(),
                        action = delegate ()
                        {
                            DisconnectSurrogates();
                        }
                    };

                    // Allow this pawn to do transfers if it is controlling a single surrogate.
                    if (surrogatePawns.Count == 1)
                    {
                        Pawn surrogate = surrogatePawns.FirstOrFallback();
                        yield return new Command_Action
                        {
                            icon = SMN_Textures.DownloadFromSkyCloud,
                            defaultLabel = "SMN_Transfer".Translate(),
                            defaultDesc = "SMN_TransferDesc".Translate(),
                            action = delegate ()
                            {
                                Find.WindowStack.Add(new Dialog_MessageBox("SMN_TransferConfirm".Translate(parent.LabelShortCap, "SMN_Surrogate".Translate()) + "\n" + "SMN_SkyMindDisconnectionRisk".Translate(), "Confirm".Translate(), buttonBText: "Cancel".Translate(), title: "SMN_Transfer".Translate(), buttonAAction: delegate
                                {
                                    surrogate.GetComp<CompSkyMindLink>().InitiateConnection(4, ThisPawn);
                                }));
                            }
                        };
                    }
                }

                // Skip all other operations.
                yield break;
            }

            // Allow this pawn to do permutations.
            yield return new Command_Action
            {
                icon = SMN_Textures.Permute,
                defaultLabel = "SMN_Permute".Translate(),
                defaultDesc = "SMN_PermuteDesc".Translate(),
                action = delegate ()
                {
                    List<FloatMenuOption> opts = new List<FloatMenuOption>();

                    foreach (Pawn colonist in ThisPawn.Map.mapPawns.FreeColonists)
                    {
                        if (SMN_Utils.IsValidMindTransferTarget(colonist) && colonist != ThisPawn && !SMN_Utils.IsSurrogate(colonist))
                        {
                            opts.Add(new FloatMenuOption(colonist.LabelShortCap, delegate ()
                            {
                                Find.WindowStack.Add(new Dialog_MessageBox("SMN_PermuteConfirm".Translate(parent.LabelShortCap, colonist.LabelShortCap) + "\n" + "SMN_SkyMindDisconnectionRisk".Translate(), "Confirm".Translate(), buttonBText: "Cancel".Translate(), title: "SMN_Permute".Translate(), buttonAAction: delegate
                                {
                                    InitiateConnection(1, colonist);
                                }));
                            }));
                        }
                    }
                    opts.SortBy((x) => x.Label);

                    if (opts.Count == 0)
                        opts.Add(new FloatMenuOption("SMN_NoAvailableTarget".Translate(), null));
                    Find.WindowStack.Add(new FloatMenu(opts, "SMN_ViableTargets".Translate()));
                }
            };

            // Uploading requires space in the SkyMind network for the intelligence.
            if (SMN_Utils.gameComp.GetSkyMindCloudCapacity() > SMN_Utils.gameComp.GetCloudPawns().Count)
            {
                yield return new Command_Action
                {
                    icon = SMN_Textures.SkyMindUpload,
                    defaultLabel = "SMN_Upload".Translate(),
                    defaultDesc = "SMN_UploadDesc".Translate(),
                    action = delegate ()
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox("SMN_UploadConfirm".Translate() + "\n" + ("SMN_SkyMindDisconnectionRisk").Translate(), "Confirm".Translate(), buttonBText: "Cancel".Translate(), title: "SMN_Upload".Translate(), buttonAAction: delegate
                        {
                            InitiateConnection(5);
                        }));
                    }
                };
            }

            yield break;
        }

        // Controller for the mental operation state of the parent. -2 = surrogate, -1 = No Op, > -1 is some sort of operation. GameComponent handles checks for linked pawns.
        public int Linked
        {
            get
            {
                return networkOperationInProgress;
            }

            set
            {
                int status = networkOperationInProgress;
                networkOperationInProgress = value;
                // Pawn's operation has ended. Close out appropriate function based on the networkOperation that had been chosen (contained in status).
                if (networkOperationInProgress == -1 && status > -1)
                {
                    // If the status is resetting because of a failure, notify that a failure occurred. HandleInterrupt takes care of actual negative events.
                    if (ThisPawn.health.hediffSet.hediffs.Any(targetHediff => targetHediff.def == SMN_HediffDefOf.SMN_MemoryCorruption || targetHediff.def == HediffDefOf.Dementia))
                    {
                        Find.LetterStack.ReceiveLetter("SMN_OperationFailure".Translate(), "SMN_OperationFailureDesc".Translate(ThisPawn.LabelShortCap), LetterDefOf.NegativeEvent, ThisPawn);
                    }
                    else
                    {
                        HandleSuccess(status);
                    }

                    // All pawns undergo a system reboot upon completion of an operation.
                    Hediff hediff = HediffMaker.MakeHediff(SMN_HediffDefOf.SMN_FeedbackLoop, ThisPawn, null);
                    hediff.Severity = 1f;
                    ThisPawn.health.AddHediff(hediff, null, null);

                    // Pawns no longer have the mind operation hediff.
                    Hediff target = ThisPawn.health.hediffSet.GetFirstHediffOfDef(SMN_HediffDefOf.SMN_MindOperation);
                    if (target != null)
                        ThisPawn.health.RemoveHediff(target);

                    // Recipients lose any MindOperation hediffs as well and also reboot.
                    if (recipientPawn != null)
                    {
                        recipientPawn.health.AddHediff(SMN_HediffDefOf.SMN_FeedbackLoop);
                        target = recipientPawn.health.hediffSet.GetFirstHediffOfDef(SMN_HediffDefOf.SMN_MindOperation);
                        if (target != null)
                            recipientPawn.health.RemoveHediff(target);
                    }

                    SMN_Utils.gameComp.PopNetworkLinkedPawn(ThisPawn);
                }
                // Operation has begun. Stand by until completion or aborted.
                else if (networkOperationInProgress > -1)
                {
                    HandleInitialization();
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder ret = new StringBuilder();

            if (parent.Map == null)
                return base.CompInspectStringExtra();

            // A SkyMind operation is in progress. State how long players must wait before the operation will be complete.
            if (networkOperationInProgress > -1 && SMN_Utils.gameComp.GetAllLinkedPawns().ContainsKey(ThisPawn))
            {
                ret.Append("SMN_SkyMindOperationInProgress".Translate((SMN_Utils.gameComp.GetLinkedPawn(ThisPawn) - Find.TickManager.TicksGame).ToStringTicksToPeriodVerbose()));
            }
            else if (networkOperationInProgress == -2)
            {
                ret.Append("SMN_SurrogateConnected".Translate(surrogatePawns.Count));
            }
            return ret.Append(base.CompInspectStringExtra()).ToString();
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);

            switch (signal)
            {
                case "SkyMindNetworkUserDisconnected":
                    // Disconnect any linked pawns, depending on the control mode of this pawn.
                    if (controlMode)
                    {
                        ToggleControlMode();
                    }
                    else
                    {
                        DisconnectController();
                    }

                    // Check to see if any mind operations were interrupted by the disconnection.
                    CheckInterruptedUpload();
                    break;
            }
        }

        // Connect to the provided surrogate, with this pawn as the controller.
        public void ConnectSurrogate(Pawn surrogate, bool external = false)
        {
            // Ensure the surrogate is connected to the SkyMind network. Abort if it can't. This step only occurs for player pawns.
            if (!external && !SMN_Utils.gameComp.AttemptSkyMindConnection(surrogate))
            {
                Messages.Message("SMN_CannotConnect".Translate(), surrogate, MessageTypeDefOf.RejectInput, false);
            }

            // Copy this pawn into the surrogate. Player surrogates are tethered to the controller.
            SMN_Utils.Duplicate(ThisPawn, surrogate, false, !external);
            CompSkyMindLink surrogateLink = surrogate.GetComp<CompSkyMindLink>();

            // Foreign controllers aren't saved, so only handle linking the surrogate and controller together if it's a player pawn.
            if (!external)
            {
                // Ensure both pawns link to one another in their surrogatePawns.
                surrogatePawns.Add(surrogate);
                surrogateLink.surrogatePawns.Add(ThisPawn);

                // If this is not a cloud pawn, both the surrogate and controller should have Hediff_SplitConsciousness.
                if (!SMN_Utils.gameComp.GetCloudPawns().Contains(ThisPawn))
                {
                    Hediff splitConsciousness = HediffMaker.MakeHediff(SMN_HediffDefOf.SMN_SplitConsciousness, surrogate);
                    surrogate.health.AddHediff(splitConsciousness);
                    if (!ThisPawn.health.hediffSet.hediffs.Any(hediff => hediff.def == SMN_HediffDefOf.SMN_SplitConsciousness))
                    {
                        splitConsciousness = HediffMaker.MakeHediff(SMN_HediffDefOf.SMN_SplitConsciousness, ThisPawn);
                        ThisPawn.health.AddHediff(splitConsciousness);
                    }
                }

                FleckMaker.ThrowDustPuffThick(surrogate.Position.ToVector3Shifted(), surrogate.Map, 4.0f, Color.blue);
                Messages.Message("SMN_SurrogateControlled".Translate(ThisPawn.LabelShortCap), ThisPawn, MessageTypeDefOf.PositiveEvent);
                Linked = -2;
                surrogateLink.Linked = -2;
            }
            else
            {
                // Foreign controllers aren't saved, and are only needed to initialize the surrogate. Foreign surrogates operate independently until downed or killed.
                isForeign = true;
                surrogateLink.isForeign = true;
                surrogate.health.AddHediff(HediffMaker.MakeHediff(SMN_HediffDefOf.SMN_ForeignConsciousness, surrogate));
            }

            // Remove the surrogate's NoHost hediff.
            Hediff target = surrogate.health.hediffSet.GetFirstHediffOfDef(SMN_HediffDefOf.SMN_NoController);
            if (target != null)
                surrogate.health.RemoveHediff(target);
        }

        // Return whether this pawn controls surrogates.
        public bool HasSurrogate()
        {
            return surrogatePawns.Any();
        }

        public IEnumerable<Pawn> GetSurrogates()
        {
            return surrogatePawns;
        }

        // Called only on controlled surrogates, this will deactivate the surrogate and inform the controller it was disconnected.
        public void DisconnectController()
        {
            if (!HasSurrogate() && !isForeign)
            {
                return;
            }

            // Apply the blank template to self.
            SMN_Utils.Duplicate(SMN_Utils.GetBlank(), ThisPawn, false, false);

            // Foreign surrogates do not have links to their controllers.
            if (!isForeign)
            {
                // Disconnect the surrogate from its controller.
                surrogatePawns.FirstOrFallback().GetComp<CompSkyMindLink>().surrogatePawns.Remove(ThisPawn);
                surrogatePawns.Clear();
            }

            ThisPawn.guest?.SetGuestStatus(Faction.OfPlayer);
            if (ThisPawn.playerSettings != null)
                ThisPawn.playerSettings.medCare = MedicalCareCategory.Best;

            // Apply NoHost hediff to player surrogates.
            if (!isForeign)
            {
                ThisPawn.health.AddHediff(SMN_HediffDefOf.SMN_NoController);
                Linked = -1;
            }
        }

        // Called on surrogate controllers, this will disconnect all connected surrogates. It will do nothing if there are none.
        public void DisconnectSurrogates()
        {
            if (!HasSurrogate())
            {
                return;
            }

            // Disconnect each surrogate from the SkyMind (and this pawn by extension).
            foreach (Pawn surrogate in new List<Pawn>(surrogatePawns))
            {
                SMN_Utils.gameComp.DisconnectFromSkyMind(surrogate);
            }
            // Forget about all surrogates.
            surrogatePawns.Clear();
            Linked = -1;
        }

        // Applies some form of corruption to the provided pawn. For organics, this is dementia. For androids, this is a slowly fading memory corruption.
        public void ApplyCorruption(Pawn pawn)
        {
            if (pawn == null)
                return;

            Hediff corruption = HediffMaker.MakeHediff(SMN_HediffDefOf.SMN_MemoryCorruption, pawn, pawn.health.hediffSet.GetBrain());
            corruption.Severity = Rand.Range(0.15f, 0.95f);
            pawn.health.AddHediff(corruption, pawn.health.hediffSet.GetBrain(), null);

            // Pawn loses corruption severity as a percent of total xp in each skill.
            foreach (SkillRecord skillRecord in pawn.skills.skills)
            {
                skillRecord.Learn((float)(-skillRecord.XpTotalEarned * corruption.Severity), true);
            }
        }

        // An operation has been started. Apply mind operation to this pawn and the recipient pawn (if it exists), and track the current operation in the game component.
        public void HandleInitialization()
        {
            ThisPawn.health.AddHediff(SMN_HediffDefOf.SMN_MindOperation);
            SMN_Utils.gameComp.PushNetworkLinkedPawn(ThisPawn, Find.TickManager.TicksGame + SkyMindNetwork_Settings.timeToCompleteSkyMindOperations * 2500);
            if (recipientPawn != null)
            {
                recipientPawn.health.AddHediff(SMN_HediffDefOf.SMN_MindOperation);
            }
            Messages.Message("SMN_OperationInitiated".Translate(ThisPawn.LabelShort), parent, MessageTypeDefOf.PositiveEvent);
        }

        // An operation was interrupted while in progress. Handle the outcome based on which operation was occurring.
        public void HandleInterrupt()
        {
            // Absorption failure kills the source pawn - they were going to die on success any way.
            if (Linked == 3)
            {
                SMN_Utils.gameComp.DisconnectFromSkyMind(ThisPawn);
                SMN_Utils.Duplicate(SMN_Utils.GetBlank(), ThisPawn, true, false);
                ThisPawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, 99999f, 999f, -1f, null, ThisPawn.health.hediffSet.GetBrain()));
                // If they're somehow not dead from that, make them dead for real.
                if (!ThisPawn.Dead)
                {
                    ThisPawn.Kill(null);
                }
                return;
            }

            // Otherwise, corrupt the source and reicipient (if it exists) pawns and delink them.
            ApplyCorruption(ThisPawn);
            if (recipientPawn != null)
            {
                ApplyCorruption(recipientPawn);
                recipientPawn.GetComp<CompSkyMindLink>().Linked = -1;
            }
            SMN_Utils.gameComp.PopNetworkLinkedPawn(ThisPawn);
            Linked = -1;
        }

        // Apply the correct effects upon successfully completing the SkyMind operation based on the status (parameter as Linked is changed to -1 just beforehand).
        public void HandleSuccess(int status)
        {
            // If there is a recipient pawn, it was a permutation, duplication, or download. Handle appropriately.
            if (recipientPawn != null)
            {
                // Permutation, swap the pawn's minds.
                if (status == 1)
                {
                    SMN_Utils.PermutePawn(ThisPawn, recipientPawn);
                }
                // Download, insert of a copy of the recipient pawn into the current pawn.
                else if (status == 4)
                {
                    // Disconnect the surrogate now to sever the connection and let it be ready for duplication.
                    SMN_Utils.gameComp.DisconnectFromSkyMind(ThisPawn);

                    // Remove any SkyMind receivers for the newly autonomous pawn and the No Controller hediff if it has any.
                    List<Hediff> surrogateHediffs = ThisPawn.health.hediffSet.hediffs;
                    for (int i = surrogateHediffs.Count - 1; i >= 0; i--)
                    {
                        if (surrogateHediffs[i].def == SMN_HediffDefOf.SMN_NoController || surrogateHediffs[i].def.GetModExtension<SMN_HediffSkyMindExtension>()?.isReceiver == true)
                        {
                            ThisPawn.health.RemoveHediff(surrogateHediffs[i]);
                        }
                    }

                    SMN_Utils.Duplicate(recipientPawn, ThisPawn, false, false);

                    // Disconnect the old controller and turn it into a blank as this is a download operation.
                    SMN_Utils.gameComp.DisconnectFromSkyMind(recipientPawn);
                    SMN_Utils.TurnIntoBlank(recipientPawn);
                }
            }

            // Absorption kills the source pawn and generates valuable hacking and skill points for the colony.
            if (status == 3)
            {
                int sum = 0;
                foreach (SkillRecord skillRecord in ThisPawn.skills.skills)
                {
                    // For each skill the pawn possesses, generate 10 * level ^ 1.5 points to be distributed between hacking and skill.
                    sum += (int)(Math.Pow(skillRecord.levelInt, 1.5) * 10);
                }

                SMN_Utils.gameComp.ChangeServerPoints(sum/2, SMN_ServerType.HackingServer);
                SMN_Utils.gameComp.ChangeServerPoints(sum/2, SMN_ServerType.SkillServer);
                SMN_Utils.Duplicate(SMN_Utils.GetBlank(), ThisPawn, true, false);
                ThisPawn.Kill(null);
            }

            // Upload moves the pawn to the SkyMind network (despawns and puts into storage).
            if (status == 5)
            {
                // Add the pawn to storage and suspend any tick-checks it performs.
                SMN_Utils.gameComp.PushCloudPawn(ThisPawn);
                Current.Game.tickManager.DeRegisterAllTickabilityFor(ThisPawn);

                try
                {
                    // Upon completion, we need to spawn a copy of the pawn to take their physical place as the original pawn despawns "into" the SkyMind Core.
                    Pawn replacement = SMN_Utils.SpawnCopy(ThisPawn, SkyMindNetwork_Settings.uploadingToSkyMindKills);
                    // If in the settings, uploading is set to Permakill, find the new pawn copy's brain and mercilessly destroy it so it can't be revived. Ensure no one cares about this.
                    if (SkyMindNetwork_Settings.uploadingToSkyMindPermaKills)
                    {
                        replacement.SetFactionDirect(null);
                        replacement.ideo?.SetIdeo(null);
                        replacement.TakeDamage(new DamageInfo(DamageDefOf.Burn, 99999f, 999f, -1f, null, replacement.health.hediffSet.GetBrain()));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[SMN] An unexpected exception occurred while attempting to spawn a corpse replacement for " + ThisPawn + ". Replacement pawn may be left in a bugged state." + ex.Message + ex.StackTrace);
                }
                finally
                {
                    // The pawn does not need to be connected to the SkyMind directly now, and should disappear.
                    SMN_Utils.gameComp.DisconnectFromSkyMind(ThisPawn);
                    ThisPawn.DeSpawn();
                    ThisPawn.ownership.UnclaimAll();
                }
            }

            // Replication simply creates a new SkyMind intelligence duplicated from another.
            if (status == 6)
            {
                // Generate the clone.
                PawnGenerationRequest request = new PawnGenerationRequest(ThisPawn.kindDef, Faction.OfPlayer, PawnGenerationContext.NonPlayer, -1, fixedBiologicalAge: ThisPawn.ageTracker.AgeBiologicalYearsFloat, fixedChronologicalAge: ThisPawn.ageTracker.AgeChronologicalYearsFloat, fixedGender: ThisPawn.gender);
                Pawn clone = PawnGenerator.GeneratePawn(request);

                // Copy the name of this pawn into the clone.
                NameTriple newName = (NameTriple)ThisPawn.Name;
                clone.Name = new NameTriple(newName.First, newName.Nick + Rand.RangeInclusive(100, 999), newName.Last);

                // Remove any Hediffs the game may have applied when generating the clone - this is to avoid weird hediffs appearing that may cause unexpected behavior.
                clone.health.RemoveAllHediffs();

                // Duplicate the intelligence of this pawn into the clone (not murder) and add them to the SkyMind network.
                SMN_Utils.Duplicate(ThisPawn, clone, false, false);
                SMN_Utils.gameComp.PushCloudPawn(clone);
            }

            Find.LetterStack.ReceiveLetter("SMN_OperationCompleted".Translate(), "SMN_OperationCompletedDesc".Translate(ThisPawn.LabelShortCap), LetterDefOf.PositiveEvent, ThisPawn);
        }


        // Check if there is an operation in progress. If there is (Linked != -1) and it is the operation source (LinkedPawn != -2), then we need to check if it's been interrupted and respond appropriately.
        public void CheckInterruptedUpload()
        {
            if (Linked > -1 && SMN_Utils.gameComp.GetLinkedPawn(ThisPawn) != -2)
            {
                // Check to see if the current pawn is no longer connected to the SkyMind network (or is dead).
                if (ThisPawn.Dead || (!ThisPawn.GetComp<CompSkyMind>().connected && !SMN_Utils.gameComp.GetCloudPawns().Contains(ThisPawn)))
                {
                    HandleInterrupt();
                    return;
                }

                // Check to see if the operation involves a recipient pawn and ensure their status is similarly acceptable if there is one.
                if (recipientPawn != null)
                {
                    if (recipientPawn.Dead || (!recipientPawn.GetComp<CompSkyMind>().connected && !SMN_Utils.gameComp.GetCloudPawns().Contains(recipientPawn)))
                    {
                        HandleInterrupt();
                        return;
                    }
                }

                // Check to see if there is a functional SkyMind Core if one is required for an operation to continue. One is required for uploading, or replicating.
                if (Linked >= 5 && !SMN_Utils.gameComp.HasSkyMindCore())
                {
                    HandleInterrupt();
                    return;
                }
            }
        }

        // A public method for other classes to initiate SkyMind operations. It will fail and do nothing if this pawn is already preoccupied.
        public void InitiateConnection(int operationType, Pawn targetRecipient = null)
        {
            if (Linked > -1)
            {
                Log.Warning("[SMN] Something attempted to initiate a connection for " + ThisPawn + " while it was busy! Command was ignored.");
                return;
            }
            else if (operationType == -2)
            {
                Log.Warning("[SMN] Surrogate connections can not be established directly with the InitiateConnection function. Use ConnectSurrogate instead.");
            }

            recipientPawn = targetRecipient;
            Linked = operationType;
        }

        // A simple public method for other assemblies to harmony patch into and decide whether a particular pawn is legal for downloading an intelligence into.
        public virtual bool MayDownloadTo(Pawn source, Pawn destination)
        {
            return true;
        }

        // Operation tracker. -2 = player surrogate operation, -1 = No operation, 1 = permutation, 2 = unused, 3 = absorption, 4 = download, 5 = upload, 6 = replication
        private int networkOperationInProgress = -1;

        // Tracker for the recipient pawn of a mind operation that requires two linked units.
        private Pawn recipientPawn = null;

        // Tracker for all surrogate pawns. If a pawn is a surrogate, it will have exactly one link - to its host. If it is a controller, it has links to all surrogates.
        private HashSet<Pawn> surrogatePawns = new HashSet<Pawn>();

        // Tracker for if this pawn is in control mode (allowing control of surrogates).
        private bool controlMode = false;

        // Tracker for whether this pawn is not a player surrogate. Foreign surrogates do not have links to their controllers and are very limited in what they can do.
        public bool isForeign = false;
    }
}