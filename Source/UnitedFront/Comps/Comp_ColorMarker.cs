using System.Collections.Generic;
using RimWorld;
using UnitedFront.Defs;
using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public sealed class CompColorMarker : ThingComp
    {
        public List<Color> ZoneColors = new List<Color>();
        private bool zonesCustomized;

        public CompPropertiesColorMarker Props => (CompPropertiesColorMarker)props;
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
            Scribe_Collections.Look(ref ZoneColors, "UnitedFrontZoneColors", LookMode.Value);
            Scribe_Values.Look(ref zonesCustomized, "UnitedFrontZonesCustomized", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                EnsureZoneDefaults();
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            KindColorExtension ext = pawn.kindDef?.GetModExtension<KindColorExtension>();
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

        private void ApplyKindDefaults(KindColorExtension ext)
        {
            if (ext == null || ext.zoneColors.NullOrEmpty()) return;
            if (!ext.overrideSaved && zonesCustomized) return;

            EnsureZoneDefaults();
            for (int i = 0; i < ZoneColors.Count && i < ext.zoneColors.Count; i++)
                ZoneColors[i] = ext.zoneColors[i];
        }
    }
}
