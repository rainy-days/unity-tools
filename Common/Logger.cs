using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace RainyDays
{
#if UNITY_EDITOR
	[UnityEditor.InitializeOnLoad]
#endif
	public static class Logger
	{
		private static StreamWriter sLogFile;
		private static object sSyncLock = new object();

		static Logger()
		{
#if UNITY_EDITOR
			var logDir = Path.GetFullPath(Application.dataPath + "/../");
#else
			var logDir = Path.GetFullPath(Application.persistentDataPath);
#endif
			var logFileName = Path.Combine(logDir, "RainyDays.log");
			sLogFile = new StreamWriter(logFileName, false, Encoding.UTF8);
			sLogFile.AutoFlush = true;

			sLogFile.WriteLine("-- Date: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
			sLogFile.WriteLine("-- Process: {0} ({1})", Process.GetCurrentProcess().MainModule.FileName, Process.GetCurrentProcess().Id);
			sLogFile.WriteLine("-- Unity: {0}", Application.unityVersion);
			sLogFile.WriteLine("-- Application: {0}, {1}, version {2}", Application.companyName, Application.productName, Application.version);
			sLogFile.WriteLine("-- Environment: ({0} @ {1}), machine {2}, OS {3}", Environment.UserName, Environment.UserDomainName, Environment.MachineName, Environment.OSVersion.ToString());
			sLogFile.WriteLine();

			Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;

			// to consider:
			//Console.SetOut(sLogFile);
			//Console.SetError(sLogFile);

			Log("Created " + logFileName);
		}

		private class LogEntry
		{
			public DateTime TimeStamp { get; private set; }
			public string Header { get; private set; }
			public int ThreadId { get; private set; }
			public string Message { get; private set; }

			public LogEntry(string header, string message, string stackTrace)
			{
				this.TimeStamp = DateTime.Now;
				this.Header = header;
				this.ThreadId = Thread.CurrentThread.ManagedThreadId;
				// combine
				var completeMessage = message + Environment.NewLine + stackTrace;
				// indent
				this.Message = completeMessage.Replace("\n", "\n\t");
			}

			public override string ToString()
			{
 				 return string.Format("{0},T{1},{2},{3}",
					 TimeStamp.ToString("HH:mm:ss.fff"),
					 ThreadId,
					 Header,
					 Message);
			}
		}

		private static void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
		{
			var entry = new LogEntry(type.ToString().ToUpperInvariant(), condition, stackTrace);
			WriteLogEntry(entry);
		}

		private static void WriteLogEntry(LogEntry entry)
		{
			lock (sSyncLock)
			{
				sLogFile.WriteLine(entry.ToString());
			}
		}

		public static void Log(string message)
		{
			UnityEngine.Debug.Log(message);
		}

		public static void Log(string message, UnityEngine.Object context)
		{
			UnityEngine.Debug.Log(message, context);
		}

		public static void LogFormat(string message, params object[] args)
		{
			UnityEngine.Debug.LogFormat(message, args);
		}
	}
}
