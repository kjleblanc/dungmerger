using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class TileService : MonoBehaviour
    {
        public GridManager grid;
        public TileBase tilePrefab;
        public TileDatabase tileDatabase;
        public TileFactory tileFactory;

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

        // Legacy enum-based spawn helpers removed

        public bool TryMergeOnDrop(TileBase source, TileBase target)
        {
            if (grid == null) return false;
            if (source == null || target == null) return false;
            if (target.currentCell == null) return false;

            // Resolve definitions
            var sourceDef = source.def;
            var targetDef = target.def;

            bool canMerge;
            TileDefinition.MergeRule defRule = null;
            TileDefinition outputDef = null;
            int defToConsume = 0;
            int defToProduce = 0;

            if (sourceDef != null && targetDef != null)
            {
                bool sameBucket = sourceDef == targetDef || (targetDef.mergesWith != null && sourceDef == targetDef.mergesWith);
                if (!sameBucket) return false;

                var originCellDef = target.currentCell;
                var groupDef = CollectConnectedTilesOfDefinition(originCellDef, targetDef, targetDef.mergesWith);
                if (source.currentCell != null)
                    groupDef.Remove(source.currentCell);
                int totalWithDropDef = groupDef.Count + 1;

                if (totalWithDropDef >= 5 && targetDef.fiveOfAKind != null && targetDef.fiveOfAKind.output != null)
                {
                    defRule = targetDef.fiveOfAKind;
                }
                else if (totalWithDropDef >= 3 && targetDef.threeOfAKind != null && targetDef.threeOfAKind.output != null)
                {
                    defRule = targetDef.threeOfAKind;
                }
                else
                {
                    return false;
                }

                defToConsume = Mathf.Max(2, defRule.countToConsume);
                defToProduce = Mathf.Max(1, defRule.outputCount);
                outputDef = defRule.output;

                if (source.currentCell != null)
                {
                    source.currentCell.ClearTileIf(source);
                }
                Destroy(source.gameObject);

                var orderedDef = groupDef.OrderBy(c => Manhattan(c, originCellDef)).ToList();
                var consumeCellsDef = new List<BoardCell>();
                for (int i = 0; i < orderedDef.Count && consumeCellsDef.Count < (defToConsume - 1); i++)
                {
                    var c = orderedDef[i];
                    if (c != null && c.tile != null)
                        consumeCellsDef.Add(c);
                }

                foreach (var c in consumeCellsDef)
                {
                    if (c.tile != null)
                    {
                        Destroy(c.tile.gameObject);
                        c.tile = null;
                    }
                }

                void PlaceUpgradeAtDef(BoardCell cell)
                {
                    TileBase nt = tileFactory != null ? tileFactory.Create(outputDef) : Instantiate(tilePrefab);
                    if (nt != null)
                    {
                        if (nt.def == null && outputDef != null) nt.SetDefinition(outputDef);
                        nt.RefreshVisual();
                        cell.SetTile(nt);
                    }
                }

                PlaceUpgradeAtDef(originCellDef);
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
                        var empties = grid.CollectEmptyCells();
                        if (empties.Count > 0)
                            second = empties[Random.Range(0, empties.Count)];
                    }
                    if (second != null)
                    {
                        PlaceUpgradeAtDef(second);
                    }
                }

                return true;
            }

            // Legacy merge path removed. Only definition-driven merges are supported now.
            return false;
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
                var n = grid.GetCell(x, y);
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
