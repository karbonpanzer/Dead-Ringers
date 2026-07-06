using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UnitedFront.Comps
{
    public class CompPropertiesBroadcastReceiver : CompProperties
    {
        public JoyKindDef joyKind = null!;
        public float radius = 6f;
        public bool requiresPower = true;
        public int tickRate = 250;
        public bool allowHumanlike = true;
        public bool allowAnimals;
        public bool drawRadiusRing = true;

        public CompPropertiesBroadcastReceiver()
        {
            compClass = typeof(CompBroadcastReceiver);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (radius > 6f)
            {
                yield return parentDef.defName + " has CompPropertiesBroadcastReceiver radius above the maximum of 6.";
            }
        }
    }
}
