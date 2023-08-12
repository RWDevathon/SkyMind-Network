using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;
using RimWorld.Planet;
using System.Linq;

namespace SkyMind
{
    public static class SMN_Utils
    {
        // Cached pawn that represents a blank pawn to be used for various duplication and modification tasks.
        private static Pawn blankPawn;

        // If a pawn has a hediff that allows it (via a defModExtension bool) to connect to the SkyMind network, return true.
        public static bool HasNetworkCapableImplant(Pawn pawn)
        {
            List<Hediff> pawnHediffs = pawn.health.hediffSet.hediffs;
            for (int i = pawnHediffs.Count - 1; i >= 0; i--)
            {
                if (pawnHediffs[i].def.GetModExtension<SMN_HediffSkyMindExtension>()?.allowsConnection == true)
                    return true;
            }

            // Pawns in the SkyMind Core are by nature considered cloud capable.
            if (gameComp.GetCloudPawns().Contains(pawn))
            {
                return true;
            }

            return false;
        }

        // Returns true if the race can ever be a surrogate.
        public static bool MayEverBeSurrogate(ThingDef thingDef)
        {
            return thingDef.race.Humanlike;
        }

        // Returns true if the pawn can ever be a surrogate.
        public static bool MayEverBeSurrogate(Pawn pawn)
        {
            return pawn.kindDef.GetModExtension<SMN_PawnKindSkyMindExtension>()?.mayBeSurrogate != false && MayEverBeSurrogate(pawn.def);
        }

        // Returns true if the pawn has a Hediff that acts as a receiver via the SMN_HediffSkyMindExtension extension marking it as one.
        public static bool IsSurrogate(Pawn pawn)
        {
            List<Hediff> pawnHediffs = pawn.health.hediffSet.hediffs;
            for (int i = pawnHediffs.Count - 1; i >= 0; i--)
            {
                if (pawnHediffs[i].def.GetModExtension<SMN_HediffSkyMindExtension>()?.isReceiver ?? false)
                    return true;
            }
            return false;
        }

        // Makes a pawn into a surrogate by adding a receiver - either using the hediff provided or one pulled from the pawn's kind or race if not provided.
        // This also provides a hook for other mods to do extra behavior when a pawn is turned into a surrogate.
        public static void TurnIntoSurrogate(Pawn pawn, Hediff hediff = null, BodyPartRecord part = null, bool killingIntelligence = false)
        {
            // If this is the players's first produced surrogate, send a letter with information about surrogates.
            if (pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer && !gameComp.hasMadeSurrogate)
            {
                Find.LetterStack.ReceiveLetter("SMN_FirstSurrogateCreated".Translate(), "SMN_FirstSurrogateCreatedDesc".Translate(), LetterDefOf.NeutralEvent);
                gameComp.hasMadeSurrogate = true;
            }

            // Remove all transceivers from the pawn to ensure no surrogate + controller issues may arise.
            List<Hediff> pawnHediffs = pawn.health.hediffSet.hediffs;
            for (int i = pawnHediffs.Count - 1; i >= 0; i--)
            {
                if (pawnHediffs[i].def.GetModExtension<SMN_HediffSkyMindExtension>()?.isTransceiver ?? false)
                {
                    pawn.health.RemoveHediff(pawnHediffs[i]);
                }
            }

            // If a hediff is provided, assume it's the desired receiver implant.
            if (hediff != null)
            {
                pawn.health.AddHediff(hediff, part);
            }
            // Give the pawn their receiver implant.
            else
            {
                // First try to take from the pawn kind.
                if (pawn.kindDef.GetModExtension<SMN_PawnKindSkyMindExtension>() is SMN_PawnKindSkyMindExtension pawnKindExtension && pawnKindExtension.receiverImplant != null)
                {
                    pawn.health.AddHediff(pawnKindExtension.receiverImplant, pawn.health.hediffSet.GetBrain());
                }
                // Then try to take from the race.
                else if (pawn.def.GetModExtension<SMN_PawnSkyMindExtension>() is SMN_PawnSkyMindExtension thingDefExtension && thingDefExtension.defaultReceiverImplant != null)
                {
                    pawn.health.AddHediff(thingDefExtension.defaultReceiverImplant, pawn.health.hediffSet.GetBrain());
                }
                // If all else fails, give them the basic Receiver.
                else
                {
                    pawn.health.AddHediff(SMN_HediffDefOf.SMN_SkyMindReceiver, pawn.health.hediffSet.GetBrain());
                }
            }

            // If killingIntelligence is true, then the pawn should be a blank and be considered murdered. Blanks are never tethered.
            if (killingIntelligence)
            {
                Duplicate(GetBlank(), pawn, true, false);
            }
        }

