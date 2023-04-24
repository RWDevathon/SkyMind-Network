using System.Collections.Generic;
using Verse;

namespace SkyMind
{
    // Mod extension for pawn kinds to control some features. These attributes are only used for humanlikes, there is no reason to provide any to non-humanlikes.
    public class SMN_PawnKindSkyMindExtension : DefModExtension
    {
        // Bool for whether pawns with this pawn kind may ever be a surrogate.
        public bool mayBeSurrogate = true;

        // Bool for whether the pawn must be a surrogate when it spawns.
        public bool mustBeSurrogate = false;

        // Defs for what hediffs are the standard to use for receiver implants for surrogate spawning purposes.
        public HediffDef receiverImplant;

        public override IEnumerable<string> ConfigErrors()
        {
            if (!mayBeSurrogate && mustBeSurrogate)
            {
                yield return "[SMN] A PawnKindDef was given a sky mind extension but set the kind to never be surrogates and also must be surrogates. This is contradictory.";
            }
        }
    }
}