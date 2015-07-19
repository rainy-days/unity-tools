using System;
using UnityEngine;

namespace RainyDays
{
	/// <summary>
	/// Base class extending the MonoBehaviour component.
	/// </summary>
	public abstract class BaseBehaviour : MonoBehaviour
	{
		/// <summary>
		/// Helper method to allocate a disposable ScopedProfilerSample instance using this
		/// behaviour as context.
		/// </summary>
		/// <param name="name">Profiler sample name</param>
		/// <returns>The scoped instance, to be used in a using statement.</returns>
		/// <remarks>
		/// This method does not allocate memory on the heap.
		/// </remarks>
		protected ScopedProfilerSample ScopedProfilerSample(string name)
		{
			return new ScopedProfilerSample(name, this);
		}
	}
}
