using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UnitedFront.Comps
{
    public class NetcastBroadcast
    {
        public string label = null!;
        public float joyGainRate = 1f;
        public HediffDef bonusHediff = null!;
        public float weight = 1f;
    }

    public class CompPropertiesNetcastRadio : CompProperties
    {
        public HediffDef hediffDef = null!;
        public JoyKindDef joyKind = null!;
        public List<NetcastBroadcast> broadcasts = null!;
        public float radius = 6f;
        public bool requiresPower = true;
        public int tickRate = 250;
        public int rerollInterval = 60000;
        public bool allowHumanlike = true;
        public bool allowAnimals;
        public bool drawRadiusRing = true;

        public CompPropertiesNetcastRadio()
        {
            compClass = typeof(CompNetcastRadio);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (hediffDef == null)
            {
                yield return parentDef.defName + " has CompPropertiesNetcastRadio with no hediffDef defined.";
            }
            if (joyKind == null)
            {
                yield return parentDef.defName + " has CompPropertiesNetcastRadio with no joyKind defined.";
            }
            if (broadcasts.NullOrEmpty())
            {
                yield return parentDef.defName + " has CompPropertiesNetcastRadio with no broadcasts defined.";
            }
            if (radius > 6f)
            {
                yield return parentDef.defName + " has CompPropertiesNetcastRadio radius above the maximum of 6.";
            }
        }
    }
}
