using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
	public class GameplayInput : ServicesConsumerBehaviour
	{
		[Header("Refs")]
		public GraphicRaycaster uiRaycaster;
		public EventSystem eventSystem;

		[Header("Gestures")]
		public float dragThresholdPixels = 8f;

		private Vector2 _pressStartScreen;
		private bool _dragging;
		private TileBase _draggedTile;

		private void Awake()
		{
			if (eventSystem == null) eventSystem = EventSystem.current;
			if (uiRaycaster == null)
			{
				var canvas = FindFirstObjectByType<Canvas>();
				if (canvas != null) uiRaycaster = canvas.GetComponent<GraphicRaycaster>();
			}
		}

		// Wire this to a Tap action (Button with Tap interaction) via PlayerInput (Unity Events)
		public void OnTap(InputAction.CallbackContext ctx)
		{
			if (!ctx.performed) return;
			var go = RaycastTopGameObject(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
			// Only clear selection on empty taps; component click handlers handle selection
			bool hasSelectable = go != null && go.GetComponentInParent<ISelectable>() != null;
			if (!hasSelectable)
			{
				UISelectionManager.Instance?.HandleClick(null);
			}
		}

		// Wire these to Pointer/Press/Drag actions or call manually from UI event bridges
		public void OnPress(InputAction.CallbackContext ctx)
		{
			var pos = ReadPointerScreen();
			if (ctx.started)
			{
				_pressStartScreen = pos;
				_dragging = false;
				_draggedTile = RaycastTopTile(pos);
			}
			else if (ctx.canceled)
			{
				if (_dragging && _draggedTile != null)
				{
					HandleDrop(pos);
				}
				_dragging = false;
				_draggedTile = null;
			}
		}

		public void OnPoint(InputAction.CallbackContext ctx)
		{
			var pos = ctx.ReadValue<Vector2>();
			if (!_dragging && (pos - _pressStartScreen).sqrMagnitude > dragThresholdPixels * dragThresholdPixels)
			{
				if (_draggedTile != null) _dragging = true;
			}
		}

		public void OnDrag(InputAction.CallbackContext ctx)
		{
			// Optional, only if you have a continuous drag action; otherwise OnPoint threshold handles it
			if (ctx.performed && !_dragging && _draggedTile != null)
			{
				_dragging = true;
			}
		}

		public void OnDragEnd(InputAction.CallbackContext ctx)
		{
			if (!ctx.performed) return;
			if (_dragging && _draggedTile != null)
			{
				HandleDrop(ReadPointerScreen());
			}
			_dragging = false;
			_draggedTile = null;
		}

		private void HandleDrop(Vector2 screenPos)
		{
			var targetCell = RaycastTopCell(screenPos);
			if (targetCell == null || services == null || _draggedTile == null) return;
			// If target has a tile, try merge first
			var targetTile = targetCell.tile;
			if (targetTile != null)
			{
				if (services.Tiles != null && services.Tiles.TryMergeOnDrop(_draggedTile, targetTile)) return;
			}
			// Otherwise try to place in cell
			services.Tiles?.TryPlaceTileInCell(_draggedTile, targetCell);
		}

		private Vector2 ReadPointerScreen()
		{
			if (Mouse.current != null) return Mouse.current.position.ReadValue();
			#if UNITY_EDITOR || UNITY_STANDALONE
			return Input.mousePosition;
			#else
			if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
				return Touchscreen.current.primaryTouch.position.ReadValue();
			return Vector2.zero;
			#endif
		}

		private GameObject RaycastTopGameObject(Vector2 screenPos)
		{
			if (uiRaycaster == null || eventSystem == null) return null;
			var ped = new PointerEventData(eventSystem);
			ped.position = screenPos;
			var results = new List<RaycastResult>();
			uiRaycaster.Raycast(ped, results);
			return results.Count > 0 ? results[0].gameObject : null;
		}

		private BoardCell RaycastTopCell(Vector2 screenPos)
		{
			if (uiRaycaster == null || eventSystem == null) return null;
			var ped = new PointerEventData(eventSystem);
			ped.position = screenPos;
			var results = new List<RaycastResult>();
			uiRaycaster.Raycast(ped, results);
			for (int i = 0; i < results.Count; i++)
			{
				var cell = results[i].gameObject.GetComponentInParent<BoardCell>();
				if (cell != null) return cell;
			}
			return null;
		}

		private TileBase RaycastTopTile(Vector2 screenPos)
		{
			if (uiRaycaster == null || eventSystem == null) return null;
			var ped = new PointerEventData(eventSystem);
			ped.position = screenPos;
			var results = new List<RaycastResult>();
			uiRaycaster.Raycast(ped, results);
			for (int i = 0; i < results.Count; i++)
			{
				var tile = results[i].gameObject.GetComponentInParent<TileBase>();
				if (tile != null) return tile;
			}
			return null;
		}
	}
}


