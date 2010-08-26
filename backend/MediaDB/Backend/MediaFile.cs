/* MediaFile.cs
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
using System.Security.Cryptography;
using MySql.Data.MySqlClient;

namespace MediaDB.Backend
{
	/// <summary>
	/// Interface for database related files
	/// </summary>
	interface IDBFile
	{
		/// <summary>
		/// Save to DB. Should handle both inserts and updates
		/// </summary>
		/// <returns></returns>
		bool Save();

		/// <summary>
		/// Populate the object from database record
		/// </summary>
		/// <param name="reader">
		/// A <see cref="MySqlDataReader"/>
		/// </param>
		void SetFromSql(MySqlDataReader reader);
	}

	/// <summary>
	/// Media file
	/// </summary>
	public class MediaFile : IDBFile, IDisposable
	{
		/// <summary>
		/// A SHA1 hash of the file content
		/// </summary>
		public string Sha1Hash {
			get {
				if (sha1Hash == null) computeFileHash();
				return sha1Hash;
			}
		}
		private string sha1Hash = null;

		/// <summary>
		/// Database id of the file
		/// </summary>
		public long Id = 0;

		/// <summary>
		/// Filename
		/// </summary>
		public string Name = null;

		/// <summary>
		/// Full path
		/// </summary>
		public string FullName = null;

		/// <summary>
		/// ID of directory this file belongs to
		/// </summary>
		public long DirectoryId = 0;

		/// <summary>
		/// File title
		/// </summary>
		public string Title = null;

		/// <summary>
		/// File description
		/// </summary>
		public string Description = null;

		/// <summary>
		/// File mimetype
		/// </summary>
		public string Mimetype = null;

		/// <summary>
		/// Copyright info
		/// </summary>
		public string Copyright = null;

		/// <summary>
		/// File width
		/// </summary>
		public int Width = 0;

		/// <summary>
		/// File height
		/// </summary>
		public int Height = 0;

		/// <summary>
		/// File size
		/// </summary>
		public long Size = 0;

		/// <summary>
		/// Raw exif info
		/// </summary>
		public string Exif = null;

		/// <summary>
		/// Keywords
		/// </summary>
		public string Keywords = null;

		/// <summary>
		/// Image resolution
		/// </summary>
		public double Resolution = 0;

		/// <summary>
		/// File creation time
		/// </summary>
		public DateTime Created = new DateTime(1970, 1, 1, 0, 0, 0, 0);

		/// <summary>
		/// File modification time
		/// </summary>
		public DateTime Modified = new DateTime(1970, 1, 1, 0, 0, 0, 0);

		/// <summary>
		/// Array of preview images
		/// </summary>
		public ArrayList Previews = new ArrayList();

		/// <summary>
		/// Array of category id's this file belongs to
		/// </summary>
		public int[] Categories = { };

		/// <summary>
		/// Compute a hash of the file content
		/// </summary>
		private void computeFileHash()
		{
			if (FullName == null) {
				throw new Exception("Can not compute file hash when property " +
														"\"FullName\" isn't set!");
			}

			//sha1Hash = Tools.ComputeFileHash(FullName);
			sha1Hash = "";
		}

		/// <summary>
		/// Creates and populates a <see cref="MediaFile" /> object
		/// from the datareader <paramref name="rd"/>
		/// </summary>
		/// <param name="rd"></param>
		/// <returns></returns>
		public static MediaFile FromSql(MySqlDataReader rd)
		{
			MediaFile m = new MediaFile();
			m.SetFromSql(rd);
			return m;
		}

		/// <summary>
		/// Populate object from database record
		/// </summary>
		/// <param name="reader">
		/// A <see cref="MySqlDataReader"/>
		/// </param>
		public void SetFromSql(MySqlDataReader reader)
		{
			for (int i = 0; i < reader.FieldCount; i++) {
				switch (reader.GetName(i)) {
					case "id": Id = reader.GetInt64(i); break;
					case "name": Name = reader.GetString(i); break;
					case "fullname": FullName = reader.GetString(i); break;
					case "sha1_hash": sha1Hash = reader.GetString(i); break;
					case "directory_id": DirectoryId = reader.GetInt64(i); break;
					case "mimetype": Mimetype = reader.GetString(i); break;
					case "title":
						Title = DB.IsNull(reader[i]) ? null : reader.GetString(i);
						break;
					case "description":
						Description = DB.IsNull(reader[i]) ? null : reader.GetString(i);
						break;
					case "copyright":
						Copyright = DB.IsNull(reader[i]) ? null : reader.GetString(i);
						break;
					case "width": Width = reader.GetInt32(i); break;
					case "height": Height = reader.GetInt32(i); break;
					case "size": Size = reader.GetInt64(i); break;
					case "resolution": Resolution = reader.GetDouble(i); break;
					case "exif":
						Exif = DB.IsNull(reader[i]) ? null : reader.GetString(i);
						break;
					case "created": Created = reader.GetDateTime(i); break;
					case "modified":
						if (!DB.IsNull(reader[i]))
							Modified = reader.GetDateTime(i);
						break;
					case "keywords":
						Keywords = DB.IsNull(reader[i]) ? null : reader.GetString(i);
						break;
				}
			}

			if (Id > 0) {
				using (var db = new DbManager(Manager.DbCon)) {
					var sql = "SELECT * FROM `preview` WHERE file_id=@id";
					if (db.QueryReader(sql, DB.Param("id", Id))) {
						while (db.DataReader.NextResult()) {
							db.DataReader.Read();
							Previews.Add(PreviewFile.FromSql(db.DataReader));
						}
					}
				}
			}
		}

		/// <summary>
		/// Save to database. Inserts if new, updates otherwise
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool Save()
		{
			if (Id == 0) {
				string sql = "INSERT INTO `file` (name, fullname, directory_id,"       +
				             " mimetype, title, description, copyright, width, height,"+
				             " size, resolution, exif, created, modified, keywords, "  +
				             " sha1_hash) "                                            +
				             "VALUES (@name, @fullname, @directory_id, @mimetype,"     +
				             " @title, @description, @copyright, @width, @height,"     +
				             " @size, @resolution, @exif, @created, @modified, "       +
				             " @keywords, @sha1_hash)";

				long tmpid;
				object mddate = null;
				if (Modified != Manager.NullDate)
					mddate = Modified;
				var db = new DbManager(Manager.DbCon);
				if (db.QueryInsert(out tmpid, sql,
				                   DB.Param("name", Name),
				                   DB.Param("fullname", FullName),
				                   DB.Param("directory_id", DirectoryId),
				                   DB.Param("mimetype", Mimetype),
				                   DB.Param("title", Title),
				                   DB.Param("description", Description),
				                   DB.Param("copyright", Copyright),
				                   DB.Param("width", Width),
				                   DB.Param("height", Height),
				                   DB.Param("size", Size),
				                   DB.Param("resolution", Resolution),
				                   DB.Param("exif", Exif),
				                   DB.Param("created", Created),
				                   DB.Param("modified", mddate),
				                   DB.Param("keywords", Keywords),
													 DB.Param("sha1_hash", Sha1Hash)))
				{
					Id = tmpid;

          SaveKeywords(Id, Keywords);

					// Update the file index in Manager
					Manager.AddToFileIndex(FullName);

					foreach (PreviewFile f in Previews) {
						f.FileId = Id;
						if (!f.Save()) {
							Log.Warning("  >>> Failed inserting preview \"{0}\" for " +
							            "\"{1}\"\n", f.Name, FullName);
						}
					}

					db.Dispose();
					return true;
				}
				else {
					Log.Debug("Failed inserting {0} into database!\n", FullName);
					db.Dispose();
					return false;
				}
			}
			else {
				// To regenerate hash
				sha1Hash = null;

				DB.Query("DELETE FROM `preview` WHERE file_id=@id",
								 DB.Param("id", Id));

				string sql = "UPDATE `file` SET "           +
										 " sha1_hash=@sha1_hash,"       +
										 " name=@name,"                 +
										 " fullname=@fullname,"         +
										 " directory_id=@directory_id," +
										 " mimetype=@mimetype,"         +
										 " title=@title,"               +
										 " description=@description,"   +
										 " copyright=@copyright,"       +
										 " width=@width,"               +
										 " height=@height,"             +
										 " resolution=@resolution,"     +
										 " exif=@exif,"                 +
										 " modified=@modified,"         +
										 " keywords=@keywords "         +
										 "WHERE id=@id";

				if (DB.Query(sql, DB.Param("sha1_hash", Sha1Hash),
				                  DB.Param("name", Name),
				                  DB.Param("fullname", FullName),
				                  DB.Param("directory_id", DirectoryId),
													DB.Param("mimetype", Mimetype),
													DB.Param("title", Title),
													DB.Param("description", Description),
													DB.Param("copyright", Copyright),
													DB.Param("width", Width),
													DB.Param("height", Height),
													DB.Param("resolution", Resolution),
													DB.Param("exif", Exif),
													DB.Param("modified", Modified),
													DB.Param("keywords", Keywords),
													DB.Param("id", Id))) 
				 {
          if (Previews.Count == 0) {
            // var fi = new FileInfo(FullName);
            // MediaType mt = Manager.GetMediaType(fi);
            // FileHandler fh = Manager.GetFileHandler(fi, mt);
            // fh.
          }
					foreach (PreviewFile f in Previews) {
						f.FileId = Id;
						if (!f.Save()) {
							Log.Warning("  >>> Failed inserting preview \"{0}\" for " +
													"\"{1}\"\n", f.Name, FullName);
						}
					}
					return true;
				}

				Log.Debug("^^^ Failed updating {0}\n", FullName);
				return false;
			}
		}

		public void DeleteFromDB()
		{
	    DB.Query("DELETE FROM `preview` WHERE file_id=@id",
	             DB.Param("@id", Id));
	    DB.Query("DELETE FROM `file` WHERE id=@id",
	             DB.Param("@id", Id));
		}

		private void SaveKeywords(long Id, string Keywords)
		{
	    if (Keywords != null) {
        string[] keys = Keywords.Split(',');

        foreach (string key in keys) {
          string lowCaseKey = key.ToLower();
          int keyword_id = -1;

          using (var db = new DbManager(Manager.DbCon)) {
            string sql = "SELECT id FROM `keywords` WHERE keyword=@keyword";
            db.QueryReader(sql, DB.Param("keyword", lowCaseKey));
            if (db.DataReader.HasRows) {
              db.DataReader.Read();
              keyword_id = (int)db.DataReader.GetValue(0);
            }
          }

          if (keyword_id == -1) {
            using (var dbinner = new DbManager(Manager.DbCon)) {
              long tmpkwdid;
              string sql = "INSERT INTO keywords (keyword) VALUES (@kwd)";
              dbinner.QueryInsert(out tmpkwdid, sql,
							                    DB.Param("kwd", lowCaseKey));
              keyword_id = (int)tmpkwdid;
            }
          }

          using (var dbinners = new DbManager(Manager.DbCon)) {
            long tmpid;
            string sql = "INSERT INTO keyword_rel (file_id,keyword_id) " +
                         "VALUES (@fileid,@keyid)";
            dbinners.QueryInsert(out tmpid, sql,
                                 DB.Param("fileid", Id),
                                 DB.Param("keyid", keyword_id));
          }
        }
	    }
		}
    
		/// <summary>
		/// String casting method
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("MediaFile(\"{0}\", \"{1}\", \"{2}x{3}\", {4}kB)",
			                     FullName, Mimetype, Width, Height, Size / 1024);
		}

		/// <summary>
		/// Disposes this object
		/// </summary>
		public void Dispose()
		{
			foreach (PreviewFile p in Previews)
				p.Dispose();

			Previews = null;
		}

    internal void Update()
    {
      /// UPDATE the db entry.
      /// -> Read new info from file
      /// -> Re-generate previews
      /// -> Save() to db, make sure to keep ID
      ///    and replace previews in db.
      throw new NotImplementedException();
    }
  }

	/// <summary>
	/// Directory in database
	/// </summary>
	public class Directory : IDBFile
	{
		/// <summary>
		/// MySQL id
		/// </summary>
		public long Id = 0;

		/// <summary>
		/// Parent directory id
		/// </summary>
		public long ParentId = 0;

		/// <summary>
		/// Id of base path this directory belongs to
		/// </summary>
		public long BasePathId = 0;

		/// <summary>
		/// Directory name
		/// </summary>
		public string Name = null;

		/// <summary>
		/// Directory path
		/// </summary>
		public string FullName = null;

		/// <summary>
		/// Path minus base path
		/// </summary>
		public string ShortName = null;

		/// <summary>
		/// Creates and populates an object from a sql record
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static Directory FromSql(MySqlDataReader reader)
		{
			Directory dir = new Directory();
			dir.SetFromSql(reader);
			return dir;
		}

		/// <summary>
		/// Save to database
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool Save()
		{
			if (Id == 0) {
				string sql = "INSERT INTO `directory` (name, fullname, shortname," +
										 " parent_id, base_path_id) " +
										 "VALUES (@name, @fullname, @shortname, @parent_id," +
										 " @base_path_id)";
				long myid;
				if (DB.QueryInsert(out myid, sql,
													 DB.Param("name", Name),
													 DB.Param("fullname", FullName),
													 DB.Param("shortname", ShortName),
													 DB.Param("parent_id", ParentId),
													 DB.Param("base_path_id", BasePathId))) {
					Id = myid;
					return true;
				}
			}
			else {
				Log.Debug("::: Update: {0}\n", FullName);
			}
			return false;
		}

		/// <summary>
		/// Populate object from database record
		/// </summary>
		/// <param name="reader">
		/// A <see cref="MySqlDataReader"/>
		/// </param>
		public void SetFromSql(MySqlDataReader reader)
		{
			Id = reader.GetInt64("id");
			ParentId = reader.GetInt64("parent_id");
			BasePathId = reader.GetInt64("base_path_id");
			Name = reader.GetString("name");
			FullName = reader.GetString("fullname");
		}
	}

	/// <summary>
	/// Preview file
	/// </summary>
	class PreviewFile : IDBFile, IDisposable
	{
		/// <summary>
		/// Database ID
		/// </summary>
		public long Id = 0;

		/// <summary>
		/// Preview of file
		/// </summary>
		public long FileId = 0;

		/// <summary>
		/// Preview's mimetype
		/// </summary>
		public string Mimetype = null;

		/// <summary>
		/// Preview identifier name
		/// </summary>
		public string Name = null;

		/// <summary>
		/// Width of preview
		/// </summary>
		public int Width = 0;

		/// <summary>
		/// Height of preview
		/// </summary>
		public int Height = 0;

		/// <summary>
		/// File size
		/// </summary>
		public long Size = 0;

		/// <summary>
		/// Image data
		/// </summary>
		public object Data = null;

		/// <summary>
		/// Save to database
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool Save()
		{
			if (Id == 0) {
				string sql = "INSERT INTO `preview` (file_id, mimetype, width,"     +
				             " height, size, name, data) "                          +
				             "VALUES (@file_id, @mimetype, @width, @height, @size," +
				             " @name, @data)";

				long tmpid;
				if (DB.QueryInsert(out tmpid, sql,
				                   DB.Param("file_id", FileId),
				                   DB.Param("mimetype", Mimetype),
				                   DB.Param("width", Width),
				                   DB.Param("height", Height),
				                   DB.Param("size", Size),
				                   DB.Param("name", Name),
				                   DB.Param("data", Data)))
				{
					Id = tmpid;
					return true;
				}
			}
			// Update
			else {

			}
			return false;
		}

		public static PreviewFile FromSql(MySqlDataReader rd)
		{
			var pf = new PreviewFile();
			pf.SetFromSql(rd);
			return pf;
		}

		/// <summary>
		/// Populate object from database record
		/// </summary>
		/// <param name="reader">
		/// A <see cref="MySqlDataReader"/>
		/// </param>
		public void SetFromSql(MySqlDataReader reader)
		{
			Id = reader.GetInt64("id");
			FileId = reader.GetInt64("file_id");
			Mimetype = reader.GetString("mimetype");
			Width = reader.GetInt32("width");
			Height = reader.GetInt32("height");
			Size = reader.GetInt64("size");
			Name = reader.GetString("name");
		}

		/// <summary>
		/// Disposes this object
		/// </summary>
		public void Dispose()
		{
			Data = null;
		}
	}
}