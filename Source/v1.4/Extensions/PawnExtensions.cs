using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SkyMind
{
    // Mod extension for races to control some features. These attributes are only used for humanlikes, there is no reason to provide any to non-humanlikes.
    // The existence of this extension on a ThingDef allows it to be used as a surrogate.
    public class SMN_PawnSkyMindExtension : DefModExtension
    {
        // Defs for what hediffs are the standard to use for controller/receiver implants for SkyMind-related spawning purposes for the race if the pawn kind extension does not specify one.
        public HediffDef defaultTransceiverImplant;
        public HediffDef defaultReceiverImplant;

        public override IEnumerable<string> ConfigErrors()
        {
            if (defaultTransceiverImplant == null)
            {
                yield return "[SMN] A ThingDef was given a sky mind extension to allow it to use the network but did not specify the default transceiver hediff for it";
            }
            if (defaultReceiverImplant == null)
            {
                yield return "[SMN] A ThingDef was given a sky mind extension to allow it to use the network but did not specify the default receiver hediff for it";
            }
        }
    }
}