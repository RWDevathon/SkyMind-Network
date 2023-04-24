using System;
using UnityEngine;
using Verse;

namespace SkyMind
{
    /*
     * Settings Extensions and Pawn Selectors courtesy of Simple Sidearms by PeteTimesSix. Without his work, this would have been exceedingly difficult to build!
     * Internal to prevent potentialy conflicts over naming and slightly reduce the odds of misuse in other projects.
     */
    internal static class ListingExtensions
    {
        public static float ColumnGap = 17f;

        public static void SliderLabeled(this Listing_Standard instance, string label, ref float value, float min, float max, float displayMult = 1, int decimalPlaces = 0, string valueSuffix = "", string tooltip = null, Action onChange = null)
        {
            instance.Label($"{label}: {(value * displayMult).ToString($"F{decimalPlaces}")}{valueSuffix}", tooltip: tooltip);
            var valueBefore = value;
            value = instance.Slider(value, min, max);
            if (value != valueBefore)
            {
                onChange?.Invoke();
            }
        }

        public static Listing_Standard BeginHiddenSection(this Listing_Standard instance, out float maxHeightAccumulator)
        {
            Rect rect = instance.GetRect(0);
            rect.height = 30000f;
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(rect);
            maxHeightAccumulator = 0f;
            return listing_Standard;
        }

        public static void NewHiddenColumn(this Listing_Standard instance, ref float maxHeightAccumulator)
        {
            if (maxHeightAccumulator < instance.CurHeight)
                maxHeightAccumulator = instance.CurHeight;
            instance.NewColumn();
        }

        public static void EndHiddenSection(this Listing_Standard instance, Listing_Standard section, float maxHeightAccumulator)
        {
            instance.GetRect(Mathf.Max(section.CurHeight, maxHeightAccumulator));
            section.End();
        }
    }
}
