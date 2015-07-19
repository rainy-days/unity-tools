using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RainyDays
{
	public class FileLogger : MonoBehaviour
	{
		public string logFile = "RainyDays.log";
		public string[] customChannels;

		public string LogFilePath { get { return Logger.CreateLogFilePath(logFile); } }
		
		private FileLogStream _log;

		private void OnEnable()
		{
			var path = LogFilePath;
			_log = new FileLogStream(path);
			if (customChannels != null && customChannels.Length > 0)
			{
				_log.Filter = new CustomLogFilter(customChannels);
			}
			Logger.AddLogStream(_log);
			UnityEngine.Debug.Log("Created log file: " + path, this);
		}

		private void OnDisable()
		{
			if (_log != null)
			{
				Logger.RemoveLogStream(_log);
				_log.Dispose();
				_log = null;
			}
		}
	}
}