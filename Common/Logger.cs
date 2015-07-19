using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RainyDays
{
	/// <summary>
	/// Logging utility.
	/// </summary>
	public static class Logger
	{
		private static object sSyncLock = new object();
		private static List<ILogStream> sLogStreams = new List<ILogStream>();

		public static string GetLogDirectory()
		{
#if UNITY_EDITOR
			return Path.GetFullPath(Application.dataPath + "/../");
#else
			return Path.GetFullPath(Application.persistentDataPath);
#endif
		}

		public static string CreateLogFilePath(string fileName)
		{
			return Path.Combine(GetLogDirectory(), fileName);
		}

		static Logger()
		{
			Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
		}

		private static void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
		{
			WriteLogEntry(new LogEntry(type.ToString().ToUpperInvariant(), condition, stackTrace, true));
		}

		public static void AddLogStream(ILogStream logStream)
		{
			Assert.IsNotNull(logStream);
			lock (sSyncLock)
			{
				sLogStreams.Add(logStream);
			}
		}

		public static bool RemoveLogStream(ILogStream logStream)
		{
			Assert.IsNotNull(logStream);
			lock (sSyncLock)
			{
				return sLogStreams.Remove(logStream);
			}
		}

		/// <summary>
		/// Writes the given LogEntry to all log streams.
		/// </summary>
		/// <remarks>
		/// Create your own LogEntry or ILogStream classes for custom logging.
		/// </remarks>
		public static void WriteLogEntry(LogEntry entry)
		{
			lock (sSyncLock)
			{
				foreach (var logStream in sLogStreams)
				{
					logStream.WriteEntry(entry);
				}
			}
		}

		/// <summary>
		/// Logs the given message to the given log channel.
		/// Does *not* log the message to the Unity console, and does not include the stack trace.
		/// </summary>
		public static void LogCustom(string channel, string message)
		{
			WriteLogEntry(new LogEntry(channel, message));
		}

		/// <summary>
		/// Logs the given formatted message to the given log channel.
		/// Does *not* log the message to the Unity console.
		/// </summary>
		public static void LogCustomFormat(string channel, string format, params object[] args)
		{
			WriteLogEntry(new LogEntry(channel, string.Format(format, args)));
		}
	}
}
