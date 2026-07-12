using RimWorld;
using UnitedFront.Defs;
using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public sealed class CompEditDecalMarker : ThingComp
    {
        public DecalProfileSet ProfileSet = DecalProfileSet.Default;

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref ProfileSet.Helmet.Active, "UnitedFrontDecalHelmetActive");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolPath, "UnitedFrontDecalHelmetPath", "");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolColor, "UnitedFrontDecalHelmetColor", Color.white);

            Scribe_Values.Look(ref ProfileSet.Armor.Active, "UnitedFrontDecalArmorActive");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolPath, "UnitedFrontDecalArmorPath", "");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolColor, "UnitedFrontDecalArmorColor", Color.white);
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            ApplyKindDefaults(pawn.kindDef?.GetModExtension<DecalKindExtension>());
        }

        private void ApplyKindDefaults(DecalKindExtension? ext)
        {
            if (ext == null) return;

            ProfileSet.Armor  = Merge(ProfileSet.Armor,  ext.armorDecalPath,  ext.armorDecalColor,  ext.overrideSaved);
            ProfileSet.Helmet = Merge(ProfileSet.Helmet, ext.helmetDecalPath, ext.helmetDecalColor, ext.overrideSaved);
        }

        private static DecalProfile Merge(DecalProfile current, string path, Color color, bool force)
        {
            if (path.NullOrEmpty()) return current;
            if (!force && !current.SymbolPath.NullOrEmpty()) return current;

            current.Active      = true;
            current.SymbolPath  = path;
            current.SymbolColor = color;
            return current;
        }
    }

    public sealed class CompPropertiesEditDecalMarker : CompProperties
    {
        public CompPropertiesEditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }
}