using Verse;

namespace SkyMind
{
    // Mod extension for factions to control SkyMind features for the factions.
    public class SMN_FactionSkyMindExtension : DefModExtension
    {
        // Bools for whether groups of this faction may have some members be remotely controlled surrogates.
        public bool canUseSurrogates = false;

        // Int for how many legal pawns must be in a group before it may have surrogates.
        public int minLegalPawnsForSurrogates = 5;

        // Int for the minimum pawn kind combat strength value to be legal for being a remotely controlled surrogate.
        public int minStrengthForSurrogates = 0;

        // Float for the chance for a group to have surrogates in it.
        public float percentChanceForGroupToHaveSurrogates = 0.3f;

        // Min/Max for how much of the group will be surrogates if a group is chosen to have surrogates.
        public float percentOfGroupToBeSurrogatesMin = 0.0f;
        public float percentOfGroupToBeSurrogatesMax = 1f;
    }
}