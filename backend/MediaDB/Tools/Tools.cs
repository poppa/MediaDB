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
 *  Martin Pedersen
 */

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml;

namespace MediaDB
{
	/// <summary>
	/// Variuos helper methods
	/// </summary>
	public static class Tools
	{
#if LINUX
		/// <summary>
		/// Directory separator character
		/// </summary>
		public const char DIR_SEPARATOR = '/';

		/// <summary>
		/// Directory separator string
		/// </summary>
		public const string DIR_SEPARATOR_S = "/";
#else
		/// <summary>
		/// Directory separator character
		/// </summary>
		public const char DIR_SEPARATOR = '\\';

		/// <summary>
		/// Directory separator string
		/// </summary>
		public const string DIR_SEPARATOR_S = "\\";
#endif

//		public static Encoding IsoEncoder = Encoding.GetEncoding("iso-8859-1");

		/// <summary>
		/// Checks if path is a directory
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool IsDir(string path)
		{
			return new DirectoryInfo(path).Exists;
		}

		/// <summary>
		/// Checks if path exists
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool FileExists(string path)
		{
			return new FileInfo(path).Exists;
		}

		/// <summary>
		/// Checks if directory path exists
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool DirectoryExists(string path)
		{
			return new DirectoryInfo(path).Exists;
		}

		//! TODO: This is ridicuosly slow on large files! Might need some other
		//! way to solve file hashing.
		/// <summary>
		/// Compute a hash of the file content.
		/// </summary>
		/// <param name="file">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static string ComputeFileHash(string file)
		{
			if (!FileExists(file))
				throw new FileNotFoundException("Can't compute hash", file);

			using (SHA1 sha1 = new SHA1CryptoServiceProvider()) {
				using (FileStream fs = new FileStream(file, FileMode.Open,
																							FileAccess.Read))
				{
					return BitConverter.ToString(sha1.ComputeHash(fs))
					                   .Replace("-", "");
				}
			}
		}

		/// <summary>
		/// Generates a temp name
		/// </summary>
		/// <returns></returns>
		public static string Tmpnam()
		{
			return "tmp-" + Guid.NewGuid().ToString();
		}

		/// <summary>
		/// Creates a full temporary file path
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static string TmpPath()
		{
			return BuildPath(Manager.TmpDir, Tmpnam());
		}

		/// <summary>
		/// Build a path from arbitrary number of arguments
		/// </summary>
		/// <param name="parts"></param>
		/// <returns></returns>
		public static string BuildPath(params string[] parts)
		{
			return String.Join(DIR_SEPARATOR_S, parts);
		}

		/// <summary>
		/// ISO-8859-1 encode string <paramref name="s"/>
		/// </summary>
		/// <param name="s">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static string IsoEncode(string s)
		{
			try {
				return Encoding.ASCII.GetString(Encoding.UTF8.GetBytes(s));
			}
			catch (Exception e) {
				Log.Debug("UTF8-TO-ISO failed: {0}\n", e.Message);
			}

			return s;
		}

		/// <summary>
		/// UTF8 encode string <paramref name="s"/>
		/// </summary>
		/// <param name="s">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static string UTF8Encode(string s)
		{
			try {
				Encoding enc = Encoding.GetEncoding(0);
				return enc.GetString(Encoding.ASCII.GetBytes(s));
			}
			catch (Exception e) {
				Log.Debug("UTF8 conversion failed: {0}\n", e.Message);
			}

			return s;
		}

		/// <summary>
		/// String representation of <paramref name="bytes"/>
		/// </summary>
		/// <param name="bytes">
		/// A <see cref="System.Int64"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static string FormatBytes(long bytes)
		{
		  const int scale = 1024;
		  string[] orders = { "GB", "MB", "KB", "Bytes" };
		  long max = (long)Math.Pow(scale, orders.Length - 1);

		  foreach (string order in orders) {
		    if (bytes > max) {
		      return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max),
					                     order);
				}
		
		    max /= scale;
		  }
		  return "0 Bytes";
		}

		/// <summary>
		/// Exit application
		/// </summary>
		public static void Exit()
		{
			Exit(0);
		}

		/// <summary>
		/// Exit application with code
		/// </summary>
		/// <param name="code"></param>
		public static void Exit(int code)
		{
#if DEBUG
			Console.WriteLine("\n------------------");
			Console.WriteLine("Hit enter to quit:");
			Console.ReadLine();
#endif
			Environment.Exit(code);
		}
	}

	/// <summary>
	/// Various XML helpers
	/// </summary>
	public static class XML
	{
		/// <summary>
		///   Find node with local name <paramref name="name"/> in
		///   <paramref name="node"/> .
		/// </summary>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="node">
		/// A <see cref="XmlNode"/>
		/// </param>
		/// <returns>
		/// A <see cref="XmlNode"/>
		/// </returns>
		public static XmlNode FindNode(string name, XmlNode node)
		{
			foreach (XmlNode child in node) {
				if (child.NodeType != XmlNodeType.Element)
					continue;

				//Log.Debug("$$$ SEARCH: {0}\n", child.Name);
				if (child.LocalName == name)
					return child;

				if (child.FirstChild != null && child.FirstChild.HasChildNodes)
					return FindNode(name, child);
			}

			return null;
		}
	}

	/// <summary>
	/// A static class for logging
	/// </summary>
	public static class Log
	{
		/// <summary>
		/// Directory path where to store the log
		/// </summary>
		public static string Path = Environment.GetEnvironmentVariable("TEMP");

		/// <summary>
		/// Name of the log file
		/// </summary>
		public static string FileName = "my.log";

		private static string logfile = null;

		/// <summary>
		/// Full path to logfile
		/// </summary>
		public static string LogFile {
			get {
				if (logfile == null)
					return Tools.BuildPath(Path, FileName);

				return logfile;
			}

			set {
				logfile = value;
			}
		}

		/// <summary>
		/// Write to stderr
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="rest"></param>
		public static void Werror(string msg, params object[] rest)
		{
			Console.Error.Write(msg, rest);
		}

		/// <summary>
		///   <para>Write the log message to the console</para>
		///   <para>Behaves like <see cref="System.Console.WriteLine()" /></para>
		/// </summary>
		/// <param name="format"><see cref="System.String.Format(string,object)" /></param>
		/// <param name="rest">Arbitrary number of replacements for format</param>
		public static void Debug(string format, params object[] rest)
		{
#if DEBUG
			if (rest.Length > 0)
				format = String.Format(format, rest);

			Console.Write(format);
#endif
		}

		/// <summary>
		/// Write a warning message to stdout
		/// </summary>
		/// <param name="format"></param>
		/// <param name="rest"></param>
		public static void Warning(string format, params object[] rest)
		{
			if (rest.Length > 0)
				format = String.Format(format, rest);

			Console.Write("[warning] {0}", format);
		}

		/// <summary>
		/// Write a notice message to stdout
		/// </summary>
		/// <param name="format"></param>
		/// <param name="rest"></param>
		public static void Notice(string format, params object[] rest)
		{
			if (rest.Length > 0)
				format = String.Format(format, rest);

			Console.Write("[notice] {0}", format);
		}

		/// <summary>
		/// Write to file
		/// </summary>
		/// <param name="format"></param>
		/// <param name="rest"></param>
		public static void File(string format, params object[] rest)
		{
			try {
				if (rest.Length > 0) format = String.Format(format, rest);
				StreamWriter s = new System.IO.StreamWriter(LogFile, true);
				s.Write(format);
				s.Close();
				s.Dispose();
			}
			catch (Exception e) {
				Log.Werror(e.Message);
			}
		}
	}
}
