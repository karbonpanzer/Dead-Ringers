using System.Collections.Generic;
using RimWorld;
using UnitedFront.Comps;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace UnitedFront.UI
{
    public sealed class DialogEditColors : Window
    {
        private sealed class Piece
        {
            public Apparel Apparel;
            public CompEditDecalMarker Comp;
            public List<Color> Working;
            public List<Color> Original;
        }

        private const int ColorCount = 2;

        private readonly Pawn _pawn;
        private readonly List<Piece> _pieces = new List<Piece>();
        private int _sel;
        private bool _committed;
        private List<Color> _allColors;

        private static readonly Vector2 ButSize = new Vector2(200f, 40f);
        private static readonly Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);
        private const float PortraitZoom = 1.3f;
        private const float LeftRectPercent = 0.42f;
        private const float TabMargin = 18f;

        public override Vector2 InitialSize => new Vector2(1000f, 760f);

        public DialogEditColors(Pawn pawn)
        {
            _pawn = pawn;
            forcePause = true;
            doCloseX = true;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;

            if (pawn.apparel != null)
            {
                foreach (Apparel ap in pawn.apparel.WornApparel)
                {
                    CompEditDecalMarker comp = ap.TryGetComp<CompEditDecalMarker>();
                    if (comp == null) continue;

                    var working = new List<Color>(comp.ZoneColors);
                    while (working.Count < ColorCount) working.Add(Color.white);
                    if (working.Count > ColorCount) working.RemoveRange(ColorCount, working.Count - ColorCount);

                    _pieces.Add(new Piece
                    {
                        Apparel = ap,
                        Comp = comp,
                        Working = working,
                        Original = new List<Color>(comp.ZoneColors)
                    });
                }
            }
        }

        public override void Close(bool doCloseSound = true)
        {
            foreach (Piece p in _pieces)
            {
                if (_committed) p.Comp.CommitZones(p.Working);
                else p.Comp.PreviewZones(p.Original);
            }
            PortraitsCache.SetDirty(_pawn);
            base.Close(doCloseSound);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (_pawn.Destroyed) { Close(false); return; }

            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect) { height = Text.LineHeight * 2f };
            Widgets.Label(titleRect, "Edit armour colours - " + _pawn.Name.ToStringShort);
            Text.Font = GameFont.Small;
            inRect.yMin = titleRect.yMax + 4f;

            if (_pieces.Count == 0)
            {
                Widgets.NoneLabelCenteredVertically(new Rect(inRect.x, inRect.y, inRect.width, inRect.height - ButSize.y));
                DrawBottomButtons(inRect);
                return;
            }

            Rect leftRect = inRect;
            leftRect.width *= LeftRectPercent;
            leftRect.yMax -= ButSize.y + 4f;
            DrawPawn(leftRect);

            Rect rightRect = inRect;
            rightRect.xMin = leftRect.xMax + 10f;
            rightRect.yMax -= ButSize.y + 4f;
            DrawRight(rightRect);

            DrawBottomButtons(inRect);
        }

        private void DrawPawn(Rect rect)
        {
            Widgets.BeginGroup(rect);
            Rect inner = new Rect(0f, 0f, rect.width, rect.height).ContractedBy(4f);
            RenderTexture portrait = PortraitsCache.Get(
                _pawn, new Vector2(inner.width, inner.height), Rot4.South,
                PortraitOffset, PortraitZoom,
                supersample: true, compensateForUIScale: true,
                renderHeadgear: true, renderClothes: true);
            GUI.DrawTexture(inner, portrait);
            Widgets.EndGroup();
        }

        private void DrawRight(Rect rect)
        {
            var tabs = new List<TabRecord>(_pieces.Count);
            for (int i = 0; i < _pieces.Count; i++)
            {
                int idx = i;
                tabs.Add(new TabRecord(_pieces[i].Apparel.LabelCap, () => _sel = idx, _sel == i));
            }

            Widgets.DrawMenuSection(rect);
            TabDrawer.DrawTabs(rect, tabs);
            rect = rect.ContractedBy(TabMargin);

            if (_sel < 0 || _sel >= _pieces.Count) _sel = 0;
            Piece p = _pieces[_sel];

            float rowGap = 14f;
            float rowH = (rect.height - rowGap * (ColorCount - 1)) / ColorCount;
            for (int c = 0; c < ColorCount; c++)
            {
                Rect row = new Rect(rect.x, rect.y + c * (rowH + rowGap), rect.width, rowH);
                DrawColorRow(row, p, c);
            }
        }

        private void DrawColorRow(Rect row, Piece p, int index)
        {
            float labelH = 26f;
            float btnH = 24f;
            float gap = 8f;

            Widgets.Label(new Rect(row.x, row.y, row.width, labelH),
                index == 0 ? "Primary colour" : "Secondary colour");

            Rect btnRow = new Rect(row.x, row.yMax - btnH, row.width, btnH);
            Rect palette = new Rect(row.x, row.y + labelH, row.width, row.height - labelH - btnH - gap);

            Color c = p.Working[index];
            Color original = c;

            DrawColorGrid(palette, AllColors(), ref c);

            if (Widgets.ButtonText(new Rect(btnRow.x, btnRow.y, 140f, btnH), "Random"))
            {
                var colors = AllColors();
                if (colors.Count > 0) c = colors[Rand.Range(0, colors.Count)];
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }

            if (!original.IndistinguishableFrom(c))
            {
                p.Working[index] = c;
                p.Comp.PreviewZones(p.Working);
                PortraitsCache.SetDirty(_pawn);
            }
        }

        private static void DrawColorGrid(Rect rect, List<Color> colors, ref Color selected)
        {
            if (colors.Count == 0) return;

            const float pad = 1f;
            const float minCell = 8f;
            const float maxCell = 24f;

            float cell = minCell;
            int cols = Mathf.Max(1, Mathf.FloorToInt(rect.width / (minCell + pad)));
            for (float s = maxCell; s >= minCell; s -= 1f)
            {
                int cc = Mathf.Max(1, Mathf.FloorToInt(rect.width / (s + pad)));
                int rr = Mathf.CeilToInt((float)colors.Count / cc);
                if (rr * (s + pad) <= rect.height)
                {
                    cell = s;
                    cols = cc;
                    break;
                }
            }

            float gridW = cols * (cell + pad) - pad;
            float x0 = rect.x + Mathf.Max(0f, (rect.width - gridW) / 2f);

            for (int i = 0; i < colors.Count; i++)
            {
                int col = i % cols;
                int rowIndex = i / cols;
                Rect box = new Rect(x0 + col * (cell + pad), rect.y + rowIndex * (cell + pad), cell, cell);
                if (box.yMax > rect.yMax + 0.5f) break;

                Widgets.DrawBoxSolid(box, colors[i]);
                if (Mouse.IsOver(box)) Widgets.DrawHighlight(box);
                if (colors[i].IndistinguishableFrom(selected)) Widgets.DrawBox(box, 2);
                if (Widgets.ButtonInvisible(box)) selected = colors[i];
            }
        }

        private void DrawBottomButtons(Rect inRect)
        {
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "Cancel"))
            {
                _committed = false;
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.xMin + inRect.width / 2f - ButSize.x / 2f, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "Reset"))
            {
                foreach (Piece p in _pieces)
                {
                    p.Working = new List<Color>(p.Original);
                    while (p.Working.Count < ColorCount) p.Working.Add(Color.white);
                    p.Comp.PreviewZones(p.Working);
                }
                PortraitsCache.SetDirty(_pawn);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }

            if (Widgets.ButtonText(new Rect(inRect.xMax - ButSize.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "Accept"))
            {
                _committed = true;
                Close();
            }
        }

        private List<Color> AllColors()
        {
            if (_allColors != null) return _allColors;

            HashSet<Color> set = new HashSet<Color>();
            foreach (ColorDef def in DefDatabase<ColorDef>.AllDefs)
            {
                if (def.colorType == ColorType.Ideo || def.colorType == ColorType.Misc || def.colorType == ColorType.Structure)
                {
                    bool dup = false;
                    foreach (Color c in set)
                    {
                        if (c.IndistinguishableFrom(def.color)) { dup = true; break; }
                    }
                    if (!dup) set.Add(def.color);
                }
            }

            _allColors = new List<Color>(set);
            _allColors.Sort((a, b) =>
            {
                Color.RGBToHSV(a, out float hA, out float sA, out _);
                Color.RGBToHSV(b, out float hB, out float sB, out _);
                int cmp = hA.CompareTo(hB);
                return (cmp != 0) ? cmp : sA.CompareTo(sB);
            });
            return _allColors;
        }
    }
}
