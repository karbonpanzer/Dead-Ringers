using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace UnitedFront.ColorMask
{

    public static class MultiColorGraphicUtil
    {

        private static readonly string[] ParamNames =
        {
            "_DrawColor", "_DrawColorTwo", "_DrawColorThree",
            "_DrawColorFour", "_DrawColorFive", "_DrawColorSix",
            "_DrawColorSeven", "_DrawColorEight", "_DrawColorNine",
            "_DrawColorTen", "_DrawColorEleven", "_DrawColorTwelve"
        };

        public static Graphic Get(string texPath, string maskPath, Shader shader,
                                  Vector2 drawSize, IReadOnlyList<Color> colors, System.Type graphicClass = null)
        {
            var shaderParameters = new List<ShaderParameter>(colors.Count);
            for (int i = 0; i < colors.Count && i < ParamNames.Length; i++)
            {
                var p  = new ShaderParameter();
                var tr = Traverse.Create(p);
                tr.Field("name").SetValue(ParamNames[i]);
                tr.Field("type").SetValue(1);
                Color c = colors[i];
                tr.Field("value").SetValue(new Vector4(c.r, c.g, c.b, c.a));
                shaderParameters.Add(p);
            }

            Color colorOne = colors.Count > 0 ? colors[0] : Color.white;
            Color colorTwo = colors.Count > 1 ? colors[1] : Color.white;

            return GraphicDatabase.Get(
                graphicClass ?? typeof(Graphic_Multi), texPath, shader, drawSize,
                colorOne, colorTwo,  null, shaderParameters, maskPath);
        }
    }
}
