#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace CorePro.UI.Editor
{
    public class UIStyleAuditWindow : EditorWindow
    {
        // Row data 

        private struct Row
        {
            public GameObject go;
            public string     goName;
            public string     path;
            public bool       hasStyle;
            public bool       missingSheet;
            public bool       isActive;
        }

        // Sort 
        private enum SortColumn { Status, Active, Name, Path }
        private enum SortDir    { Asc, Desc }

        // Data 
        private List<Row> _imgRows = new List<Row>();
        private List<Row> _txtRows = new List<Row>();

        // Filtered + sorted views
        private List<Row> _visImg = new List<Row>();
        private List<Row> _visTxt = new List<Row>();

        private int _imgTotal, _imgMissing;
        private int _txtTotal, _txtMissing;

        // Sort state per section
        private SortColumn _imgSortCol = SortColumn.Status; private SortDir _imgSortDir = SortDir.Asc;
        private SortColumn _txtSortCol = SortColumn.Status; private SortDir _txtSortDir = SortDir.Asc;

        // Selection (multi-select with Ctrl/Shift)
        private HashSet<int> _selImg = new HashSet<int>();
        private HashSet<int> _selTxt = new HashSet<int>();
        private int _lastClickImg = -1;
        private int _lastClickTxt = -1;

        // Scroll (pure float – used with GUI.BeginScrollView) 
        private Vector2 _scrollImg;
        private Vector2 _scrollTxt;

        // GUI state 
        private bool _showImages  = true;
        private bool _showTexts   = true;
        private bool _onlyMissing = false;

        // Layout constants 
        private const float ToolbarH  = 20f;
        private const float TitleH    = 24f;
        private const float HeaderH   = 20f;
        private const float RowH      = 22f;
        private const float WStatus   = 76f;
        private const float WActive   = 48f;
        private const float WName     = 150f;
        private const float WAction   = 144f;
        private const float Pad       = 4f;

        // GUIStyles 
        private GUIStyle _headerBtn;
        private GUIStyle _rowLabel;
        private GUIStyle _rowLabelSel;
        private GUIStyle _miniBtn;
        private GUIStyle _pillOk, _pillMissing, _pillWarning;
        private bool     _stylesBuilt;

        private static readonly Color BgTitle    = new Color(0.14f, 0.14f, 0.14f);
        private static readonly Color BgHeader   = new Color(0.17f, 0.17f, 0.17f);
        private static readonly Color BgRowEven  = new Color(0.21f, 0.21f, 0.21f);
        private static readonly Color BgRowOdd   = new Color(0.19f, 0.19f, 0.19f);
        private static readonly Color BgRowInact = new Color(0.17f, 0.17f, 0.20f);
        private static readonly Color BgRowSel   = new Color(0.17f, 0.36f, 0.53f);
        private static readonly Color ColSep     = new Color(0.30f, 0.30f, 0.30f);
        private static readonly Color ColOk      = new Color(0.22f, 0.68f, 0.37f);
        private static readonly Color ColMissing = new Color(0.78f, 0.24f, 0.20f);
        private static readonly Color ColWarning = new Color(0.83f, 0.58f, 0.07f);

        // Menu 
        [MenuItem("Tools/CorePro/UI Style Audit")]
        public static void Open()
        {
            var w = GetWindow<UIStyleAuditWindow>("UI Style Audit");
            w.minSize = new Vector2(620, 440);
            w.Scan();
        }

        // Scan 
        private void Scan()
        {
            _imgRows.Clear();
            _txtRows.Clear();
            _selImg.Clear();
            _selTxt.Clear();
            _imgTotal = _imgMissing = _txtTotal = _txtMissing = 0;

            foreach (var c in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var s = c.GetComponent<UIStyleImage>();
                bool has = s != null, noSheet = has && s.styleSheet == null;
                _imgRows.Add(new Row { go = c.gameObject, goName = c.gameObject.name,
                    path = BuildPath(c.transform), hasStyle = has,
                    missingSheet = noSheet, isActive = c.gameObject.activeInHierarchy });
                _imgTotal++;
                if (!has || noSheet) _imgMissing++;
            }

            foreach (var c in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var s = c.GetComponent<UIStyleText>();
                bool has = s != null, noSheet = has && s.styleSheet == null;
                _txtRows.Add(new Row { go = c.gameObject, goName = c.gameObject.name,
                    path = BuildPath(c.transform), hasStyle = has,
                    missingSheet = noSheet, isActive = c.gameObject.activeInHierarchy });
                _txtTotal++;
                if (!has || noSheet) _txtMissing++;
            }

            RebuildViews();
            Repaint();
        }

        // Filter & sort 
        private void RebuildViews()
        {
            _visImg = Filtered(_imgRows);
            _visTxt = Filtered(_txtRows);
            DoSort(_visImg, _imgSortCol, _imgSortDir);
            DoSort(_visTxt, _txtSortCol, _txtSortDir);
        }

        private List<Row> Filtered(List<Row> src)
        {
            if (!_onlyMissing) return new List<Row>(src);
            var r = new List<Row>(src.Count);
            foreach (var row in src)
                if (!row.hasStyle || row.missingSheet) r.Add(row);
            return r;
        }

        private static void DoSort(List<Row> list, SortColumn col, SortDir dir)
        {
            list.Sort((a, b) =>
            {
                int c = col switch
                {
                    SortColumn.Status => StatusPrio(a).CompareTo(StatusPrio(b)),
                    SortColumn.Active => a.isActive.CompareTo(b.isActive),
                    SortColumn.Name   => string.Compare(a.goName, b.goName, System.StringComparison.OrdinalIgnoreCase),
                    SortColumn.Path   => string.Compare(a.path,   b.path,   System.StringComparison.OrdinalIgnoreCase),
                    _                 => 0,
                };
                return dir == SortDir.Asc ? c : -c;
            });
        }

        private static int StatusPrio(Row r) => !r.hasStyle ? 0 : r.missingSheet ? 1 : 2;

        private void ToggleSort(ref SortColumn cur, ref SortDir dir, SortColumn clicked, bool isImage)
        {
            if (cur == clicked) dir = dir == SortDir.Asc ? SortDir.Desc : SortDir.Asc;
            else { cur = clicked; dir = SortDir.Asc; }
            RebuildViews();
        }

        // OnGUI – layout via pure Rect math, NO GUILayout inside scroll 
        private void OnGUI()
        {
            BuildStylesIfNeeded();

            float w = position.width;
            float h = position.height;
            float y = 0f;

            // Toolbar
            DrawToolbar(new Rect(0, y, w, ToolbarH));
            y += ToolbarH + 2f;

            float remaining = h - y;

            if (_showImages && _showTexts)
            {
                float half = Mathf.Floor(remaining * 0.5f);
                DrawSection(new Rect(0, y, w, half), _visImg, _imgTotal, _imgMissing,
                    ref _scrollImg, ref _imgSortCol, ref _imgSortDir,
                    ref _selImg, ref _lastClickImg, isImage: true);
                y += half;
                DrawSection(new Rect(0, y, w, remaining - half), _visTxt, _txtTotal, _txtMissing,
                    ref _scrollTxt, ref _txtSortCol, ref _txtSortDir,
                    ref _selTxt, ref _lastClickTxt, isImage: false);
            }
            else if (_showImages)
            {
                DrawSection(new Rect(0, y, w, remaining), _visImg, _imgTotal, _imgMissing,
                    ref _scrollImg, ref _imgSortCol, ref _imgSortDir,
                    ref _selImg, ref _lastClickImg, isImage: true);
            }
            else if (_showTexts)
            {
                DrawSection(new Rect(0, y, w, remaining), _visTxt, _txtTotal, _txtMissing,
                    ref _scrollTxt, ref _txtSortCol, ref _txtSortDir,
                    ref _selTxt, ref _lastClickTxt, isImage: false);
            }
        }

        // Toolbar 
        private void DrawToolbar(Rect r)
        {
            GUI.Box(r, GUIContent.none, EditorStyles.toolbar);
            float x = r.x + 2f;

            if (GUI.Button(new Rect(x, r.y, 60, ToolbarH), "⟳ Scan", EditorStyles.toolbarButton))
                Scan();
            x += 62f;

            bool prev = _onlyMissing;
            _onlyMissing = GUI.Toggle(new Rect(x, r.y, 120, ToolbarH), _onlyMissing,
                "Show only missing", EditorStyles.toolbarButton);
            if (_onlyMissing != prev) RebuildViews();
            x += 122f;

            GUI.Label(new Rect(x, r.y, 8, ToolbarH), "|", EditorStyles.toolbarButton);
            x += 10f;

            _showImages = GUI.Toggle(new Rect(x, r.y, 56, ToolbarH), _showImages, "Images", EditorStyles.toolbarButton);
            x += 58f;
            _showTexts  = GUI.Toggle(new Rect(x, r.y, 50, ToolbarH), _showTexts,  "Texts",  EditorStyles.toolbarButton);

            // Right-side stats & bulk button
            int missing = _imgMissing + _txtMissing;
            int total   = _imgTotal   + _txtTotal;

            if (missing > 0)
            {
                string bulkLabel = $"+ Add All Missing ({missing})";
                float  bulkW     = EditorStyles.toolbarButton.CalcSize(new GUIContent(bulkLabel)).x + 10f;
                if (GUI.Button(new Rect(r.xMax - bulkW - 120f, r.y, bulkW, ToolbarH), bulkLabel, EditorStyles.toolbarButton))
                    AddAllMissing();

                string statLabel = $"⚠  {missing} / {total} need attention";
                GUI.Label(new Rect(r.xMax - 118f, r.y + 1, 114f, ToolbarH - 2), statLabel, _pillWarning);
            }
            else if (total > 0)
            {
                string statLabel = $"✓  All {total} covered";
                GUI.Label(new Rect(r.xMax - 148f, r.y + 1, 144f, ToolbarH - 2), statLabel, _pillOk);
            }
        }

        // Section 
        private void DrawSection(
            Rect rect,
            List<Row> rows, int total, int missing,
            ref Vector2 scroll,
            ref SortColumn sortCol, ref SortDir sortDir,
            ref HashSet<int> selection, ref int lastClick,
            bool isImage)
        {
            float y = rect.y;
            float w = rect.width;

            // Title bar
            EditorGUI.DrawRect(new Rect(0, y, w, TitleH), BgTitle);
            string titleText = isImage ? "Images" : "Texts";
            GUI.Label(new Rect(Pad, y + 4, 120, TitleH), titleText, EditorStyles.boldLabel);

            string stat      = missing == 0 ? $"✓  {total} / {total}" : $"⚠  {total - missing} / {total}";
            GUIStyle statSty = missing == 0 ? _pillOk : _pillWarning;
            Vector2 statSz   = statSty.CalcSize(new GUIContent(stat));
            GUI.Label(new Rect(w - statSz.x - 6, y + 3, statSz.x, TitleH - 4), stat, statSty);
            y += TitleH;

            // Column headers
            DrawHeaders(new Rect(0, y, w, HeaderH), ref sortCol, ref sortDir, isImage);
            y += HeaderH;

            // Separator
            EditorGUI.DrawRect(new Rect(0, y, w, 1), ColSep);
            y += 1f;

            // Scroll area
            float scrollH    = rect.yMax - y;
            float contentH   = rows.Count * RowH;
            Rect  viewRect   = new Rect(0, y, w, scrollH);
            Rect  contentRect = new Rect(0, 0, w - (contentH > scrollH ? 14f : 0f), contentH);

            scroll = GUI.BeginScrollView(viewRect, scroll, contentRect);

            // Virtual: only draw visible rows
            int first = Mathf.Max(0, Mathf.FloorToInt(scroll.y / RowH));
            int last  = Mathf.Min(rows.Count - 1, Mathf.CeilToInt((scroll.y + scrollH) / RowH));

            for (int i = first; i <= last; i++)
            {
                bool selected = selection.Contains(i);
                Rect rowRect  = new Rect(0, i * RowH, contentRect.width, RowH);
                DrawRow(rowRect, rows[i], i, selected, contentRect.width, isImage,
                    ref selection, ref lastClick, rows);
            }

            GUI.EndScrollView();
        }

        // Column headers 
        private void DrawHeaders(Rect r, ref SortColumn sortCol, ref SortDir sortDir, bool isImage)
        {
            EditorGUI.DrawRect(r, BgHeader);
            float x  = r.x;
            float scrollbarOffset = 14f;
            float pathW = Mathf.Max(60f, r.width - WStatus - WActive - WName - WAction - scrollbarOffset - Pad * 2);

            DrawHeaderBtn(new Rect(x, r.y, WStatus, r.height), "Status", SortColumn.Status,
                ref sortCol, ref sortDir, isImage);
            x += WStatus;

            DrawHeaderBtn(new Rect(x, r.y, WActive, r.height), "Active", SortColumn.Active,
                ref sortCol, ref sortDir, isImage);
            x += WActive;

            DrawHeaderBtn(new Rect(x, r.y, WName, r.height), "GameObject", SortColumn.Name,
                ref sortCol, ref sortDir, isImage);
            x += WName;

            DrawHeaderBtn(new Rect(x, r.y, pathW, r.height), "Path", SortColumn.Path,
                ref sortCol, ref sortDir, isImage);
        }

        private void DrawHeaderBtn(Rect r, string label, SortColumn col,
            ref SortColumn sortCol, ref SortDir sortDir, bool isImage)
        {
            string arrow = sortCol == col ? (sortDir == SortDir.Asc ? " ▲" : " ▼") : "";
            if (GUI.Button(r, label + arrow, _headerBtn))
                ToggleSort(ref sortCol, ref sortDir, col, isImage);
        }

        // Row 
        private void DrawRow(
            Rect r, Row row, int index, bool selected,
            float totalW, bool isImage,
            ref HashSet<int> selection, ref int lastClick,
            List<Row> rows)
        {
            Event e = Event.current;

            // Background
            Color bg = selected     ? BgRowSel   :
                       !row.isActive ? BgRowInact :
                       index % 2 == 0 ? BgRowEven : BgRowOdd;
            EditorGUI.DrawRect(r, bg);

            // Calculate action button rect up-front so row-click handler can
            // exclude it – otherwise MouseDown fires on the row before GUI.Button
            // gets the MouseUp it needs, stealing every click on the button.
            float _pathW  = Mathf.Max(60f, totalW - WStatus - WActive - WName - WAction - Pad * 2 - 4f);
            float _btnX   = r.x + 2f + WStatus + WActive + WName + _pathW + Pad;
            Rect  btnRect = new Rect(_btnX, r.y + 3, WAction - 6, RowH - 6);

            // Click anywhere on row EXCEPT the action button = select
            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition) && !btnRect.Contains(e.mousePosition))
            {
                if (e.button == 0)
                {
                    bool ctrl  = e.control || e.command;
                    bool shift = e.shift;

                    if (shift && lastClick >= 0)
                    {
                        // Range select
                        int lo = Mathf.Min(index, lastClick);
                        int hi = Mathf.Max(index, lastClick);
                        if (!ctrl) selection.Clear();
                        for (int i = lo; i <= hi; i++) selection.Add(i);
                    }
                    else if (ctrl)
                    {
                        if (selection.Contains(index)) selection.Remove(index);
                        else selection.Add(index);
                    }
                    else
                    {
                        selection.Clear();
                        selection.Add(index);
                    }

                    lastClick = index;

                    // Sync Unity Selection to all selected rows
                    SyncUnitySelection(rows, selection);

                    e.Use();
                    Repaint();
                }
            }

            // Double-click = ping + focus hierarchy
            if (e.type == EventType.MouseDown && e.clickCount == 2 && r.Contains(e.mousePosition))
            {
                EditorGUIUtility.PingObject(row.go);
                e.Use();
            }

            float x = r.x + 2f;

            // Status pill
            GUIStyle pillSty; string pillLabel;
            if (!row.hasStyle)        { pillSty = _pillMissing; pillLabel = "No Style"; }
            else if (row.missingSheet){ pillSty = _pillWarning; pillLabel = "No Sheet"; }
            else                      { pillSty = _pillOk;      pillLabel = "✓ OK";      }
            GUI.Label(new Rect(x + 2, r.y + 3, WStatus - 6, RowH - 6), pillLabel, pillSty);
            x += WStatus;

            // Active dot
            Color prev = GUI.color;
            GUI.color = row.isActive ? ColOk : new Color(0.5f, 0.5f, 0.5f);
            GUI.Label(new Rect(x, r.y, WActive, RowH), row.isActive ? "●" : "○", EditorStyles.centeredGreyMiniLabel);
            GUI.color = prev;
            x += WActive;

            // GameObject name
            GUI.Label(new Rect(x, r.y + 2, WName - 2, RowH - 4), row.goName, _rowLabel);
            x += WName;

            // Path (stretches)
            float pathW = Mathf.Max(60f, totalW - WStatus - WActive - WName - WAction - Pad * 2 - 4f);
            GUI.Label(new Rect(x, r.y + 2, pathW, RowH - 4), row.path, EditorStyles.miniLabel);
            x += pathW + Pad;

            // Action button (btnRect already calculated above for click exclusion)
            if (!row.hasStyle)
            {
                if (GUI.Button(btnRect, "+ Add UIStyle", _miniBtn))
                { AddStyle(row.go, isImage); Scan(); }
            }
            else if (row.missingSheet)
            {
                if (GUI.Button(btnRect, "Auto-assign Sheet", _miniBtn))
                { AutoAssignSheet(row.go, isImage); Scan(); }
            }
            else
            {
                if (GUI.Button(btnRect, "Ping", _miniBtn))
                    EditorGUIUtility.PingObject(row.go);
            }
        }

        // Selection sync 
        private static void SyncUnitySelection(List<Row> rows, HashSet<int> selection)
        {
            var gos = new List<Object>(selection.Count);
            foreach (int i in selection)
                if (i < rows.Count) gos.Add(rows[i].go);
            Selection.objects = gos.ToArray();
        }

        // Bulk add 
        private void AddAllMissing()
        {
            int count = 0;
            foreach (var r in _imgRows)
            {
                if (!r.hasStyle)        { AddStyle(r.go, true);  count++; }
                else if (r.missingSheet){ AutoAssignSheet(r.go, true); count++; }
            }
            foreach (var r in _txtRows)
            {
                if (!r.hasStyle)        { AddStyle(r.go, false); count++; }
                else if (r.missingSheet){ AutoAssignSheet(r.go, false); count++; }
            }
            Debug.Log($"[UIStyleAudit] Added/fixed {count} components.");
            Scan();
        }

        // Component actions 
        private static void AddStyle(GameObject go, bool isImage)
        {
            Undo.RecordObject(go, "Add UIStyle Component");
            UIStyle comp = isImage
                ? (UIStyle)Undo.AddComponent<UIStyleImage>(go)
                : Undo.AddComponent<UIStyleText>(go);
            AutoAssignSheet(comp);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        private static void AutoAssignSheet(GameObject go, bool isImage)
        {
            var style = isImage
                ? (UIStyle)go.GetComponent<UIStyleImage>()
                : go.GetComponent<UIStyleText>();
            if (style != null) AutoAssignSheet(style);
        }

        private static void AutoAssignSheet(UIStyle style)
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(UIStyleSheet));
            if (guids.Length == 0) return;
            var sheet = AssetDatabase.LoadAssetAtPath<UIStyleSheet>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
            if (sheet == null) return;
            Undo.RecordObject(style, "Assign UIStyleSheet");
            style.styleSheet = sheet;
            EditorUtility.SetDirty(style);
        }

        // Helpers 
        private static string BuildPath(Transform t)
        {
            var sb = new System.Text.StringBuilder();
            var cur = t.parent;
            while (cur != null)
            {
                if (sb.Length > 0) sb.Insert(0, " / ");
                sb.Insert(0, cur.name);
                cur = cur.parent;
            }
            return sb.Length == 0 ? "-" : sb.ToString();
        }

        // GUIStyle builder 
        private void BuildStylesIfNeeded()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _headerBtn = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize  = 10,
            };

            _rowLabel = new GUIStyle(EditorStyles.label)
            {
                fontSize  = 11,
                clipping  = TextClipping.Clip,
            };

            _rowLabelSel = new GUIStyle(_rowLabel)
            {
                normal = { textColor = Color.white },
            };

            _miniBtn = new GUIStyle(EditorStyles.miniButton) { fontSize = 10 };

            _pillOk      = MakePill(ColOk);
            _pillMissing = MakePill(ColMissing);
            _pillWarning = MakePill(ColWarning);
        }

        private static GUIStyle MakePill(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color * 0.65f);
            tex.Apply();
            return new GUIStyle(EditorStyles.miniLabel)
            {
                normal    = { background = tex, textColor = Color.white },
                padding   = new RectOffset(5, 5, 1, 1),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize  = 10,
            };
        }
    }
}
#endif