using UnityEngine;
using System.Collections.Generic;

namespace RainyDays
{
	/// <summary>
	/// Component attached to a game object managed by a Pool.
	/// </summary>
	public class PooledObject : MonoBehaviour
	{
		/// <summary>
		/// Pool that currently manages this game object.
		/// </summary>
		/// <value>The pool.</value>
		public Pool Pool { get; internal set; }

		internal int IndexInPool { get; set; }
		internal bool IsActiveInPool { get; set; }

		internal Dictionary<System.Type, Component[]> ManagedComponents { get; set; }

		public void ReturnToPool()
		{
			if (Pool) {
				Pool.Return (this);
			}
		}

		public void StealFromPool()
		{
			if (Pool) {
				Pool.Steal (this);
			}
		}

		private void OnDestroy()
		{
			// clean disconnect
			StealFromPool ();
		}
	}
}
