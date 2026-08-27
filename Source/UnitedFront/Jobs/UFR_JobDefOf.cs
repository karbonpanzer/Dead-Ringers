using RimWorld;
using Verse;

namespace UnitedFront.Jobs
{
    [DefOf]
    public static class UFR_JobDefOf
    {
        public static JobDef UFR_EditColorsAtStation;

        static UFR_JobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(UFR_JobDefOf));
    }
}
