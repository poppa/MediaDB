/* Indexer.cs
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using MediaDB.Backend;

/// <summary>
/// Async method template for processing a file
/// </summary>
delegate MediaDB.Backend.MediaFile
         FileProcessor(MediaDB.Backend.CrawlerFile cf);

namespace MediaDB.Backend
{
	/// <summary>
	/// Container class for <see cref="Indexer"/>s
	/// </summary>
	public class Scanner : IDisposable
	{
		/// <summary>
		/// Total number of files collected
		/// </summary>
		public int TotalFiles { get; private set; }

		/// <summary>
		/// Number of files processed
		/// </summary>
		public int FilesDone { get; private set; }

		/// <summary>
		/// List of <see cref="Indexer"/>s
		/// </summary>
		private List<Indexer> indexers = new List<Indexer>();

		/// <summary>
		/// Starts a scanning/indexing session in <paramref name="paths"/>
		/// </summary>
		/// <param name="paths">
		/// A <see cref="List<BasePath>"/>
		/// </param>
		public static void Scan(List<BasePath> paths)
		{
			DateTime now = DateTime.Now;

			Console.Write("\n::: Starting scanner pass: {0}\n", now);

			Scanner scanner = new Scanner(paths);
			Console.Write("::: Collected {0} files,", scanner.TotalFiles);
			Console.Write(" starting indexer...\n\n");
			scanner.Run();

			while (scanner.FilesDone < scanner.TotalFiles)
				Thread.Sleep(50);

			Console.Write("\n::: Scanning took: {0}\n", DateTime.Now - now);

			scanner.Dispose();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="paths">
		/// A <see cref="List<BasePath>"/>
		/// </param>
		public Scanner(List<BasePath> paths)
		{
			foreach (BasePath p in paths) {
				var idx = new Indexer(p, this);
				indexers.Add(idx);
				TotalFiles += idx.Files.Count;
			}
		}

		/// <summary>
		/// Start indexing
		/// </summary>
		public void Run()
		{
			foreach (Indexer idx in indexers)
				idx.Start();
		}

		/// <summary>
		/// Called from a <see cref="Indexer"/> when a file has been processed
		/// </summary>
		public void NotifyDone()
		{
			FilesDone++;
			Log.Debug("+++ {0,5} of {1} ({2,3}%) done!\n",
			          FilesDone, TotalFiles,
			          Math.Floor(((double)FilesDone/(double)TotalFiles)*100));
			if (FilesDone == TotalFiles) {
				// We're really done!
			}
		}

		/// <summary>
		/// Disposes this object
		/// </summary>
		public void Dispose()
		{
			foreach (Indexer idx in indexers)
				idx.Dispose();

			indexers.Clear();
			indexers = null;
		}
	}

	/// <summary>
	/// Class for indexing a directory path. Scans recursively through the
	/// directory.
	/// </summary>
	internal class Indexer : IDisposable
	{
		//private static readonly object threadlock = new Object();

		/// <summary>
		/// The root path
		/// </summary>
		public BasePath Path { get; private set; }

		/// <summary>
		/// Array of found files
		/// </summary>
		public ArrayList Files { get; private set; }

		/// <summary>
		/// List of collected directories
		/// </summary>
		public List<Directory> Directories { get; private set; }

		/// <summary>
		/// Number of concurrent threads to use
		/// </summary>
		private int slots = 5;

		/// <summary>
		/// Number of used threads
		/// </summary>
		private int taken = 0;

		/// <summary>
		/// The scanner object owning this object
		/// </summary>
		private Scanner scanner;

		/// <summary>
		/// Contructor
		/// </summary>
		/// <param name="path">
		/// A <see cref="System.String"/>
		/// </param>
		public Indexer(BasePath path, Scanner scanner)
		{
			this.scanner = scanner;
			Path = path;
			if (Manager.Threads > 0)
				slots = Manager.Threads;

			Files = new ArrayList();
			Directories = new List<Directory>();

			Directory d;
			if ((d = Manager.GetDirectory(Path.Name)) == null) {
				d = new Directory();
				d.Name = "";
				d.ShortName = "";
				d.FullName = Path.Name;
				d.BasePathId = Path.Id;
				if (d.Save())
					Manager.Directories.Add(d);
				else {
					Log.Warning("Unable to save root dir {0} to directories!\n",
										  Path.Name);
				}
			}

			Directories.Add(d);

			crawl(Path.Name);
		}

		/// <summary>
		/// Frees a slot for a thread
		/// </summary>
		private void freeSlot()
		{
			// TODO: Haven't figured out if this lock is needed. Had some random
			// crashes before which seems to have gone away. Haven't proven this
			// lock has anything to do with that though...
			lock (this) {
				scanner.NotifyDone();
				taken--;
			}
		}

		/// <summary>
		/// Start indexing
		/// </summary>
		public void Start()
		{
			foreach (CrawlerFile cf in Files) {
				while (taken >= slots)
					System.Threading.Thread.Sleep(300);

				taken++;
				FileProcessor fproc = Processor;
				fproc.BeginInvoke(cf, onProcess, fproc);
			}
		}

		/// <summary>
		/// Callback for when a file has been processed
		/// </summary>
		private void onProcess(IAsyncResult syncr)
		{
			freeSlot();
		}

		/// <summary>
		/// Async method for processing files
		/// </summary>
		/// <param name="cf">
		/// A <see cref="CrawlerFile"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		private MediaFile Processor(CrawlerFile cf)
		{
			//Log.Debug(">>> Process file: {0}\n", cf.File.FullName);

			FileHandler h = null;
			switch (cf.MediaType.Mimetype)
			{
				case "image/bmp":
				case "image/jpeg":
				case "image/tiff":
				case "image/gif":
				case "image/png":
					h = new IMGHandler(cf.File, cf.MediaType);
					((IMGHandler)h).Process();
					break;

				case "image/x-eps":
					h =  new EPSHandler(cf.File, cf.MediaType);
					((EPSHandler)h).Process();
					break;

				case "image/svg+xml":
					h =  new SVGHandler(cf.File, cf.MediaType);
					((SVGHandler)h).Process();
					break;

				case "application/pdf":
					h =  new PDFHandler(cf.File, cf.MediaType);
					((PDFHandler)h).Process();
					break;
			}

			cf = null;
			MediaFile m = h.MediaFile;
			h.Dispose();
			h = null;
			return m;
		}

		/// <summary>
		/// Disposes the object
		/// </summary>
		public void Dispose()
		{
			Path = null;

			Files.Clear();
			Files = null;

			Directories.Clear();
			Directories = null;
		}

		/// <summary>
		/// Finds the parent firectory for <paramref name="dir"/>
		/// </summary>
		/// <param name="dir"></param>
		/// <returns></returns>
		private Directory findParentDirectory(DirectoryInfo dir)
		{
			string pp = dir.Parent.FullName;
			return Directories.Find(delegate(Directory d)
			{
				return d.FullName == pp;
			});
		}

		/// <summary>
		/// Returns the directory name of <paramref name="path"/>
		/// minus the the base path
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private string getDirectoryShortName(string path)
		{
			return path.Substring(Path.Name.Length + 1);
		}

		/// <summary>
		/// Crawl directory for files
		/// </summary>
		/// <param name="path">
		/// A <see cref="System.String"/>
		/// </param>
		private void crawl(string path)
		{
			DirectoryInfo dir = new DirectoryInfo(path);

			try {
				foreach (FileInfo file in dir.GetFiles()) {
					MediaType mt;
					if ((mt = Manager.GetMediaType(file)) != null)
						Files.Add(new CrawlerFile(file, mt, Path));
				}
			}
			catch (Exception e) {
				Log.Werror("File error: {0}\n", e.Message);
			}

			try {
				foreach (DirectoryInfo sub in dir.GetDirectories()) {
					string shortName = getDirectoryShortName(sub.FullName);
					Directory d = Manager.GetDirectory(sub.FullName);
					if (d == null) {
						d = new Directory();
						d.FullName = sub.FullName;
						d.Name = sub.Name;
						d.ShortName = shortName;
						d.BasePathId = Path.Id;

						Directory parent = findParentDirectory(sub);
						if (parent != null)
							d.ParentId = parent.Id;

						if (d.Save())
							Manager.Directories.Add(d);
						else 
							Log.Warning("Failed saving Directory\n", d.Id);
					}

					Directories.Add(d);

					crawl(sub.FullName);
				}
			}
			catch (Exception e) {
				Log.Werror("Directory error: {0}\n", e.Message);
			}
		}
	}

	/// <summary>
	/// Container for a file and its media type
	/// </summary>
	internal class CrawlerFile
	{
		/// <summary>
		/// The file object
		/// </summary>
		public FileInfo File;

		/// <summary>
		/// The media type
		/// </summary>
		public MediaType MediaType;

		/// <summary>
		/// The basepath this file recides in
		/// </summary>
		public BasePath BasePath;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="file">
		/// A <see cref="FileInfo"/>
		/// </param>
		/// <param name="mediatype">
		/// A <see cref="MediaType"/>
		/// </param>
		public CrawlerFile(FileInfo file, MediaType mediatype, BasePath basepath)
		{
			File = file;
			MediaType = mediatype;
			BasePath = basepath;
		}
	}
}
