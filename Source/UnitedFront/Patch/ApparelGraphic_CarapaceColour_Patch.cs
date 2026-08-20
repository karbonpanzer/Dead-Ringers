using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnitedFront.ColorMask;
using UnitedFront.Comps;
using UnitedFront.Defs;
using UnityEngine;
using Verse;

namespace UnitedFront.HarmonyPatches
{
    [HarmonyPatch]
    public static class ApparelGraphic_CarapaceColour_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel",
                new[] { typeof(Apparel), typeof(BodyTypeDef), typeof(bool), typeof(ApparelGraphicRecord).MakeByRefType() });
        }

        public static void Postfix(Apparel apparel, BodyTypeDef bodyType, bool forStatue, ref ApparelGraphicRecord rec, bool __result)
        {
            if (forStatue || !__result || apparel == null) return;

            CompEditDecalMarker comp = apparel.GetComp<CompEditDecalMarker>();
            if (comp == null || comp.ZoneColors.NullOrEmpty()) return;

            Shader shader = UFR_ShaderBundle.CutoutCarapace;
            if (shader == null || shader == ShaderDatabase.Cutout || shader == ShaderDatabase.CutoutComplex) return;

            string basePath = apparel.WornGraphicPath;
            if (basePath.NullOrEmpty()) return;

            ApparelLayerDef last = apparel.def.apparel.LastLayer;
            bool perBodyType = last != ApparelLayerDefOf.Overhead
                            && last != ApparelLayerDefOf.EyeCover
                            && !apparel.RenderAsPack()
                            && basePath != BaseContent.PlaceholderImagePath
                            && basePath != BaseContent.PlaceholderGearImagePath;

            string path = perBodyType ? basePath + "_" + bodyType.defName : basePath;

            string maskPath = null;
            MaskPatternDef pat = comp.pattern;
            if (pat != null && !pat.setsNull && !pat.maskPath.NullOrEmpty())
            {
                maskPath = pat.maskPath;
                if (perBodyType && pat.useBodyTypes)
                    maskPath = maskPath + "_" + bodyType.defName;
            }

            Graphic graphic = MultiColorGraphicUtil.Get(path, maskPath, shader, apparel.def.graphicData.drawSize, comp.ZoneColors);
            if (graphic != null)
                rec = new ApparelGraphicRecord(graphic, apparel);
        }
    }
}
