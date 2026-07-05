using System.Collections.Generic;
using UnitedFront.Comps;
using UnityEngine;
using Verse;

namespace UnitedFront.PlaceWorker
{
    public class PlaceWorker_NetcastRadius : Verse.PlaceWorker
    {
        private static List<IntVec3> _tmpCells = new List<IntVec3>();

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null!)
        {
            CompPropertiesNetcastRadio props = def.GetCompProperties<CompPropertiesNetcastRadio>();
            if (props == null)
            {
                return;
            }
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }
            _tmpCells.Clear();
            Room centerRoom = center.GetRoom(map);
            int num = GenRadial.NumCellsInRadius(props.radius);
            for (int i = 0; i < num; i++)
            {
                IntVec3 cell = center + GenRadial.RadialPattern[i];
                if (!cell.InBounds(map))
                {
                    continue;
                }
                if (centerRoom != null && cell.GetRoom(map) != centerRoom)
                {
                    continue;
                }
                _tmpCells.Add(cell);
            }
            GenDraw.DrawFieldEdges(_tmpCells);
        }
    }
}
