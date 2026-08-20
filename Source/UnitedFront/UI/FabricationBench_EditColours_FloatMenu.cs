using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnitedFront.Decals;
using UnitedFront.Jobs;
using Verse;
using Verse.AI;

namespace UnitedFront.HarmonyPatches
{
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetFloatMenuOptions))]
    public static class FabricationBench_EditColours_FloatMenu
    {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result,
                                                           ThingWithComps __instance, Pawn selPawn)
        {
            foreach (FloatMenuOption option in __result)
                yield return option;

            if (__instance.def == null || __instance.def.defName != "FabricationBench")
                yield break;

            if (selPawn == null || !selPawn.RaceProps.Humanlike || !DecalUtil.Wears(selPawn))
                yield break;

            if (!selPawn.CanReach(__instance, PathEndMode.InteractionCell, Danger.Deadly)
                || !selPawn.CanReserve(__instance))
            {
                yield return new FloatMenuOption("UFR_EditArmourUnreachable".Translate(), null);
                yield break;
            }

            yield return new FloatMenuOption("UFR_EditArmour".Translate(), delegate
            {
                Job job = JobMaker.MakeJob(UFR_JobDefOf.UFR_EditDecalsAtStation, __instance);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
    }
}
