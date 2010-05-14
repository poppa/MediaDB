/* FileHandler.cs
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
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using Goheer.EXIF;
using System.Collections;
using System.Collections.Generic;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Xml;
using Svg;

namespace MediaDB.Backend
{
	/// <summary>
	/// Base handler class
	/// </summary>
	public class FileHandler : IDisposable
	{
		/// <summary>
		/// The real file object
		/// </summary>
		public FileInfo File { get; private set; }

		/// <summary>
		/// The media type of the file
		/// </summary>
		public MediaType MediaType { get; private set; }

		/// <summary>
		/// The media file object
		/// </summary>
		public MediaFile MediaFile { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="file">
		/// A <see cref="FileInfo"/>
		/// </param>
		/// <param name="mediatype">
		/// A <see cref="MediaType"/>
		/// </param>
		public FileHandler(FileInfo file, MediaType mediatype)
		{
			File = file;
			MediaType = mediatype;
		}

		/// <summary>
		/// Disposes this object
		/// </summary>
		public void Dispose()
		{
			File = null;
			MediaType = null;
			MediaFile.Dispose();
			MediaFile = null;
		}

		/// <summary>
		/// Process the file. Collects various info about the file.
		/// Override this in subclasses to extract file specific info.
		/// </summary>
		public void Process()
		{
			if ((MediaFile = Manager.GetMediaFile(File.FullName)) == null) {
				MediaFile = new MediaFile();
				MediaFile.Name = File.Name;
				MediaFile.FullName = File.FullName;
				MediaFile.Size = File.Length;
				MediaFile.Created = File.CreationTime;
				MediaFile.Modified = File.LastWriteTime;
				MediaFile.Mimetype = MediaType.Mimetype;
				Directory dir = Manager.GetDirectory(File.Directory.FullName);
				if (dir != null)
					MediaFile.DirectoryId = dir.Id;
			}
		}

		/// <summary>
		/// Generate prieviews
		/// </summary>
		/// <param name="img"></param>
		protected void GeneratePreviews(Bitmap img)
		{
			int x = MediaFile.Width;
			int y = MediaFile.Height;

			ImageFormat fmt;
			string mime;
			GetPreviewFormat(out fmt, out mime);
			EncoderParameters eparams = new EncoderParameters(1);
			eparams.Param[0] = new EncoderParameter(Encoder.Quality,
			                                        Manager.PreviewQuality);
			ImageCodecInfo ici = Gfx.GetEncoderInfo(mime);

			if (x > Manager.PreviewMinWidth || y > Manager.PreviewMinHeight) {
				//Log.Debug("### Generate previews for {0}\n", MediaFile.FullName);
				foreach (Preview p in Manager.Previews) {
					if (x < p.Width && y < p.Height)
						continue;

					PreviewFile pf = lowGenImg(ref img, p, fmt, mime, ici, eparams);
					MediaFile.Previews.Add(pf);
				}
			}
			else {
				Preview p = new Preview();
				p.Width = img.Width;
				p.Height = img.Height;
				p.Name = "default";

				PreviewFile pf = lowGenImg(ref img, p, fmt, mime, ici, eparams);
				MediaFile.Previews.Add(pf);
			}

			eparams.Dispose();
			eparams = null;
			ici = null;
			fmt = null;
		}

		bool orgIsScaled = false;

		/// <summary>
		/// Generates a preview image and populates a new
		/// <see cref="PreviewFile"/> object.
		/// </summary>
		/// <param name="img"></param>
		/// <param name="p"></param>
		/// <param name="fmt"></param>
		/// <param name="mime"></param>
		/// <param name="ici"></param>
		/// <param name="eparams"></param>
		/// <returns></returns>
		private PreviewFile lowGenImg(ref Bitmap img,
		                              Preview p,
		                              ImageFormat fmt,
		                              string mime,
		                              ImageCodecInfo ici,
		                              EncoderParameters eparams)
		{
			int[] c = Gfx.GetConstraints(MediaFile.Width, MediaFile.Height,
			                             p.Width, p.Height);

			using (Bitmap n = Gfx.ScaleImage(img, c[0], c[1])) {
				if (!orgIsScaled) {
					img = (Bitmap)n.Clone();
					orgIsScaled = true;
				}

				using (MemoryStream s = new MemoryStream()) {
					if (ici != null)
						n.Save(s, ici, eparams);
					else
						n.Save(s, fmt);

					PreviewFile pf = new PreviewFile();
					pf.Width = n.Width;
					pf.Height = n.Height;
					pf.Size = s.Length;
					pf.Mimetype = mime;
					pf.Name = p.Name;
					pf.Data = s.ToArray();

					s.Close();
					return pf;
				}
			}
		}

		/// <summary>
		/// Creates a name for a preview image.
		/// </summary>
		/// <param name="pv"></param>
		/// <param name="fmt"></param>
		/// <returns></returns>
		protected void GetPreviewFormat(out ImageFormat fmt, out string mimetype)
		{
			fmt = ImageFormat.Jpeg;
			mimetype = "image/jpeg";

			switch (MediaType.Mimetype) {
				case "image/gif":
				case "image/png":
				case "image/x-eps":
				case "image/svg+xml":
					fmt = ImageFormat.Png;
					mimetype = "image/png";
					break;
			}
		}

		/// <summary>
		/// Should we continue <see cref="Process"/> in derived classes.
		/// If the file isn't changed no further processing is neccessary!
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		protected bool ContiueProcessing()
		{
			// TODO: Add more logic...
			if (MediaFile.Id > 0) {
				/*
				if (MediaFile.Sha1Hash == Tools.ComputeFileHash(MediaFile.FullName))
					return false;
				*/
				if (MediaFile.Modified == File.LastWriteTime)
					return false;

				MediaFile.Modified = File.LastWriteTime;
			}

			return true;
		}

		/// <summary>
		/// Save the media file to database
		/// </summary>
		protected void SaveFile()
		{
			Manager.mutex.WaitOne();
			MediaFile.Save();
			Manager.mutex.ReleaseMutex();
		}
	}

	/// <summary>
	/// Handler for JPEG, TIFF and PNG
	/// </summary>
	public class IMGHandler : FileHandler
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="file">
		/// A <see cref="FileInfo"/>
		/// </param>
		/// <param name="mediatype">
		/// A <see cref="MediaType"/>
		/// </param>
		public IMGHandler(FileInfo file, MediaType mediatype)
			: base(file, mediatype) {}

		/// <summary>
		/// Collects metadata about the file
		/// </summary>
		public new void Process()
		{
			base.Process();

			if (ContiueProcessing()) {
				try {
					Bitmap bmp = new Bitmap(File.FullName);
					MediaFile.Width = bmp.Width;
					MediaFile.Height = bmp.Height;
					MediaFile.Resolution = bmp.HorizontalResolution;

					try {
						EXIFextractor exif = new EXIFextractor(ref bmp, "");
						object o;
						if ((o = exif["Image Description"]) != null)
							MediaFile.Description = Tools.IsoEncode(o.ToString());

						if ((o = exif["Copyright"]) != null)
							MediaFile.Copyright = Tools.IsoEncode(o.ToString());

						string sexif = exif.ToString().Trim();
						if (sexif.Length > 0)
							MediaFile.Exif = Tools.IsoEncode(sexif);
					}
					catch (Exception e) {
						Log.Notice("Unable to extract EXIF from {0}: {1}\n",
						           File.FullName, e.Message);
					}

					GeneratePreviews(bmp);

					bmp.Dispose();
					bmp = null;

					SaveFile();
				}
				catch (Exception e) {
					Log.Warning("Unable to handle file ({0}): {1}\n{2}\n",
					            File.FullName, e.Message, e.StackTrace);
				}
			}
		}
	}

	/// <summary>
	/// Handler for EPS files
	/// </summary>
	public class EPSHandler : FileHandler
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="file">
		/// A <see cref="FileInfo"/>
		/// </param>
		/// <param name="mediatype">
		/// A <see cref="MediaType"/>
		/// </param>
		public EPSHandler(FileInfo file, MediaType mediatype)
			: base(file, mediatype) {}

		/// <summary>
		/// Collects metadata about the file
		/// </summary>
		public new void Process()
		{
			base.Process();

			if (ContiueProcessing()) {
				try {
					byte[] b = Gfx.Eps2Png(MediaFile.FullName);
					if (b != null) {
						MemoryStream ms = new MemoryStream(b);
						Bitmap img = new Bitmap(ms);
						MediaFile.Width = img.Width;
						MediaFile.Height = img.Height;
						GeneratePreviews(img);

						ms.Close();
						ms.Dispose();
						ms = null;
						img.Dispose();
						img = null;
					}
				}
				catch (Exception e) {
					Log.Warning("{0}\n{1}\n", e.Message, e.StackTrace);
				}

				SaveFile();
			}
		}
	}

	/// <summary>
	/// Handler for PDF files
	/// </summary>
	public class PDFHandler : FileHandler
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="file">
		/// A <see cref="FileInfo"/>
		/// </param>
		/// <param name="mediatype">
		/// A <see cref="MediaType"/>
		/// </param>
		public PDFHandler(FileInfo file, MediaType mediatype)
			: base(file, mediatype) {}

		/// <summary>
		/// Collects metadata about the file
		/// </summary>
		public new void Process()
		{
			base.Process();

			if (ContiueProcessing()) {
				PdfReader rd = new PdfReader(MediaFile.FullName);
				if (rd.Info.ContainsKey("Title")) {
					string title = rd.Info["Title"].Trim();
					if (title.Length > 0)
						MediaFile.Title = title;
				}

				//r.NumberOfPages
				//r.PdfVersion;
				var p1 = rd.GetPageN(1);
				var rect = rd.GetPageSize(p1);

				MediaFile.Width = (int)rect.Width;
				MediaFile.Height = (int)rect.Height;

				try {
					byte[] b = Gfx.Pdf2Jpeg(MediaFile.FullName);
					if (b != null) {
						MemoryStream ms = new MemoryStream(b);
						Bitmap img = new Bitmap(ms);
						GeneratePreviews(img);
						ms.Close();
						ms.Dispose();
						ms = null;
						img.Dispose();
						img = null;
					}
				}
				catch (Exception e) {
					Log.Debug("Nonon: {0}\n{1}\n", e.Message, e.StackTrace);
				}

				rd.Close();
				rd = null;

				SaveFile();
			}
		}
	}

	/// <summary>
	/// Handler for SVG files
	/// </summary>
	public class SVGHandler : FileHandler
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="file">
		/// A <see cref="FileInfo"/>
		/// </param>
		/// <param name="mediatype">
		/// A <see cref="MediaType"/>
		/// </param>
		public SVGHandler(FileInfo file, MediaType mediatype)
			: base(file, mediatype) {}

		/// <summary>
		/// Collects metadata about the file and generates previews
		/// </summary>
		public new void Process()
		{
			base.Process();

			if (ContiueProcessing()) {
				try {
					XmlDocument xdoc = new XmlDocument();
					xdoc.Load(MediaFile.FullName);

					XmlNodeList list = xdoc.GetElementsByTagName("svg");

					if (list.Count == 0) {
						Log.Warning("\"{0}\" is not an SVG file!\n", MediaFile.FullName);
						return;
					}

					XmlNode svg = list[0];

					if (svg != null && svg.Name == "svg") {
						Hashtable meta = Gfx.SvgMetadata(svg);

						if (meta.ContainsKey("title"))
							MediaFile.Title = meta["title"].ToString();

						if (meta.ContainsKey("author"))
							MediaFile.Exif = "Artist: " + meta["author"].ToString();

						if (meta.ContainsKey("description"))
							MediaFile.Description = meta["description"].ToString();

						if (meta.ContainsKey("copyright"))
							MediaFile.Copyright = meta["copyright"].ToString();

						if (meta.ContainsKey("keywords"))
							MediaFile.Keywords = meta["keywords"].ToString();

						SvgDocument sdoc = SvgDocument.Open(MediaFile.FullName);
						MediaFile.Width  = Convert.ToInt32(sdoc.Width.Value);
						MediaFile.Height = Convert.ToInt32(sdoc.Height.Value);

						Bitmap img = sdoc.Draw();
						GeneratePreviews(img);

						meta = null;
						sdoc = null;
						img.Dispose();
						img = null;
					}

					list = null;
					svg = null;
					xdoc = null;
				}
				catch (Exception e) {
					Log.Debug("\"{0}\": {1}\n{2}\n",
					          MediaFile.FullName, e.Message, e.StackTrace);
				}

				SaveFile();
			}
		}
	}
}
