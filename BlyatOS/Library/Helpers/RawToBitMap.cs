using System;
using Cosmos.System.Graphics;
using System.IO;
using Sys = Cosmos.System;
using Cosmos.HAL;
using Cosmos.Core;
using System.Diagnostics;

namespace BlyatOS.Library.Helpers
{
    internal class RawToBitMap
    {
        /// <summary>
        /// Loads a .raw image file and converts it to a Bitmap object.
        /// </summary>
        /// <param name="filePath">Path to the .raw file</param>
        /// <param name="width">Width of the image in pixels</param>
        /// <param name="height">Height of the image in pixels</param>
        /// <returns>A Bitmap object containing the loaded image</returns>
        /// <exception cref="FileNotFoundException">Thrown when the specified file doesn't exist</exception>
        /// <exception cref="ArgumentException">Thrown when width or height are less than or equal to 0</exception>
        public static Bitmap LoadRawImage(string filePath, int width, int height)
        {
            Bitmap bitmap = null;
            byte[] rawData = null;
            
                // Input validation
                if (string.IsNullOrEmpty(filePath))
                    throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                    
                if (width <= 0 || height <= 0)
                    throw new ArgumentException("Width and height must be greater than 0.");

                // Check cache first
                if (BitMaps.Bitmaps.TryGetValue(filePath, out Bitmap cachedBitmap))
                {
                    Console.WriteLine($"[Cache] Using cached bitmap for: {filePath}");
                    return cachedBitmap;
                }

                // Verify file exists
                if (!Sys.FileSystem.VFS.VFSManager.FileExists(filePath))
                    throw new FileNotFoundException($"[Error] File not found: {filePath}");

                // Read file with memory check
                using (var stream = Sys.FileSystem.VFS.VFSManager.GetFile(filePath).GetFileStream())
                {
                    long fileSize = stream.Length;
                    Console.WriteLine($"[IO] Reading {fileSize} bytes from {filePath}");
                    
                    // Check if we have enough memory (simplified check)
                    ulong freeMemory = Cosmos.Core.GCImplementation.GetAvailableRAM();
                    if (fileSize > (long)(freeMemory * 0.5)) // Don't use more than 50% of free memory
                    {
                        throw new OutOfMemoryException($"Not enough memory to load image. Required: {fileSize / 1024}KB, Available: {freeMemory / 1024 / 1024}MB");
                    }
                    
                    rawData = new byte[fileSize];
                    int bytesRead = stream.Read(rawData, 0, (int)fileSize);
                    Console.WriteLine($"[IO] Successfully read {bytesRead} bytes");
                }
                
                // Validate data size
                int expectedSize = width * height * 4; // 4 bytes per pixel (ARGB)
                if (rawData.Length < expectedSize)
                {
                    throw new InvalidDataException(
                        $"[Error] Raw data size ({rawData.Length} bytes) is smaller than expected " +
                        $"({expectedSize} bytes) for a {width}x{height} 32bpp image.");
                }

                // Create bitmap with error handling
                try
                {
                    bitmap = new Bitmap((uint)width, (uint)height, ColorDepth.ColorDepth32);
                    Console.WriteLine($"[Bitmap] Created {width}x{height} bitmap");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create bitmap: {ex.Message}", ex);
                }

                // Process pixels in chunks to avoid stack overflow
                int pixelsProcessed = 0;
                int chunkSize = 16; // Process 16x16 chunks at a time
                
                for (int chunkY = 0; chunkY < height; chunkY += chunkSize)
                {
                    for (int chunkX = 0; chunkX < width; chunkX += chunkSize)
                    {
                        int chunkHeight = Math.Min(chunkSize, height - chunkY);
                        int chunkWidth = Math.Min(chunkSize, width - chunkX);
                        
                        for (int y = chunkY; y < chunkY + chunkHeight; y++)
                        {
                            for (int x = chunkX; x < chunkX + chunkWidth; x++)
                            {
                                int index = (y * width + x) * 4;
                                 
                                // Check bounds
                                if (index + 3 >= rawData.Length)
                                    continue;
                                    
                                // Read BGRA values (common for .raw files)
                                byte b = rawData[index];
                                byte g = rawData[index + 1];
                                byte r = rawData[index + 2];
                                byte a = rawData[index + 3];

                                // Combine into ARGB format
                                int color = (a << 24) | (r << 16) | (g << 8) | b;

                                // Set pixel
                                bitmap.rawData[y * width + x] = color;
                                pixelsProcessed++;
                            }
                        }
                        
                        // Small delay to prevent system lockup
                        Cosmos.HAL.Global.PIT.WaitNS(10);
                    }
                }

                // Cache the bitmap if everything succeeded
                BitMaps.Bitmaps[filePath] = bitmap;
                Console.WriteLine($"[Success] Processed {pixelsProcessed} pixels");
                
                return bitmap;
            
        }
    }
}
