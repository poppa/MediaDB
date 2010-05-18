/* Graphics.cs
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
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml;

namespace MediaDB
{
	/// <summary>
	/// Various graphics related methods
	/// </summary>
	class Gfx
	{
		/// <summary>
		/// Scale with constraint proportions
		/// </summary>
		/// <param name="org_x"></param>
		/// <param name="org_y"></param>
		/// <param name="max_x"></param>
		/// <param name="max_y"></param>
		/// <returns></returns>
		public static int[] GetConstraints(int org_x, int org_y,
		                                   int max_x, int max_y)
		{
			int[] r = new int[2];
			float s = Math.Min((float)max_x / (float)org_x,
			                   (float)max_y / (float)org_y);
			r[0] = (int)Math.Round(s * org_x);
			r[1] = (int)Math.Round(s * org_y);

			return r;
		}

		/// <summary>
		/// Scale image to <paramref name="width"/> and <paramref name="height"/>
		/// </summary>
		/// <param name="img"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		public static Bitmap ScaleImage(Bitmap img, int width, int height)
		{
			Bitmap bmp = new Bitmap(img, width, height);
			bmp.SetResolution(72, 72);
			Graphics g = Graphics.FromImage(bmp);
			g.InterpolationMode =
			System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			g.DrawImage(img,
			            new Rectangle(0, 0, width, height),
			            new Rectangle(0, 0, img.Width, img.Height),
			            GraphicsUnit.Pixel);

			g.Dispose();
			g = null;
			return bmp;
		}

		/// <summary>
		/// Returns a <see cref="ImageCodecInfo"/> info for
		/// <paramref name="mimetype"/> that can be used for compression settings.
		/// </summary>
		/// <param name="mimetype"></param>
		/// <returns></returns>
		public static ImageCodecInfo GetEncoderInfo(string mimetype)
		{
			foreach (ImageCodecInfo ici in ImageCodecInfo.GetImageDecoders()) {
			if (ici.MimeType == mimetype)
				return ici;
			}

			return null;
		}

		/// <summary>
		/// Create a JPEG image of PDF file <paramref name="path"/>.
		/// </summary>
		/// <param name="path">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Byte"/> array
		/// </returns>
		public static byte[] Pdf2Jpeg(string path)
		{
			string tmp = Tools.TmpPath() + ".jpg";
			string args = String.Format("-quality 80 -units PixelsPerInch " +
			                            "-density 72 \"{0}[0]\" \"{1}\"",
			                            path, tmp);

			return iMagickConvert(args, tmp);
		}

		/// <summary>
		/// Convert eps to png.
		/// </summary>
		/// <param name="path">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Byte"/> array
		/// </returns>
		public static byte[] Eps2Png(string path)
		{
			string tmp = Tools.TmpPath() + ".png";
			string args = String.Format("-flatten -alpha Opaque -density 72 " +
			                            "-format PNG24 -define png:bit-depth=8 " +
			                            "-quality 9 \"{0}[0]\" \"{1}\"", path, tmp);

			return iMagickConvert(args, tmp);
		}

		/// <summary>
		/// Execute ImageMagicks "convert".
		/// </summary>
		/// <param name="args">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="outfile">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Byte"/> array
		/// </returns>
		private static byte[] iMagickConvert(string args, string outfile)
		{
			// TODO: Resolve the relative exe issue for Windows
			System.Diagnostics.Process proc;
			proc = new System.Diagnostics.Process();
			proc.EnableRaisingEvents = false;
#if LINUX
			proc.StartInfo.FileName = "convert";
#else
			proc.StartInfo.FileName = @"C:\Program Files\ImageMagick-6.6.1-Q16\convert.exe";
			//proc.StartInfo.FileName = "\"convert.exe\"";
#endif
			proc.StartInfo.Arguments = args;
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardError = true;
			proc.Start();

			string error = proc.StandardError.ReadToEnd();

			if (!proc.HasExited)
				proc.WaitForExit();

			if (proc.ExitCode == 0) {
				var fi = new FileInfo(outfile);
				var fs = new FileStream(outfile, FileMode.Open, FileAccess.Read);
				byte[] buf = new byte[fi.Length];
				fs.Read(buf, 0, buf.Length);
				fs.Close();
				fs.Dispose();
				fi.Delete();
				fi = null;

				proc.Close();
				proc.Dispose();
				proc = null;

				return buf;
			}
			else {
				Log.Warning("Unable to \"convert\": {0}\n### {1}\n", args, error);
			}

			proc.Close();
			proc.Dispose();
			proc = null;

			return null;
		}

		/// <summary>
		/// Retrieves metadata from an SVG file
		/// </summary>
		/// <param name="svg">
		/// A <see cref="XmlNode"/>
		/// </param>
		/// <returns>
		/// A <see cref="Hashtable"/>
		/// </returns>
		public static Hashtable SvgMetadata(XmlNode svg)
		{
			XmlNode md = null;
			foreach (XmlNode c in svg.ChildNodes) {
				if (c.Name == "metadata") {
					md = c;
					break;
				}
			}

			Hashtable ht = new Hashtable();

			if (md == null)
				return ht;

			XmlNode work = XML.FindNode("Work", md);

			if (work != null) {
				XmlNode n;

				string xpath =
					"*[local-name() = 'title' or local-name() = 'creator' or"  +
					"  local-name() = 'rights' or local-name() = 'subject' or" +
					"  local-name() = 'description']";

				XmlNodeList l = work.SelectNodes(xpath);

				foreach (XmlNode hit in l) {
					switch (hit.LocalName) {
						case "title":
							ht.Add("title", hit.FirstChild.Value);
							break;

						case "description":
							ht.Add("description", hit.FirstChild.Value);
							break;

						case "creator":
							if ((n = XML.FindNode("title", hit)) != null)
								ht.Add("author", n.FirstChild.Value);
							break;

						case "rights":
							if ((n = XML.FindNode("title", hit)) != null)
								ht.Add("copyright", n.FirstChild.Value);
							break;

						case "subject":
							ArrayList s = new ArrayList();
							if ((n = XML.FindNode("Bag", hit)) != null) {
								foreach (XmlNode li in n)
									if (li.LocalName == "li")
										s.Add(li.FirstChild.Value);

								ht.Add("keywords",
								       String.Join(",", (string[])s.ToArray(typeof(string))));
							}
							break;
					}
				}
			}

			return ht;
		}
	}
}