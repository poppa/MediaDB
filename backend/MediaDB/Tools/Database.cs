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
