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
//using iTextSharp.text.pdf;

/// <summary>
/// Async method template for processing a file
/// </summary>
delegate MediaDB.MediaFile FileProcessor(MediaDB.CrawlerFile cf,
                                         MediaDB.Indexer idx);

namespace MediaDB
{
	/// <summary>
	/// Class for indexing a directory path. Scans recursively through the
	/// directory.
	/// </summary>
	public class Indexer
	{
    public static int TOTAL_FILES = 0;
    public static int FILES_DONE = 0;

		/// <summary>
		/// The root path
		/// </summary>
		public string Path { get; private set; }

		/// <summary>
		/// Array of found files
		/// </summary>
		public ArrayList Files { get; private set; }

		/// <summary>
		/// Number of concurrent threads to use
		/// </summary>
		private int slots = 15;

		/// <summary>
		/// Number of used threads
		/// </summary>
		private int taken = 0;

		/// <summary>
		/// Contructor
		/// </summary>
		/// <param name="path">
		/// A <see cref="System.String"/>
		/// </param>
		public Indexer(string path)
		{
			Path = path;
			if (Settings.Threads > 0)
				slots = Settings.Threads;

      Files = new ArrayList();

      Log.File("\n+++ Starting crawler in {0} +++\n", Path);

      crawl(Path);

      Log.File("--- Found {0} files in {1} ---\n",
               Files.Count, Path);

      TOTAL_FILES += Files.Count;
		}

		/// <summary>
		/// Frees a slot for a thread
		/// </summary>
		public void FreeSlot()
		{
      lock (this) {
        FILES_DONE++;
        Log.Debug("\n    +++++ {0} of {1} ({2}%) done!\n\n", 
                  FILES_DONE, TOTAL_FILES,
                  Math.Round(((double)FILES_DONE/(double)TOTAL_FILES)*100));
        taken--;
      };
		}

		/// <summary>
		/// Start indexing
		/// </summary>
		public void Start()
		{
			if (!Tools.DirectoryExists(Path)) {
				Log.Warning("File {0} doesn't exist or isn't a directory!\n", Path);
				return;
			}

      Log.File("\n+++ Starting indexer in {0} +++\n", Path);

			foreach (CrawlerFile cf in Files) {
				while (taken >= slots)
					System.Threading.Thread.Sleep(1);

				taken++;
				FileProcessor fproc = Processor;
				fproc.BeginInvoke(cf, this, onProcess, fproc);
			}
		}

		/// <summary>
		/// Callback for when a file has been processed
		/// </summary>
		private static void onProcess(IAsyncResult syncr)
		{
			FileProcessor fproc = (FileProcessor)syncr.AsyncState;
			MediaFile mf = (MediaFile)fproc.EndInvoke(syncr);
			Log.Debug("<<< Process done: {0}\n", mf.FullName);
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
		private static MediaFile Processor(CrawlerFile cf, Indexer idx)
		{
			Log.Debug(">>> Process file: {0}\n", cf.File.FullName);

			FileHandler h = null;
			switch (cf.MediaType.Mimetype)
			{
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
			idx.FreeSlot();
			return h.MediaFile;
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
					if ((mt = Settings.GetMediaType(file)) != null)
						Files.Add(new CrawlerFile(file, mt));
				}
			}
			catch (Exception e) {
				Log.Werror("File error: {0}\n", e.Message);
			}

			try {
				foreach (DirectoryInfo sub in dir.GetDirectories())
					crawl(sub.FullName);
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
		/// Constructor
		/// </summary>
		/// <param name="file">
		/// A <see cref="FileInfo"/>
		/// </param>
		/// <param name="mediatype">
		/// A <see cref="MediaType"/>
		/// </param>
		public CrawlerFile(FileInfo file, MediaType mediatype)
		{
			File = file;
			MediaType = mediatype;
		}
	}
}
