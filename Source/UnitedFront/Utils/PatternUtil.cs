using System.Collections.Generic;
using RimWorld;
using UnitedFront.Defs;
using UnityEngine;
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
                if (d.setsNull)
                {
                    result.Add(d);
                    continue;
                }
                if (!d.appliesTo.NullOrEmpty() && !d.appliesTo.Contains(apparel.def.defName))
                    continue;
                if (MaskExists(d, null))
                    result.Add(d);
            }
            result.SortBy(d => d.sortOrder);
            return result;
        }

        private static bool MaskExists(MaskPatternDef d, string basePath)
        {
            if (d.texPath.NullOrEmpty()) return false;
            string probe = d.useBodyTypes ? d.texPath + "_" + BodyTypeDefOf.Male.defName : d.texPath;
            return ContentFinder<Texture2D>.Get(probe + "_south", false) != null;
        }
    }
}
