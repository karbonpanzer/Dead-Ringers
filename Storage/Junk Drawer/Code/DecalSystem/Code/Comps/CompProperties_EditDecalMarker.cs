using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public sealed class CompPropertiesEditDecalMarker : CompProperties
    {
        public int zoneCount = 2;

        public List<Color> defaultZoneColors;

        public CompPropertiesEditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }
}
