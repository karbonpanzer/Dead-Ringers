using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UnitedFront.Comps
{
    public class CompPropertiesBroadcastSource : CompProperties
    {
        public List<NetcastBroadcast> broadcasts = null!;
        public int rerollInterval = 60000;

        public CompPropertiesBroadcastSource()
        {
            compClass = typeof(CompBroadcastSource);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (broadcasts.NullOrEmpty())
            {
                yield return parentDef.defName + " has CompPropertiesBroadcastSource with no broadcasts defined.";
            }
        }
    }
}
