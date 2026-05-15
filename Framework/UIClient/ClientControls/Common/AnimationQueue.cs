using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.UI.Client.ClientControls.Common
{
	public class AnimationQueue
	{
		private ConcurrentQueue<AnimationParameter> queue;
		private AutoResetEvent eventHandler;
		public event EventHandler<StatusUpdateArgs> StatusUpdated;
		private bool idle;
		private ReaderWriterLockSlim idleLocker;
		private readonly string name;

		public AnimationQueue(string name)
		{
			idleLocker = new ReaderWriterLockSlim();
			eventHandler = new AutoResetEvent(false);
			queue = new ConcurrentQueue<AnimationParameter>();
			idle = true;
			this.name = name;
			Task.Run(() => Consume());
		}

		public void EnqueueStatus(AnimationParameter parameter)
		{
			queue.Enqueue(parameter);
			try
			{
				idleLocker.EnterReadLock();
				if (idle)
				{
					eventHandler.Set();
					idle = false;
				}
			}
			finally
			{
				idleLocker.ExitReadLock();
			}
		}

		public AnimationParameter LastStatus
		{
			get;
			private set;
		}

		private void Consume()
		{
			Thread.CurrentThread.Name = name;
			while (true)
			{
				eventHandler.WaitOne();
				AnimationParameter parameter = null;
				if (queue.TryDequeue(out parameter))
				{
					LastStatus = parameter;
					if (StatusUpdated != null)
					{
						try
						{
							idleLocker.EnterWriteLock();
							idle = false;
						}
						finally
						{
							idleLocker.ExitWriteLock();
						}
						StatusUpdated(this, new StatusUpdateArgs() { Event = eventHandler, Parameter = parameter });
					}
				}
				else
				{
					try
					{
						idleLocker.EnterWriteLock();
						idle = true;
					}
					finally
					{
						idleLocker.ExitWriteLock();
					}
				}
			}
		}
	}

	public class StatusUpdateArgs : EventArgs
	{
		public AutoResetEvent Event { get; set; }
		public AnimationParameter Parameter { get; set; }
	}

	public class AnimationParameter
	{
		public string Target { get; set; }
		public string ArmA { get; set; }
		public string ArmB { get; set; }
	}
}
