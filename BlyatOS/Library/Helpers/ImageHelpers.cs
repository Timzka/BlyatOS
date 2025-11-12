using Cosmos.System.Graphics;
using System;
using System.IO;
using Sys = Cosmos.System;

namespace BlyatOS;
public static class ImageHelpers
{
    public static Bitmap LoadBMP(string path)
    {
        Bitmap bmp = new Bitmap(path);

        // BMP ist im Format BGRA, Cosmos erwartet ARGB
        for (int i = 0; i < bmp.RawData.Length; i++)
        {
            int color = bmp.RawData[i];

            byte b = (byte)((color >> 24) & 0xFF);  // Blau war ganz oben
            byte g = (byte)((color >> 16) & 0xFF);  // Grün
            byte r = (byte)((color >> 8) & 0xFF);   // Rot
            byte a = (byte)(color & 0xFF);          // Alpha ganz unten

            // Umwandeln zu ARGB (Alpha, Rot, Grün, Blau)
            bmp.RawData[i] = (a << 24) | (r << 16) | (g << 8) | b;
        }

        return bmp;
    }
}
