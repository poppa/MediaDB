/* Settings.cs
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MySql.Data.MySqlClient;
using MediaDB.Backend;

namespace MediaDB
{
	/// <summary>
	/// Static class for reading the config file
	/// </summary>
	public static class Manager
	{
		/// <summary>
		/// The path to the config file
		/// </summary>
		public static string File { get; private set; }

		/// <summary>
		/// Database connection info, host, db, user, password
		/// </summary>
		public static DBInfo DatabaseInfo { get; private set; }
		private static string dbstr = null;

		/// <summary>
		/// List of paths to index/scan
		/// </summary>
		public static List<BasePath> BasePaths { get; private set; }

		/// <summary>
		/// List of file types to collect
		/// </summary>
		public static List<MediaType> MediaTypes { get; private set; }

		/// <summary>
		/// Array of preview templates
		/// </summary>
		public static ArrayList Previews { get; private set; }

		/// <summary>
		/// List of directories
		/// </summary>
		public static List<MediaDB.Backend.Directory> 
			Directories { get; set; }

		/// <summary>
		/// The width of the smallest preview template
		/// </summary>
		public static int PreviewMinWidth { get; private set; }

		/// <summary>
		/// The height of the smallest preview template
		/// </summary>
		public static int PreviewMinHeight { get; private set; }

		/// <summary>
		/// Number of concurrent threads to run when indexing
		/// </summary>
		public static int Threads { get; private set; }

		private static string _tmpdir = null;

		/// <summary>
		/// Path to the applications temp directory
		/// </summary>
		public static string TmpDir {
			get {
				if (_tmpdir == null) {
#if LINUX
					_tmpdir = "/tmp";
#else
					_tmpdir = Path.GetTempPath();
#endif
					if (_tmpdir == null) {
						Log.Werror("Unable to locate tmp directory!\n");
						return null;
					}

					_tmpdir = Path.Combine(_tmpdir, "mediadb");

					if (!Tools.DirectoryExists(_tmpdir)) {
						try {
							System.IO.Directory.CreateDirectory(_tmpdir);
						}
						catch (Exception e) {
							Log.Werror("Error creating tmpdir: {0}\n", e.Message);
							_tmpdir = null;
						}
					}
				}

				return _tmpdir;
			}
		}

		/// <summary>
		/// The database connection
		/// </summary>
		public static MySqlConnection DbCon { get { return dbcon; }}
		private static MySqlConnection dbcon;

		/// <summary>
		/// Global mutex object
		/// </summary>
		public static readonly Mutex mutex = new Mutex();

		/// <summary>
		/// Null date
		/// </summary>
		public static DateTime NullDate = new DateTime(1970, 01, 01, 0, 0, 0);

		private static string previewQuality = null;
		/// <summary>
		/// Quality of preview images
		/// </summary>
		public static long PreviewQuality
		{
			get {
				return Convert.ToInt64(previewQuality);
			}
		}

		/// <summary>
		/// Max number of bytes the application can consume during indexing
		/// </summary>
		public static long MaxBytes { get; private set; }

		/// <summary>
		/// List of all files
		/// </summary>
		public static List<string> FileIndex { get; set; }

		/// <summary>
		/// Read the config file and populate this class
		/// </summary>
		/// <param name="file">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static bool Init(string file)
		{
			if (!Tools.DirectoryExists(TmpDir)) {
				Log.Warning("Tmpdir doesn't exist");
				return false;
			}

			FileInfo fi = new FileInfo(file);
			if (!fi.Exists) {
				Console.Error.Write("{0} doesn't exist!\n", file);
				return false;
			}

			File = file;
			MediaTypes = new List<MediaType>();
			Previews = new ArrayList();
			DatabaseInfo = new DBInfo();
			BasePaths = new List<BasePath>();
			Directories = new List<MediaDB.Backend.Directory>();
			FileIndex = new List<string>();

			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(File);

			if (xdoc.FirstChild.Name != "mediadb") {
				Log.Warning("Malformed config file. Root node name isn't \"mediadb\"!");
				return false;
			}

			ArrayList basePaths = new ArrayList();

			foreach (XmlNode child in xdoc.FirstChild.ChildNodes) {
				if (child.NodeType != XmlNodeType.Element)
					continue;

				switch (child.Name)
				{
					case "threads":
						Threads = Convert.ToInt32(child.FirstChild.Value);
						break;

					case "database":
						foreach (XmlNode c in child.ChildNodes) {
							if (c.NodeType != XmlNodeType.Element)
								continue;

							string v = c.FirstChild.Value;
							switch (c.Name) {
								case "host": DatabaseInfo.Host = v; break;
								case "database": DatabaseInfo.Name = v; break;
								case "username": DatabaseInfo.Username = v; break;
								case "password": DatabaseInfo.Password = v; break;
							}
						}

						break;

					case "mediatypes":
						foreach (XmlNode c in child.ChildNodes) {
							if (c.Name == "mediatype") {
								MediaType mt = new MediaType();
								mt.Extension = c.Attributes["extension"].Value.ToString();
								mt.Mimetype  = c.Attributes["mimetype"].Value.ToString();
								MediaTypes.Add(mt);
							}
						}
						break;

					case "maxbytes":
						MaxBytes = (1024*1024)*long.Parse(child.FirstChild.Value);
						break;

					case "previews":
						previewQuality = child.Attributes["quality"].Value.ToString();
						foreach (XmlNode c in child.ChildNodes) {
							if (c.Name == "preview") {
								Preview pv = new Preview();
								pv.Name = c.Attributes["name"].Value.ToString();
								pv.Width = Convert.ToInt32(c.Attributes["width"].Value);
								pv.Height = Convert.ToInt32(c.Attributes["height"].Value);

								if (PreviewMinWidth == 0 || pv.Width < PreviewMinWidth)
								    PreviewMinWidth = pv.Width;

								if (PreviewMinHeight == 0 || pv.Height < PreviewMinHeight)
								    PreviewMinHeight = pv.Height;

								Previews.Add(pv);
							}
						}
						break;

					case "paths":
						foreach (XmlNode c in child.ChildNodes) {
							if (c.Name == "path")
								basePaths.Add(c.FirstChild.Value);
						}
						break;
				}
			}

			Previews.Sort();
			Previews.Reverse();

			dbstr = String.Format("server={0};database={1};userid={2};password={3};",
			                      DatabaseInfo.Host, DatabaseInfo.Name,
			                      DatabaseInfo.Username, DatabaseInfo.Password);

			Log.Debug(">>> Connecting to database...");

			try {
				dbcon = new MySqlConnection(dbstr);
				dbcon.Open();
#if DEBUG
				DB.Query("TRUNCATE `file`");
				DB.Query("TRUNCATE `preview`");
        DB.Query("TRUNCATE `keyword`");
        DB.Query("TRUNCATE `keyword_rel`");
#endif
				Log.Debug("OK!\n");
			}
			catch (Exception e) {
				Log.Debug("FAILED! ");
				Log.Werror("{0}\n", e.Message);
				return false;
			}

			var db = new DbManager();

			// Collect base paths from database
			if (db.QueryReader("SELECT * FROM base_path")) {
				while (db.DataReader.Read()) {
					string p = db.DataReader.GetString("path");
					long id = db.DataReader.GetInt64("id");

					if (!(basePaths.Contains(p) && Tools.DirectoryExists(p))) {
						Log.Notice("Base path \"{0}\" is removed from config or file " +
						           "system! Removing from database...\n", p);
						// NOTE! This uses the global DB object. It's not supposed to
						// use the local one since it would conflict with the DataReader.
						DB.Query("DELETE FROM base_path WHERE path = @path",
						         DB.Param("path", p));
					}
					else
						BasePaths.Add(new BasePath(id, p));
				}
			}

			// Sync paths from config file with path from database
			foreach (string path in basePaths) {
				var t = BasePaths.Find(delegate(BasePath tp) {
					return tp.Name == path;
				});
				if (t == null) {
					if (!Tools.DirectoryExists(path)) {
						Log.Warning("Path \"{0}\" in config file doesn't exits in file " +
						            "system!\n");
					}
					else {
						Log.Debug("+++ Add {0} to base paths\n", path);
						long myid;
						string sql = "INSERT INTO base_path(path) VALUES (@path)";
						// NOTE! This uses the global DB object. It's not supposed to
						// use the local one since it would conflict with the DataReader.
						if (DB.QueryInsert(out myid, sql, DB.Param("path", path)))
							BasePaths.Add(new BasePath(myid, path));
						else
							Log.Warning("Failed adding {0} to base_path!\n", path);
					}
				}
			}

			// Setup the directory list
			if (db.QueryReader("SELECT * FROM directory")) {
				while (db.DataReader.Read()) {
					MediaDB.Backend.Directory d = 
						MediaDB.Backend.Directory.FromSql(db.DataReader);
          if (Tools.DirectoryExists(d.FullName) && InBasePaths(d.FullName)) {
						Directories.Add(d);
          }
          else {
						DB.Query("DELETE FROM directory WHERE id='" + d.Id + "'");
          }
				}
			}

			// Setup the list of files
			if (db.QueryReader("SELECT fullname FROM `file`")) {
				while (db.DataReader.Read())
					FileIndex.Add(db.DataReader.GetString("fullname"));
			}

			filesCount = FileIndex.Count;

			db.Dispose();

			return true;
		}

    public static bool InBasePaths(string path)
    {
			foreach (BasePath bp in BasePaths)
				if (path.StartsWith(bp.Name))
					return true;

			return false;
    }

		/// <summary>
		/// Returns the directory object for <paramref name="path"/> if
		/// it exists.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static MediaDB.Backend.Directory GetDirectory(string path)
		{
			return Directories.Find(delegate(MediaDB.Backend.Directory dd) {
				return dd.FullName == path;
			});
		}

		/// <summary>
		/// Returns the media type for the given file or null if non is found
		/// </summary>
		/// <param name="file">
		/// A <see cref="FileInfo"/>
		/// </param>
		/// <returns>
		/// A <see cref="MediaType"/>
		/// </returns>
		public static MediaType GetMediaType(FileInfo file)
		{
			return MediaTypes.Find(delegate(MediaType mt) {
				return mt.HasExtension(file.Extension);
			});
		}

		private static long filesCount = 0;

		/// <summary>
		/// Get media file for <paramref name="fullname"/>.
		/// If it exists in the database the media file object will be
		/// populated. Otherwise an empty <see cref="MediaFile"/> object will
		/// be returned.
		/// </summary>
		/// <param name="fullname"></param>
		/// <returns></returns>
		public static MediaFile GetMediaFile(string fullname)
		{
			if (filesCount == 0) {
				return null;
			}

			MediaFile mf = null;
			//! TODO: I'm note sure what performance gain this lookup has. One thing
			//! is certain though: It's a pain keeping it in sync...
			if (FileIndex.Contains(fullname)) {
				// This method is called from an async method in Indexer.cs.
				// So lock during the DB call and release when db done.
				mutex.WaitOne();

				using (var db = new DbManager()) {
					try {
						string sql = "SELECT * FROM `file` WHERE fullname = @fn";
						// Log.Debug("@@@ GetMEdiaFile({0})\n", sql.Replace("@fn", fullname));
						if (db.QueryReader(sql, DB.Param("fn", fullname))) {
							if (db.DataReader.HasRows) {
								db.DataReader.Read();
								mf = MediaFile.FromSql(db.DataReader);
							}
						}
					}
					catch (Exception e) {
						Log.Warning("DB error: {0} {1}\n", e.Message, e.StackTrace);
					}
				}

				// Release the thread
				mutex.ReleaseMutex();
			}
			return mf;
		}

		/// <summary>
		/// Get a <see cref="FileHandler"/> object for <paramref name="file"/> of
		/// mediatype <paramref name="type"/>.
		/// </summary>
		/// <param name="file">
		/// A <see cref="FileInfo"/>
		/// </param>
		/// <param name="type">
		/// A <see cref="MediaType"/>
		/// </param>
		/// <returns>
		/// A <see cref="FileHandler"/>
		/// </returns>
		public static FileHandler GetFileHandler(FileInfo file, MediaType type)
		{
			switch (type.Mimetype)
			{
				case "image/bmp":
				case "image/gif":
				case "image/png":
				case "image/jpeg":
				case "image/tiff":
					return new IMGHandler(file, type);

				case "image/x-eps":
				case "application/illustrator":
					return new EPSHandler(file, type);

				case "application/pdf":
					return new PDFHandler(file, type);

				case "image/svg+xml":
					return new SVGHandler(file, type);
			}

			return null;
		}

		/// <summary>
		/// Add file with path <paramref name="path"/> to the database
		/// </summary>
		/// <param name="path">
		/// A <see cref="System.String"/>
		/// </param>
		public static void AddFile(string path)
		{
			AddFile(new FileInfo(path));
		}

		/// <summary>
		/// Add file of <paramref name="path"/> to the database.
		/// </summary>
		/// <param name="path">
		/// A <see cref="FileInfo"/>
		/// </param>
		public static void AddFile(FileInfo path)
		{
			if (!path.Exists) {
				Log.Debug("Can't add non-exisiting file \"{0}\"",path.FullName);
				return;
			}

			var mt = GetMediaType(path);
			if (mt == null) {
				Log.Debug("Unhandled file: {0}\n", path.FullName);
				return;
			}

			var fh = GetFileHandler(path, mt);
			if (fh != null) {
				try {
					Log.Debug("+++ Adding {0} to database\n", path.FullName);
					fh.Process();
				}
				catch (Exception e) {
					Log.Debug("Unable to process file {0} in Manager.AddFile: {1}\n",
					          path.FullName, e.Message);
				}

				fh.Dispose();
				fh = null;
				AddToFileIndex(path.FullName);
			}
		}

		/// <summary>
		/// Deletefile from database.
		/// </summary>
		/// <param name="path">
		/// A string
		/// </param>
		public static void DeleteFile(string path)
		{
	    MediaFile mf = GetMediaFile(path);
	    if (mf != null) {
        try {
          Log.Debug("--- Deleting {0} from database\n", path);
          mf.DeleteFromDB();
          mf.Dispose();
        }
        catch (Exception e) {
          Log.Debug("--- ERROR: Could not delete {0} from database. " +
					          "Sorry mate : {1}\n", path, e.Message);
        }
	    }
		}
        
		/// <summary>
		/// Add <paramref name="path"/> to the <see cref="FileIndex">file index</see>
		/// </summary>
		/// <param name="path"></param>
		public static void AddToFileIndex(string path)
		{
			if (!FileIndex.Contains(path)) {
				FileIndex.Add(path);
				filesCount = FileIndex.Count;
			}
		}

		/// <summary>
		/// Dispose, close db e t c.
		/// </summary>
		public static void Dispose()
		{
			if (dbcon != null) {
				try {
					dbcon.Close();
					dbcon.Dispose();
					dbcon = null;
				}
				catch (Exception e) {
					Log.Debug("@ {0}\n{1}\n", e.Message, e.StackTrace);
				}
			}
		}
	}

	/// <summary>
	/// Class for storing info about the database connection
	/// </summary>
	public class DBInfo
	{
		/// <summary>
		/// The database host name
		/// </summary>
		public string Host;

		/// <summary>
		/// The database name
		/// </summary>
		public string Name;

		/// <summary>
		/// The database username
		/// </summary>
		public string Username;

		/// <summary>
		/// The database user's password
		/// </summary>
		public string Password;
	}

	/// <summary>
	/// Media type object
	/// </summary>
	public class MediaType
	{
		private string ext;
		/// <summary>
		/// Extensions (comma separated string) associated to this media type
		/// </summary>
		public string Extension
		{
			get { return ext; }
			set {
				exts = new ArrayList();
				ext = value;
				foreach (string t in ext.Split(new char[] { ',' }))
					exts.Add(t.Trim().ToLower());
			}
		}

		private ArrayList exts = new ArrayList();

		/// <summary>
		/// Extensions associated to this media
		/// </summary>
		public ArrayList Extensions
		{
			get { return exts; }
			private set { exts = value; }
		}

		/// <summary>
		/// Mimetype of this media type
		/// </summary>
		public string Mimetype;

		/// <summary>
		/// Checks if <paramref name="extension"/> is handled by this mediatype
		/// </summary>
		/// <param name="extension"></param>
		/// <returns></returns>
		public bool HasExtension(string extension)
		{
			if (!extension.StartsWith("."))
				extension = "." + extension;

			return exts.Contains(extension.ToLower());
		}

		/// <summary>
		/// Cast to string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("MediaType(\"{0}\", \"{1}\")",
			                     ext, Mimetype);
		}
	}

	/// <summary>
	/// Class representing a preview image template
	/// </summary>
	public class Preview : IComparable
	{
		/// <summary>
		/// Max width of the preview
		/// </summary>
		public int Width;

		/// <summary>
		/// Max height of the preview
		/// </summary>
		public int Height;

		/// <summary>
		/// Arbitrary name of the template
		/// </summary>
		public string Name;

		/// <summary>
		/// Constructor
		/// </summary>
		public Preview() {}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">
		/// A <see cref="System.UInt32"/>
		/// </param>
		/// <param name="height">
		/// A <see cref="System.UInt32"/>
		/// </param>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public Preview(int width, int height, string name)
		{
			Width = width;
			Height = height;
			Name = name;
		}

		/// <summary>
		/// Comparer method
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(object other)
		{
			Preview p = (Preview)other;
			return (Width * Height).CompareTo(p.Width * p.Height);
		}
	}
}
