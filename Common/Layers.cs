using System;
using UnityEngine;

namespace RainyDays
{
	public static class Layers
	{
		public static readonly string IsolateLayerName = "RainyDays-Isolate";

		public static int GetOrCreateByName(string layerName)
		{
			int layer = LayerMask.NameToLayer(layerName);
			if (layer == -1)
			{
#if UNITY_EDITOR && UNITY_5
				if (string.IsNullOrEmpty(layerName))
				{
					throw new ArgumentException("null or empty", "layerName");
				}

				// note: if Unity changes its serialization this method breaks
				// tested using "serializedVersion: 2" in Unity 5.1
				const string TagManagerAssetPath = "ProjectSettings/TagManager.asset";
				var tagManager = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(TagManagerAssetPath)[0]);
				var prop = tagManager.GetIterator();
				var success = false;
				while (!success && prop.NextVisible(true))
				{
					if (prop.isArray && prop.name == "layers")
					{
						// skip the first 8 layers (built-in)
						for (int i = 8; i < 32; ++i)
						{
							var layerProp = prop.GetArrayElementAtIndex(i);
							if (string.IsNullOrEmpty(layerProp.stringValue))
							{
								layerProp.stringValue = layerName;
								success = true;
								break;
							}
						}
						break;
					}
				}
				if (success &&
					tagManager.ApplyModifiedProperties() &&
					(-1 != (layer = LayerMask.NameToLayer(layerName))))
				{
					Debug.Log("Created layer \"" + layerName + "\" at index " + layer);
				}
				else
				{
					Debug.LogError("No more layers available. Could not create layer named \"" + layerName + "\".");
				}
#else
				Debug.LogError("Could not find layer named: " + layerName);
#endif
			}
			return layer;
		}
	}
}
