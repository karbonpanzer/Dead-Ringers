using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace UnitedFront.ColorMask
{
    public static class MultiColorGraphicUtil
    {
        public static Graphic Get(string texPath, string maskPath, Shader shader,
                                  Vector2 drawSize, IReadOnlyList<Color> colors, System.Type graphicClass = null)
        {
            Color colorOne = colors.Count > 0 ? colors[0] : Color.white;
            Color colorTwo = colors.Count > 1 ? colors[1] : Color.white;

            return GraphicDatabase.Get(
                graphicClass ?? typeof(Graphic_Multi), texPath, shader, drawSize,
                colorOne, colorTwo, null, null, maskPath);
        }
    }
}
