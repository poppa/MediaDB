/* Main.cs
 *
 * Copyright (C) 2010  Pontus Östlund
 *
 * This library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this library.  If not, see <http://www.gnu.org/licenses/>.
 *
 * Author:
 * 	Pontus Östlund <pontus@poppa.se>
 */

using System;
using System.Threading;
using System.IO;
using System.Collections;

namespace MediaDB
{
	/// <summary>
	/// Main class
	/// </summary>
	class MainClass
	{
		private const string HEADER =
		"***********************************************************************\n"+
		"* Starting MediaDB @ {0}                                               \n"+
		"* ----------------------------                                         \n"+
		"* Type EXIT or QUIT to kill the applicaion at any time.                \n"+
		"*********************************************************************\n\n";

		/// <summary>
		/// Main entry point
		/// </summary>
		/// <param name="args"></param>
		public static void Main (string[] args)
		{
			DateTime start = DateTime.Now;
			Console.Clear();
			Console.Write(HEADER, start);

#if DEBUG
			string cfg = @"C:\Users\marped\tmp\config.xml";
			Log.LogFile = @"C:\Users\marped\tmp\mediadb.log";
			//string cfg = @"C:\test\bonkers\cfg\config.xml";
			//Log.LogFile = @"C:\test\bonkers\cfg\mediadb.log";
#if LINUX
			var t = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			Log.LogFile = t + "/temp/mediadb.log";
			cfg  = t + "/temp/config.xml";
#else
			//Log.LogFile = Path.Combine(@"\tmp", "mediadb.log");
      //Log.LogFile = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\tmp\mediadb.log";
			//string cfg = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\tmp\config.xml";
#endif
#else
			string cfg = args[0];
#endif

			if (!Manager.Init(cfg)) {
				Log.Werror("Error starting application!\n");
				Environment.Exit(1);
			}

			StartWorker();
			MainLoop();
			Manager.Dispose();
			Environment.Exit(0);
		}

		/// <summary>
		/// The background worker
		/// </summary>
		private static System.ComponentModel.BackgroundWorker worker;

		/// <summary>
		/// Is the current worker done or not.
		/// </summary>
		private static bool workerCompleted = false;

		/// <summary>
		/// Starts the backgrpund worker thread for a scanning pass
		/// </summary>
		public static void StartWorker()
		{
			workerCompleted = false;

			if (worker != null) {
				worker.Dispose();
				worker = null;
			}

			worker = new System.ComponentModel.BackgroundWorker();
			worker.WorkerReportsProgress = false;
			worker.WorkerSupportsCancellation = false;

			worker.DoWork += (sender, args) =>
			{
				Backend.Scanner.Scan(Manager.BasePaths);
			};

			worker.RunWorkerCompleted += (sender, args) =>
			{
				workerCompleted = true;
			};

			worker.RunWorkerAsync();
		}

		/// <summary>
		/// Enter the MainLoop
		/// </summary>
		public static void MainLoop()
		{
			while (true) {
				Thread.Sleep(100);
				string x = Console.ReadLine();
				if (!String.IsNullOrEmpty(x)) {
					switch (x.ToLowerInvariant())
					{
						case "index":
							if (!workerCompleted) {
								Log.Warning("A worker is still running. Wait for it to quit!\n");
							}
							else
								StartWorker();
							break;

						case "clear":
							Console.Clear();
							break;

						case "quit":
						case "exit":
							Console.Write("------\nBye Bye!\n");
							Environment.Exit(0);
							break;
					}
				}
			}
		}
	}
}
