using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public class PlaceWorker_NetcastRadius : PlaceWorker
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
