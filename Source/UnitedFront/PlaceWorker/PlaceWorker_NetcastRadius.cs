using UnitedFront.Comps;
using UnityEngine;
using Verse;

namespace UnitedFront.PlaceWorker
{
    public class PlaceWorker_NetcastRadius : Verse.PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null!)
        {
            CompPropertiesNetcastRadio props = def.GetCompProperties<CompPropertiesNetcastRadio>();
            if (props != null)
            {
                GenDraw.DrawRadiusRing(center, props.radius);
            }
        }
    }
}
