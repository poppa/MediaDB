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
				MySqlCommand cmd = Manager.DbCon.CreateCommand();
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
				MySqlCommand cmd = Manager.DbCon.CreateCommand();
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
		/// Plain "NonQuery" query. No query result is expected.
		/// </summary>
		/// <param name="sql"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static bool Query(string sql, params MySqlParameter[] args)
		{
			try {
				MySqlConnection mycon = Manager.DbCon.Clone();
				mycon.Open();
				MySqlCommand cmd = mycon.CreateCommand();
				cmd.CommandText = sql;

				if (args.Length > 0)
					foreach (MySqlParameter p in args)
						cmd.Parameters.Add(p);

				cmd.ExecuteNonQuery();
				cmd.Dispose();
				cmd = null;
				mycon.Close();
				mycon.Dispose();
				mycon = null;
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
}
