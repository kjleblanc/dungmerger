using System;
using UnityEngine;

namespace MergeDungeon.Core
{
	[CreateAssetMenu(menuName = "Events/VoidEvent")]
	public class VoidEventChannelSO : ScriptableObject
	{
		public event Action Raised;
		public void Raise()
		{
			Raised?.Invoke();
		}
	}
}


