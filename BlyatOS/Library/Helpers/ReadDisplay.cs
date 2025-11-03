using System;
using System.Drawing;
using System.IO;
using System.Text;
using Cosmos.HAL;
using Cosmos.System.FileSystem.VFS;
using Cosmos.System.Graphics;

namespace BlyatOS.Library.Helpers
{
    public static class ReadDisplay
    {
        /// <summary>
        /// Displays a bitmap file on a full-screen canvas
        /// </summary>
        /// <param name="path">Path to the bitmap file</param>
        /// <param name="displayTimeMs">Time in milliseconds to display the image (default: 5000ms)</param>
        public static void DisplayBitmap(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            if (!path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("File must be a .bmp file", nameof(path));

            try
            {
                // Load and prepare the bitmap
                Bitmap bmp = ImageHelpers.LoadBMP(path);
                
                // Get the existing canvas from DisplaySettings
                var canvas = Configs.DisplaySettings.Canvas;
                var screenWidth = Configs.DisplaySettings.ScreenWidth;
                var screenHeight = Configs.DisplaySettings.ScreenHeight;
                
                // Calculate center position for the image
                int x = (screenWidth - (int)bmp.Width) / 2;
                int y = (screenHeight - (int)bmp.Height) / 2;
                
                // Ensure the position is not negative (in case image is larger than canvas)
                x = Math.Max(0, x);
                y = Math.Max(0, y);
                
                // Clear the screen and draw the image
                canvas.Clear(Color.Black);
                canvas.DrawImage(bmp, x, y);
                canvas.Display();
                
                // Wait for a key press to continue
                while (!Cosmos.System.KeyboardManager.KeyAvailable)
                {
                    // Small delay to prevent high CPU usage
                    Cosmos.HAL.Global.PIT.Wait(10);
                }
                
                // Clear the key that was pressed
                Cosmos.System.KeyboardManager.ReadKey();
                
                // Clear the screen and restore the console
                canvas.Clear(Configs.DisplaySettings.BackgroundColor);
                canvas.Display();
            }
            catch (Exception ex)
            {
                ConsoleHelpers.WriteLine($"Error displaying image: {ex.Message}", Color.Red);
                throw;
            }
        }

        /// <summary>
        /// Reads the contents of a text file and returns it as a string
        /// </summary>
        /// <param name="filePath">Path to the text file</param>
        /// <returns>Contents of the file as a string</returns>
        public static string ReadTextFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var file = VFSManager.GetFile(filePath);
            if (file == null)
                throw new FileNotFoundException($"File not found: {filePath}");

            using var stream = file.GetFileStream();
            if (!stream.CanRead)
                throw new UnauthorizedAccessException($"Cannot read from file: {filePath}");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Reads a text file in chunks and processes each chunk with the provided action
        /// </summary>
        /// <param name="filePath">Path to the text file</param>
        /// <param name="chunkSize">Size of each chunk in bytes (default: 8192)</param>
        /// <param name="processChunk">Action to process each chunk of text</param>
        public static void ReadTextFileInChunks(string filePath, int chunkSize = 8192, Action<string> processChunk = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var file = VFSManager.GetFile(filePath);
            if (file == null)
                throw new FileNotFoundException($"File not found: {filePath}");

            using var stream = file.GetFileStream();
            if (!stream.CanRead)
                throw new UnauthorizedAccessException($"Cannot read from file: {filePath}");

            using var reader = new StreamReader(stream);
            char[] buffer = new char[chunkSize];
            int bytesRead;

            while ((bytesRead = reader.ReadBlock(buffer, 0, buffer.Length)) > 0)
            {
                string chunk = new string(buffer, 0, bytesRead);
                if (processChunk != null)
                {
                    processChunk(chunk);
                }
                else
                {
                    ConsoleHelpers.Write(chunk);
                }
            }
        }
    }
}
