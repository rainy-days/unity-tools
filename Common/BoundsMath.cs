using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RainyDays
{
	public static class BoundsMath
	{
		public static IEnumerable<Vector3> EnumCorners(Bounds bounds)
		{
			var min = bounds.min;
			var max = bounds.max;
			yield return min;
			yield return max;
			yield return new Vector3(min.x, min.y, max.z);
			yield return new Vector3(min.x, max.y, min.z);
			yield return new Vector3(max.x, min.y, min.z);
			yield return new Vector3(min.x, max.y, max.z);
			yield return new Vector3(max.x, min.y, max.z);
			yield return new Vector3(max.x, max.y, min.z);
		}

		public static Bounds InverseTransform(Bounds bounds, Transform t)
		{
			var worldToLocal = t.worldToLocalMatrix;
			var result = new Bounds(worldToLocal.MultiplyPoint(bounds.center), Vector3.zero);

			foreach (var c in EnumCorners(bounds))
			{
				result.Encapsulate(worldToLocal.MultiplyPoint(c));
			}
			return result;
		}

		public static float MinDistanceFromPlane(Bounds bounds, Vector3 planeNormal, Vector3 planePoint)
		{
			float dist = float.MaxValue;
			var plane = new Plane(planeNormal, planePoint);
			foreach (var c in EnumCorners(bounds))
			{
				dist = Mathf.Min(dist, plane.GetDistanceToPoint(c));
			}
			return dist;
		}

		public static float MaxProjectedDistanceFromPlane(Bounds bounds, Vector3 planeNormal, Vector3 planePoint, Vector3 projDir)
		{
			float dist = 0.0f, projDist;
			var plane = new Plane(planeNormal, planePoint);
			foreach (var c in EnumCorners(bounds))
			{
				var ray = new Ray(c, projDir);
				if (plane.Raycast(ray, out projDist))
				{
					dist = Mathf.Max(dist, projDist);
				}
			}
			return dist;
		}
	}
}