        // Turns the recipient pawn into a blank pawn, and remove all SkyMind connecting hediffs if requested.
        public static void TurnIntoBlank(Pawn pawn, bool shouldRemoveSkyMindImplants = true)
        {
            Duplicate(GetBlank(), pawn, false, false);

            if (shouldRemoveSkyMindImplants)
            {
                // Ensure the pawn has no SkyMind capable implants any more.
                List<Hediff> pawnHediffs = pawn.health.hediffSet.hediffs;
                for (int i = pawnHediffs.Count - 1; i >= 0; i--)
                {
                    if (pawnHediffs[i].def.GetModExtension<SMN_HediffSkyMindExtension>()?.allowsConnection == true)
                    {
                        pawnHediffs.RemoveAt(i);
                    }
                }
            }

            // Killed SkyMind intelligences cease to exist.
            if (gameComp.GetCloudPawns().Contains(pawn))
            {
                gameComp.PopCloudPawn(pawn);
                pawn.Destroy();
            }
        }

        // Get a cached Blank pawn (to avoid having to create a new pawn whenever a surrogate is made, disconnects, downed, etc.)
        public static Pawn GetBlank()
        {
            if (blankPawn == null)
            {
                // Create the Blank pawn that will be used for all non-controlled surrogates, blank androids, etc.
                PawnGenerationRequest request = new PawnGenerationRequest(Faction.OfPlayer.def.basicMemberKind, null, PawnGenerationContext.PlayerStarter, canGeneratePawnRelations: false, forceNoIdeo: true, forceBaselinerChance: 1, colonistRelationChanceFactor: 0f, forceGenerateNewPawn: true, fixedGender: Gender.None);
                Pawn pawn = PawnGenerator.GeneratePawn(request);
                pawn.story.Childhood = SMN_BackstoryDefOf.SMN_BlankChildhood;
                pawn.story.Adulthood = SMN_BackstoryDefOf.SMN_BlankAdulthood;
                pawn.story.traits.allTraits.Clear();
                pawn.skills.Notify_SkillDisablesChanged();
                pawn.skills.skills.ForEach(delegate (SkillRecord record)
                {
                    record.passion = 0;
                    record.Level = 0;
                    record.xpSinceLastLevel = 0;
                    record.xpSinceMidnight = 0;
                });
                pawn.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
                pawn.workSettings.DisableAll();
                pawn.playerSettings = new Pawn_PlayerSettings(pawn)
                {
                    AreaRestriction = null,
                    hostilityResponse = HostilityResponseMode.Flee
                };
                if (ModsConfig.BiotechActive)
                {
                    for (int i = pawn.genes.GenesListForReading.Count - 1; i >= 0; i--)
                    {
                        pawn.genes.RemoveGene(pawn.genes.GenesListForReading[i]);
                    }
                }
                if (pawn.ideo != null)
                {
                    pawn.ideo.SetIdeo(null);
                }
                if (pawn.timetable == null)
                    pawn.timetable = new Pawn_TimetableTracker(pawn);
                if (pawn.playerSettings == null)
                    pawn.playerSettings = new Pawn_PlayerSettings(pawn);
                if (pawn.foodRestriction == null)
                    pawn.foodRestriction = new Pawn_FoodRestrictionTracker(pawn);
                if (pawn.drugs == null)
                    pawn.drugs = new Pawn_DrugPolicyTracker(pawn);
                if (pawn.outfits == null)
                    pawn.outfits = new Pawn_OutfitTracker(pawn);
                pawn.Name = new NameTriple("SMN_BlankPawnFirstName".Translate(), "SMN_BlankPawnNickname".Translate(), "SMN_BlankPawnLastName".Translate());
                blankPawn = pawn;
            }

            return blankPawn;
        }

