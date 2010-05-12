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
	class MainClass
	{
		public static void Main (string[] args)
		{
			DateTime start = DateTime.Now;
#if DEBUG
#if LINUX
			Log.Debug("=== DEBUG/LINUX MODE ===\n");
			var t = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			Log.LogFile = Tools.BuildPath(t,"temp","mediadb.log");
			string cfg = "/home/pontus/temp/config.xml";
#else
      Log.LogFile = Path.Combine(@"\tmp", "mediadb.log");
			string cfg = @"\tmp\config.xml";
#endif
#else
			string cfg = args[1];
#endif

      if (Manager.Init(cfg)) {
        ArrayList idx = new ArrayList();
        foreach (string path in Manager.Paths)
          idx.Add(new Backend.Indexer(path));

        foreach (Backend.Indexer i in idx)
          i.Start();
      }

      Log.End();

			Log.Debug(" # {0}\n", DateTime.Now - start);

			/*
			Console.Write("\n----\nHit any key to exit!\n");*/
			Console.ReadLine();
			Console.Write("--- Bye bye\n");

			//Thread backend = new Thread(new ThreadStart(MainLoop));
			//backend.Start();
			//backend.Join();
      
      Manager.Dispose();

			Environment.Exit(0);
		}

		public static void MainLoop()
		{
			while (true) {
				Thread.Sleep(100);
			}
		}
	}
}
