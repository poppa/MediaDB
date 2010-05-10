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
			MediaFile = new MediaFile();
		}

		/// <summary>
		/// Process the file. Collects various info about the file.
		/// Override this in subclasses to extract file specific info.
		/// </summary>
		public void Process()
		{
			MediaFile.Name = File.Name;
			MediaFile.FullName = File.FullName;
			MediaFile.Size = File.Length;
			MediaFile.Created = File.CreationTime;
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

				bmp.Dispose();
				bmp = null;
			}
			catch (Exception e) {
				Log.Warning("Unable to handle file: {0}\n", e.Message);
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
		}
	}
}
