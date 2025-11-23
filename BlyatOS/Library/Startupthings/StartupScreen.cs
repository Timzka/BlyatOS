using System;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Ressources;
using Sys = Cosmos.System;

namespace BlyatOS.Library.Startupthings
{
    public class StartupScreen
    {
        private static readonly string PromptText = "Press any key to Login";
        private static readonly string PlaceholderText = "[ blyatlogo.bmp missing ]";

        public static void Show()
        {
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;

            // Bildschirm löschen
            canvas.Clear(DisplaySettings.BackgroundColor);

            bool logoDisplayed = false;
            uint logoBottomY = 0;

                try
                {
                    var logo = BMP.BlyatLogo;

                    int x = (int)((DisplaySettings.ScreenWidth - logo.Width) / 2);
                    int y = (int)((DisplaySettings.ScreenHeight - logo.Height) / 3);

                    canvas.DrawImage(logo, x, y);
                    logoDisplayed = true;
                    logoBottomY = (uint)(y + logo.Height);
                }
                catch
                {
                    logoDisplayed = false;
                }

            // === Falls kein Logo vorhanden, Placeholder-Text anzeigen ===
            if (!logoDisplayed)
            {
                string missingText = PlaceholderText;
                int textWidth = missingText.Length * font.Width;
                int textHeight = font.Height;

                int x = (int)((DisplaySettings.ScreenWidth - (uint)textWidth) / 2);
                int y = (int)((DisplaySettings.ScreenHeight / 3) - (uint)(textHeight / 2));

                canvas.DrawString(missingText, font, DisplaySettings.ForegroundColor, x, y);
                logoBottomY = (uint)(y + textHeight);
            }

            // === Prompt-Text unterhalb des Logos oder Platzhalters ===
            {
                int promptWidth = PromptText.Length * font.Width;
                int x = (int)((DisplaySettings.ScreenWidth - (uint)promptWidth) / 2);
                int y = (int)(logoBottomY + 40); // Abstand unterhalb des Logos/Texts

                canvas.DrawString(PromptText, font, DisplaySettings.ForegroundColor, x, y);
            }

            // === Anzeige aktualisieren ===
            canvas.Display();

            // Auf Tasteneingabe warten
            Console.ReadKey();

            // Bildschirm leeren
            canvas.Clear(DisplaySettings.BackgroundColor);
            canvas.Display();
        }
    }
}
