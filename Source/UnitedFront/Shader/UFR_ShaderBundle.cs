using UnityEngine;
using Verse;
using RimWorld;

namespace UnitedFront.ColorMask
{
    [StaticConstructorOnStartup]
    public static class UFR_ShaderBundle
    {
        private const string ShaderName = "UFR_CutoutCarapace";

        public static readonly Shader CutoutCarapace = Load();

        private static Shader Load()
        {
            Shader s = null;
            try { s = ShaderDatabase.LoadShader(ShaderName); } catch { s = null; }

            if (s == null || s.name != ShaderName)
            {
                Log.Warning("[UnitedFront] Shader '" + ShaderName + "' not found in AssetBundles/. Falling back to CutoutComplex.");
                s = ShaderDatabase.CutoutComplex;
            }
            return s;
        }
    }
}
