using System;
using System.Threading;
using System.Threading.Tasks;

namespace InsolvencniRejstrik
{
	abstract class BaseConnector
	{
		private readonly TaskFactory TaskFactory;

		public BaseConnector()
		{
			TaskFactory = new TaskFactory();
		}

		protected Task RunTask(Action action) {
			var task = TaskFactory.StartNew(action);
			while (task.Status != TaskStatus.Running) Thread.Sleep(10);
			return task;
		}

		protected Task[] RunTasks(int count, Action action)
		{
			var tasks = new Task[count];
			for (int i = 0; i < count; i++)
			{
				tasks[i] = TaskFactory.StartNew(action);
				while (tasks[i].Status != TaskStatus.Running) Thread.Sleep(10);
			}
			return tasks;
		}

		protected void PrintHeader()
		{
			Console.Clear();
			Console.WriteLine("HlidacStatu - datova sada Insolvencni rejstrik");
			Console.WriteLine("----------------------------------------------");
			Console.WriteLine();
		}
	}
}
