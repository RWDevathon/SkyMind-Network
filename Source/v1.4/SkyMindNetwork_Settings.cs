using UnityEngine;
using Verse;
using RimWorld;

namespace SkyMind
{
    public class SkyMindNetwork_Settings : ModSettings
    {
            // Settings for Enemy hacks
        public static bool enemyHacksOccur;
        public static float chanceAlliesInterceptHack;
        public static float pointsGainedOnInterceptPercentage;
        public static float enemyHackAttackStrengthModifier;
        public static float percentageOfValueUsedForRansoms;

            // Settings for player hacks
        public static bool playerCanHack = true;
        public static bool receiveHackingAlert = true;
        public static float retaliationChanceOnFailure = 0.4f;
        public static float minHackSuccessChance = 0.05f;
        public static float maxHackSuccessChance = 0.95f;

            // Settings for Surrogates
        public static bool surrogatesAllowed = true;
        public static bool otherFactionsAllowedSurrogates = true;

        public static bool displaySurrogateControlIcon = true;
        public static int safeSurrogateConnectivityCountBeforePenalty = 1;

            // Settings for Skill Points
        public static bool receiveSkillAlert = true;
        public static int skillPointInsertionRate = 100;
        public static float skillPointConversionRate = 0.5f;
        public static int passionSoftCap = 8;
        public static float basePointsNeededForPassion = 5000f;

            // Settings for Cloud
        public static bool uploadingToSkyMindKills = true;
        public static bool uploadingToSkyMindPermaKills = true;
        public static int timeToCompleteSkyMindOperations = 24;

        Vector2 scrollPosition = Vector2.zero;
        float cachedScrollHeight = 0;

