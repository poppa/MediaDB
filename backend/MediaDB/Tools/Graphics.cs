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
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Drawing;

namespace MediaDB.Backend
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
  }
}
