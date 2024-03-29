﻿using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace SkyMind
{
    public class SMN_GameComponent : GameComponent
    {

        public SMN_GameComponent(Game game)
        {
            SMN_Utils.gameComp = this;
            AllocateIfNull();
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();

            AllocateIfNull();
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref skillPointCapacity, "SMN_skillPointCapacity", 0);
            Scribe_Values.Look(ref skillPoints, "SMN_skillPoints", 0);
            Scribe_Values.Look(ref securityPointCapacity, "SMN_securityPointCapacity", 0);
            Scribe_Values.Look(ref securityPoints, "SMN_securityPoints", 0);
            Scribe_Values.Look(ref hackingPointCapacity, "SMN_hackingPointCapacity", 0);
            Scribe_Values.Look(ref hackingPoints, "SMN_hackingPoints", 0);
            Scribe_Values.Look(ref SkyMindNetworkCapacity, "SMN_SkyMindNetworkCapacity", 0);
            Scribe_Values.Look(ref SkyMindCloudCapacity, "SMN_SkyMindCloudCapacity", 0);
            Scribe_Values.Look(ref hackCostTimePenalty, "SMN_hackCostTimePenalty", 0);
            Scribe_Values.Look(ref cachedSkillGeneration, "SMN_cachedSkillGeneration", 0);
            Scribe_Values.Look(ref cachedSecurityGeneration, "SMN_cachedSecurityGeneration", 0);
            Scribe_Values.Look(ref cachedHackingGeneration, "SMN_cachedHackingGeneration", 0);
            Scribe_Values.Look(ref hasBuiltAndroid, "SMN_hasBuiltAndroid", false);
            Scribe_Values.Look(ref hasBuiltDrone, "SMN_hasBuiltDrone", false);
            Scribe_Values.Look(ref hasMadeSurrogate, "SMN_hasMadeSurrogate", false);

            Scribe_Collections.Look(ref skillServers, "SMN_skillServers", LookMode.Reference);
            Scribe_Collections.Look(ref securityServers, "SMN_securityServers", LookMode.Reference);
            Scribe_Collections.Look(ref hackingServers, "SMN_hackingServers", LookMode.Reference);
            Scribe_Collections.Look(ref networkedDevices, "SMN_networkedDevices", LookMode.Reference);
            Scribe_Collections.Look(ref cloudPawns, "SMN_cloudPawns", LookMode.Deep);
            Scribe_Collections.Look(ref virusedDevices, "SMN_virusedDevices", LookMode.Reference, LookMode.Value, ref thingKeyCopy, ref thingValueCopy);
            Scribe_Collections.Look(ref networkLinkedPawns, "SMN_networkLinkedPawns", LookMode.Reference, LookMode.Value, ref pawnKeyCopy, ref pawnValueCopy);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                AllocateIfNull();
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            int CGT = Find.TickManager.TicksGame;

            // Check virus and mind operation timers every 10 seconds.
            if(CGT % 600 == 0)
            {
                CheckVirusedThings();
                CheckNetworkLinkedPawns();
                CheckServers();
            }
            if (CGT % 6000 == 0)
            {
                CheckHackTimePenalty();
            }
        }

        // Check the hack timer penalty and reduce it if it is non-zero.
        public void CheckHackTimePenalty()
        {
            if (hackCostTimePenalty > 0)
            {
                // The penalty decays by 1% every 6000 ticks. If it is small enough, simply set the penalty to 0.
                float decayedPenalty = hackCostTimePenalty * 0.99f;
                if (decayedPenalty <= 10)
                {
                    hackCostTimePenalty = 0;
                }
                else
                {
                    hackCostTimePenalty = (int)decayedPenalty;
                }
            }
        }

        // Check to see if any virused things have elapsed their infected timers. Remove viruses that have elapsed.
        public void CheckVirusedThings()
        {
            if (virusedDevices.Count == 0)
                return;

            int GT = Find.TickManager.TicksGame;

            // We make it into a list here so that it stores a copy. If we didn't, the list may change size as virusedDevices thins while it is still running, throwing an exception.
            foreach (KeyValuePair<Thing, int> virusedDevice in virusedDevices.ToList())
            {
                // If for whatever reason the device has no CompSkyMind, pass over it.
                CompSkyMind csm = virusedDevice.Key.TryGetComp<CompSkyMind>();
                if (csm == null)
                    continue;

                // Timer has expired. Inform it that it is no longer virused. The CompSkyMind will handle the rest.
                if (virusedDevice.Value != -1 && virusedDevice.Value <= GT )
                {
                    csm.Breached = -1;
                }
            }
        }

        // Check to see if any SkyMind operations have elapsed their timers. Remove operations that have elapsed and handle any interrupts.
        public void CheckNetworkLinkedPawns()
        {
            if (networkLinkedPawns.Count == 0)
                return;

            int GT = Find.TickManager.TicksGame;


            // We make it into a list here so that it stores a copy. If we didn't, the list may change size as networkedLinkedPawns thins while it is still running, throwing an exception.
            foreach (KeyValuePair<Pawn, int> networkLinkedPawn in networkLinkedPawns.ToList())
            {
                // If for whatever reason the device has no CompSkyMindLink, pass over it.
                CompSkyMindLink cso = networkLinkedPawn.Key.GetComp<CompSkyMindLink>();
                if (cso == null)
                    continue;

                // Timer has expired. Inform it that it the SkyMind operation has ended. The CompSurrogateOwner will handle the rest.
                if (networkLinkedPawn.Value != -1 && networkLinkedPawn.Value <= GT)
                {
                    cso.Linked = -1;
                    continue;
                }

                // Check to see if the operation has been interrupted for any reason.
                cso.CheckInterruptedUpload();
            }
        }

        // Add the cached point generations from active servers. When servers are added or removed, the cached amount is recalculated automatically.
        public void CheckServers()
        {
            ChangeServerPoints(cachedSkillGeneration, SMN_ServerType.SkillServer);
            ChangeServerPoints(cachedSecurityGeneration, SMN_ServerType.SecurityServer);
            ChangeServerPoints(cachedHackingGeneration, SMN_ServerType.HackingServer);
        }

        // Calculate the point capacity and point generation for the given server type from the appropriate server list. Cache results for easy usage elsewhere.
        public void ResetServers(SMN_ServerType type)
        {
            CompComputer compComputer;
            CompSuperComputer compSuperComputer;
            switch (type)
            {
                case SMN_ServerType.SkillServer:
                    skillPointCapacity = 0;
                    cachedSkillGeneration = 0;

                    // Calculate Skill server points using CompComputer or CompSuperComputer (or both if it has them). Cache results.
                    foreach (Building building in skillServers.ToList())
                    {
                        compComputer = building.GetComp<CompComputer>();
                        compSuperComputer = building.GetComp<CompSuperComputer>();
                        if (compComputer != null)
                        {
                            skillPointCapacity += compComputer.Props.pointStorage;
                            cachedSkillGeneration += compComputer.Props.passivePointGeneration;
                        }
                        if (compSuperComputer != null)
                        {
                            skillPointCapacity += compSuperComputer.Props.pointStorage;
                            cachedSkillGeneration += compSuperComputer.Props.passivePointGeneration;
                        }
                    }
                    break;
                case SMN_ServerType.SecurityServer:
                    securityPointCapacity = 0;
                    cachedSecurityGeneration = 0;

                    // Calculate Security server points using CompComputer or CompSuperComputer (or both if it has them). Cache results.
                    foreach (Building building in securityServers.ToList())
                    {
                        compComputer = building.GetComp<CompComputer>();
                        compSuperComputer = building.GetComp<CompSuperComputer>();
                        if (compComputer != null)
                        {
                            securityPointCapacity += compComputer.Props.pointStorage;
                            cachedSecurityGeneration += compComputer.Props.passivePointGeneration;
                        }
                        if (compSuperComputer != null)
                        {
                            securityPointCapacity += compSuperComputer.Props.pointStorage;
                            cachedSecurityGeneration += compSuperComputer.Props.passivePointGeneration;
                        }
                    }
                    break;
                case SMN_ServerType.HackingServer:
                    hackingPointCapacity = 0;
                    cachedHackingGeneration = 0;

                    // Calculate Hacking server points using CompComputer or CompSuperComputer (or both if it has them). Cache results.
                    foreach (Building building in hackingServers)
                    {
                        compComputer = building.GetComp<CompComputer>();
                        compSuperComputer = building.GetComp<CompSuperComputer>();
                        if (compComputer != null)
                        {
                            hackingPointCapacity += compComputer.Props.pointStorage;
                            cachedHackingGeneration += compComputer.Props.passivePointGeneration;
                        }
                        if (compSuperComputer != null)
                        {
                            hackingPointCapacity += compSuperComputer.Props.pointStorage;
                            cachedHackingGeneration += compSuperComputer.Props.passivePointGeneration;
                        }
                    }
                    break;
                default:
                    Log.Error("[SMN] SMN_GC.ResetServers: Attempted illegal server type reset. No changes made. This may generate errors.");
                    return;
            }
        }

        // This will reset all point servers of all categories when called.
        public void ResetServers()
        {
            ResetServers(SMN_ServerType.SkillServer);
            ResetServers(SMN_ServerType.SecurityServer);
            ResetServers(SMN_ServerType.HackingServer);
        }

        public float GetPointCapacity(SMN_ServerType pointMode)
        {
            switch (pointMode)
            {
                case SMN_ServerType.SkillServer:
                    return skillPointCapacity;
                case SMN_ServerType.SecurityServer:
                    return securityPointCapacity;
                case SMN_ServerType.HackingServer:
                    return hackingPointCapacity;
                default:
                    return 0;
            }
        }

        public float GetPoints(SMN_ServerType pointMode)
        {
            switch (pointMode)
            {
                case SMN_ServerType.SkillServer:
                    return skillPoints;
                case SMN_ServerType.SecurityServer:
                    return securityPoints;
                case SMN_ServerType.HackingServer:
                    return hackingPoints;
                default:
                    return 0;
            }
        }

        // Determine if the provided pawn is connected to the SkyMind Network.
        public bool HasSkyMindConnection(Pawn pawn)
        {
            return networkedDevices.Contains(pawn) || cloudPawns.Contains(pawn);
        }

        // Attempt to connect the provided thing to the SkyMind network.
        public bool AttemptSkyMindConnection(Thing thing)
        {
            // If the thing is already connected, return true.
            if (networkedDevices.Contains(thing))
            {
                return true;
            }

            // If there is no available space in the network, return false. No connection is made.
            if (networkedDevices.Count >= SkyMindNetworkCapacity)
            {
                return false;
            }

            // Add the device to a list of devices in the SkyMind.
            networkedDevices.Add(thing);

            // Inform the comps of the pawn that the user successfully connected. This configures details like the SkyMind assistance hediff and a few bools.
            ((ThingWithComps)thing).BroadcastCompSignal("SkyMindNetworkUserConnected");
            return true;
        }

        // Disconnect the provided thing from the SkyMind network. This does nothing if it wasn't connected already.
        public void DisconnectFromSkyMind(Thing thing)
        {
            if (networkedDevices.Contains(thing))
            {
                // Remove the device from the network.
                networkedDevices.Remove(thing);
                // Inform all comps that the thing is no longer connected to the SkyMind network.
                ((ThingWithComps)thing).BroadcastCompSignal("SkyMindNetworkUserDisconnected");
            }
        }

        public HashSet<Thing> GetSkyMindDevices()
        {
            return networkedDevices;
        }

        public bool HasNetworkedPawn()
        {
            return GetSkyMindDevices().Any(thing => thing is Pawn);
        }

        public bool HasSkyMindCore()
        {
            return SkyMindCloudCapacity > 0;
        }

        // Add capacity to the SkyMind for networked devices.
        public void AddTower(CompSkyMindTower tower)
        {
            SkyMindNetworkCapacity += tower.Props.SkyMindSlotsProvided;
        }

        // Remove capacity to the SkyMind for networked devices. Disconnect random devices if this would result in exceeding the limit.
        public void RemoveTower(CompSkyMindTower tower)
        {
            SkyMindNetworkCapacity -= tower.Props.SkyMindSlotsProvided;

            while (SkyMindNetworkCapacity < networkedDevices.Count && networkedDevices.Count > 0)
            {
                Thing device = networkedDevices.RandomElement();
                DisconnectFromSkyMind(device);
            }
        }

        // Return the current total capacity of SkyMind Towers currently active.
        public int GetSkyMindNetworkSlots()
        {
            return SkyMindNetworkCapacity;
        }

        // Add capacity to the SkyMind for cloud-stored pawns.
        public void AddCore(int capacity)
        {
            SkyMindCloudCapacity += capacity;
        }

        // Remove capacity to the SkyMind for cloud-stored pawns. Murder random stored intelligences if this would result in exceeding the limit.
        public void RemoveCore(int capacity)
        {
            SkyMindCloudCapacity -= capacity;

            while (SkyMindCloudCapacity < cloudPawns.Count && cloudPawns.Count > 0)
            {
                // Killing the pawn will automatically handle any interrupted mind operations or surrogate connections.
                Pawn victim = cloudPawns.RandomElement();
                cloudPawns.Remove(victim);
                victim.Kill(null);
            }
        }

        // Return the maximum number of cloud pawns that may be stored in the SkyMind network currently.
        public int GetSkyMindCloudCapacity()
        {
            return SkyMindCloudCapacity;
        }

        // Add a server to the appropriate list based on serverMode
        public void AddServer(Building building, SMN_ServerType serverMode)
        {
            switch (serverMode)
            {
                case SMN_ServerType.SkillServer:
                    skillServers.Add(building);
                    ResetServers(SMN_ServerType.SkillServer);
                    break;
                case SMN_ServerType.SecurityServer:
                    securityServers.Add(building);
                    ResetServers(SMN_ServerType.SecurityServer);
                    break;
                case SMN_ServerType.HackingServer:
                    hackingServers.Add(building);
                    ResetServers(SMN_ServerType.HackingServer);
                    break;
                default:
                    Log.Message("[SMN] Attempted to add a server of an invalid mode. No server was added. The building responsible should have a Gizmo to fix the issue.");
                    return;
            }
        }

        // If a capacity is provided instead of a serverMode, assume this capacity is to be added to all servers
        public void AddServer(Building building)
        {
            skillServers.Add(building);
            securityServers.Add(building);
            hackingServers.Add(building);
            ResetServers();
        }

        // Remove the building from the task group it is assigned to.
        public void RemoveServer(Building building, SMN_ServerType serverMode)
        {
            switch (serverMode)
            {
                case SMN_ServerType.SkillServer: // Remove from skill servers list.
                    if (skillServers.Contains(building))
                    {
                        skillServers.Remove(building);
                        ResetServers(SMN_ServerType.SkillServer);
                    }
                    break;
                case SMN_ServerType.SecurityServer: // Remove from security servers list.
                    if (securityServers.Contains(building))
                    {
                        securityServers.Remove(building);
                        ResetServers(SMN_ServerType.SecurityServer);
                    }
                    break;
                case SMN_ServerType.HackingServer: // Remove from hacking servers list.
                    if (hackingServers.Contains(building))
                    {
                        hackingServers.Remove(building);
                        ResetServers(SMN_ServerType.HackingServer);
                    }
                    break;
                default:
                    Log.Error("[SMN] SMN_GC.RemoveServer was given an invalid SMN_ServerType. All servers recached.");
                    ResetServers();
                    return;
            }
        }

        // If a capacity is provided instead of a serverMode, assume this capacity is to removed from all servers
        public void RemoveServer(Building building)
        {
            skillServers.Remove(building);
            securityServers.Remove(building);
            hackingServers.Remove(building);
            ResetServers();
        }

        // This always adds the points to the appropriate category. It assumes negative changes are given in the parameter. It also handles illegal types (do nothing).
        // It handles numbers going out of bounds by ensuring it doesn't drop below 0 or go above the capacity.
        public void ChangeServerPoints(float toChange, SMN_ServerType serverMode)
        {
            switch (serverMode)
            {
                case SMN_ServerType.None:
                    Log.Error("[SMN] Can't add points to a None server type! No points changed.");
                    return;
                case SMN_ServerType.SkillServer:
                    skillPoints = Mathf.Clamp(skillPoints + toChange, 0, skillPointCapacity);
                    break;
                case SMN_ServerType.SecurityServer:
                    securityPoints = Mathf.Clamp(securityPoints + toChange, 0, securityPointCapacity);
                    break;
                case SMN_ServerType.HackingServer:
                    hackingPoints = Mathf.Clamp(hackingPoints + toChange, 0, hackingPointCapacity);
                    break;
            }
        }

        // Add a new virused thing to the dictionary. If it was already contained there, update the endTick. If it wasn't, add it in with the key and endTick provided.
        public void PushVirusedThing(Thing thing, int endTick)
        {
            if (virusedDevices.ContainsKey(thing))
            {
                virusedDevices[thing] = endTick;
            }
            else
            {
                virusedDevices.Add(thing, endTick);
            }
        }

        // Remove a virused thing from the dictionary. Nothing happens if it already wasn't contained.
        public void PopVirusedThing(Thing thing)
        {
            if (virusedDevices.ContainsKey(thing))
                virusedDevices.Remove(thing);
        }

        // Get the virus end timer for the given device. Return -1 if it wasn't found.
        public int GetVirusedDevice(Thing device)
        {
            if (!virusedDevices.ContainsKey(device))
                return -1;
            return virusedDevices[device];
        }

        // Return all virused devices. Keys are the virused devices themselves, the ints represent how long until they are released.
        public Dictionary<Thing, int> GetAllVirusedDevices()
        {
            return virusedDevices;
        }

        // Add a new network linked pawn to the dictionary. If it was already contained there, update the endTick. If it wasn't, add it in with the key and endTick provided.
        public void PushNetworkLinkedPawn(Pawn pawn, int endTick)
        {
            if (networkLinkedPawns.ContainsKey(pawn))
            {
                networkLinkedPawns[pawn] = endTick;
            }
            else
            {
                networkLinkedPawns.Add(pawn, endTick);
            }
        }

        // Remove a network linked pawn from the dictionary. Nothing happens if it already wasn't contained.
        public void PopNetworkLinkedPawn(Pawn pawn)
        {
            if (networkLinkedPawns.ContainsKey(pawn))
                networkLinkedPawns.Remove(pawn);
        }

        // Get the mind operation end timer for the given device. Return -2 if it wasn't found.
        public int GetLinkedPawn(Pawn device)
        {
            if (!networkLinkedPawns.ContainsKey(device))
                return -2;
            return networkLinkedPawns[device];
        }

        // Return all virused devices. Keys are the virused devices themselves, the ints represent how long until they are released.
        public Dictionary<Pawn, int> GetAllLinkedPawns()
        {
            return networkLinkedPawns;
        }

        // Add a new pawn to the cloud set. If it was already contained there, do nothing.
        public void PushCloudPawn(Pawn pawn)
        {
            if (!cloudPawns.Contains(pawn))
            {
                cloudPawns.Add(pawn);
            }
        }

        // Remove a pawn from the cloud set. Nothing happens if it already wasn't contained.
        public void PopCloudPawn(Pawn pawn)
        {
            if (cloudPawns.Contains(pawn))
                cloudPawns.Remove(pawn);
        }

        // Return all of the things in the cloud set.
        public HashSet<Pawn> GetCloudPawns()
        {
            return cloudPawns;
        }

        private void AllocateIfNull()
        {
            if (skillServers == null)
                skillServers = new HashSet<Building>();
            if (securityServers == null)
                securityServers = new HashSet<Building>();
            if (hackingServers == null)
                hackingServers = new HashSet<Building>();
            if (virusedDevices == null)
                virusedDevices = new Dictionary<Thing, int>();
            if (networkLinkedPawns == null)
                networkLinkedPawns = new Dictionary<Pawn, int>();
            if (cloudPawns == null)
                cloudPawns = new HashSet<Pawn>();
        }

        // Floats for storing capacities of various comps. Servers also have current point values.
        private float skillPointCapacity = 0;
        private float skillPoints = 0;
        private float securityPointCapacity = 0;
        private float securityPoints = 0;
        private float hackingPointCapacity = 0;
        private float hackingPoints = 0;
        private int SkyMindNetworkCapacity = 0;
        private int SkyMindCloudCapacity = 0;

        // Cached point generation amounts for efficiency. Recalculated when servers are added/removed.
        private float cachedSkillGeneration = 0;
        private float cachedSecurityGeneration = 0;
        private float cachedHackingGeneration = 0;

        // Simple tracker for the extra cost penalty for initiating player hacks after having done one recently.
        public int hackCostTimePenalty = 0;

        // Networked devices are things that are connected to the SkyMind network, including free pawns, surrogates, and buildings.
        public HashSet<Thing> networkedDevices = new HashSet<Thing>();

        // Temperature sensitive devices that should be tracked for alert purposes.
        public HashSet<ThingWithComps> temperatureSensitiveDevices = new HashSet<ThingWithComps>();

        // Cloud Pawns are pawns that are stored in the SkyMind Network. This is important as their gizmo's are inaccessible and can't be connected to the SkyMind (but should be considered as if they are).
        private HashSet<Pawn> cloudPawns = new HashSet<Pawn>();

        // Servers have 3 different active states they may be in, and may be saved/changed independently of each other. They must also be saved so points may be generated.
        private HashSet<Building> skillServers = new HashSet<Building>();
        private HashSet<Building> securityServers = new HashSet<Building>();
        private HashSet<Building> hackingServers = new HashSet<Building>();

        // Virused devices are things with their values being the tick at which to release them. This avoids the CompSkyMind having to store this information.
        private Dictionary<Thing, int> virusedDevices = new Dictionary<Thing, int>();

        // Network Linked devices are things (pawns) that are currently undergoing some sort of SkyMind procedure like uploading, duplicating, etc. The value stores the tick at which to release them.
        private Dictionary<Pawn, int> networkLinkedPawns = new Dictionary<Pawn, int>();

        // Local reserved storage for saving/loading virusedDevices and newtorkedLinkedPawns in the ExposeData method.
        private List<Thing> thingKeyCopy = new List<Thing>();
        private List<int> thingValueCopy = new List<int>();
        private List<Pawn> pawnKeyCopy = new List<Pawn>();
        private List<int> pawnValueCopy = new List<int>();

        // Simple booleans for whether players have encountered some mechanics yet to display educational letters. Some letters are handled by researches.
        public bool hasBuiltDrone = false;
        public bool hasBuiltAndroid = false;
        public bool hasMadeSurrogate = false;

    }
}