using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MergeDungeon.Core
{
    public class CraftingStationTile : TileBase
    {
        [Header("Station")]
        public StationType stationType = StationType.Campfire;
        public CraftingBook craftingBook;

        // Simple inventory of deposited ingredients
        private readonly Dictionary<TileDefinition, int> _inventory = new();

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData); // selection only
        }

        public override void OnActivateTap()
        {
            // Try craft one recipe when tapped again while selected
            TryCraftOne();
        }

        public bool Deposit(TileBase ingredient)
        {
            if (ingredient == null || ingredient == this) return false;
            // Add to inventory and refresh label
            var key = ingredient.def;
            if (key == null) return false;
            _inventory.TryGetValue(key, out int cur);
            _inventory[key] = cur + 1;
            RefreshInventoryLabel();
            return true;
        }

        private void RefreshInventoryLabel()
        {
            if (label == null) return;
            var sb = new StringBuilder();
            sb.Append(stationType.ToString());
            // Show up to 3 inventory entries for brevity
            int shown = 0;
            foreach (var kv in _inventory)
            {
                if (kv.Value <= 0) continue;
                sb.Append('\n');
                sb.Append(kv.Key != null ? kv.Key.DisplayName : "?");
                sb.Append(" x");
                sb.Append(kv.Value);
                shown++;
                if (shown >= 3) break;
            }
            label.text = sb.ToString();
        }

        private void TryCraftOne()
        {
            if (craftingBook == null) return;

            foreach (var recipe in craftingBook.GetRecipesForStation(stationType))
            {
                if (recipe == null) continue;
                if (HasIngredients(recipe))
                {
                    ConsumeIngredients(recipe);
                    ProduceOutput(recipe);
                    RefreshInventoryLabel();
                    break; // craft only one per click for MVP
                }
            }
        }

        private bool HasIngredients(CraftingRecipe recipe)
        {
            if (recipe.ingredients == null) return false;
            foreach (var ing in recipe.ingredients)
            {
                if (ing == null) return false;
                var def = ing.tile != null ? ing.tile.Resolve(services != null ? services.TileDatabase : null) : null;
                if (def == null) return false;
                _inventory.TryGetValue(def, out int have);
                if (have < Mathf.Max(1, ing.count)) return false;
            }
            return true;
        }

        private void ConsumeIngredients(CraftingRecipe recipe)
        {
            foreach (var ing in recipe.ingredients)
            {
                int need = Mathf.Max(1, ing.count);
                var def = ing.tile != null ? ing.tile.Resolve(services != null ? services.TileDatabase : null) : null;
                if (def == null) continue;
                _inventory.TryGetValue(def, out int have);
                _inventory[def] = Mathf.Max(0, have - need);
            }
        }

        private void ProduceOutput(CraftingRecipe recipe)
        {
            int count = Mathf.Max(1, recipe.outputCount);
            var outputDef = recipe.output != null ? recipe.output.Resolve(services != null ? services.TileDatabase : null) : null;
            if (outputDef == null) return;
            for (int i = 0; i < count; i++)
            {
                // Spawn into a random empty cell for simplicity
                var empty = services != null && services.Board != null ? services.Board.CollectEmptyCells() : new List<BoardCell>();
                if (empty.Count == 0) return;
                var cell = empty[Random.Range(0, empty.Count)];
                var t = services != null && services.TileFactory != null ? services.TileFactory.Create(outputDef) : null;
                if (t != null) cell.SetTile(t);
            }
        }
    }
}
