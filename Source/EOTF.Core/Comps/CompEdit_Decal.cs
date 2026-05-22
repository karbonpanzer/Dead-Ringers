using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace EOTF.Core.DecalSystem
{
    public sealed class CompEditDecalMarker : ThingComp
    {
        public DecalProfileSet ProfileSet = DecalProfileSet.Default;

        //Saves decal state per-item so it persists across saves
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref ProfileSet.Helmet.Active, "eotfDecalHelmetActive");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolPath, "eotfDecalHelmetPath", "");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolColor, "eotfDecalHelmetColor", Color.white);

            Scribe_Values.Look(ref ProfileSet.Armor.Active, "eotfDecalArmorActive");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolPath, "eotfDecalArmorPath", "");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolColor, "eotfDecalArmorColor", Color.white);
        }

        //Register with WorldComponent when equipped so the cache knows about this pawn
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            WorldComponentDecalPawns.Instance?.Register(pawn);
        }

        //Only unregister if no other decal gear is still on the pawn
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (wornApparel[i].def.HasComp<CompEditDecalMarker>())
                    return;
            }
            WorldComponentDecalPawns.Instance?.Unregister(pawn);
        }
    }

    public sealed class CompPropertiesEditDecalMarker : CompProperties
    {
        public CompPropertiesEditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }
}