        public void DoSettingsWindowContents(Rect inRect)
        {
            Color colorSave = GUI.color;
            TextAnchor anchorSave = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;

            bool needToScroll = cachedScrollHeight > inRect.height;
            var viewRect = new Rect(inRect);
            if (needToScroll)
            {
                viewRect.width -= 20f;
                viewRect.height = cachedScrollHeight;
                Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            }

            Listing_Standard listingStandard = new Listing_Standard
            {
                maxOneColumn = true
            };
            listingStandard.Begin(viewRect);

            // HACKING

            listingStandard.CheckboxLabeled("SMN_EnemyHacksOccur".Translate(), ref enemyHacksOccur);
            if (enemyHacksOccur)
            {
                ListingExtensions.SliderLabeled(listingStandard, "SMN_EnemyHackAttackStrengthModifier".Translate(), ref enemyHackAttackStrengthModifier, 0.01f, 5f, displayMult: 100, valueSuffix: "%", tooltip: "SMN_EnemyHackAttackStrengthModifierDesc".Translate());
                ListingExtensions.SliderLabeled(listingStandard, "SMN_ChanceAlliesInterceptHack".Translate(), ref chanceAlliesInterceptHack, 0.01f, 1f, displayMult: 100, valueSuffix: "%", tooltip: "SMN_ChanceAlliesInterceptHackDesc".Translate());
                ListingExtensions.SliderLabeled(listingStandard, "SMN_PointsGainedOnInterceptPercentage".Translate(), ref pointsGainedOnInterceptPercentage, 0.00f, 3f, displayMult: 100, valueSuffix: "%", tooltip: "SMN_PointsGainedOnInterceptPercentageDesc".Translate());
                ListingExtensions.SliderLabeled(listingStandard, "SMN_PercentageOfValueUsedForRansoms".Translate(), ref percentageOfValueUsedForRansoms, 0.01f, 2f, displayMult: 100, valueSuffix: "%");
            }

            listingStandard.CheckboxLabeled("SMN_PlayerCanHack".Translate(), ref playerCanHack);
            if (playerCanHack)
            {
                listingStandard.CheckboxLabeled("SMN_receiveFullHackingAlert".Translate(), ref receiveHackingAlert);
                ListingExtensions.SliderLabeled(listingStandard, "SMN_RetaliationChanceOnFailure".Translate(), ref retaliationChanceOnFailure, 0.0f, 1f, displayMult: 100, valueSuffix: "%");
                ListingExtensions.SliderLabeled(listingStandard, "SMN_MinHackSuccessChance".Translate(), ref minHackSuccessChance, 0.0f, maxHackSuccessChance, displayMult: 100, valueSuffix: "%");
                ListingExtensions.SliderLabeled(listingStandard, "SMN_MaxHackSuccessChance".Translate(), ref maxHackSuccessChance, minHackSuccessChance, 1f, displayMult: 100, valueSuffix: "%");
            }

            listingStandard.GapLine();

            // SURROGATES
            listingStandard.CheckboxLabeled("SMN_surrogatesAllowed".Translate(), ref surrogatesAllowed);
            if (surrogatesAllowed)
            {
                listingStandard.CheckboxLabeled("SMN_otherFactionsAllowedSurrogates".Translate(), ref otherFactionsAllowedSurrogates);
                listingStandard.CheckboxLabeled("SMN_displaySurrogateControlIcon".Translate(), ref displaySurrogateControlIcon);
                string safeSurrogateConnectivityCountBeforePenaltyBuffer = safeSurrogateConnectivityCountBeforePenalty.ToString();
                listingStandard.TextFieldNumericLabeled("SMN_safeSurrogateConnectivityCountBeforePenalty".Translate(), ref safeSurrogateConnectivityCountBeforePenalty, ref safeSurrogateConnectivityCountBeforePenaltyBuffer, 1, 40);
            }
            listingStandard.GapLine();

            // SKILL POINTS
            string skillPointInsertionRateBuffer = skillPointInsertionRate.ToString();
            string skillPointConversionRateBuffer = skillPointConversionRate.ToString();
            string passionSoftCapBuffer = passionSoftCap.ToString();
            string basePointsNeededForPassionBuffer = basePointsNeededForPassion.ToString();
            listingStandard.CheckboxLabeled("SMN_receiveFullSkillAlert".Translate(), ref receiveSkillAlert);
            listingStandard.TextFieldNumericLabeled("SMN_skillPointInsertionRate".Translate(), ref skillPointInsertionRate, ref skillPointInsertionRateBuffer, 1f);
            listingStandard.TextFieldNumericLabeled("SMN_skillPointConversionRate".Translate(), ref skillPointConversionRate, ref skillPointConversionRateBuffer, 0.01f, 10);
            listingStandard.TextFieldNumericLabeled("SMN_passionSoftCap".Translate(), ref passionSoftCap, ref passionSoftCapBuffer, 0, 50);
            listingStandard.TextFieldNumericLabeled("SMN_basePointsNeededForPassion".Translate(), ref basePointsNeededForPassion, ref basePointsNeededForPassionBuffer, 10, 10000);
            listingStandard.GapLine();

            // CLOUD
            listingStandard.CheckboxLabeled("SMN_UploadingKills".Translate(), ref uploadingToSkyMindKills);
            listingStandard.CheckboxLabeled("SMN_UploadingPermakills".Translate(), ref uploadingToSkyMindPermaKills);
            string SkyMindOperationTimeBuffer = timeToCompleteSkyMindOperations.ToString();
            listingStandard.TextFieldNumericLabeled("SMN_SkyMindOperationTimeRequired".Translate(), ref timeToCompleteSkyMindOperations, ref SkyMindOperationTimeBuffer, 1, 256);
            listingStandard.GapLine();

            // Ending

            cachedScrollHeight = listingStandard.CurHeight;
            listingStandard.End();

            if (needToScroll)
            {
                Widgets.EndScrollView();
            }

            GUI.color = colorSave;
            Text.Anchor = anchorSave;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // Hostile Hacks
            Scribe_Values.Look(ref enemyHacksOccur, "SMN_EnemyHacksOccur", true);
            Scribe_Values.Look(ref chanceAlliesInterceptHack, "SMN_ChanceAlliesInterceptHack", 0.05f);
            Scribe_Values.Look(ref pointsGainedOnInterceptPercentage, "SMN_PointsGainedOnInterceptPercentage", 0.25f);
            Scribe_Values.Look(ref enemyHackAttackStrengthModifier, "SMN_EnemyHackAttackStrengthModifier", 1.0f);
            Scribe_Values.Look(ref percentageOfValueUsedForRansoms, "SMN_PercentageOfValueUsedForRansoms", 0.25f);

            // Player Hacks
            Scribe_Values.Look(ref playerCanHack, "SMN_PlayerCanHack", true);
            Scribe_Values.Look(ref receiveHackingAlert, "SMN_receiveHackingAlert", true);
            Scribe_Values.Look(ref retaliationChanceOnFailure, "SMN_RetaliationChanceOnFailure", 0.4f);
            Scribe_Values.Look(ref minHackSuccessChance, "SMN_MinHackSuccessChance", 0.05f);
            Scribe_Values.Look(ref maxHackSuccessChance, "SMN_MaxHackSuccessChance", 0.95f);

            // Surrogates
            Scribe_Values.Look(ref surrogatesAllowed, "SMN_surrogatesAllowed", true);
            Scribe_Values.Look(ref otherFactionsAllowedSurrogates, "SMN_otherFactionsAllowedSurrogates", true);
            Scribe_Values.Look(ref displaySurrogateControlIcon, "SMN_displaySurrogateControlIcon", true);
            Scribe_Values.Look(ref safeSurrogateConnectivityCountBeforePenalty, "SMN_safeSurrogateConnectivityCountBeforePenalty", 1);

            // Skills
            Scribe_Values.Look(ref receiveSkillAlert, "SMN_receiveSkillAlert", true);
            Scribe_Values.Look(ref skillPointInsertionRate, "SMN_skillPointInsertionRate", 100);
            Scribe_Values.Look(ref skillPointConversionRate, "SMN_skillPointConversionRate", 0.5f);
            Scribe_Values.Look(ref passionSoftCap, "SMN_passionSoftCap", 8);
            Scribe_Values.Look(ref basePointsNeededForPassion, "SMN_basePointsNeededForPassion", 5000f);

            // Cloud
            Scribe_Values.Look(ref uploadingToSkyMindKills, "SMN_UploadingToSkyMindKills", true);
            Scribe_Values.Look(ref uploadingToSkyMindPermaKills, "SMN_UploadingToSkyMindPermaKills", true);
            Scribe_Values.Look(ref timeToCompleteSkyMindOperations, "SMN_timeToCompleteSkyMindOperations", 24);
        }
    }

}