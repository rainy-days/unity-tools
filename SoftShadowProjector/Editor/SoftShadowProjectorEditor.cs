using UnityEditor;
using UnityEngine;
using System.Collections;

namespace RainyDays
{
	[CustomEditor(typeof(SoftShadowProjector))]
	[InitializeOnLoad]
	public class SoftShadowProjectorEditor : Editor
	{
		static SoftShadowProjectorEditor()
		{
			SoftShadowProjector.ConfigureLayers();
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
		}
	}
}
