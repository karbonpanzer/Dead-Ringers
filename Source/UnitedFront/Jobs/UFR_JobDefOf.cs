using RimWorld;
using Verse;

namespace UnitedFront.Jobs
{
    [DefOf]
    public static class UFR_JobDefOf
    {
        public static JobDef UFR_EditDecalsAtStation;

        static UFR_JobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(UFR_JobDefOf));
    }
}
