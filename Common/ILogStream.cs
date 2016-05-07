using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace RainyDays
{
	/// <summary>
	/// Logging interface.
	/// </summary>
	/// <see cref="Logger"/>
	public interface ILogStream
	{
		void WriteEntry(LogEntry entry);
	}

	/// <summary>
	/// A simple immutable log entry.
	/// </summary>
	[Serializable]
	public class LogEntry
	{
		public bool IsDebug { get; private set; }
		public DateTime TimeStamp { get; private set; }
		public string Channel { get; private set; }
		public int ThreadId { get; private set; }
		public string Message { get; private set; }

		[NonSerialized]
		protected string _cachedString;

		public LogEntry(string channel, string message)
			: this(channel, message, null, false)
		{
		}

		public LogEntry(LogType channel, string message, string stackTrace)
			: this(channel.ToString().ToUpperInvariant(), message, stackTrace, true)
		{
		}

		public LogEntry(string channel, string message, string stackTrace, bool isUnityDebug)
		{
			this.IsDebug = isUnityDebug;
			this.TimeStamp = DateTime.Now;
			this.Channel = channel;
			this.ThreadId = Thread.CurrentThread.ManagedThreadId;
			// combine
			var completeMessage = message;
			if (!string.IsNullOrEmpty(stackTrace))
			{
				completeMessage += Environment.NewLine + stackTrace;
			}
			// indent
			this.Message = completeMessage.Replace("\n", "\n\t");
		}

		public override string ToString()
		{
			if (_cachedString == null)
			{
				_cachedString = string.Format("{0},T{1},{2},{3}",
					TimeStamp.ToString("HH:mm:ss.fff"),
					ThreadId,
					Channel,
					Message);
			}
			return _cachedString;
		}
	}

	public interface ILogFilter
	{
		bool KeepEntry(LogEntry entry);
	}

	public class DebugLogFilter : ILogFilter
	{
		public bool KeepEntry(LogEntry entry) { return entry.IsDebug; }
	}

	public class CustomLogFilter : ILogFilter
	{
		public HashSet<string> Channels { get; set; }

		public CustomLogFilter(IEnumerable<string> channels)
		{
			Channels = new HashSet<string>(channels);
		}

		public bool KeepEntry(LogEntry entry)
		{
			return (Channels == null || Channels.Count == 0 || Channels.Contains(entry.Channel));
		}
	}

	/// <summary>
	/// ILogger implementation writing entries to a text file.
	/// </summary>
	public class FileLogStream : ILogStream, IDisposable
	{
		public ILogFilter Filter { get; set; }
		public StreamWriter Writer { get; private set; }

		public FileLogStream(string path)
			: this(path, false, Encoding.UTF8)
		{
		}

		public FileLogStream(string path, bool append, Encoding encoding)
		{
			Filter = new DebugLogFilter();
			
			var file = new FileInfo(path);
			file.Directory.Create();

			var prevFile = new FileInfo(Path.Combine(file.Directory.FullName, Path.GetFileNameWithoutExtension(path) + "_Prev" + Path.GetExtension(path)));
			if (prevFile.Exists)
			{
				prevFile.Delete();
			}
			if (file.Exists)
			{
				file.MoveTo(prevFile.FullName);
			}

			Writer = new StreamWriter(path, append, encoding);
			Writer.AutoFlush = true;

			WriteHeader();
		}

		protected virtual void WriteHeader()
		{
			Writer.WriteLine("-- Date: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
#if UNITY_EDITOR
			// FIXME: Process.GetCurrentProcess().MainModule crashes in built players?
			Writer.WriteLine("-- Process: {0} ({1})", Process.GetCurrentProcess().MainModule.FileName, Process.GetCurrentProcess().Id);
#endif
			Writer.WriteLine("-- Unity: {0}", Application.unityVersion);
			Writer.WriteLine("-- Application: {0}, {1}, version {2}", Application.companyName, Application.productName, Application.version);
			Writer.WriteLine("-- Environment: ({0} @ {1}), machine {2}, OS {3}", Environment.UserName, Environment.UserDomainName, Environment.MachineName, Environment.OSVersion.ToString());
			Writer.WriteLine();
		}

		public virtual void WriteEntry(LogEntry entry)
		{
			if (Filter == null || Filter.KeepEntry(entry))
			{
				Writer.WriteLine(entry.ToString());
			}
		}

		public virtual void Dispose()
		{
			if (Writer != null)
			{
				Writer.Dispose();
				Writer = null;
			}
		}
	}
}
