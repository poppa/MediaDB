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
using System.Collections;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace MediaDB
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
	public class MediaFile : IDBFile
	{
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
    /// Directory in dir tree this file blongs to
    /// </summary>
    public int DirecotryTreeId = 0;

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
					case "fullname":
						FullName = reader.GetString(i);
						break;
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
					case "modified": Modified = reader.GetDateTime(i); break;
					case "keywords":
						Keywords = DB.IsNull(reader[i]) ? null : reader.GetString(i);
						break;
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
        string sql = "INSERT INTO `file` (name, fullname, mimetype,"    +
                     " title, description, copyright, width, height,"   +
                     " size, resolution, exif, created, modified,"      +
                     " keywords) "                                      +
                     "VALUES (@name, @fullname, @mimetype, @title,"     +
                     " @description, @copyright, @width, @height,"      +
                     " @size, @resolution, @exif, @created, @modified," +
                     " @keywords)";

        long tmpid;
        if (Settings.QueryInsert(out tmpid, sql,
                                 DB.Param("name", Name),
                                 DB.Param("fullname", FullName),
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
                                 DB.Param("modified", Modified),
                                 DB.Param("keywords", Keywords))) {
          Id = tmpid;
          Log.Debug("Inserted {0} OK (ID:{1})\n", FullName, Id);

					foreach (PreviewFile f in Previews) {
						f.FileId = Id;
						if (f.Save())
							Log.Debug("  >>> Inserted preview: {0}\n", f.Name);
						else
							Log.Warning("  >>> Failed inserting preview: {0}\n", f.Name);
					}

          return true;
        }
        else {
          Log.Debug("Failed inserting {0} into database!\n", FullName);
          return false;
        }
      }
      else {
        return false;
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
	}

  /// <summary>
  /// Preview file
  /// </summary>
  class PreviewFile : IDBFile
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
    public string Data = null;

		/// <summary>
		/// Save to database
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
    public bool Save()
    {
			if (Id == 0) {
				string sql = "INSERT INTO `preview` (file_id, mimetype, width," +
					           " height, size, name, data) " +
						         "VALUES (@file_id, @mimetype, @width, @height, @size," +
						         " @name, @data)";

				long tmpid;
				if (Settings.QueryInsert(out tmpid, sql,
				                         DB.Param("file_id", Id),
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

		/// <summary>
		/// Populate object from database record
		/// </summary>
		/// <param name="reader">
		/// A <see cref="MySqlDataReader"/>
		/// </param>
		public void SetFromSql(MySqlDataReader reader)
		{

		}
  }
}
