using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RainyDays
{
	public static class BoundsMath
	{
		public struct CornerEnumerator : IEnumerator<Vector3>
		{
			private Vector3 _min, _max, _current;
			private int _index;

			public CornerEnumerator(Bounds b)
			{
				_min = b.min;
				_max = b.max;
				_index = -1;
				_current = Vector3.zero;
			}

			public Vector3 Current
			{
				get { return _current; }
			}

			object IEnumerator.Current
			{
				get { return _current; }
			}

			public bool MoveNext()
			{
				++_index;
				switch (_index)
				{
					case 0: _current = _min; break;
					case 1: _current = _max; break;
					case 2: _current = new Vector3(_min.x, _min.y, _max.z); break;
					case 3: _current = new Vector3(_min.x, _max.y, _min.z); break;
					case 4: _current = new Vector3(_max.x, _min.y, _min.z); break;
					case 5: _current = new Vector3(_min.x, _max.y, _max.z); break;
					case 6: _current = new Vector3(_max.x, _min.y, _max.z); break;
					case 7: _current = new Vector3(_max.x, _max.y, _min.z); break;
					default: break;
				}
				return _index < 8;
			}

			public void Reset()
			{
				_index = -1;
			}

			public void Dispose()
			{
			}
		}

		public static Bounds InverseTransform(Bounds bounds, Transform t)
		{
			using (new ScopedProfilerSample("InverseTransform"))
			{
				var worldToLocal = t.worldToLocalMatrix;
				var result = new Bounds(worldToLocal.MultiplyPoint(bounds.center), Vector3.zero);

				var corners = new CornerEnumerator(bounds);
				while (corners.MoveNext())
				{
					result.Encapsulate(worldToLocal.MultiplyPoint(corners.Current));
				}
				return result;
			}
		}

		public static float MinDistanceFromPlane(Bounds bounds, Vector3 planeNormal, Vector3 planePoint)
		{
			using (new ScopedProfilerSample("MinDistanceFromPlane"))
			{
				float dist = float.MaxValue;
				var plane = new Plane(planeNormal, planePoint);
				var corners = new CornerEnumerator(bounds);
				while (corners.MoveNext())
				{
					dist = Mathf.Min(dist, plane.GetDistanceToPoint(corners.Current));
				}
				return dist;
			}
		}

		public static float MaxProjectedDistanceFromPlane(Bounds bounds, Vector3 planeNormal, Vector3 planePoint, Vector3 projDir)
		{
			using (new ScopedProfilerSample("MaxProjectedDistanceFromPlane"))
			{
				float dist = 0.0f, projDist;
				var plane = new Plane(planeNormal, planePoint);
				var corners = new CornerEnumerator(bounds);
				while (corners.MoveNext())
				{
					var ray = new Ray(corners.Current, projDir);
					if (plane.Raycast(ray, out projDist))
					{
						dist = Mathf.Max(dist, projDist);
					}
				}
				return dist;
			}
		}
	}
}
