/* MediaFile.cs
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

namespace MediaDB
{
	/// <summary>
	/// Media file
	/// </summary>
	public class MediaFile
	{
		/// <summary>
		/// Database id of the file
		/// </summary>
		public int Id = 0;

		/// <summary>
		/// Filename
		/// </summary>
		public string Name = null;

		/// <summary>
		/// Full path
		/// </summary>
		public string FullName = null;

		/// <summary>
		/// File title
		/// </summary>
		public string Title = null;

		/// <summary>
		/// File description
		/// </summary>
		public string Description = null;

		/// <summary>
		/// Copyright info
		/// </summary>
		public string Copyright = null;

		/// <summary>
		/// File width
		/// </summary>
		public int Width = 0;

		/// <summary>
		/// File height
		/// </summary>
		public int Height = 0;

		/// <summary>
		/// File size
		/// </summary>
		public long Size = 0;

		/// <summary>
		/// Image resolution
		/// </summary>
		public double Resolution = 0;

		/// <summary>
		/// File creation time
		/// </summary>
		public DateTime Created = new DateTime(1970, 1, 1, 0, 0, 0, 0);
	}
}
