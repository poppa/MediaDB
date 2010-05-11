/* Handler.cs
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

namespace MediaDB
{
	/// <summary>
	/// Base handler class
	/// </summary>
	public class FileHandler
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

    //private static Mutex mtx = new Mutex();

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
		/// Process the file. Collects various info about the file.
		/// Override this in subclasses to extract file specific info.
		/// </summary>
		public void Process()
		{
      if ((MediaFile = Settings.GetMediaFile(File.FullName)) == null) {
        MediaFile = new MediaFile();
        MediaFile.Name = File.Name;
        MediaFile.FullName = File.FullName;
        MediaFile.Size = File.Length;
        MediaFile.Created = File.CreationTime;
        MediaFile.Mimetype = MediaType.Mimetype;
      }
		}

    /// <summary>
    /// Generate prieviews
    /// </summary>
    /// <param name="img"></param>
    protected void GeneratePreviews(Bitmap img)
    {
			if (MediaFile.Id > 0)
				return;

      int x = MediaFile.Width;
      int y = MediaFile.Height;

      if (x > Settings.PreviewMinWidth || y > Settings.PreviewMinHeight) {
        Log.Debug("### Generate previews for {0}\n", MediaFile.FullName);

        foreach (Preview p in Settings.Previews) {
          int[] c = Backend.Gfx.GetConstraints(x, y, (int)p.Width,
					                                     (int)p.Height);
          Bitmap n = Backend.Gfx.ScaleImage(img, c[0], c[1]);
          ImageFormat fmt;
					string mime;
          GetPreviewFormat(out fmt, out mime);

					MemoryStream s = new MemoryStream();
					n.Save(s, fmt);

					PreviewFile pf = new PreviewFile();
					pf.Width = n.Width;
					pf.Height = n.Height;
					pf.Size = s.Length;
					pf.Mimetype = mime;
					pf.Name = p.Name;

					byte[] buf = new byte[s.Length];
					s.Read(buf, 0, buf.Length);
					pf.Data = System.Text.Encoding.Default.GetString(buf);

					MediaFile.Previews.Add(pf);

					s.Close();
					s.Dispose();
					s = null;

					buf = null;

          n.Dispose();
          n = null;
        }
      }
      else {
        Log.Debug("### Copy org to preview for {0}\n", MediaFile.FullName);

        Bitmap n = Backend.Gfx.ScaleImage(img, img.Width, img.Height);
        ImageFormat fmt;
				string mime;
        GetPreviewFormat(out fmt, out mime);

				Log.Debug("SKIPPING SMALL IMAGE FOR NOW\n");

        //n.Save(Tools.BuildPath(Settings.TmpDir, tname), fmt);
        n.Dispose();
        n = null;
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

      if (MediaType.Mimetype == "image/png") {
        fmt = ImageFormat.Png;
				mimetype = "image/png";
      }
      else if (MediaType.Mimetype == "image/gif") {
        fmt = ImageFormat.Gif;
				mimetype = "image/gif";
      }
    }

    /// <summary>
    /// Save the media file to database
    /// </summary>
    protected void SaveFile()
    {
      Settings.mutex.WaitOne();
      MediaFile.Save();
      Settings.mutex.ReleaseMutex();
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
          Log.Warning("Unable to extract EXIF from {0}: {1}\n", 
                      File.FullName, e.Message);
        }

        GeneratePreviews(bmp);

				bmp.Dispose();
				bmp = null;

        SaveFile();
			}
			catch (Exception e) {
				Log.Warning("Unable to handle file ({0}): {1} {2}\n", 
                    File.FullName, e.Message, e.StackTrace);
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
      SaveFile();
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
      SaveFile();
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
		/// Collects metadata about the file
		/// </summary>
		public new void Process()
		{
			base.Process();
      SaveFile();
		}
	}
}
