using System.Collections.Generic;
using RimWorld;
using UnitedFront.Defs;
using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public sealed class CompEditDecalMarker : ThingComp
    {
        public DecalProfileSet ProfileSet = DecalProfileSet.Default;

        public List<Color> ZoneColors = new List<Color>();
        private bool zonesCustomized;

        public CompPropertiesEditDecalMarker Props => (CompPropertiesEditDecalMarker)props;
        public int ZoneCount => Props.zoneCount;

        public override void PostPostMake()
        {
            base.PostPostMake();
            EnsureZoneDefaults();
            ApplyArmorDefaults();
        }

        private void ApplyArmorDefaults()
        {
            if (zonesCustomized) return;
            ArmorColorExtension ext = parent.def.GetModExtension<ArmorColorExtension>();
            if (ext == null) return;

            EnsureZoneDefaults();
            Color drawColor = parent is Apparel ap ? ap.DrawColor : Color.white;
            if (ZoneColors.Count > 0) ZoneColors[0] = ext.setColorOne ? ext.colorOne : drawColor;
            if (ZoneColors.Count > 1) ZoneColors[1] = ext.setColorTwo ? ext.colorTwo : drawColor;
            SetDirty();
        }

        private void EnsureZoneDefaults()
        {
            ZoneColors ??= new List<Color>();
            while (ZoneColors.Count < ZoneCount)
            {
                int i = ZoneColors.Count;
                Color d = (Props.defaultZoneColors != null && i < Props.defaultZoneColors.Count)
                    ? Props.defaultZoneColors[i]
                    : Color.white;
                ZoneColors.Add(d);
            }
            if (ZoneColors.Count > ZoneCount)
                ZoneColors.RemoveRange(ZoneCount, ZoneColors.Count - ZoneCount);
        }

        public Color GetZone(int index) => (index >= 0 && index < ZoneColors.Count) ? ZoneColors[index] : Color.white;

        public void SetZone(int index, Color c, bool markCustomized = true)
        {
            EnsureZoneDefaults();
            if (index < 0 || index >= ZoneColors.Count) return;
            ZoneColors[index] = c;
            if (markCustomized) zonesCustomized = true;
            SetDirty();
        }

        public void PreviewZones(List<Color> colors)
        {
            ZoneColors = new List<Color>(colors);
            EnsureZoneDefaults();
            SetDirty();
        }

        public void CommitZones(List<Color> colors)
        {
            ZoneColors = new List<Color>(colors);
            zonesCustomized = true;
            EnsureZoneDefaults();
            SetDirty();
        }

        private void SetDirty()
        {
            if (parent is Apparel ap && ap.Wearer != null)
                ap.Wearer.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref ProfileSet.Helmet.Active, "UnitedFrontDecalHelmetActive");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolPath, "UnitedFrontDecalHelmetPath", "");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolColor, "UnitedFrontDecalHelmetColor", Color.white);

            Scribe_Values.Look(ref ProfileSet.Armor.Active, "UnitedFrontDecalArmorActive");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolPath, "UnitedFrontDecalArmorPath", "");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolColor, "UnitedFrontDecalArmorColor", Color.white);

            Scribe_Collections.Look(ref ZoneColors, "UnitedFrontZoneColors", LookMode.Value);
            Scribe_Values.Look(ref zonesCustomized, "UnitedFrontZonesCustomized", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                EnsureZoneDefaults();
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            DecalKindExtension ext = pawn.kindDef?.GetModExtension<DecalKindExtension>();
            ApplyKindDefaults(ext);
            if (!zonesCustomized && (ext == null || ext.zoneColors.NullOrEmpty()) && Props.defaultZoneColors.NullOrEmpty() && parent is Apparel ap)
            {
                EnsureZoneDefaults();
                ArmorColorExtension armorExt = parent.def.GetModExtension<ArmorColorExtension>();
                if (armorExt != null)
                {
                    if (ZoneColors.Count > 0) ZoneColors[0] = armorExt.setColorOne ? armorExt.colorOne : ap.DrawColor;
                    if (ZoneColors.Count > 1) ZoneColors[1] = armorExt.setColorTwo ? armorExt.colorTwo : ap.DrawColor;
                }
                else
                {
                    for (int i = 0; i < ZoneColors.Count; i++)
                        ZoneColors[i] = ap.DrawColor;
                }
                SetDirty();
            }
        }

        private void ApplyKindDefaults(DecalKindExtension? ext)
        {
            if (ext == null) return;

            ProfileSet.Armor = Merge(ProfileSet.Armor, ext.armorDecalPath, ext.armorDecalColor, ext.overrideSaved);
            ProfileSet.Helmet = Merge(ProfileSet.Helmet, ext.helmetDecalPath, ext.helmetDecalColor, ext.overrideSaved);

            if (!ext.zoneColors.NullOrEmpty() && (ext.overrideSaved || !zonesCustomized))
            {
                EnsureZoneDefaults();
                for (int i = 0; i < ZoneColors.Count && i < ext.zoneColors.Count; i++)
                    ZoneColors[i] = ext.zoneColors[i];
            }
        }

        private static DecalProfile Merge(DecalProfile current, string path, Color color, bool force)
        {
            if (path.NullOrEmpty()) return current;
            if (!force && !current.SymbolPath.NullOrEmpty()) return current;

            current.Active = true;
            current.SymbolPath = path;
            current.SymbolColor = color;
            return current;
        }
    }
}
