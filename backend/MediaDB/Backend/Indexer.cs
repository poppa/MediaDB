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
using MediaDB.Backend;

/// <summary>
/// Async method template for processing a file
/// </summary>
delegate MediaDB.Backend.MediaFile
         FileProcessor(MediaDB.Backend.CrawlerFile cf,
                       MediaDB.Backend.Indexer idx);

namespace MediaDB.Backend
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
		public BasePath Path { get; private set; }

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
		public Indexer(BasePath path)
		{
			Path = path;
			if (Manager.Threads > 0)
				slots = Manager.Threads;

			Files = new ArrayList();

			Log.Debug("\n>>> Starting crawler in {0}\n", Path.Name);

			crawl(Path.Name);

			Log.Debug("<<< Found {0} files in {1}\n",
			          Files.Count, Path.Name);

			TOTAL_FILES += Files.Count;
		}

		/// <summary>
		/// Frees a slot for a thread
		/// </summary>
		public void FreeSlot()
		{
			// TODO: Haven't figured out if this lock is needed. Had some random
			// crashes before which seems to have gone away. Haven't proven this
			// lock has anything to do with that though...
			lock (this) {
				FILES_DONE++;
				Log.Debug("    +++++ {0,5} of {1} ({2,3}%) done!\n",
				          FILES_DONE, TOTAL_FILES,
				          Math.Floor(((double)FILES_DONE/(double)TOTAL_FILES)*100));
				taken--;

				if (FILES_DONE == TOTAL_FILES) {
					Files.Clear();
					Files = new ArrayList();
				}
			}
		}

		/// <summary>
		/// Start indexing
		/// </summary>
		public void Start()
		{
			Log.Debug("\n$$$ Starting indexer in {0} +++\n", Path.Name);

			foreach (CrawlerFile cf in Files) {
				while (taken >= slots)
					System.Threading.Thread.Sleep(300);

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
			//MediaFile mf = (MediaFile)fproc.EndInvoke(syncr);
			fproc.EndInvoke(syncr);
			//Log.Debug("<<< Process done: {0}\n", mf.FullName);
			fproc = null;
			syncr = null;
			//mf = null;
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
			idx.FreeSlot();
			MediaFile m = h.MediaFile;
			h = null;
			return m;
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
