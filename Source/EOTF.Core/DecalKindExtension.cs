using UnityEngine;
using Verse;

namespace EOTF.Core.DecalSystem
{
    //Slap this on a PawnKindDef to force a default decal when the pawn spawns with decal gear
    //Player can still override it in the dialog, NPCs just get whatever's set here
    public class DecalKindExtension : DefModExtension
    {
        public string armorDecalPath = "";
        public Color armorDecalColor = Color.white;

        public string helmetDecalPath = "";
        public Color helmetDecalColor = Color.white;

        //Force these even if the pawn already has a saved profile
        public bool overrideSaved = false;
    }
}
