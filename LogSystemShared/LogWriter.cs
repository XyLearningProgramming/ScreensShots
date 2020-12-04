using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace LogSystemShared
{
	public static class LogWriter
	{
		static LogWriter()
		{
			LogConsumer.Init();
		}
		public static void WriteLine(string message, string title="", bool verbose = true,
			[CallerMemberName] string memberName = "", 
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			MessageQueue<LogMessage> messageQueue = MessageQueue<LogMessage>.FindOrCreate(Constants.MessageQueueName);
			LogMessage logMessage = new LogMessage(message, title, verbose, memberName, sourceFilePath, sourceLineNumber);
			messageQueue.Send(logMessage);
		}
		/// <summary>
		/// Purge all the rest of messages in messagebox immediately. Called before applicaiton ends.
		/// </summary>
		public static void PurgeAll()
		{
			LogConsumer.PurgeAll();
		}
	}
	internal static class Constants
	{
		public static string Delimiter { get; } = "\t";
		public static string LogFileName { get; }
		public static string MessageQueueName { get; }
		public static TimeSpan ConsumerTick;
		public static int LogsKeepingDays;
		static Constants()
		{
			LogFileName = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Logs", DateTime.Now.ToString("yyyy_MM_dd")+ ".log");
			MessageQueueName = $@".\PRIVATE$\msmq";
			ConsumerTick = TimeSpan.FromSeconds(0.1);
			LogsKeepingDays = 30;
		}
	}

	internal sealed class LogMessage 
	{
		public string TimeStamp;
		public string Title;
		public string Message;
		public string MemberName;
		public string SourceFilePath;
		public int SourceLineNumber;
		public bool Verbose;
		public LogMessage(string Message_, string Title_, bool Verbose_, string MemberName_, string SourceFilePath_, int SourceLineNumber_)
		{
			TimeStamp = DateTime.Now.ToString("yy_MM_dd hh:mm:ss:fff");
			Message = Message_;
			Title = Title_;
			MemberName = MemberName_;
			SourceFilePath = SourceFilePath_;
			SourceLineNumber = SourceLineNumber_;
			Verbose = Verbose_;
		}

		public override string ToString()
		{
			if(Verbose)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(TimeStamp); sb.Append(Constants.Delimiter);
				if(string.IsNullOrEmpty(Title) == false)
				{
					sb.Append(Title);
				}
				sb.Append(Constants.Delimiter);
				sb.Append(Message); sb.Append(Constants.Delimiter);
				sb.Append("Called From: "); sb.Append(MemberName); sb.Append(Constants.Delimiter);
				sb.Append("File: "); sb.Append(SourceFilePath); sb.Append(Constants.Delimiter);
				sb.Append("Line: "); sb.Append(SourceLineNumber); sb.Append(Constants.Delimiter);
				sb.Append(Environment.NewLine);
				return sb.ToString();
			}
			else
			{
				return Message + Environment.NewLine;
			}
		}
	}

	internal sealed class LogConsumer
	{
		private static LogConsumer _instance = null;
		public static void Init()
		{
			if(_instance == null)
			{
				_instance = new LogConsumer();
			}
		}
		public static void PurgeAll()
		{
			_instance?.PurgeAllNow();
		}

		private CancellationTokenSource _token;
		private LogConsumer()
		{
			try
			{
				// start on another thread the long running task to read messagequeue
				if(!File.Exists(Constants.LogFileName))
				{
					string dir = Path.GetDirectoryName(Constants.LogFileName);
					if(!Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}
					else
					{
						// delete some logs over than certain days
						try
						{
							List<string> logsToDelete = new List<string>();
							foreach(var logName in Directory.EnumerateFiles(dir, ".log", SearchOption.AllDirectories))
							{
								var logDate = DateTime.Parse(Path.GetFileNameWithoutExtension(logName));
								if((DateTime.Now - logDate).Days>= Constants.LogsKeepingDays)
								{
									logsToDelete.Add(logName);
								}
							}
							foreach(string file in logsToDelete)
							{
								File.Delete(file);
								LogWriter.WriteLine(file, "Delete old log: ");
							}
						}
						catch (Exception e)
						{
							LogWriter.WriteLine(e.Message, "Error enumerating old logs");
						}
					}
					// if log file doesn't exist 
					File.Create(Constants.LogFileName);
				}
				_token = new CancellationTokenSource();
				Task.Factory.StartNew(()=>WriteAll(_token.Token), TaskCreationOptions.LongRunning);
			}catch(Exception e)
			{
				throw e; // just throw this out
			}

		}

		private void PurgeAllNow()
		{
			_token.Cancel();
			try
			{
				if(MessageQueue<LogMessage>.Exists(Constants.MessageQueueName))
				{
					MessageQueue<LogMessage> messageQueue = MessageQueue<LogMessage>.FindOrCreate(Constants.MessageQueueName);
					List<LogMessage> messages = messageQueue.GetAllMessages();
					if(messages.Count != 0)
					{
						string ToWrite = string.Join("", messages.Select(m => m.ToString()));
						using(FileStream fs = new FileStream(Constants.LogFileName, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
						{
							using(StreamWriter sw = new StreamWriter(fs))
							{
								sw.Write(ToWrite);
							}
						}
					}
				}
			}
			catch(Exception e)
			{
				throw e; // just throw this out
			}
		}
		private async Task WriteAll(CancellationToken cancelToken)
		{
			while(!cancelToken.IsCancellationRequested)
			{
				try
				{
					if(MessageQueue<LogMessage>.Exists(Constants.MessageQueueName))
					{
						MessageQueue<LogMessage> messageQueue = MessageQueue<LogMessage>.FindOrCreate(Constants.MessageQueueName);
						List<LogMessage> messages = messageQueue.GetAllMessages();
						if(messages.Count != 0)
						{
							string ToWrite = string.Join("", messages.Select(m =>m.ToString()));
							using(FileStream fs = new FileStream(Constants.LogFileName, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
							{
								using(StreamWriter sw = new StreamWriter(fs))
								{
									await sw.WriteAsync(ToWrite);
								}
							}
						}
					}
					await Task.Delay(Constants.ConsumerTick);
				}
				catch (Exception e)
				{
					throw e; // just throw this out
				}
			}
		}
	}

	/// <summary>
	/// A messagequeue implementation for communication between different threads
	/// </summary>
	internal class MessageQueue<T>
	{
		private BlockingCollection<T> _messages = new BlockingCollection<T>(new ConcurrentQueue<T>());
		private static ConcurrentDictionary<string, MessageQueue<T>> _commonCache = new ConcurrentDictionary<string, MessageQueue<T>>();
		public static bool Exists(string name)
		{
			return _commonCache.ContainsKey(name);
		}
		public static MessageQueue<T> FindOrCreate(string name)
		{
			_commonCache.AddOrUpdate(name, new MessageQueue<T>(), (oldKey, oldVal) => oldVal);
			return _commonCache[name];
		}
		private MessageQueue(){}
		public List<T> GetAllMessages()
		{
			List<T> result = new List<T>();
			while(_messages.TryTake(out T item))
			{
				result.Add(item);
			}

			return result;
		}
		public void Send(T data)
		{
			_messages.Add(data);
		}
	}

}