        // Misc
        // When a SkyMind is breached, all users of the SkyMind receive a mood debuff. It is especially bad for direct victims.
        public static void ApplySkyMindAttack(IEnumerable<Pawn> victims = null, ThoughtDef forVictim = null, ThoughtDef forWitness = null)
        {
            try
            {
                // Victims were directly attacked by a hack and get a worse mood debuff
                if (victims != null && victims.Count() > 0)
                {
                    foreach (Pawn pawn in victims)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemoryFast(forVictim ?? SMN_ThoughtDefOf.SMN_AttackedViaSkyMind);
                    }
                }

                // Witnesses (connected to SkyMind but not targetted directly) get a minor mood debuff
                foreach (Thing thing in gameComp.GetSkyMindDevices())
                {
                    if (thing is Pawn pawn && (victims == null || !victims.Contains(pawn)))
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemoryFast(forWitness ?? SMN_ThoughtDefOf.SMN_AttackedViaSkyMind);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("[SMN] Error applying SkyMind attack mood thoughts. " + ex.Message + " " + ex.StackTrace);
            }
        }

        // Remove viruses from the provided things. While they are assumed to have viruses, no errors will occur if non-virused things are provided.
        public static void RemoveViruses(IEnumerable<Thing> virusedThings)
        {
            if (virusedThings == null)
            {
                return;
            }

            // Remove the viruses from each provided thing. No errors will occur if the thing does not have a SkyMind comp or does not have a virus.
            foreach (Thing virusedThing in virusedThings)
            {
                CompSkyMind csm = virusedThing.TryGetComp<CompSkyMind>();

                if (csm == null)
                    continue;
                csm.Breached = -1;
                gameComp.PopVirusedThing(virusedThing);
            }
        }

        // Utilities not available for direct player editing but not reserved by this mod
        public static SMN_GameComponent gameComp;

        // Duplicate the source pawn into the destination pawn. If overwriteAsDeath is true, then it is considered murdering the destination pawn.
        // if isTethered is true, then the duplicated pawn will actually share the class with the source so changing one will affect the other automatically.
        public static void Duplicate(Pawn source, Pawn dest, bool overwriteAsDeath=true, bool isTethered = true)
        {
            try
            {
                DuplicateStory(ref source, ref dest);

                DuplicateSkills(source, dest, isTethered);

                // If this duplication is considered to be killing a sapient individual, then handle some relations before they're duplicated.
                if (overwriteAsDeath)
                {
                    PawnDiedOrDownedThoughtsUtility.TryGiveThoughts(dest, null, PawnDiedOrDownedThoughtsKind.Died);
                    Pawn spouse = dest.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Spouse);
                    if (spouse != null && !spouse.Dead && spouse.needs.mood != null)
                    {
                        MemoryThoughtHandler memories = spouse.needs.mood.thoughts.memories;
                        memories.RemoveMemoriesOfDef(ThoughtDefOf.GotMarried);
                        memories.RemoveMemoriesOfDef(ThoughtDefOf.HoneymoonPhase);
                    }
                    Traverse.Create(dest.relations).Method("AffectBondedAnimalsOnMyDeath").GetValue();
                    dest.health.NotifyPlayerOfKilled(null, null, null);
                    dest.relations.ClearAllRelations();
                }

                // Duplicate relations.
                DuplicateRelations(source, dest, isTethered);

                // If Ideology dlc is active, duplicate pawn ideology into destination.
                if (ModsConfig.IdeologyActive)
                {
                    DuplicateIdeology(source, dest, isTethered);
                }

                // If Royalty dlc is active, then handle it. Royalty is non-transferable, but it should be checked for the other details that have been duplicated.
                if (ModsConfig.RoyaltyActive)
                {
                    DuplicateRoyalty(source, dest, isTethered);
                }

                // Duplicate faction. No difference if tethered or not.
                if (source.Faction != dest.Faction)
                    dest.SetFaction(source.Faction);

                // Duplicate source needs into destination. This is not tetherable.
                DuplicateNeeds(source, dest);

                // Only duplicate source settings for player pawns as foreign pawns don't need them. Can not be tethered as otherwise pawns would be forced to have same work/time/role settings.
                if (source.Faction != null && dest.Faction != null && source.Faction.IsPlayer && dest.Faction.IsPlayer)
                {
                    DuplicatePlayerSettings(source, dest);
                }

                // Duplicate source name into destination.
                NameTriple sourceName = (NameTriple)source.Name;
                dest.Name = new NameTriple(sourceName.First, sourceName.Nick, sourceName.Last);

                dest.Drawer.renderer.graphics.ResolveAllGraphics();
            }
            catch(Exception e)
            {
                Log.Error("[SMN] Utils.Duplicate: Error occurred duplicating " + source + " into " + dest + ". This will have severe consequences. " + e.Message + e.StackTrace);
            }
        }

