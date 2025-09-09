using System;
using UnityEngine;

namespace MergeDungeon.Core
{
	[CreateAssetMenu(menuName = "Events/RoomChanged")]
	public class RoomChangedEventChannelSO : ScriptableObject
	{
		public event Action<int, int, int> Raised; // floor, room, roomsPerFloor
		public void Raise(int floor, int room, int roomsPerFloor)
		{
			Raised?.Invoke(floor, room, roomsPerFloor);
		}
	}
}


