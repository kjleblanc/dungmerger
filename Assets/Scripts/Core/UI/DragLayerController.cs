using UnityEngine;

namespace MergeDungeon.Core
{
    public class DragLayerController : MonoBehaviour
    {
        [Header("Drag Layer")]
        public Transform dragLayer; // Top-level rect to host drags
        public bool forceDragAbove = true;
        public int dragSortingOrder = 4500;

        public void Setup()
        {
            if (dragLayer == null) return;
            dragLayer.SetAsLastSibling();
            if (forceDragAbove)
            {
                var canvas = dragLayer.GetComponent<Canvas>();
                if (canvas == null) canvas = dragLayer.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = dragSortingOrder;
                var cg = dragLayer.GetComponent<CanvasGroup>();
                if (cg == null) cg = dragLayer.gameObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
        }

        public void InitializeFrom(GridManager grid)
        {
            Setup();
        }
    }
}

