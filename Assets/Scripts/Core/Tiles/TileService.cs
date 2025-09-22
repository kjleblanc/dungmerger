using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class TileService : ServicesConsumerBehaviour
    {
        public GridManager grid; // optional legacy reference; prefers services.Grid
        public TileBase tilePrefab;
        public TileDatabase tileDatabase;
        public TileFactory tileFactory;

        [Header("Merge Animation")]
        public float mergeTravelDuration = 0.2f;
        [Tooltip("Random per-tile start offset (seconds)")]
        public float mergeStaggerRange = 0.05f;
        [Tooltip("Arc offset as a fraction of distance (0-0.5)")]
        [Range(0f, 0.5f)] public float arcHeightFactor = 0.15f;
        public AnimationCurve moveCurve = null;   // 0->1 easing for movement
        public AnimationCurve scaleCurve = null;  // 1->0 curve for consumed tiles
        [Header("Anticipation")]
        public bool anticipationOnOrigin = true;
        public float anticipationScale = 1.08f;
        public float anticipationDuration = 0.07f;

        [Header("Impact FX")]
        public bool hitStopEnabled = true;
        public float hitStopDuration = 0.06f;
        public bool microShakeEnabled = true;
        public float microShakeAmplitude = 8f; // px
        public float microShakeDuration = 0.1f;
        public GameObject shockwavePrefab; // optional UI Image/circle prefab

        [Header("Spawn Visuals")]
        public bool popSpawnOnMerge = true;
        public float spawnPopDuration = 0.2f;
        public AnimationCurve spawnPopCurve = null; // 0->1 with overshoot

        private void Awake()
        {
            // Default curves if not assigned
            if (moveCurve == null || moveCurve.keys == null || moveCurve.keys.Length == 0)
            {
                moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
            if (scaleCurve == null || scaleCurve.keys == null || scaleCurve.keys.Length == 0)
            {
                // Starts at 1.0, dips slightly (compression), then to 0
                scaleCurve = new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.5f, 0.85f),
                    new Keyframe(1f, 0f)
                );
            }
            if (spawnPopCurve == null || spawnPopCurve.keys == null || spawnPopCurve.keys.Length == 0)
            {
                // 0 -> 1.12 -> 1.0
                spawnPopCurve = new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.6f, 1.12f),
                    new Keyframe(1f, 1f)
                );
            }
        }

        public bool TryPlaceTileInCell(TileBase tile, BoardCell cell)
        {
            var gridManager = services != null ? services.Grid : grid;
            if (gridManager != null && gridManager.ArePlayerActionsLocked) return false;
            if (cell == null) return false;
            if (!cell.IsFreeForTile()) return false;
            if (tile.currentCell != null)
            {
                tile.currentCell.ClearTileIf(tile);
            }
            cell.SetTile(tile);
            return true;
        }

        // Legacy enum-based spawn helpers removed

        public bool TryMergeOnDrop(TileBase source, TileBase target)
        {
            var gridManager = services != null ? services.Grid : grid;
            if (gridManager != null && gridManager.ArePlayerActionsLocked) return false;
            if (services == null && grid == null) return false;
            if (source == null || target == null) return false;
            if (target.currentCell == null) return false;

            // Resolve definitions
            var sourceDef = source.def;
            var targetDef = target.def;

            TileDefinition.MergeRule defRule = null;
            int defToConsume = 0;

            if (sourceDef != null && targetDef != null)
            {
                // Consider merges symmetric between the two definitions
                bool sameBucket = SameBucket(sourceDef, targetDef);
                if (!sameBucket) return false;

                var originCellDef = target.currentCell;
                // Collect contiguous tiles matching either source or target definition (symmetric)
                var groupDef = CollectConnectedTilesOfEither(originCellDef, sourceDef, targetDef);
                if (source.currentCell != null)
                    groupDef.Remove(source.currentCell);
                int totalWithDropDef = groupDef.Count + 1;
                var sourceMerge = sourceDef != null ? sourceDef.MergeModule : null;
                var targetMerge = targetDef != null ? targetDef.MergeModule : null;


                // Pick rule from the definition that actually defines it
                if (totalWithDropDef >= 5)
                {
                    if (targetMerge != null && targetMerge.fiveOfAKind != null && targetMerge.fiveOfAKind.output != null) defRule = targetMerge.fiveOfAKind;
                    else if (sourceMerge != null && sourceMerge.fiveOfAKind != null && sourceMerge.fiveOfAKind.output != null) defRule = sourceMerge.fiveOfAKind;
                }
                if (defRule == null && totalWithDropDef >= 3)
                {
                    if (targetMerge != null && targetMerge.threeOfAKind != null && targetMerge.threeOfAKind.output != null) defRule = targetMerge.threeOfAKind;
                    else if (sourceMerge != null && sourceMerge.threeOfAKind != null && sourceMerge.threeOfAKind.output != null) defRule = sourceMerge.threeOfAKind;
                }
                if (defRule == null) return false;

                defToConsume = Mathf.Max(2, defRule.countToConsume);
                
                var orderedDef = groupDef.OrderBy(c => Manhattan(c, originCellDef)).ToList();
                var consumeCellsDef = new List<BoardCell>();
                for (int i = 0; i < orderedDef.Count && consumeCellsDef.Count < (defToConsume - 1); i++)
                {
                    var c = orderedDef[i];
                    if (c != null && c.tile != null)
                        consumeCellsDef.Add(c);
                }
                var allConsumed = new List<BoardCell>();
                if (source.currentCell != null)
                {
                    allConsumed.Add(source.currentCell);
                }
                allConsumed.AddRange(consumeCellsDef);

                StartCoroutine(AnimateMerge(originCellDef, allConsumed, () => FinalizeMerge(originCellDef, defRule, consumeCellsDef)));

                return true;
            }

            // Legacy merge path removed. Only definition-driven merges are supported now.
            return false;
        }

        private static bool SameBucket(TileDefinition a, TileDefinition b)
        {
            if (a == null || b == null) return false;
            if (a == b) return true;
            if (a.mergesWith == b) return true;
            if (b.mergesWith == a) return true;
            return false;
        }

        public List<BoardCell> CollectConnectedTilesOfEither(BoardCell originCell, TileDefinition a, TileDefinition b)
        {
            var visited = new HashSet<BoardCell>();
            var list = new List<BoardCell>();
            var q = new Queue<BoardCell>();
            if (originCell == null) return list;
            visited.Add(originCell);
            q.Enqueue(originCell);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c.tile != null)
                {
                    var td = c.tile.def;
                    if (td != null && (td == a || td == b))
                    {
                        list.Add(c);
                        TryEnqueue(c.x + 1, c.y);
                        TryEnqueue(c.x - 1, c.y);
                        TryEnqueue(c.x, c.y + 1);
                        TryEnqueue(c.x, c.y - 1);
                    }
                }
            }
            return list;

            void TryEnqueue(int x, int y)
            {
                var n = services != null && services.Board != null ? services.Board.GetCell(x, y) : null;
                if (n != null && !visited.Contains(n))
                {
                    visited.Add(n);
                    var nd = n.tile != null ? n.tile.def : null;
                    if (nd != null && (nd == a || nd == b))
                    {
                        q.Enqueue(n);
                    }
                }
            }
        }

        public IEnumerator AnimateMerge(BoardCell origin, List<BoardCell> consumed, System.Action onComplete)
        {
                var dragLayer = services != null && services.DragLayer != null ? services.DragLayer.dragLayer : null;
            var rts = new List<RectTransform>();
            var startPos = new List<Vector3>();
            var startScale = new List<Vector3>();
            var controls = new List<Vector3>();
            var offsets = new List<float>();

            // Optional anticipation pulse on the origin tile
            if (anticipationOnOrigin && origin != null && origin.tile != null)
            {
                var ort = origin.tile.GetComponent<RectTransform>();
                if (ort != null) StartCoroutine(PulseScale(ort, anticipationScale, anticipationDuration));
            }

            // Prepare moving tiles
            foreach (var cell in consumed)
            {
                if (cell != null && cell.tile != null)
                {
                    var rt = cell.tile.GetComponent<RectTransform>();
                    if (rt == null) continue;
                    rts.Add(rt);
                    startPos.Add(rt.position);
                    startScale.Add(rt.localScale);
                    // Compute arc control point
                    Vector3 p0 = rt.position;
                    Vector3 p1 = origin.rectTransform.position;
                    Vector3 dir = (p1 - p0);
                    float dist = dir.magnitude;
                    Vector3 mid = p0 + dir * 0.5f;
                    Vector3 perp = Vector3.Cross(dir.normalized, Vector3.forward).normalized; // 2D UI, Z-forward
                    float sign = Random.value < 0.5f ? -1f : 1f;
                    float arc = Mathf.Clamp01(arcHeightFactor) * dist;
                    controls.Add(mid + perp * arc * sign);
                    // Stagger per tile
                    offsets.Add(Random.Range(0f, Mathf.Max(0f, mergeStaggerRange)));
                    // Reparent and draw above
                    rt.SetParent(dragLayer, true);
                    rt.SetAsLastSibling();
                    // Clear tile->cell link to block further interactions during anim
                    cell.tile.currentCell = null;
                }
            }

            // Animate along arcs with easing and scaling down
            float elapsed = 0f;
            Vector3 targetPos = origin.rectTransform.position;
            float duration = Mathf.Max(0.01f, mergeTravelDuration);
            while (elapsed < duration + (offsets.Count > 0 ? Mathf.Max(0f, Mathf.Max(offsets.ToArray())) : 0f))
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < rts.Count; i++)
                {
                    var rt = rts[i];
                    if (rt == null) continue;
                    float u = Mathf.Clamp01((elapsed - offsets[i]) / duration);
                    if (u <= 0f) continue; // hasn't started
                    float tt = moveCurve != null ? moveCurve.Evaluate(u) : u;
                    // Quadratic Bezier
                    Vector3 p0 = startPos[i];
                    Vector3 c = controls[i];
                    Vector3 p1 = targetPos;
                    Vector3 a = Vector3.Lerp(p0, c, tt);
                    Vector3 b = Vector3.Lerp(c, p1, tt);
                    rt.position = Vector3.Lerp(a, b, tt);
                    // Scale via curve (1->0)
                    float s = scaleCurve != null ? Mathf.Max(0f, scaleCurve.Evaluate(u)) : (1f - u);
                    Vector3 baseScale = startScale[i];
                    rt.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
                }
                yield return null;
            }

            // Cleanup consumed tiles
            foreach (var cell in consumed)
            {
                if (cell != null && cell.tile != null)
                {
                    var discarded = cell.tile;
                    cell.ClearTileIf(discarded);
                    if (discarded != null)
                    {
                        Destroy(discarded.gameObject);
                    }
                }
            }

            // Impact FX (optional)
            if (hitStopEnabled)
            {
                float prev = Time.timeScale;
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, hitStopDuration));
                Time.timeScale = prev;
            }

            // Board-wide shake removed; shake will be applied to spawned output tile instead.

            // Optional shockwave prefab at impact point
            if (shockwavePrefab != null && origin != null && origin.rectTransform != null)
            {
                Transform parent = GetFxLayerParent();
                var sw = Instantiate(shockwavePrefab, parent != null ? parent : origin.rectTransform.parent);
                var rt = sw.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.position = origin.rectTransform.position;
                    rt.SetAsLastSibling();
                }
            }

            onComplete?.Invoke();
        }

        private void FinalizeMerge(BoardCell originCellDef, TileDefinition.MergeRule defRule, List<BoardCell> consumeCellsDef)
        {
            // Remove existing tile at origin, if any
            if (originCellDef != null && originCellDef.tile != null)
            {
                var existing = originCellDef.tile;
                originCellDef.ClearTileIf(existing);
                if (existing != null)
                {
                    Destroy(existing.gameObject);
                }
            }
            var outputDef = defRule.output;
            int defToProduce = Mathf.Max(1, defRule.outputCount);

            void PlaceUpgradeAtDef(BoardCell cell, bool shake)
            {
                TileBase nt = tileFactory != null ? tileFactory.Create(outputDef) : Instantiate(tilePrefab);
                if (nt != null)
                {
                    if (nt.def == null && outputDef != null) nt.SetDefinition(outputDef);
                    nt.RefreshVisual();
                    cell.SetTile(nt);
                    if (popSpawnOnMerge)
                    {
                        var rt = nt.GetComponent<RectTransform>();
                        if (rt != null) StartCoroutine(SpawnPop(rt, spawnPopDuration));
                    }
                    if (microShakeEnabled && shake)
                    {
                        var rt = nt.GetComponent<RectTransform>();
                        if (rt != null) StartCoroutine(MicroShake(rt, microShakeAmplitude, microShakeDuration));
                    }
                }
            }

            PlaceUpgradeAtDef(originCellDef, true);
            if (defToProduce > 1)
            {
                BoardCell second = null;
                foreach (var c in consumeCellsDef)
                {
                    if (c != null && c != originCellDef)
                    {
                        second = c;
                        break;
                    }
                }
                if (second == null)
                {
                    var empties = services != null && services.Board != null ? services.Board.CollectEmptyCells() : new List<BoardCell>();
                    if (empties.Count > 0)
                        second = empties[Random.Range(0, empties.Count)];
                }
                if (second != null)
                {
                    PlaceUpgradeAtDef(second, false);
                }
            }
        }

        // --- Helpers ---
        private IEnumerator PulseScale(RectTransform rt, float targetScale, float duration)
        {
            if (rt == null) yield break;
            float dur = Mathf.Max(0.01f, duration);
            Vector3 start = rt.localScale;
            Vector3 peak = start * Mathf.Max(1f, targetScale);
            float half = dur * 0.5f;
            float t = 0f;
            // Up
            while (t < half)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / half);
                rt.localScale = Vector3.Lerp(start, peak, u);
                yield return null;
            }
            // Down
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / half);
                rt.localScale = Vector3.Lerp(peak, start, u);
                yield return null;
            }
            rt.localScale = start;
        }

        private IEnumerator MicroShake(RectTransform rt, float amplitude, float duration)
        {
            if (rt == null) yield break;
            Vector2 original = rt.anchoredPosition;
            float dur = Mathf.Max(0.01f, duration);
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float damper = 1f - Mathf.Clamp01(t / dur);
                float ax = (Random.value * 2f - 1f) * amplitude * damper;
                float ay = (Random.value * 2f - 1f) * amplitude * damper;
                rt.anchoredPosition = original + new Vector2(ax, ay);
                yield return null;
            }
            rt.anchoredPosition = original;
        }

        private IEnumerator SpawnPop(RectTransform rt, float duration)
        {
            if (rt == null) yield break;
            float dur = Mathf.Max(0.01f, duration);
            Vector3 baseScale = rt.localScale;
            // Start from zero scale
            rt.localScale = Vector3.zero;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float s = spawnPopCurve != null ? spawnPopCurve.Evaluate(u) : u;
                // Overshoot handled in curve (e.g., 1.12)
                rt.localScale = baseScale * Mathf.Max(0f, s);
                yield return null;
            }
            rt.localScale = baseScale;
        }

        private RectTransform GetShakeTarget()
        {
            if (services != null && services.Board != null && services.Board.boardContainer != null)
                return services.Board.boardContainer;
            if (services != null && services.DragLayer != null && services.DragLayer.dragLayer != null)
                return services.DragLayer.dragLayer as RectTransform;
            return null;
        }

        private Transform GetFxLayerParent()
        {
            // Prefer FX layer if available; otherwise default to drag layer
            if (services != null)
            {
                var vfx = services.Fx;
                if (vfx != null && vfx.fxLayer != null) return vfx.fxLayer;
                if (services.DragLayer != null && services.DragLayer.dragLayer != null) return services.DragLayer.dragLayer;
            }
            return null;
        }

        public List<BoardCell> CollectConnectedTilesOfDefinition(BoardCell originCell, TileDefinition def, TileDefinition mergesWith)
        {
            var visited = new HashSet<BoardCell>();
            var list = new List<BoardCell>();
            var q = new Queue<BoardCell>();
            visited.Add(originCell);
            q.Enqueue(originCell);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c.tile != null)
                {
                    var td = c.tile.def;
                    if (td != null && (td == def || (mergesWith != null && td == mergesWith)))
                    {
                        list.Add(c);
                        TryEnqueue(c.x + 1, c.y);
                        TryEnqueue(c.x - 1, c.y);
                        TryEnqueue(c.x, c.y + 1);
                        TryEnqueue(c.x, c.y - 1);
                    }
                }
            }
            return list;

            void TryEnqueue(int x, int y)
            {
                var n = services != null && services.Board != null ? services.Board.GetCell(x, y) : null;
                if (n != null && !visited.Contains(n))
                {
                    visited.Add(n);
                    var nd = n.tile != null ? n.tile.def : null;
                    if (nd != null && (nd == def || (mergesWith != null && nd == mergesWith)))
                    {
                        q.Enqueue(n);
                    }
                }
            }
        }

        private int Manhattan(BoardCell a, BoardCell b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
