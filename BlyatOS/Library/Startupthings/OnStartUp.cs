using System;
using System.Drawing;
using Cosmos.HAL;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using BlyatOS.Library.Configs;

namespace BlyatOS.Library.Startupthings
{
    public class OnStartUp
    {
        public static void RunLoadingScreenThing()
        {
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;
            if (canvas == null || font == null) return;

            int maxLength = 37;
            int filterLength = 10;
            Random rand = new Random();

            string[] smokePatterns =
            {
                "  ~~~     ",
                "  ~~      ",
                " ~~~      ",
                "  ~~      ",
                "  ~       "
            };

            Pen bgPen = DisplaySettings.GetBackgroundPen();
            Pen grayPen = DisplaySettings.GetPen(Color.Gray);
            Pen darkGoldenrodPen = DisplaySettings.GetPen(Color.DarkGoldenrod);
            Pen whitePen = DisplaySettings.GetPen(Color.White);
            Pen redPen = DisplaySettings.GetPen(Color.Red);
            Pen yellowPen = DisplaySettings.GetPen(Color.Yellow);

            int totalCigaretteLength = filterLength + maxLength + 10; // +10 für Rauch/Reserve
            int cigarettePixelWidth = totalCigaretteLength * font.Width;
            int cigarettePixelHeight = font.Height;

            int screenWidth = DisplaySettings.ScreenWidth;
            int screenHeight = DisplaySettings.ScreenHeight;

            // Zentrierte Startpositionen in Pixeln
            int startX = (screenWidth - cigarettePixelWidth) / 2 / font.Width;
            int yCigarette = (screenHeight / 2 - cigarettePixelHeight / 2) / font.Height;

            canvas.Clear(DisplaySettings.BackgroundColor);

            for (int len = maxLength; len >= 0; len--)
            {
                int smokePos = filterLength + len + 2;
                uint randTime = (uint)(50 + rand.Next(0, 250));

                int smokeY = yCigarette - 1;
                if (smokeY >= 0)
                {
                    int smokeStartX = startX + filterLength + len + 4;
                    int smokeWidth = 10;

                    // Hintergrund löschen 
                    canvas.DrawFilledRectangle(
                        bgPen,
                        smokeStartX * font.Width,
                        smokeY * font.Height,
                        smokeWidth * font.Width,
                        font.Height
                    );

                    // Rauchmuster zeichnen 
                    canvas.DrawString(
                        smokePatterns[len % smokePatterns.Length],
                        font,
                        grayPen,
                        smokeStartX * font.Width,
                        smokeY * font.Height
                    );
                }

                int px = startX * font.Width;
                int py = yCigarette * font.Height;

                // Linie löschen 
                canvas.DrawFilledRectangle(
                    bgPen,
                    px, py,
                    cigarettePixelWidth,
                    font.Height
                );

                // Filter 
                for (int i = 0; i < filterLength; i++)
                    canvas.DrawFilledRectangle(darkGoldenrodPen, (startX + i) * font.Width, py, font.Width, font.Height);

                // Körper 
                for (int i = 0; i < len; i++)
                    canvas.DrawFilledRectangle(whitePen, (startX + filterLength + i) * font.Width, py, font.Width, font.Height);

                // Glühende Spitze
                if (len > 0)
                {
                    Pen tipPen = len % 3 == 0 ? darkGoldenrodPen :
                                 len % 3 == 1 ? redPen : yellowPen;

                    canvas.DrawFilledRectangle(tipPen, (startX + filterLength + len) * font.Width, py, font.Width, font.Height);
                }

                // Rauchspur 
                string trail = "~~~~      ";
                canvas.DrawString(trail, font, grayPen, (startX + filterLength + len + 1) * font.Width, py);

                canvas.Display();
                Global.PIT.Wait(randTime);
            }

            canvas.Clear(DisplaySettings.BackgroundColor);
            canvas.Display();
        }
    }
}