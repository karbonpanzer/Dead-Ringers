using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnitedFront.Decals;
using UnitedFront.UI;
using UnityEngine;
using Verse;

namespace UnitedFront.Harmony
{
    [StaticConstructorOnStartup]
    public static class DecalBootstrap
    {
        static DecalBootstrap()
        {
            new HarmonyLib.Harmony("UnitedFront.Decals").PatchAll();
            Log.Message("[UnitedFront] Decal system patched.");
        }
    }

    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_GetGizmos_Decals
    {
        private static readonly Texture2D GizmoIcon = ContentFinder<Texture2D>.Get("UFR/UI/Decal/UFR_CustomizeDecal");

        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.Faction != Faction.OfPlayerSilentFail) return;
            if (!__instance.RaceProps.Humanlike) return;
            if (!DecalUtil.Wears(__instance)) return;

            __result = WithDecalCommand(__result, __instance);
        }

        private static IEnumerable<Gizmo> WithDecalCommand(IEnumerable<Gizmo> source, Pawn pawn)
        {
            foreach (Gizmo g in source) yield return g;

            yield return new Command_Action
            {
                defaultLabel = "UnitedFront_StyleDecalsGizmo".Translate(pawn.LabelCap),
                defaultDesc  = "UnitedFront_StyleDecalsDesc".Translate(),
                icon         = GizmoIcon,
                action       = () => Find.WindowStack.Add(new DialogEditDecals(pawn))
            };
        }
    }
}