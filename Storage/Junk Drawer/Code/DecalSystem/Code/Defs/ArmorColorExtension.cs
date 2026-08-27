using UnityEngine;
using Verse;

namespace UnitedFront.Defs
{
    public class ArmorColorExtension : DefModExtension
    {
        public bool setColorOne = false;
        public Color colorOne = Color.white;

        public bool setColorTwo = true;
        public Color colorTwo = new Color(0.2f, 0.2f, 0.2f);
    }
}
