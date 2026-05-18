using UnityEngine;
using Verse;

namespace EOTF.Core.DecalSystem
{
    //Shared between Helmet and Armor, they just get scaled and offset differently
    public sealed class DecalSymbolDef : Def
    {
        public string Path = "";
        
        //Groups symbols under dividers in the UI, i.e. "Engineer", "Medic"
        public string role = "";
    }

    public enum DecalSlot { Helmet, Armor }

    public struct DecalProfile
    {
        public bool Active;
        public string SymbolPath;
        public Color SymbolColor;

        public DecalProfile(bool active, string path, Color color)
        {
            Active = active;
            SymbolPath = path;
            SymbolColor = color;
        }

        public static DecalProfile Default => new DecalProfile(false, "", Color.white);
    }

    public struct DecalProfileSet
    {
        public DecalProfile Helmet;
        public DecalProfile Armor;

        public DecalProfileSet(DecalProfile helmet, DecalProfile armor)
        {
            Helmet = helmet;
            Armor = armor;
        }

        public static DecalProfileSet Default => new DecalProfileSet(DecalProfile.Default, DecalProfile.Default);
    }
}
