using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class TileService : MonoBehaviour
    {
        public GridManager grid;
        public TileBase tilePrefab;
        public MergeRules mergeRules;

        private void Awake()
        {
            if (grid == null) grid = GridManager.Instance;
        }

        public bool TryPlaceTileInCell(TileBase tile, BoardCell cell)
        {
            if (cell == null) return false;
            if (!cell.IsFreeForTile()) return false;
            if (tile.currentCell != null)
            {
                tile.currentCell.ClearTileIf(tile);
            }
            cell.SetTile(tile);
            return true;
        }

        public bool TrySpawnTileAtRandom(TileKind kind)
        {
            var empty = grid != null ? grid.CollectEmptyCells() : null;
            if (empty == null || empty.Count == 0) return false;
            var cell = empty[Random.Range(0, empty.Count)];
            var t = Instantiate(tilePrefab);
            t.kind = kind;
            t.RefreshVisual();
            cell.SetTile(t);
            return true;
        }

        public void SpawnTileAtRandom(TileKind kind)
        {
            TrySpawnTileAtRandom(kind);
        }

        public bool TryMergeOnDrop(TileBase source, TileBase target)
        {
            if (mergeRules == null || grid == null) return false;
            if (source == null || target == null) return false;
            if (target.currentCell == null) return false;
            if (source.kind != target.kind) return false;

            MergeRules.MergeRecipe recipe;
            if (!TryGetMergeRecipe(target.kind, out recipe)) return false;

            var originCell = target.currentCell;
            var group = CollectConnectedTilesOfKind(originCell, target.kind);
            if (source.currentCell != null)
                group.Remove(source.currentCell);
            int groupCount = group.Count;
            int totalWithDrop = groupCount + 1;

            int toConsume;
            int toProduce;
            if (totalWithDrop >= 5)
            {
                toConsume = 5;
                toProduce = 2;
            }
            else if (totalWithDrop >= 3)
            {
                toConsume = 3;
                toProduce = 1;
            }
            else
            {
                return false;
            }

            if (source.currentCell != null)
            {
                source.currentCell.ClearTileIf(source);
            }
            Destroy(source.gameObject);

            var ordered = group.OrderBy(c => Manhattan(c, originCell)).ToList();
            var consumeCells = new List<BoardCell>();
            for (int i = 0; i < ordered.Count && consumeCells.Count < (toConsume - 1); i++)
            {
                var c = ordered[i];
                if (c != null && c.tile != null)
                    consumeCells.Add(c);
            }

            foreach (var c in consumeCells)
            {
                if (c.tile != null)
                {
                    Destroy(c.tile.gameObject);
                    c.tile = null;
                }
            }

            void PlaceUpgradeAt(BoardCell cell)
            {
                var t = Instantiate(tilePrefab);
                t.kind = recipe.output;
                t.RefreshVisual();
                cell.SetTile(t);
            }

            PlaceUpgradeAt(originCell);
            if (toProduce > 1)
            {
                BoardCell second = null;
                foreach (var c in consumeCells)
                {
                    if (c != null && c != originCell)
                    {
                        second = c;
                        break;
                    }
                }
                if (second == null)
                {
                    var empties = grid.CollectEmptyCells();
                    if (empties.Count > 0)
                        second = empties[Random.Range(0, empties.Count)];
                }
                if (second != null)
                {
                    PlaceUpgradeAt(second);
                }
            }

            return true;
        }

        public List<BoardCell> CollectConnectedTilesOfKind(BoardCell originCell, TileKind kind)
        {
            var visited = new HashSet<BoardCell>();
            var list = new List<BoardCell>();
            var q = new Queue<BoardCell>();
            visited.Add(originCell);
            q.Enqueue(originCell);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c.tile != null && c.tile.kind == kind)
                {
                    list.Add(c);
                    TryEnqueue(c.x + 1, c.y);
                    TryEnqueue(c.x - 1, c.y);
                    TryEnqueue(c.x, c.y + 1);
                    TryEnqueue(c.x, c.y - 1);
                }
            }
            return list;

            void TryEnqueue(int x, int y)
            {
                var n = grid.GetCell(x, y);
                if (n != null && !visited.Contains(n))
                {
                    visited.Add(n);
                    if (n.tile != null && n.tile.kind == kind)
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

        private bool TryGetMergeRecipe(TileKind kind, out MergeRules.MergeRecipe recipe)
        {
            if (mergeRules != null && mergeRules.TryGetRecipe(kind, out recipe))
                return true;
            // Fallback default rules if no asset is assigned
            switch (kind)
            {
                case TileKind.SwordStrike:
                    recipe = new MergeRules.MergeRecipe { input = kind, count = 3, output = TileKind.Cleave };
                    return true;
                case TileKind.Spark:
                    recipe = new MergeRules.MergeRecipe { input = kind, count = 3, output = TileKind.Fireball };
                    return true;
                case TileKind.Goo:
                    recipe = new MergeRules.MergeRecipe { input = kind, count = 3, output = TileKind.GooJelly };
                    return true;
                case TileKind.Mushroom:
                    recipe = new MergeRules.MergeRecipe { input = kind, count = 3, output = TileKind.MushroomStew };
                    return true;
                default:
                    recipe = null;
                    return false;
            }
        }
    }
}