        // Duplicate all appropriate details from the StoryTracker of the source into the destination.
        public static void DuplicateStory(ref Pawn source, ref Pawn dest)
        {
            if (source.story == null || dest.story == null)
            {
                Log.Warning("[SMN] A Storytracker for a duplicate operation was null. Destination story unchanged. This will have no further effects.");
                return;
            }

            try
            {
                // Clear all destination traits first to avoid issues. Only remove traits that are unspecific to genes.
                foreach (Trait trait in dest.story.traits.allTraits.ToList().Where(trait => trait.sourceGene == null))
                {
                    dest.story.traits.RemoveTrait(trait);
                }

                // Add all source traits to the destination. Only add traits that are unspecific to genes.
                foreach (Trait trait in source.story.traits?.allTraits.Where(trait => trait.sourceGene == null))
                {
                    dest.story.traits.GainTrait(new Trait(trait.def, trait.Degree, true));
                }

                // Copy some backstory related details, and double check work types and skill modifiers.
                dest.story.Childhood = source.story.Childhood;
                dest.story.Adulthood = source.story.Adulthood;
                dest.story.title = source.story.title;
                dest.story.favoriteColor = source.story.favoriteColor;
                dest.Notify_DisabledWorkTypesChanged();
                dest.skills.Notify_SkillDisablesChanged();
            }
            catch (Exception exception)
            {
                Log.Warning("[SMN] An unexpected error occurred during story duplication between " + source + " " + dest + ". The destination StoryTracker may be left unstable!" + exception.Message + exception.StackTrace);
            }
        }

