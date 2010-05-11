using System;
using System.IO;
using System.Collections;

namespace MediaDB
{
	class MainClass
	{
		public static void Main (string[] args)
		{
#if DEBUG
#if LINUX
			Log.Debug("=== DEBUG/LINUX MODE ===\n");
			var t = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			Log.LogFile = Tools.BuildPath(t,"temp","mediadb.log");
			string cfg = "/home/pontus/temp/config.xml";
#else
      Log.LogFile = Path.Combine("", "tmp", "mediadb.log");
			string cfg = @"\tmp\config.xml";
#endif
#else
			string cfg = args[1];
#endif

      if (Settings.Init(cfg)) {
        ArrayList idx = new ArrayList();
        foreach (string path in Settings.Paths)
          idx.Add(new Indexer(path));

        foreach (Indexer i in idx)
          i.Start();
      }

      Log.End();

			Console.Write("\n----\nHit any key to exit!\n");
			Console.ReadLine();
			Console.Write("--- Bye bye\n");
      
      Settings.Dispose();

			Environment.Exit(0);
		}
	}
}
