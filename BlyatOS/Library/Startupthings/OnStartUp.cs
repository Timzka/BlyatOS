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
            if (canvas == null || font == null)
                return;

            const int maxLength = 37;
            const int filterLength = 10;
            Random rand = new Random();

            string[] smokePatterns =
            {
                "  ~~~     ",
                "  ~~      ",
                " ~~~      ",
                "  ~~      ",
                "  ~       "
            };

            // === Zentrierung berechnen ===
            int totalCigaretteLength = filterLength + maxLength + 10; // +10 für Rauchreserve
            int cigarettePixelWidth = totalCigaretteLength * font.Width;
            int cigarettePixelHeight = font.Height;

            int screenWidth = (int)DisplaySettings.ScreenWidth;
            int screenHeight = (int)DisplaySettings.ScreenHeight;

            int startX = Math.Max(0, ((screenWidth - cigarettePixelWidth) / 2) / font.Width);
            int yCigarette = Math.Max(0, ((screenHeight / 2 - cigarettePixelHeight / 2) / font.Height));

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
                        DisplaySettings.BackgroundColor,
                        smokeStartX * font.Width,
                        smokeY * font.Height,
                        smokeWidth * font.Width,
                        font.Height
                    );

                    // Rauchmuster
                    canvas.DrawString(
                        smokePatterns[len % smokePatterns.Length],
                        font,
                        Color.Gray,
                        smokeStartX * font.Width,
                        smokeY * font.Height
                    );
                }

                int px = startX * font.Width;
                int py = yCigarette * font.Height;

                // Linie löschen
                canvas.DrawFilledRectangle(
                    DisplaySettings.BackgroundColor,
                    px,
                    py,
                    cigarettePixelWidth,
                    font.Height
                );

                // Filter zeichnen
                for (int i = 0; i < filterLength; i++)
                    canvas.DrawFilledRectangle(
                        Color.DarkGoldenrod,
                        (startX + i) * font.Width,
                        py,
                        font.Width,
                        font.Height
                    );

                // Zigarettenkörper
                for (int i = 0; i < len; i++)
                    canvas.DrawFilledRectangle(
                        Color.White,
                        (startX + filterLength + i) * font.Width,
                        py,
                        font.Width,
                        font.Height
                    );

                // Glühende Spitze
                if (len > 0)
                {
                    var colorindex = len % 3; 
                    Color tipColor = colorindex switch 
                    { 
                        0 => Color.DarkGoldenrod, 
                        1 => Color.Red, 
                        _ => Color.Yellow 
                    };

                    canvas.DrawFilledRectangle(
                        tipColor,
                        (startX + filterLength + len) * font.Width,
                        py,
                        font.Width,
                        font.Height
                    );
                }

                // Rauchspur
                string trail = "~~~~      ";
                canvas.DrawString(
                    trail,
                    font,
                    Color.Gray,
                    (startX + filterLength + len + 1) * font.Width,
                    py
                );

                canvas.Display();
                Global.PIT.Wait(randTime);
            }

            canvas.Clear(DisplaySettings.BackgroundColor);
            canvas.Display();
        }
    }
}