        // Duplicate ideology details from the source to the destination.
        public static void DuplicateIdeology(Pawn source, Pawn dest, bool isTethered)
        {
            try
            {
                // If source ideology is null, then destination's ideology should also be null. Vanilla handles null ideologies relatively gracefully.
                if (source.ideo == null)
                {
                    dest.ideo = null;
                }
                // If untethered, copy the details of the ideology over, as a separate copy.
                else if (!isTethered)
                {
                    dest.ideo = new Pawn_IdeoTracker(dest);
                    dest.ideo.SetIdeo(source.Ideo);
                    dest.ideo.OffsetCertainty(source.ideo.Certainty - dest.ideo.Certainty);
                    dest.ideo.joinTick = source.ideo.joinTick;
                }
                // If tethered, the destination and source will share a single IdeologyTracker.
                else
                {
                    dest.ideo = source.ideo;
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[SMN] An unexpected error occurred during ideology duplication between " + source + " " + dest + ". The destination IdeoTracker may be left unstable!" + exception.Message + exception.StackTrace);
            }
        }

        // Royalty status can not actually be duplicated, but duplicating a pawn should still handle cases around royal abilities/details.
        public static void DuplicateRoyalty(Pawn source, Pawn dest, bool isTethered)
        {
            try
            {
                if (source.royalty != null)
                {
                    source.royalty.UpdateAvailableAbilities();
                    if (source.needs != null)
                        source.needs.AddOrRemoveNeedsAsAppropriate();
                    source.abilities.Notify_TemporaryAbilitiesChanged();
                }
                if (dest.royalty != null)
                {
                    dest.royalty.UpdateAvailableAbilities();
                    if (dest.needs != null)
                        dest.needs.AddOrRemoveNeedsAsAppropriate();
                    dest.abilities.Notify_TemporaryAbilitiesChanged();
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[SMN] An unexpected error occurred during royalty duplication between " + source + " " + dest + ". No further issues are anticipated." + exception.Message + exception.StackTrace);
            }
        }

        // Duplicate all skill levels, xp gains, and passions into the destination.
        public static void DuplicateSkills(Pawn source, Pawn dest, bool isTethered)
        {
            try
            {
                // If untethered, create a copy of the source SkillTracker for the destination to use.
                // Explicitly create a new skill tracker to avoid any issues with tethered skill trackers.
                if (!isTethered)
                {
                    Pawn_SkillTracker destSkills = new Pawn_SkillTracker(dest);
                    foreach (SkillDef skillDef in DefDatabase<SkillDef>.AllDefsListForReading)
                    {
                        SkillRecord newSkill = destSkills.GetSkill(skillDef);
                        SkillRecord sourceSkill = source.skills.GetSkill(skillDef);
                        newSkill.Level = sourceSkill.Level;
                        newSkill.passion = sourceSkill.passion;
                        newSkill.xpSinceLastLevel = sourceSkill.xpSinceLastLevel;
                        newSkill.xpSinceMidnight = sourceSkill.xpSinceMidnight;
                    }
                    dest.skills = destSkills;
                }
                // If tethered, the destination and source will share their skill tracker directly.
                else
                {
                    dest.skills = source.skills;
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[SMN] An unexpected error occurred during skill duplication between " + source + " " + dest + ". The destination SkillTracker may be left unstable!" + exception.Message + exception.StackTrace);
            }
        }

        // Duplicate relations from the source to the destination. This should also affect other pawn relations, and any animals involved.
        public static void DuplicateRelations(Pawn source, Pawn dest, bool isTethered)
        {
            try
            {
                // If untethered, copy all relations that involve the source pawn and apply them to the destination. As animals may have only one master, assign it to the destination.
                if (!isTethered)
                {
                    // Create a new tracker to avoid overwriting anything previously tethered and dealing with old data.
                    Pawn_RelationsTracker destRelations = new Pawn_RelationsTracker(dest);

                    List<Pawn> checkedOtherPawns = new List<Pawn>();
                    // Duplicate all of the source's relations. Ensure that other pawns with relations to the source also have them to the destination.
                    foreach (DirectPawnRelation pawnRelation in source.relations?.DirectRelations?.ToList())
                    {
                        // Ensure that we check the pawn relations for the opposite side only once to avoid doing duplicate relations.
                        if (!checkedOtherPawns.Contains(pawnRelation.otherPawn))
                        {
                            // Iterate through all of the other pawn's relations and copy any they have with the source onto the destination.
                            foreach (DirectPawnRelation otherPawnRelation in pawnRelation.otherPawn.relations?.DirectRelations.ToList())
                            {
                                if (otherPawnRelation.otherPawn == source)
                                {
                                    pawnRelation.otherPawn.relations?.AddDirectRelation(otherPawnRelation.def, dest);
                                }
                            }
                            checkedOtherPawns.Add(pawnRelation.otherPawn);
                        }
                        destRelations.AddDirectRelation(pawnRelation.def, pawnRelation.otherPawn);
                    }

                    destRelations.everSeenByPlayer = true;
                    dest.relations = destRelations;

                    // Transfer animal master status to destination
                    foreach (Map map in Find.Maps)
                    {
                        foreach (Pawn animal in map.mapPawns.SpawnedColonyAnimals)
                        {
                            if (animal.playerSettings == null)
                                continue;

                            if (animal.playerSettings.Master != null && animal.playerSettings.Master == source)
                                animal.playerSettings.Master = dest;
                        }
                    }
                }
                // Tether destination relations to the source.
                else
                {
                    dest.relations = source.relations;
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[SMN] An unexpected error occurred during relation duplication between " + source + " " + dest + ". The destination RelationTracker may be left unstable!" + exception.Message + exception.StackTrace);
            }
        }

        // Duplicate applicable needs from the source to the destination. This includes mood thoughts, memories, and ensuring it updates its needs as appropriate.
        public static void DuplicateNeeds(Pawn source, Pawn dest)
        {
            try
            {
                Pawn_NeedsTracker newNeeds = new Pawn_NeedsTracker(dest);
                if (source.needs?.mood != null)
                {
                    foreach (Thought_Memory memory in source.needs.mood.thoughts.memories.Memories)
                    {
                        newNeeds.mood.thoughts.memories.TryGainMemory(memory, memory.otherPawn);
                    }
                }
                dest.needs = newNeeds;
                dest.needs?.AddOrRemoveNeedsAsAppropriate();
                dest.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();
            }
            catch (Exception exception)
            {
                Log.Warning("[SMN] An unexpected error occurred during need duplication between " + source + " " + dest + ". The destination NeedTracker may be left unstable!" + exception.Message + exception.StackTrace);
            }
        }

        public static void DuplicatePlayerSettings(Pawn source, Pawn dest)
        {
            try
            {
                // Initialize source work settings if not initialized.
                if (source.workSettings == null)
                {
                    source.workSettings = new Pawn_WorkSettings(source);
                }
                source.workSettings.EnableAndInitializeIfNotAlreadyInitialized();

                // Initialize destination work settings if not initialized.
                if (dest.workSettings == null)
                {
                    dest.workSettings = new Pawn_WorkSettings(dest);
                }
                dest.workSettings.EnableAndInitializeIfNotAlreadyInitialized();

                // Apply work settings to destination from the source
                if (source.workSettings != null && source.workSettings.EverWork)
                {
                    foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                    {
                        if (!dest.WorkTypeIsDisabled(workTypeDef))
                            dest.workSettings.SetPriority(workTypeDef, source.workSettings.GetPriority(workTypeDef));
                    }
                }

                // Duplicate source restrictions from into destination.
                for (int i = 0; i != 24; i++)
                {
                    dest.timetable.SetAssignment(i, source.timetable.GetAssignment(i));
                }

                dest.playerSettings = new Pawn_PlayerSettings(dest);
                dest.playerSettings.AreaRestriction = source.playerSettings.AreaRestriction;
                dest.playerSettings.hostilityResponse = source.playerSettings.hostilityResponse;
                dest.outfits = new Pawn_OutfitTracker(dest);
                dest.outfits.CurrentOutfit = source.outfits.CurrentOutfit;
            }
            catch (Exception exception)
            {
                Log.Warning("[SMN] An unexpected error occurred during player setting duplication between " + source + " " + dest + ". The destination PlayerSettings may be left unstable!" + exception.Message + exception.StackTrace);
            }
        }

        public static void PermutePawn(Pawn firstPawn, Pawn secondPawn)
        {
            try
            {
                if (firstPawn == null || secondPawn == null)
                    return;

                // Permute all major mind-related components to each other via a temp copy.
                PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.Colonist, null, PawnGenerationContext.PlayerStarter, forceGenerateNewPawn: true);
                Pawn tempCopy = PawnGenerator.GeneratePawn(request);
                Duplicate(firstPawn, tempCopy, false, false);
                Duplicate(secondPawn, firstPawn, false, false);
                Duplicate(tempCopy, secondPawn, false, false);


                // Swap all log entries between the two pawns as appropriate.
                foreach (LogEntry log in Find.PlayLog.AllEntries)
                {
                    if (log.Concerns(firstPawn) || log.Concerns(secondPawn))
                    {
                        Traverse tlog = Traverse.Create(log);
                        Pawn initiator = tlog.Field("initiator").GetValue<Pawn>();
                        Pawn recipient = tlog.Field("recipient").GetValue<Pawn>();

                        if (initiator == firstPawn)
                            initiator = secondPawn;
                        else if (initiator == secondPawn)
                            initiator = firstPawn;

                        if (recipient == secondPawn)
                            recipient = secondPawn;
                        else if (recipient == firstPawn)
                            recipient = secondPawn;

                        tlog.Field("initiator").SetValue(initiator);
                        tlog.Field("recipient").SetValue(recipient);
                    }
                }
            }
            catch(Exception e)
            {
                Log.Message("[SMN] Utils.PermutePawn : " + e.Message + " - " + e.StackTrace);
            }
        }

        // Check if the targetted pawn is a valid target for receiving mind transfer operations.
        public static bool IsValidMindTransferTarget(Pawn pawn)
        {
            // Only player pawns that are connected to the SkyMind, not suffering from a security breach, and not currently in a SkyMind operation are legal targets.
            if ((pawn.Faction != null && pawn.Faction != Faction.OfPlayer && pawn.HostFaction != Faction.OfPlayer) || !gameComp.HasSkyMindConnection(pawn) || pawn.GetComp<CompSkyMind>().Breached != -1 || pawn.GetComp<CompSkyMindLink>().Linked > -1)
            {
                return false;
            }

            // Pawns afflicted with a Hediff that prevents SkyMind connections, or who are already subjects of mind operations, are not permissible targets for mind operations.
            List<Hediff> targetHediffs = pawn.health.hediffSet.hediffs;
            bool hasImplantEnablingTransfer = false;
            for (int i = targetHediffs.Count - 1; i >= 0; i--)
            {
                SMN_HediffSkyMindExtension skyMindExtension = targetHediffs[i].def.GetModExtension<SMN_HediffSkyMindExtension>();
                if (skyMindExtension?.blocksConnection == true)
                {
                    return false;
                }
                else if (skyMindExtension?.isTransceiver == true || skyMindExtension?.isReceiver == true)
                {
                    hasImplantEnablingTransfer = true;
                }
            }

            // If the pawn has a cloud capable implant or is in the SkyMind network already, then it is valid.
            return hasImplantEnablingTransfer;
        }

        // Returns a list of all surrogates without hosts in caravans. Return null if there are none.
        public static IEnumerable<Pawn> GetHostlessCaravanSurrogates()
        {
            // If surrogates aren't allowed, there can be no hostless surrogates.
            if (!SkyMindNetwork_Settings.surrogatesAllowed)
                return null;

            HashSet<Pawn> hostlessSurrogates = new HashSet<Pawn>();
            foreach (Caravan caravan in Find.World.worldObjects.Caravans)
            {
                foreach (Pawn pawn in caravan.pawns)
                {
                    if (IsSurrogate(pawn) && !pawn.GetComp<CompSkyMindLink>().HasSurrogate())
                    {
                        hostlessSurrogates.AddItem(pawn);
                    }
                }
            }
            return hostlessSurrogates.Count == 0 ? null : hostlessSurrogates;
        }

        // Create as close to a perfect copy of the provided pawn as possible. If kill is true, then we're trying to make a corpse copy of it.
        public static Pawn SpawnCopy(Pawn pawn, bool kill=true)
        {
            // Generate a new pawn.
            PawnGenerationRequest request = new PawnGenerationRequest(pawn.kindDef, faction: null, context: PawnGenerationContext.NonPlayer, canGeneratePawnRelations: false, fixedBiologicalAge: pawn.ageTracker.AgeBiologicalYearsFloat, fixedChronologicalAge: pawn.ageTracker.AgeChronologicalYearsFloat, fixedGender: pawn.gender);
            Pawn copy = PawnGenerator.GeneratePawn(request);

            // Gene generation is a bit strange, so we manually handle it ourselves.
            copy.genes = new Pawn_GeneTracker(copy);
            copy.genes.SetXenotypeDirect(pawn.genes?.Xenotype);
            foreach (Gene gene in pawn.genes?.Xenogenes)
            {
                copy.genes.AddGene(gene.def, true);
            }
            foreach (Gene gene in pawn.genes?.Endogenes)
            {
                copy.genes.AddGene(gene.def, false);
            }
            // Melanin is controlled via genes. If the pawn has one, use it. Otherwise just take whatever skinColorBase the pawn has.
            if (copy.genes?.GetMelaninGene() != null && pawn.genes?.GetMelaninGene() != null)
            {
                copy.genes.GetMelaninGene().skinColorBase = pawn.genes.GetMelaninGene().skinColorBase;
            }
            copy.story.skinColorOverride = pawn.story?.skinColorOverride;
            copy.story.SkinColorBase = pawn.story.SkinColorBase;

            // Get rid of any items it may have spawned with.
            copy.equipment?.DestroyAllEquipment();
            copy.apparel?.DestroyAll();
            copy.inventory?.DestroyAll();

            // Copy the pawn's physical attributes.
            copy.Rotation = pawn.Rotation;
            copy.story.bodyType = pawn.story.bodyType;
            copy.story.HairColor = pawn.story.HairColor;
            copy.story.hairDef = pawn.story.hairDef;

            // Attempt to transfer all items the pawn may be carrying over to its copy.
            if (pawn.inventory != null && pawn.inventory.innerContainer != null && copy.inventory != null && copy.inventory.innerContainer != null)
            {
                try
                {
                    pawn.inventory.innerContainer.TryTransferAllToContainer(copy.inventory.innerContainer);
                }
                catch (Exception ex)
                {
                    Log.Error("[SMN] Utils.SpawnCopy.TransferInventory " + ex.Message + " " + ex.StackTrace);
                }
            }

            // Attempt to transfer all equipment the pawn may have to the copy.
            if (pawn.equipment != null && copy.equipment != null)
            {
                foreach (ThingWithComps equipment in pawn.equipment.AllEquipmentListForReading.ToList())
                {
                    try
                    {
                        pawn.equipment.Remove(equipment);
                        copy.equipment.AddEquipment(equipment);
                    }
                    catch (Exception ex)
                    {
                        Log.Message("[SMN] Utils.SpawnCopy.TransferEquipment " + ex.Message + " " + ex.StackTrace);
                    }
                }
            }

            // Transfer all apparel from the pawn to the copy.
            if (pawn.apparel != null)
            {
                foreach (Apparel apparel in pawn.apparel.WornApparel.ToList())
                {
                    pawn.apparel.Remove(apparel);
                    copy.apparel.Wear(apparel);
                }
            }

            // Copy all hediffs from the pawn to the copy. Remove the hediff from the host to ensure it isn't saved across both pawns.
            copy.health.RemoveAllHediffs();
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs.ToList())
            {
                try
                {
                    if (hediff.def != HediffDefOf.MissingBodyPart && hediff.def != SMN_HediffDefOf.SMN_MindOperation)
                    {
                        hediff.pawn = copy;
                        copy.health.AddHediff(hediff, hediff.Part);
                        pawn.health.RemoveHediff(hediff);
                    }
                }
                catch(Exception ex)
                {
                    Log.Error("[SMN] Utils.SpawnCopy.TransferHediffs " + ex.Message + " " + ex.StackTrace);
                }
            }

            // If we are "killing" the pawn, that means the body is now a blank. Properly duplicate those features.
            if (kill)
            {
                TurnIntoBlank(copy);
                copy.health.AddHediff(SMN_HediffDefOf.SMN_FeedbackLoop);
            }
            // Else, duplicate all mind-related things to the copy. This is not considered murder.
            else
            {
                Duplicate(pawn, copy, false, false);
            }

            // Spawn the copy.
            GenSpawn.Spawn(copy, pawn.Position, pawn.Map);

            // Draw the copy.
            copy.Drawer.renderer.graphics.ResolveAllGraphics();
            return copy;
        }

        // Calculate the number of skill points required in order to give a pawn a new passion.
        public static int GetSkillPointsToIncreasePassion(Pawn pawn, int passionCount)
        {
            // Assign base cost based on settings. Default is 5000.
            float result = SkyMindNetwork_Settings.basePointsNeededForPassion;

            // Multiply result by the pawn's global learning factor (inverse relation, as higher learning factor should reduce cost).
            result *= 1 / pawn.GetStatValue(StatDef.Named("GlobalLearningFactor"));

            if (passionCount > SkyMindNetwork_Settings.passionSoftCap)
            { // If over the soft cap for number of passions, each additional passion adds 25% cost to buying another passion.
                result *= (float) Math.Pow(1.25, passionCount - SkyMindNetwork_Settings.passionSoftCap);
            }

            // Return the end result as an integer for nice display numbers and costs.
            return (int) result;
        }
    }
}
