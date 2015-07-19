using UnityEditor;
using UnityEngine;
using System.Collections;
using System.IO;

namespace RainyDays
{
	[CustomEditor(typeof(FileLogger))]
	public class FileLoggerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var fileLogger = (this.target as FileLogger);
			GUI.enabled = !EditorApplication.isPlaying || !fileLogger.enabled;
			DrawDefaultInspector();

			GUI.enabled = true;
			if (GUILayout.Button("Open Log File"))
			{
				var filePath = fileLogger.LogFilePath;
				if (File.Exists(filePath))
				{
					EditorUtility.OpenWithDefaultApp(fileLogger.LogFilePath);
				}
				else
				{
					Debug.LogErrorFormat("File {0} does not exist", filePath);
				}
			}
		}
	}
}
