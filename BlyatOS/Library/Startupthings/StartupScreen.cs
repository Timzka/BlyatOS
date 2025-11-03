using System;
using System.Drawing;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using BlyatOS.Library.Configs;
using Sys = Cosmos.System;

namespace BlyatOS.Library.Startupthings;

public class StartupScreen
{
    private static readonly string LogoPath = @"0:\blyatos\blyatlogo.bmp";
    private static readonly string PromptText = "Press any key to Login";
    private static readonly string PlaceholderText = "[ blyatlogo.bmp missing ]";

    public static void Show()
    {
        // Clear the screen with background color
        var canvas = DisplaySettings.Canvas;
        canvas.Clear(DisplaySettings.BackgroundColor);

        bool logoDisplayed = false;
        int logoBottomY = 0;
        var font = DisplaySettings.Font;
        var pen = DisplaySettings.GetForegroundPen();

        // Try to load and display the logo
        if (Sys.FileSystem.VFS.VFSManager.FileExists(LogoPath))
        {
            try
            {
                var logo = ImageHelpers.LoadBMP(LogoPath);

                // Center the logo
                int x = (DisplaySettings.ScreenWidth - (int)logo.Width) / 2;
                int y = (DisplaySettings.ScreenHeight - (int)logo.Height) / 3;

                canvas.DrawImage(logo, x, y);
                logoDisplayed = true;
                logoBottomY = y + (int)logo.Height;
            }
            catch
            {
                logoDisplayed = false;
            }
        }

        // Show placeholder text if no logo is displayed
        if (!logoDisplayed)
        {
            string missingText = PlaceholderText;
            int textWidth = missingText.Length * font.Width;
            int textHeight = font.Height;
            int x = (DisplaySettings.ScreenWidth - textWidth) / 2;
            int y = (DisplaySettings.ScreenHeight / 3) - (textHeight / 2);
            canvas.DrawString(missingText, font, pen, x, y);
            logoBottomY = y + textHeight;
        }

        // Show prompt text below logo or placeholder
        {
            int promptWidth = PromptText.Length * font.Width;
            int x = (DisplaySettings.ScreenWidth - promptWidth) / 2;
            int y = logoBottomY + 40; // Add some space below the logo/placeholder
            canvas.DrawString(PromptText, font, pen, x, y);
        }

        // Update the display
        canvas.Display();
        Console.ReadKey();

        // Clear the screen
        canvas.Clear(DisplaySettings.BackgroundColor);
        //canvas.Disable();
    }
}
