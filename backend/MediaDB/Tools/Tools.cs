/* Tools.cs
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

namespace MediaDB
{
	public class Tools
	{
		public static bool IsDir(string path)
		{
			return new DirectoryInfo(path).Exists;
		}

		public static bool FileExists(string path)
		{
			return new FileInfo(path).Exists;
		}

		public static bool DirectoryExists(string path)
		{
			return new DirectoryInfo(path).Exists;
		}
	}

	public static class Log
	{
		public static void Debug(string fmt, params object[] args)
		{
#if DEBUG
			Console.Write(fmt, args);
#endif
		}

		public static void Werror(string msg, params object[] rest)
		{
			Console.Error.Write(msg, rest);
		}

		public static void Warning(string fmt, params object[] args)
		{
			Console.Write("[warning] " + fmt, args);
		}

		public static void File(string fmt, params object[] args)
		{
			Console.Write(fmt, args);
		}
	}
}
