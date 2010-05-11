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
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace MediaDB
{
	public class Tools
	{
#if LINUX
    public const char DIR_SEPARATOR = '/';
    public const string DIR_SEPARATOR_S = "/";
#else
    public const char DIR_SEPARATOR = '\\';
    public const string DIR_SEPARATOR_S = "\\";
#endif

		public static Encoding IsoEncoder = Encoding.GetEncoding("iso-8859-1");

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

    /// <summary>
    /// Build a path from arbitrary number of arguments
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static string BuildPath(params string[] parts)
    {
      return String.Join(DIR_SEPARATOR_S, parts);
    }

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
    /// 	<para>Write the log message to the console</para>
    /// 	<para>Behaves like <see cref="System.Console.WriteLine" /></para>
    /// </summary>
    /// <param name="format"><see cref="System.String.Format" /></param>
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

    private static System.IO.TextWriter tw;

    /// <summary>
    /// Write to file
    /// </summary>
    /// <param name="format"></param>
    /// <param name="rest"></param>
    public static void File(string format, params object[] rest)
    {
      try {
        if (rest.Length > 0) format = String.Format(format, rest);
        if (tw == null) tw = new System.IO.StreamWriter(LogFile, true);
        tw.Write(format);
      }
      catch (Exception e) {
        Log.Werror(e.Message);
      }
    }

    /// <summary>
    /// End the logger. Closes the file handler if it's been used
    /// </summary>
    public static void End()
    {
      if (tw != null) {
        tw.Close();
        tw.Dispose();
        tw = null;
      }
    }
	}
}
