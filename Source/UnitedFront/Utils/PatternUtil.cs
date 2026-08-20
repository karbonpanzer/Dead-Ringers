using System.Collections.Generic;
using RimWorld;
using UnitedFront.Defs;
using Verse;

namespace UnitedFront.Decals
{
    public static class PatternUtil
    {
        public static List<MaskPatternDef> AvailableFor(Apparel apparel)
        {
            List<MaskPatternDef> result = new List<MaskPatternDef>();
            if (apparel == null) return result;
            foreach (MaskPatternDef d in DefDatabase<MaskPatternDef>.AllDefsListForReading)
            {
                if (d.appliesTo.NullOrEmpty() || d.appliesTo.Contains(apparel.def.defName))
                    result.Add(d);
            }
            result.SortBy(d => d.sortOrder);
            return result;
        }
    }
}
