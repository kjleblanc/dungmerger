using UnityEngine;

namespace MergeDungeon.Core
{
    public class UISelectionManager : MonoBehaviour
    {
        [Header("Visual Defaults")]
        public bool overrideTileVisuals = false;
        public bool overrideHighlightVisuals = true;
        public Color selectedColor = new Color(1f, 0.95f, 0.3f, 0.9f);
        public bool scaleOnSelect = true;
        [Min(1f)] public float selectionScale = 1.06f;
        public Vector2 outlineEffectDistance = new Vector2(2f, -2f);
        [Header("Fade Defaults")]
        public bool overrideHighlightFade = true;
        public bool highlightFadeEnabled = true;
        [Min(0f)] public float highlightFadeIn = 0.12f;
        [Min(0f)] public float highlightFadeOut = 0.12f;
        public AnimationCurve highlightFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private static UISelectionManager _instance;
        public static UISelectionManager Instance
        {
            get
            {
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("UISelectionManager: Duplicate instance found; destroying this one.");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            // Comment in if you want this to persist across scenes:
            // DontDestroyOnLoad(gameObject);
        }

        private ISelectable _selected;
        private GameObject _selectedGO;
        private BoardCell _selectedCell;
        private SelectableHighlight _selectedHighlight;
        public GameObject CurrentSelectedGO => _selectedGO;

        public void HandleClick(GameObject clickedGO)
        {
            if (clickedGO == null)
            {
                ClearSelection();
                return;
            }
            var selectable = clickedGO.GetComponentInParent<ISelectable>();
            if (selectable == null)
            {
                ClearSelection();
                return;
            }
            if (selectable == _selected)
            {
                // Second tap on same target -> activate
                _selected.OnActivateTap();
                return;
            }
            // New selection
            SetSelection(selectable, (selectable as Component)?.gameObject ?? clickedGO);
        }

        public void ClearSelection()
        {
            // Prefer clearing the stored highlight directly so we don't depend on the selected GO still existing
            if (_selectedHighlight != null)
            {
                _selectedHighlight.SetSelected(false);
            }
            else if (_selectedGO != null)
            {
                // Fallback path
                ApplySelectedVisual(_selectedGO, false);
            }
            _selected = null;
            _selectedGO = null;
            _selectedCell = null;
            _selectedHighlight = null;
        }

        // Clears without touching visuals (useful when the target is being destroyed)
        public void ClearSelectionSilently()
        {
            // Same as ClearSelection but kept for API compatibility
            ClearSelection();
        }

        private void SetSelection(ISelectable sel, GameObject go)
        {
            // Clear previous selection visuals
            ClearSelection();

            _selected = sel;
            _selectedGO = go;
            _selectedCell = go != null ? go.GetComponentInParent<BoardCell>() : null;
            sel.OnSelectTap();

            // Acquire or create highlight on the cell background and enable it
            _selectedHighlight = GetCellHighlight(_selectedCell, createIfMissing: true);
            if (_selectedHighlight != null)
            {
                // For cell highlight, avoid scaling to not affect layout
                if (overrideHighlightVisuals)
                {
                    _selectedHighlight.ApplyStyle(selectedColor, false, 1f, outlineEffectDistance);
                }
                _selectedHighlight.SetSelected(true);
            }
            else if (_selectedGO != null)
            {
                // Fallback if no cell is found
                ApplySelectedVisual(_selectedGO, true);
            }
        }

        private SelectableHighlight GetCellHighlight(BoardCell cell, bool createIfMissing)
        {
            if (cell == null) return null;
            var target = cell.bg != null ? cell.bg.gameObject : cell.gameObject;
            var hl = target.GetComponent<SelectableHighlight>();
            if (hl == null && createIfMissing)
            {
                hl = target.AddComponent<SelectableHighlight>();
            }
            return hl;
        }

        private void ApplySelectedVisual(GameObject go, bool selected)
        {
            if (go == null) return;
            // Prefer highlighting the BoardCell (tile slot) so selection surrounds the cell, not the occupant
            var cell = go.GetComponentInParent<BoardCell>();
            if (cell != null)
            {
                GameObject cellTarget = cell.bg != null ? cell.bg.gameObject : cell.gameObject;
                var cellHl = cellTarget.GetComponent<SelectableHighlight>();
                if (cellHl == null && selected)
                {
                    cellHl = cellTarget.AddComponent<SelectableHighlight>();
                }
                if (cellHl != null)
                {
                    // For cell highlight, avoid scaling to not affect layout
                    var scaleFlag = false;
                    var scaleVal = 1f;
                    var color = selectedColor;
                    var dist = outlineEffectDistance;
                if (overrideHighlightVisuals)
                {
                    cellHl.ApplyStyle(color, scaleFlag, scaleVal, dist);
                }
                if (overrideHighlightFade)
                {
                    cellHl.ApplyFadeStyle(highlightFadeEnabled, highlightFadeIn, highlightFadeOut, highlightFadeCurve);
                }
                cellHl.SetSelected(selected);
                return;
            }
            }

            // Fallbacks: tile visuals or generic highlight on clicked object
            var tile = go.GetComponentInParent<TileBase>();
            if (tile != null)
            {
                if (overrideTileVisuals)
                {
                    tile.selectionColor = selectedColor;
                    tile.scaleOnSelect = scaleOnSelect;
                    tile.selectionScale = selectionScale;
                }
                tile.SetSelected(selected);
                return;
            }
            // Else use a SelectableHighlight (no add during deselect to avoid destroy race)
            var hl = go.GetComponentInParent<SelectableHighlight>();
            if (hl != null)
            {
                if (overrideHighlightVisuals)
                {
                    hl.ApplyStyle(selectedColor, scaleOnSelect, selectionScale, outlineEffectDistance);
                }
                if (overrideHighlightFade)
                {
                    hl.ApplyFadeStyle(highlightFadeEnabled, highlightFadeIn, highlightFadeOut, highlightFadeCurve);
                }
                hl.SetSelected(selected);
                return;
            }
            if (selected)
            {
                hl = go.AddComponent<SelectableHighlight>();
                if (overrideHighlightVisuals)
                {
                    hl.ApplyStyle(selectedColor, scaleOnSelect, selectionScale, outlineEffectDistance);
                }
                if (overrideHighlightFade)
                {
                    hl.ApplyFadeStyle(highlightFadeEnabled, highlightFadeIn, highlightFadeOut, highlightFadeCurve);
                }
                hl.SetSelected(true);
            }
        }
    }
}
