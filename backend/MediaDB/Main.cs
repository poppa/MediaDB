using System;

namespace MediaDB
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			string cfg = "/home/pontus/temp/config.xml";

			if (!Settings.Init(cfg))
				return;

			foreach (string path in Settings.Paths)
				new Indexer(path).Start();

			Console.Write("\n----\nHit any key to exit!\n");
			Console.ReadLine();
			Console.Write("--- Bye bye\n");
			Environment.Exit(0);
		}
	}
}
