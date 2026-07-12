using System.Collections.Generic;
using RimWorld;
using UnitedFront.Comps;
using UnitedFront.Defs;
using Verse;

namespace UnitedFront.Decals
{
    public static class DecalUtil
    {
        private static List<DecalSymbol>? _armorSymbols;
        private static List<DecalSymbol>? _helmetSymbols;

        public static CompEditDecalMarker? MarkerOn(Pawn? pawn)
        {
            Pawn_ApparelTracker? tracker = pawn?.apparel;
            if (tracker == null) return null;

            List<Apparel> worn = tracker.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                if (!worn[i].def.HasComp<CompEditDecalMarker>()) continue;
                var comp = worn[i].TryGetComp<CompEditDecalMarker>();
                if (comp != null) return comp;
            }
            return null;
        }

        public static bool Wears(Pawn? pawn) => MarkerOn(pawn) != null;

        public static DecalProfileSet ProfileSetOn(Pawn pawn) => MarkerOn(pawn)?.ProfileSet ?? DecalProfileSet.Default;

        public static DecalProfile ProfileOn(Pawn pawn, DecalSlot slot)
        {
            var comp = MarkerOn(pawn);
            if (comp == null) return DecalProfile.Default;
            return slot == DecalSlot.Helmet ? comp.ProfileSet.Helmet : comp.ProfileSet.Armor;
        }

        public static void Preview(Pawn pawn, DecalProfileSet profileSet)
        {
            var comp = MarkerOn(pawn);
            if (comp == null) return;
            comp.ProfileSet = profileSet;
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        public static void FinishPreview(Pawn pawn, bool keep, DecalProfileSet fallback)
        {
            if (!keep)
            {
                var comp = MarkerOn(pawn);
                if (comp != null) comp.ProfileSet = fallback;
            }
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        public static List<DecalSymbol> SymbolsForSlot(DecalSlot slot)
        {
            if (slot == DecalSlot.Armor)
                return _armorSymbols ??= Filter(s => !s.helmetOnly);

            return _helmetSymbols ??= Filter(s => !s.armorOnly);
        }

        private static List<DecalSymbol> Filter(System.Func<DecalSymbol, bool> keep)
        {
            List<DecalSymbol> all = DefDatabase<DecalSymbol>.AllDefsListForReading;
            var result = new List<DecalSymbol>(all.Count);
            for (int i = 0; i < all.Count; i++)
            {
                if (keep(all[i])) result.Add(all[i]);
            }
            return result;
        }
    }
}