using System;
using System.Collections.Generic;
using RimWorld;
using UnitedFront.Comps;
using UnitedFront.Decals;
using UnitedFront.Defs;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace UnitedFront.UI
{
    [StaticConstructorOnStartup]
    public sealed class DialogEditDecals : Window
    {
        private readonly Pawn _pawn;
        private DecalProfileSet _profileSet;
        private readonly DecalProfileSet _original;
        private List<DecalSymbol> _symbols;

        private int _selectedHelmetIndex, _selectedArmorIndex;

        private bool _committed;

        private DecalSlot _curTab = DecalSlot.Armor;
        private Vector2 _armorScrollPosition;
        private Vector2 _helmetScrollPosition;

        private float _viewRectHeight;
        private float _colorsHeight;

        private List<Color>? _allColors;

        private readonly Dictionary<string, Texture2D> _thumbCache = new Dictionary<string, Texture2D>();

        private readonly CompEditDecalMarker? _colorComp;
        private List<Color>? _workingZones;
        private List<Color>? _originalZones;
        private bool _showColours;
        private static readonly string[] ZoneKeys = { "UnitedFront_Zone_Body", "UnitedFront_Zone_LeftShoulder", "UnitedFront_Zone_RightShoulder", "UnitedFront_Zone_Emblem", "UnitedFront_Zone_Codpiece", "UnitedFront_Zone_Hips" };

        private static readonly Vector2 ButSize = new Vector2(200f, 40f);
        private static readonly Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);
        private const float PortraitZoom = 1.3f;
        private const float TabMargin = 18f;
        private const float IconSize = 60f;
        private const float LeftRectPercent = 0.42f;

        public override Vector2 InitialSize => new Vector2(1200f, 800f);

        public DialogEditDecals(Pawn pawn)
        {
            _pawn = pawn;
            forcePause = true;
            doCloseX = true;
            closeOnAccept = false;
            closeOnCancel = false;
            absorbInputAroundWindow = true;

            _profileSet = DecalUtil.ProfileSetOn(_pawn);
            _original = _profileSet;

            _symbols = new List<DecalSymbol>(DecalUtil.SymbolsForSlot(_curTab));
            _symbols.Sort((a, b) => string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.Ordinal));

            _selectedHelmetIndex = FindSymbolIndex(_profileSet.Helmet.SymbolPath);
            _selectedArmorIndex = FindSymbolIndex(_profileSet.Armor.SymbolPath);
            SyncSelection();
            DecalUtil.Preview(_pawn, _profileSet);

            _colorComp = DecalUtil.MarkerOn(_pawn);
            if (_colorComp != null)
            {
                _originalZones = new List<Color>(_colorComp.ZoneColors);
                _workingZones = new List<Color>(_colorComp.ZoneColors);
            }
        }

        public override void Close(bool doCloseSound = true)
        {
            DecalUtil.FinishPreview(_pawn, _committed, _original);

            if (_colorComp != null)
            {
                if (_committed && _workingZones != null) _colorComp.CommitZones(_workingZones);
                else if (_originalZones != null) _colorComp.PreviewZones(_originalZones);
            }

            base.Close(doCloseSound);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (_pawn.Destroyed) { Close(false); return; }

            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect)
            {
                height = Text.LineHeight * 2f
            };
            Widgets.Label(titleRect, "UnitedFront_StyleDecalsTitle".Translate(_pawn.Name.ToStringShort));
            Text.Font = GameFont.Small;
            inRect.yMin = titleRect.yMax + 4f;

            Rect leftRect = inRect;
            leftRect.width *= LeftRectPercent;
            leftRect.yMax -= ButSize.y + 4f;
            DrawPawn(leftRect);

            Rect rightRect = inRect;
            rightRect.xMin = leftRect.xMax + 10f;
            rightRect.yMax -= ButSize.y + 4f;
            DrawTabs(rightRect);

            DrawBottomButtons(inRect);
        }

        private void DrawPawn(Rect rect)
        {
            Widgets.BeginGroup(rect);
            Rect innerPortrait = new Rect(0f, 0f, rect.width, rect.height).ContractedBy(4f);
            RenderTexture portrait = PortraitsCache.Get(
                _pawn,
                new Vector2(innerPortrait.width, innerPortrait.height),
                Rot4.South,
                PortraitOffset,
                PortraitZoom,
                supersample: true,
                compensateForUIScale: true,
                renderHeadgear: true,
                renderClothes: true);
            GUI.DrawTexture(innerPortrait, portrait);
            Widgets.EndGroup();
        }

        private void DrawTabs(Rect rect)
        {
            var tabs = new List<TabRecord>
            {
                new TabRecord("UnitedFront_Decals_Armor".Translate(),  () => { _showColours = false; SetTab(DecalSlot.Armor); },  !_showColours && _curTab == DecalSlot.Armor),
                new TabRecord("UnitedFront_Decals_Helmet".Translate(), () => { _showColours = false; SetTab(DecalSlot.Helmet); }, !_showColours && _curTab == DecalSlot.Helmet),
                new TabRecord("UnitedFront_Decals_Colours".Translate(), () => { _showColours = true; }, _showColours)
            };

            Widgets.DrawMenuSection(rect);
            TabDrawer.DrawTabs(rect, tabs);
            rect = rect.ContractedBy(TabMargin);

            if (_showColours)
            {
                DrawZoneColours(rect);
                return;
            }

            bool isArmor = (_curTab == DecalSlot.Armor);

            float controlBarH = 28f;
            Rect controlBar = new Rect(rect.x, rect.y, rect.width, controlBarH);

            bool active = isArmor ? _profileSet.Armor.Active : _profileSet.Helmet.Active;
            bool wasActive = active;
            Widgets.CheckboxLabeled(new Rect(controlBar.x, controlBar.y, controlBar.width * 0.5f, controlBarH),
                "UnitedFront_Decals_Enabled".Translate(), ref active);
            if (wasActive != active)
            {
                if (isArmor) _profileSet.Armor.Active = active;
                else _profileSet.Helmet.Active = active;
                PushLive();
            }

            if (Widgets.ButtonText(new Rect(controlBar.xMax - 140f, controlBar.y, 140f, controlBarH),
                "UnitedFront_Decals_RandomSymbol".Translate()) && _symbols.Count > 0)
            {
                if (isArmor) _selectedArmorIndex = Rand.Range(0, _symbols.Count);
                else _selectedHelmetIndex = Rand.Range(0, _symbols.Count);
                SyncSelection();
                PushLive();
            }

            rect.yMin = controlBar.yMax + 6f;
            rect.yMax -= _colorsHeight;

            Vector2 scrollPos = isArmor ? _armorScrollPosition : _helmetScrollPosition;
            DrawSymbolGrid(rect, ref scrollPos);
            if (isArmor) _armorScrollPosition = scrollPos;
            else _helmetScrollPosition = scrollPos;

            DrawColors(new Rect(rect.x, rect.yMax + 10f, rect.width, _colorsHeight));
        }

        private void DrawZoneColours(Rect rect)
        {
            if (_colorComp == null || _workingZones == null || _workingZones.Count == 0)
            {
                Widgets.NoneLabelCenteredVertically(rect);
                return;
            }

            int zones = _workingZones.Count;
            float rowGap = 12f;
            float rowH = (rect.height - rowGap * (zones - 1)) / zones;

            for (int z = 0; z < zones; z++)
            {
                Rect row = new Rect(rect.x, rect.y + z * (rowH + rowGap), rect.width, rowH);
                DrawZoneRow(row, z);
            }
        }

        private void DrawZoneRow(Rect row, int z)
        {
            float labelH = 26f;
            float btnH = 24f;
            float gap = 8f;

            Widgets.Label(new Rect(row.x, row.y, row.width, labelH),
                z < ZoneKeys.Length ? ZoneKeys[z].Translate().ToString() : "Zone " + (z + 1));

            Rect btnRow = new Rect(row.x, row.yMax - btnH, row.width, btnH);
            Rect palette = new Rect(row.x, row.y + labelH, row.width, row.height - labelH - btnH - gap);

            Color c = _workingZones[z];
            Color original = c;

            List<Color> palColors = AllColors();
            DrawColorGrid(palette, palColors, ref c);

            float bgap = 10f;
            bool showIdeo = ModsConfig.IdeologyActive && _pawn.Ideo != null && !Find.IdeoManager.classicMode;
            bool showFav = TryGetFavoriteColor(_pawn, out Color favColor);
            int count = 1 + (showIdeo ? 1 : 0) + (showFav ? 1 : 0);
            float btnW = (btnRow.width - bgap * (count - 1)) / count;
            float btnX = btnRow.x;

            if (showIdeo)
            {
                if (Widgets.ButtonText(new Rect(btnX, btnRow.y, btnW, btnH), "UnitedFront_Decals_IdeoColor".Translate()))
                {
                    c = _pawn.Ideo.ApparelColor;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
                btnX += btnW + bgap;
            }

            if (Widgets.ButtonText(new Rect(btnX, btnRow.y, btnW, btnH), "UnitedFront_Decals_RandomColor".Translate()))
            {
                if (palColors.Count > 0) c = palColors[Rand.Range(0, palColors.Count)];
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            btnX += btnW + bgap;

            if (showFav)
            {
                if (Widgets.ButtonText(new Rect(btnX, btnRow.y, btnW, btnH), "UnitedFront_Decals_FavColor".Translate()))
                {
                    c = favColor;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }

            if (!original.IndistinguishableFrom(c))
            {
                _workingZones[z] = c;
                _colorComp.PreviewZones(_workingZones);
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
                int c = Mathf.Max(1, Mathf.FloorToInt(rect.width / (s + pad)));
                int r = Mathf.CeilToInt((float)colors.Count / c);
                if (r * (s + pad) <= rect.height)
                {
                    cell = s;
                    cols = c;
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

        private void SetTab(DecalSlot slot)
        {
            if (_curTab == slot) return;
            _curTab = slot;
            _symbols = new List<DecalSymbol>(DecalUtil.SymbolsForSlot(_curTab));
            _symbols.Sort((a, b) => string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.Ordinal));
            _selectedHelmetIndex = FindSymbolIndex(_profileSet.Helmet.SymbolPath);
            _selectedArmorIndex = FindSymbolIndex(_profileSet.Armor.SymbolPath);
        }

        private void DrawSymbolGrid(Rect rect, ref Vector2 scrollPosition)
        {
            bool isArmor = (_curTab == DecalSlot.Armor);
            int selectedIdx = isArmor ? _selectedArmorIndex : _selectedHelmetIndex;

            if (_symbols.Count == 0)
            {
                Widgets.NoneLabelCenteredVertically(rect);
                return;
            }

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, _viewRectHeight);
            int columns = Mathf.FloorToInt(viewRect.width / IconSize) - 1;
            if (columns < 1) columns = 1;
            float xPadding = (viewRect.width - columns * IconSize - (columns - 1) * 10f) / 2f;

            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float curY = 0f;
            int col = 0;

            for (int i = 0; i < _symbols.Count; i++)
            {
                if (col >= columns)
                {
                    col = 0;
                    curY += IconSize + 10f;
                }

                Rect iconRect = new Rect(
                    xPadding + col * IconSize + col * 10f,
                    curY,
                    IconSize, IconSize);

                Widgets.DrawHighlight(iconRect);

                if (Mouse.IsOver(iconRect))
                {
                    Widgets.DrawHighlight(iconRect);
                    TooltipHandler.TipRegion(iconRect, _symbols[i].LabelCap);
                }

                Texture2D? thumb = GetThumb(_symbols[i]);
                if (thumb != null)
                {
                    GUI.DrawTexture(iconRect.ContractedBy(4f), thumb, ScaleMode.ScaleToFit);
                }

                if (i == selectedIdx)
                {
                    Widgets.DrawBox(iconRect, 2);
                }

                if (Widgets.ButtonInvisible(iconRect))
                {
                    if (isArmor) _selectedArmorIndex = i;
                    else _selectedHelmetIndex = i;
                    SyncSelection();
                    PushLive();
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }

                col++;
            }

            curY += IconSize + 10f;

            if (Event.current.type == EventType.Layout)
            {
                _viewRectHeight = curY + 10f;
            }

            Widgets.EndScrollView();
        }

        private void DrawColors(Rect rect)
        {
            bool isArmor = (_curTab == DecalSlot.Armor);
            float curY = rect.y;

            Color color = isArmor ? _profileSet.Armor.SymbolColor : _profileSet.Helmet.SymbolColor;
            Color original = color;

            Widgets.ColorSelector(
                new Rect(rect.x, curY, rect.width, _colorsHeight),
                ref color, AllColors(), out _colorsHeight);
            curY += _colorsHeight;

            float spacerGap = 12f;
            curY += spacerGap;

            float totalGap = 12f;
            bool showIdeo = ModsConfig.IdeologyActive && _pawn.Ideo != null && !Find.IdeoManager.classicMode;
            bool showFav = TryGetFavoriteColor(_pawn, out Color favColor);

            int activeButtons = 1;
            if (showIdeo) activeButtons++;
            if (showFav) activeButtons++;

            float btnW = (rect.width - (totalGap * (activeButtons - 1))) / activeButtons;
            float btnH = 24f;
            float btnX = rect.x;

            if (showIdeo)
            {
                if (Widgets.ButtonText(new Rect(btnX, curY, btnW, btnH), "UnitedFront_Decals_IdeoColor".Translate()))
                {
                    if (_pawn.Ideo != null) color = _pawn.Ideo.ApparelColor;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
                btnX += btnW + totalGap;
            }

            if (Widgets.ButtonText(new Rect(btnX, curY, btnW, btnH), "UnitedFront_Decals_RandomColor".Translate()))
            {
                var colors = AllColors();
                if (colors.Count > 0)
                {
                    color = colors[Rand.Range(0, colors.Count)];
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }
            btnX += btnW + totalGap;

            if (showFav)
            {
                if (Widgets.ButtonText(new Rect(btnX, curY, btnW, btnH), "UnitedFront_Decals_FavColor".Translate()))
                {
                    color = favColor;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }

            if (!original.IndistinguishableFrom(color))
            {
                if (isArmor) _profileSet.Armor.SymbolColor = color;
                else _profileSet.Helmet.SymbolColor = color;
                PushLive();
            }

            _colorsHeight += (Text.LineHeight * 2f) + spacerGap;
        }

        private void DrawBottomButtons(Rect inRect)
        {
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "UnitedFront_Decals_Cancel".Translate()))
            {
                _committed = false;
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.xMin + inRect.width / 2f - ButSize.x / 2f, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "UnitedFront_Decals_Reset".Translate()))
            {
                _profileSet = _original;
                _selectedHelmetIndex = FindSymbolIndex(_profileSet.Helmet.SymbolPath);
                _selectedArmorIndex = FindSymbolIndex(_profileSet.Armor.SymbolPath);
                SyncSelection();
                PushLive();

                if (_colorComp != null && _originalZones != null)
                {
                    _workingZones = new List<Color>(_originalZones);
                    _colorComp.PreviewZones(_workingZones);
                }

                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }

            if (Widgets.ButtonText(new Rect(inRect.xMax - ButSize.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "UnitedFront_Decals_Accept".Translate()))
            {
                _committed = true;
                Close();
            }
        }

        private Texture2D? GetThumb(DecalSymbol def)
        {
            if (def.path.NullOrEmpty()) return null;
            if (_thumbCache.TryGetValue(def.path, out Texture2D cached)) return cached;
            Texture2D? tex = ContentFinder<Texture2D>.Get(def.path + "_south", false)
                          ?? ContentFinder<Texture2D>.Get(def.path, false);
            if (tex != null) _thumbCache[def.path] = tex;
            return tex;
        }

        private int FindSymbolIndex(string path)
        {
            if (path.NullOrEmpty() || _symbols.Count == 0) return 0;
            return Mathf.Max(0, _symbols.FindIndex(d => d.path == path));
        }

        private void SyncSelection()
        {
            if (_symbols.Count == 0) return;
            _selectedHelmetIndex = Mathf.Clamp(_selectedHelmetIndex, 0, _symbols.Count - 1);
            _profileSet.Helmet.SymbolPath = _symbols[_selectedHelmetIndex].path;
            _selectedArmorIndex = Mathf.Clamp(_selectedArmorIndex, 0, _symbols.Count - 1);
            _profileSet.Armor.SymbolPath = _symbols[_selectedArmorIndex].path;
        }

        private List<Color> AllColors()
        {
            if (_allColors != null) return _allColors;

            HashSet<Color> colorSet = new HashSet<Color>();

            if (ModsConfig.IdeologyActive && _pawn.Ideo != null && !Find.IdeoManager.classicMode)
                colorSet.Add(_pawn.Ideo.ApparelColor);

            if (TryGetFavoriteColor(_pawn, out Color favColor))
                colorSet.Add(favColor);

            foreach (ColorDef def in DefDatabase<ColorDef>.AllDefs)
            {
                if (def.colorType == ColorType.Ideo || def.colorType == ColorType.Misc || def.colorType == ColorType.Structure)
                {
                    bool duplicate = false;
                    foreach (Color c in colorSet)
                    {
                        if (c.IndistinguishableFrom(def.color))
                        {
                            duplicate = true;
                            break;
                        }
                    }
                    if (!duplicate)
                        colorSet.Add(def.color);
                }
            }

            _allColors = new List<Color>(colorSet);

            _allColors.Sort((a, b) =>
            {
                Color.RGBToHSV(a, out float hA, out float sA, out _);
                Color.RGBToHSV(b, out float hB, out float sB, out _);
                int c = hA.CompareTo(hB);
                return (c != 0) ? c : sA.CompareTo(sB);
            });

            return _allColors;
        }

        private static bool TryGetFavoriteColor(Pawn? pawn, out Color c)
        {
            c = Color.white;
            if (!ModsConfig.IdeologyActive || pawn?.story == null || pawn.DevelopmentalStage.Baby()) return false;
            ColorDef def = pawn.story.favoriteColor;
            if (def == null) return false;
            c = def.color;
            return true;
        }

        private void PushLive() => DecalUtil.Preview(_pawn, _profileSet);
    }
}
