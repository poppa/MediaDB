/* Database.cs
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
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace MediaDB
{
	/// <summary>
	/// Database helper class
	/// </summary>
	class DB
	{
		/// <summary>
		/// Creates a MySqlParameter
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static MySqlParameter Param(string name, object value)
		{
			if (!name.StartsWith("@"))
				name = "@" + name;
			return new MySqlParameter("@" + name, value);
		}

		/// <summary>
		/// Disposes a MySqlDataReader object
		/// </summary>
		/// <param name="r"></param>
		public static void EndReader(ref MySqlDataReader r)
		{
			if (r != null) {
				if (!r.IsClosed)
					r.Close();

				r.Dispose();
				r = null;
			}
		}

		/// <summary>
		/// Performs a database query
		/// </summary>
		/// <param name="rd"></param>
		/// <param name="sql"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static bool QueryReader(out MySqlDataReader rd,
		                               string sql,
		                               params MySqlParameter[] args)
		{
			try {
				/*
				using (MySqlConnection mycon = Manager.DbCon.Clone()) {
					mycon.Open();
					using (MySqlCommand cmd = mycon.CreateCommand()) {
						cmd.CommandText = sql;

						if (args.Length > 0)
							foreach (object o in args)
								cmd.Parameters.Add(o);

						rd = cmd.ExecuteReader();
						rd.
					}
					mycon.Close();
				}
				*/

				Manager.mutex.WaitOne();

				MySqlCommand cmd = Manager.DbCon.CreateCommand();
				cmd.CommandText = sql;

				if (args.Length > 0)
					foreach (object o in args)
						cmd.Parameters.Add(o);

				rd = cmd.ExecuteReader();
				cmd.Dispose();
				cmd = null;

				Manager.mutex.ReleaseMutex();

				return true;
			}
			catch (Exception e) {
				Log.Debug("DB error: {0} {1}\n", e.Message, e.StackTrace);
			}

			rd = null;

			return false;
		}

		/// <summary>
		/// Query database with insert statement.
		/// </summary>
		/// <param name="id">Will be populated with the insert ID.</param>
		/// <param name="sql">The SQL query</param>
		/// <param name="args">Arbitrary replacement parameters for 
		///  <paramref name="sql"/>
		/// </param>
		/// <returns><see cref="System.Boolean"/></returns>
		public static bool QueryInsert(out long id, string sql,
		                               params MySqlParameter[] args)
		{
			try {
				using (MySqlConnection mycon = Manager.DbCon.Clone()) {
					mycon.Open();
					using (MySqlCommand cmd = mycon.CreateCommand()) {
						cmd.CommandText = sql;

						if (args.Length > 0)
							foreach (MySqlParameter p in args)
								cmd.Parameters.Add(p);

						cmd.ExecuteNonQuery();
						id = cmd.LastInsertedId;
					}
					mycon.Close();
				}
			}
			catch (Exception e) {
				Log.Warning("DB error: {0} {1}\n", e.Message, e.StackTrace);
				id = 0;
				return false;
			}

			return true;
		}

		/// <summary>
		/// Plain "NonQuery" query. No query result is expected.
		/// </summary>
		/// <param name="sql"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static bool Query(string sql, params MySqlParameter[] args)
		{
			try {
				using (MySqlConnection mycon = Manager.DbCon.Clone()) {
					mycon.Open();
					using (MySqlCommand cmd = mycon.CreateCommand()) {
						cmd.CommandText = sql;

						if (args.Length > 0)
							foreach (MySqlParameter p in args)
								cmd.Parameters.Add(p);

						cmd.ExecuteNonQuery();
					}
					mycon.Close();
				}
			}
			catch (Exception e) {
				Log.Warning("DB error: {0} {1}\n", e.Message, e.StackTrace);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Checks if db column <paramref name="col"/> is null
		/// </summary>
		/// <param name="col">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static bool IsNull(object col)
		{
			return col == DBNull.Value;
		}
	}

	/// <summary>
	///  <para>Database manager object.</para>
	///  <para>This will clone the <see cref="Manager.DbCon">database connection</see>
	///   in <see cref="Manager" />.
	///  </para>
	/// </summary>
	public class DbManager : IDisposable
	{
		/// <summary>
		/// Database connection object. This is a clone of
		/// <see cref="Manager.DbCon" />.
		/// </summary>
		public MySqlConnection Connection { get; private set; }

		/// <summary>
		/// Sql data reader object
		/// </summary>
		public MySqlDataReader DataReader { get; private set; }

		/// <summary>
		/// Creates a new DbManager
		/// </summary>
		public DbManager()
		{
			Connection = Manager.DbCon.Clone();
			Connection.Open();
		}

		private bool dontClose = false;

		/// <summary>
		/// Creates a new DbManager and sets the connection object to
		/// the one given as <paramref name="dbcon"/>. NOTE! This object
		/// should be an open connection
		/// </summary>
		public DbManager(MySqlConnection dbcon)
		{
			Connection = dbcon;
			dontClose = true;
		}

		/// <summary>
		/// Performs a database query
		/// </summary>
		/// <param name="sql"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool QueryReader(string sql, params MySqlParameter[] args)
		{
			try {
				if (DataReader != null) {
					if (!DataReader.IsClosed)
						DataReader.Close();
					DataReader.Dispose();
				}

				using (MySqlCommand cmd = Connection.CreateCommand()) {
					cmd.CommandText = sql;

					if (args.Length > 0)
						foreach (object o in args)
							cmd.Parameters.Add(o);

					DataReader = cmd.ExecuteReader();
				}

				return true;
			}
			catch (Exception e) {
				Log.Debug("DB error: {0} {1}\n", e.Message, e.StackTrace);
			}

			return false;
		}

		/// <summary>
		/// Plain "NonQuery" query. No query result is expected. Query.
		/// </summary>
		/// <param name="sql"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool Query(string sql, params MySqlParameter[] args)
		{
			try {
				using (MySqlCommand cmd = Connection.CreateCommand()) {
					cmd.CommandText = sql;

					if (args.Length > 0)
						foreach (MySqlParameter p in args)
							cmd.Parameters.Add(p);

					cmd.ExecuteNonQuery();
				}
			}
			catch (Exception e) {
				Log.Warning("DB error: {0} {1}\n", e.Message, e.StackTrace);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Query database with insert statement.
		/// </summary>
		/// <param name="id">Will be populated with the insert ID.</param>
		/// <param name="sql">The SQL query</param>
		/// <param name="args">Arbitrary replacement parameters for 
		///  <paramref name="sql"/>
		/// </param>
		/// <returns><see cref="System.Boolean"/></returns>
		public bool QueryInsert(out long id, string sql,
		                        params MySqlParameter[] args)
		{
			try {
				using (MySqlCommand cmd = Connection.CreateCommand()) {
					cmd.CommandText = sql;

					if (args.Length > 0)
						foreach (MySqlParameter p in args)
							cmd.Parameters.Add(p);

					cmd.ExecuteNonQuery();
					id = cmd.LastInsertedId;
				}
			}
			catch (Exception e) {
				Log.Warning("DB error: {0} {1}\n", e.Message, e.StackTrace);
				id = 0;
				return false;
			}

			return true;
		}

		/// <summary>
		/// Disposes the object.
		/// </summary>
		public void Dispose()
		{
			if (DataReader != null) {
				if (!DataReader.IsClosed)
					DataReader.Close();
				DataReader.Dispose();
				DataReader = null;
			}

			if (!dontClose && Connection != null) {
				Connection.Close();
				Connection.Dispose();
				Connection = null;
			}
		}
	}
}
