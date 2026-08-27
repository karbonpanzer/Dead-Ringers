using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public sealed class CompPropertiesColorMarker : CompProperties
    {
        public int zoneCount = 2;

        public List<Color> defaultZoneColors;

        public CompPropertiesColorMarker() => compClass = typeof(CompColorMarker);
    }
}
