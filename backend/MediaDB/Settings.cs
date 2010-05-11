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
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;

namespace MediaDB
{
	/// <summary>
	/// Static class for reading the config file
	/// </summary>
	public static class Settings
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
		/// Array of paths to index/scan
		/// </summary>
		public static ArrayList Paths { get; private set; }

		/// <summary>
		/// Array of file types to collect
		/// </summary>
		public static ArrayList MediaTypes { get; private set; }

		/// <summary>
		/// Array of preview templates
		/// </summary>
		public static ArrayList Previews { get; private set; }

		/// <summary>
		/// The width of the smallest preview template
		/// </summary>
		public static uint PreviewMinWidth { get; private set; }

		/// <summary>
		/// The height of the smallest preview template
		/// </summary>
		public static uint PreviewMinHeight { get; private set; }

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

					Log.Debug("TMPDIR: {0}\n", _tmpdir);

					if (!Tools.DirectoryExists(_tmpdir)) {
						try {
							Directory.CreateDirectory(_tmpdir);
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

		private static MySqlConnection dbcon;

		/// <summary>
		/// The database connection
		/// </summary>
		public static MySqlConnection DbCon { get { return dbcon; }}

    public static readonly Mutex mutex = new Mutex();

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
			Paths = new ArrayList();
			MediaTypes = new ArrayList();
			Previews = new ArrayList();
			DatabaseInfo = new DBInfo();

			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(File);

			if (xdoc.FirstChild.Name != "mediadb") {
				Log.Warning("Malformed config file. Root node name isn't \"mediadb\"!");
				return false;
			}

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

					case "previews":
						foreach (XmlNode c in child.ChildNodes) {
							if (c.Name == "preview") {
								Preview pv = new Preview();
								pv.Name = c.Attributes["name"].Value.ToString();
								pv.Width = Convert.ToUInt32(c.Attributes["width"].Value);
								pv.Height = Convert.ToUInt32(c.Attributes["height"].Value);

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
								Paths.Add(c.FirstChild.Value);
						}
						break;
				}
			}

			dbstr = String.Format("server={0}; "  +
			                      "database={1};" +
                            "userid={2};"   +
			                      "password={3};",
			                      DatabaseInfo.Host,
			                      DatabaseInfo.Name,
			                      DatabaseInfo.Username,
			                      DatabaseInfo.Password);
			try {
        Log.Debug("Con str: {0}\n", dbstr);
				dbcon = new MySqlConnection(dbstr);
        Log.Debug("Database db: {0}\n", DatabaseInfo.Name);
				dbcon.Open();
        //dbcon.Close();
			}
			catch (Exception e) {
				Log.Werror("Unable to connect to database: {0}\n", e.Message);
				return false;
			}

			return true;
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
      foreach (MediaType mt in MediaTypes)
        if (mt.HasExtension(file.Extension))
          return mt;

      return null;
    }

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
      // This method is called from an async method in Indexer.cs.
      // So lock during the DB call and release when db done.
      mutex.WaitOne();

      MediaFile mf = null;
			MySqlDataReader r = null;
      try {
        string sql = "SELECT * FROM `file` WHERE fullname = @fn";
        if (Query(out r, sql, DB.Param("fn", fullname))) {
          if (r.HasRows) {
						r.Read();
            mf = new MediaFile();
						mf.SetFromSql(r);
            Log.Debug("   @@@ Found file {0} in database!\n", fullname);
          }

          DB.EndReader(ref r);
        }
      }
      catch (Exception e) {
        Log.Warning("DB error: {0} {1}\n", e.Message, e.StackTrace);
				DB.EndReader(ref r);
      }

      // Release the thread
      mutex.ReleaseMutex();

      return mf;
    }

		/// <summary>
		/// Query database with insert statement.
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.Int64"/>. Will be populated with the insert ID.
		/// </param>
		/// <param name="sql">
		/// A <see cref="System.String"/>. The SQL query
		/// </param>
		/// <param name="args">
		/// A <see cref="MySqlParameter[]"/>. Query parameters.
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
    public static bool QueryInsert(out long id, string sql,
                                   params MySqlParameter[] args)
    {
      try {
        MySqlCommand cmd = dbcon.CreateCommand();
        cmd.CommandText = sql;

        if (args.Length > 0)
          foreach (MySqlParameter p in args)
            cmd.Parameters.Add(p);

        cmd.ExecuteNonQuery();
        id = cmd.LastInsertedId;
        cmd.Dispose();
        cmd = null;
      }
      catch (Exception e) {
        Log.Warning("DB error: {0} {1}\n", e.Message, e.StackTrace);
        id = 0;
        return false;
      }

      return true;
    }

    /// <summary>
    /// Performs a database query
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static bool Query(out MySqlDataReader rd,
                             string sql,
                             params MySqlParameter[] args)
    {
      try {
        MySqlCommand cmd = dbcon.CreateCommand();
        cmd.CommandText = sql;

        if (args.Length > 0)
          foreach (object o in args)
            cmd.Parameters.Add(o);

        rd = cmd.ExecuteReader();
        cmd.Dispose();
        cmd = null;

        return true;
      }
      catch (Exception e) {
        Log.Debug("DB error: {0} {1}\n", e.Message, e.StackTrace);
      }

      rd = null;

      return false;
    }

    /// <summary>
    /// Dispose, close db e t c.
    /// </summary>
    public static void Dispose()
    {
      if (dbcon != null) {
        dbcon.Clone();
        dbcon.Dispose();
        dbcon = null;
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
	public class Preview
	{
		/// <summary>
		/// Max width of the preview
		/// </summary>
		public uint Width;

		/// <summary>
		/// Max height of the preview
		/// </summary>
		public uint Height;

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
		public Preview(uint width, uint height, string name)
		{
			Width = width;
			Height = height;
			Name = name;
		}
	}
}
