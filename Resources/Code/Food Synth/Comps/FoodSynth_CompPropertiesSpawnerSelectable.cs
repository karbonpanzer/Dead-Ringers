using System.Collections.Generic;
using Verse;

namespace UnitedFront.Comps
{
    public class CompPropertiesSpawnerSelectable : CompProperties
    {

        public List<ThingDefCountClass> spawnOptions = null!;

        public IntRange spawnIntervalRange = new IntRange(600000, 600000);
        public int spawnMaxAdjacent = -1;
        public bool spawnForbidden;
        public bool requiresPower = true;
        public bool writeTimeLeftToSpawn = true;
        public bool showMessageIfOwned = true;
        public bool inheritFaction;
        [NoTranslate]
        public string saveKeysPrefix = null!;

        public CompPropertiesSpawnerSelectable()
        {
            compClass = typeof(FoodSynth_CompSpawnerSelectable);
        }
        
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (spawnOptions.NullOrEmpty())
            {
                yield return "CompProperties_SpawnerSelectable has no spawnOptions defined on "
                             + parentDef.defName;
            }
        }
    }
}