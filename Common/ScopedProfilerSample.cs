using System;

namespace RainyDays
{
	/// <summary>
	/// Helper structure to facilitate calls to UnityEngine.Profiler.Begin/EndSample.
	/// </summary>
	public struct ScopedProfilerSample : IDisposable
	{
		public ScopedProfilerSample(string name)
		{
			UnityEngine.Profiler.BeginSample(name);
		}

		public ScopedProfilerSample(string name, UnityEngine.Object targetObject)
		{
			UnityEngine.Profiler.BeginSample(name, targetObject);
		}

		public void Dispose()
		{
			UnityEngine.Profiler.EndSample();
		}
	}
}
