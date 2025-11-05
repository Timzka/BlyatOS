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

            // === Berechne Zentrierung ===
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

                // === Rauch über Zigarette ===
                int smokeY = yCigarette - 1;
                if (smokeY >= 0)
                {
                    int smokeStartX = startX + filterLength + len + 4;
                    int smokeWidth = 10;

                    // Hintergrund löschen
                    canvas.DrawFilledRectangle(
                        new Pen(DisplaySettings.BackgroundColor),
                        smokeStartX * font.Width,
                        smokeY * font.Height,
                        smokeWidth * font.Width,
                        font.Height
                    );

                    // Rauchmuster zeichnen
                    canvas.DrawString(
                        smokePatterns[len % smokePatterns.Length],
                        font,
                        new Pen(Color.Gray),
                        smokeStartX * font.Width,
                        smokeY * font.Height
                    );
                }

                int px = startX * font.Width;
                int py = yCigarette * font.Height;

                // Linie löschen
                canvas.DrawFilledRectangle(
                    new Pen(DisplaySettings.BackgroundColor),
                    px, py,
                    cigarettePixelWidth,
                    font.Height
                );

                // Filter
                for (int i = 0; i < filterLength; i++)
                    canvas.DrawFilledRectangle(new Pen(Color.DarkGoldenrod), (startX + i) * font.Width, py, font.Width, font.Height);

                // Körper
                for (int i = 0; i < len; i++)
                    canvas.DrawFilledRectangle(new Pen(Color.White), (startX + filterLength + i) * font.Width, py, font.Width, font.Height);

                // Glühende Spitze
                if (len > 0)
                {
                    Color tipColor = len % 3 == 0 ? Color.DarkGoldenrod :
                                     len % 3 == 1 ? Color.Red : Color.Yellow;

                    canvas.DrawFilledRectangle(new Pen(tipColor), (startX + filterLength + len) * font.Width, py, font.Width, font.Height);
                }

                // Rauchspur
                string trail = "~~~~      ";
                canvas.DrawString(trail, font, new Pen(Color.Gray), (startX + filterLength + len + 1) * font.Width, py);

                canvas.Display();
                Global.PIT.Wait(randTime);
            }

            canvas.Clear(DisplaySettings.BackgroundColor);
            canvas.Display();
        }
    }
}
