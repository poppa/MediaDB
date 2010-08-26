/* BasePath.cs
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
 *  Martin Pedersen
 */

using System;
using System.IO;
using System.Security;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace MediaDB.Backend
{
	/// <summary>
	/// Base path
	/// </summary>
	public class BasePath
	{
		/// <summary>
		/// MySql ID
		/// </summary>
		public long Id;

		/// <summary>
		/// Path name
		/// </summary>
		public string Name;

		/// <summary>
		/// Constructor
		/// </summary>
		public BasePath() { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.Int64"/>
		/// </param>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public BasePath(long id, string name)
		{
			Id = id;
			Name = name;

			initWatcher();
		}

		// File system watcher object
		private FileSystemWatcher fsw;

		// Initialize the file system watcher
		private void initWatcher()
		{
			fsw = new FileSystemWatcher();
			fsw.Path = Name;
			fsw.IncludeSubdirectories = true;
			fsw.NotifyFilter = NotifyFilters.LastWrite
											 | NotifyFilters.CreationTime
											 | NotifyFilters.FileName
											 | NotifyFilters.DirectoryName;

			fsw.Filter = "*.*";
			fsw.Changed += new FileSystemEventHandler(onChanged);
			fsw.Created += new FileSystemEventHandler(onChanged);
			fsw.Deleted += new FileSystemEventHandler(onChanged);
			fsw.Renamed += new RenamedEventHandler(onRenamed);

			fsw.EnableRaisingEvents = true;
		}

		ArrayList addQueue = new ArrayList();

		// Callback for Changed, Created and Deleted
		private void onChanged(object source, FileSystemEventArgs args)
		{
			if (Tools.IsDir(args.FullPath)) {
				Log.Debug("Change in dir, skip for now...\n");
				return;
			}

			var fi = new FileInfo(args.FullPath);
			MediaType mt = Manager.GetMediaType(fi);

			if (mt == null) {
				//Log.Debug("Unhandled file type...{0}\n", args.Name);
				return;
			}

			switch (args.ChangeType) {
				case WatcherChangeTypes.Changed:
					Log.Debug("File {0} changed\n", args.FullPath);

					if (addQueue.Contains(args.FullPath)) {
						addQueue.Remove(args.FullPath);
						FileHandler fh = Manager.GetFileHandler(fi, mt);
						if (fh != null)
							Manager.AddFile(fi);
					}
					else {
						Log.Debug("File's really changed\n");
						var f = Manager.GetMediaFile(fi.FullName);
						if (f != null) {
							if (Tools.ComputeFileHash(fi.FullName) != f.Sha1Hash) {
								Log.Debug("File contents changed in {0}\n", f.FullName);
                // Save changes.
                f.Update();
							}
							else {
								Log.Debug("No change in content of {0}\n", f.FullName);
							}
						}
					}
					break;

				case WatcherChangeTypes.Created:
					Log.Debug("File {0} added\n", args.FullPath);
					if (!Manager.FileIndex.Contains(args.FullPath)) {
						addQueue.Add(args.FullPath);
						addQueue.Remove(args.FullPath);
						FileHandler fh = Manager.GetFileHandler(fi, mt);
						if (fh != null)
							Manager.AddFile(fi);
					}
					break;

				case WatcherChangeTypes.Deleted:
					Log.Debug("File {0} deleted\n", args.FullPath);
                    Manager.DeleteFile(args.FullPath);
					break;
			}

		}

		// Callback for Renamed
		private void onRenamed(object source, RenamedEventArgs args)
		{
			Log.Debug("onRenamed({0} >> {1})\n", args.OldFullPath, args.FullPath);

			if (Tools.IsDir(args.FullPath)) {
				Log.File("Rename directory: {0} > {1}\n",
								 args.OldFullPath, args.FullPath);
			}
			else {
				if (Manager.GetMediaType(new FileInfo(args.OldFullPath)) == null)
					return;

				Log.Debug("Rename file: {0} > {1}\n", args.OldFullPath, args.FullPath);

				var mf = Manager.GetMediaFile(args.OldFullPath);
				if (mf != null) {
					var fi = new FileInfo(args.FullPath);
					mf.Name = fi.Name;
					mf.FullName = fi.FullName;

					// FREAKHACK XL:  -->
					// Sunkigt som fasen men så får det bli. Man kan ju
					// FIXME: snyggare generering av previews. Detta är vansinnigt.
					MediaType mt = Manager.GetMediaType(fi);
					FileHandler fh = Manager.GetFileHandler(fi, mt);
					fh.ProcessNS();
					mf.Previews = fh.MediaFile.Previews;
					// <--

					if (mf.Save()) {
						Log.Debug("Renamed \"{0}\" to \"{1}\" successfully!\n",
						          args.OldFullPath, args.FullPath);
					}
					else {
						Log.Debug("Failed renaming \"{0}\" to \"{1}\"!\n",
										  args.OldFullPath, args.FullPath);
					}
					fh.Dispose();
				}
				else {
					Log.Debug("Unable to locate media file for path \"{0}\" in " +
					          "BasePath.onRenamed()\n", args.OldFullPath);
				}
			}

			//MediaFile mf = Manager.GetMediaFile(args.OldFullPath);
		}
	}

	class SimpleBasePath : BasePath
	{
		public SimpleBasePath() { }

		public SimpleBasePath(long id, string name)
		{
			Id = id;
			Name = name;
		}
	}
}
