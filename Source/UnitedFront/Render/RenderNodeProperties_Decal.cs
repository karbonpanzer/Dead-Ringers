using UnitedFront.Defs;
using UnityEngine;
using Verse;

namespace UnitedFront.Render
{
    public class PawnRenderNodePropertiesDecal : PawnRenderNodeProperties
    {
        public DecalSlot? explicitSlot = null;

        public PawnRenderNodePropertiesDecal()
        {
            nodeClass = typeof(PawnRenderNodeDecal);
            workerClass = typeof(PawnRenderNodeWorkerDecal);
            color = new Color(0.2f, 0.2f, 0.2f);
        }
    }
}