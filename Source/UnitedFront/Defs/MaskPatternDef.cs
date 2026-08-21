using System.Collections.Generic;
using Verse;

namespace UnitedFront.Defs
{
    public class MaskPatternDef : Def
    {
        public string texPath = null;
        public List<string> appliesTo = new List<string>();
        public bool useBodyTypes = true;
        public bool setsNull = false;
        public int sortOrder = 0;
    }
}
