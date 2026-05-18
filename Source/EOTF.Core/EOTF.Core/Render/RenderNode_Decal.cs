using UnityEngine;
using Verse;

namespace EOTF.Core.DecalSystem
{
    public class PawnRenderNodeDecal : PawnRenderNode
    {

        private readonly DecalSlot _slot;

        private Graphic? _cachedGraphic;
        private string?  _cachedPath;
        private Color    _cachedColor;

        public PawnRenderNodeDecal(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
            _slot = DetermineSlot(props);
        }

        //Graphic lookup with caching so we're not hammering GraphicDatabase every goddamn frame
        public override Graphic? GraphicFor(Pawn pawn)
        {
            var eotfProps = Props as PawnRenderNodePropertiesOmni;

            DecalProfile profile   = DecalUtil.ReadProfileFrom(pawn, _slot);
            string       basePath  = profile.Active ? profile.SymbolPath : GetDefaultPath(pawn);
            Color        finalColor = profile.Active ? profile.SymbolColor : (eotfProps?.Color ?? new Color(0.2f, 0.2f, 0.2f));

            if (basePath.NullOrEmpty()) return null;

            // Try body-type-specific texture first, fall back to base path
            string path = ResolveBodyTypePath(pawn, basePath);

            if (_cachedPath == path && _cachedColor == finalColor)
                return _cachedGraphic;

            _cachedPath    = path;
            _cachedColor   = finalColor;
            _cachedGraphic = GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, Vector2.one, finalColor);

            return _cachedGraphic;
        }

        // Resolves the best texture path for this pawn's body type.
        // Graphic_Multi handles directional variants (_south, _east, etc) internally,
        // including falling back to _south for _north when _north doesn't exist.
        // All we need to do is pick the right base path:
        //   1. path_BodyType (e.g. Cog_Female) — if any texture exists for it
        //   2. path (e.g. Cog) — fallback, works with or without directional suffixes
        private static string ResolveBodyTypePath(Pawn pawn, string basePath)
        {
            var bodyType = pawn.story?.bodyType;
            if (bodyType == null) return basePath;

            string bodyTypePath = basePath + "_" + bodyType.defName;

            // Check if body-type-specific textures exist in any form
            // Try with directional suffix first, then bare — either means the variant exists
            if (ContentFinder<Texture2D>.Get(bodyTypePath + "_south", false) != null
                || ContentFinder<Texture2D>.Get(bodyTypePath, false) != null)
                return bodyTypePath;

            // No body type variant found, fall back to base path
            // Graphic_Multi will handle _south/_east or single texture from here
            return basePath;
        }

        //Figures out if this is helmet or armor, defaults to armor if XML doesn't specify
        private static DecalSlot DetermineSlot(PawnRenderNodeProperties props)
        {
            if (props is PawnRenderNodePropertiesOmni eotfProps && eotfProps.ExplicitSlot.HasValue)
                return eotfProps.ExplicitSlot.Value;

            if (props.parentTagDef != null)
            {
                string tagName = props.parentTagDef.defName;
                if (tagName.Contains("Head") || tagName.Contains("Headgear") || tagName.Contains("Helmet"))
                    return DecalSlot.Helmet;
            }

            return DecalSlot.Armor;
        }

        //Default texture path when nobody's picked a decal, falls back to whatever's in the XML texPaths
        private string GetDefaultPath(Pawn pawn)
        {
            if (Props is PawnRenderNodePropertiesOmni eotfProps && eotfProps.texPaths.Count > 0)
            {
                int seed = pawn.Faction?.loadID ?? pawn.thingIDNumber;
                return eotfProps.texPaths[seed % eotfProps.texPaths.Count];
            }
            return "";
        }
    }
}
