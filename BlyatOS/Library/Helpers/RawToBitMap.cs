using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmos.System.Graphics;
using System.IO;

namespace BlyatOS.Library.Helpers; //a library of functions that shall convert .raw files into byte[] into BitMap objects

internal class RawToBitMap
{
    public static Bitmap LoadRawImage(string path)
    {
        try
        {
            // Prüfe ob die Datei existiert (Cosmos-spezifisch)
            if (!File.Exists(path))
            {
                Console.WriteLine($"FEHLER: Datei '{path}' existiert nicht! Verfügbare Dateien im Verzeichnis:");

                try
                {
                    string[] files = Directory.GetFiles(@"0:\");
                    foreach (string file in files.Take(10)) // Nur erste 10 anzeigen
                    {
                        Console.WriteLine($"  - {file} ({new FileInfo(file).Length} Bytes)");
                    }
                    if (files.Length > 10)
                    {
                        Console.WriteLine($"  ... und {files.Length - 10} weitere Dateien");
                    }
                }
                catch (Exception dirEx)
                {
                    Console.WriteLine($"  (Kann Verzeichnis nicht lesen: {dirEx.Message})");
                }

                throw new FileNotFoundException($"Datei '{path}' wurde nicht gefunden!", path);
            }

            // Datei einlesen
            byte[] rawData = File.ReadAllBytes(path);
            
            if (rawData.Length == 0)
            {
                throw new Exception("Raw-Datei ist leer oder existiert nicht!");
            }

            Console.WriteLine($"DEBUG: RAW-Datei '{path}' hat {rawData.Length} Bytes (0x{rawData.Length:X} Bytes)");
            Console.WriteLine("DEBUG: Erste 16 Bytes: " + string.Join(" ", rawData.Take(16).Select(b => b.ToString("X2"))));

            if (rawData.Length % 4 != 0)
            {
                throw new Exception($"Ungültiges RAW: Länge {rawData.Length} ist nicht durch 4 teilbar. Nicht im RGBA32 Format?");
            }

            int pixelCount = rawData.Length / 4;
            double sqrtPixels = Math.Sqrt(pixelCount);
            int size = (int)Math.Round(sqrtPixels);

            Console.WriteLine($"DEBUG: Pixel-Anzahl: {pixelCount}, Quadratwurzel: {sqrtPixels:F2}, gerundet: {size}");

            if (Math.Abs(sqrtPixels - size) > 0.001)
            {
                throw new Exception($"RAW-Datei ist nicht quadratisch! Pixel: {pixelCount}, sqrt: {sqrtPixels:F2}, sollte ganze Zahl sein.");
            }

            if (size * size != pixelCount)
            {
                throw new Exception($"RAW-Datei ist nicht quadratisch oder ungültig. Berechnete Größe: {size}x{size} = {size*size}, aber Pixel: {pixelCount}");
            }

            uint width = (uint)size;
            uint height = (uint)size;

            Console.WriteLine($"DEBUG: Erstelle Bitmap {width}x{height} ({width*height} Pixel)");

            // Leere Bitmap erstellen
            Bitmap bmp = new Bitmap(width, height, ColorDepth.ColorDepth32);

            // RAW direkt in bmp.RawData kopieren
            if (bmp.rawData.Length != rawData.Length)
            {
                throw new Exception($"Bitmap-Puffergröße stimmt nicht überein! Bitmap: {bmp.rawData.Length} Bytes, RAW: {rawData.Length} Bytes");
            }

            Array.Copy(rawData, bmp.rawData, rawData.Length);
            Console.WriteLine("DEBUG: RAW-Daten erfolgreich in Bitmap kopiert!");

            return bmp;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FEHLER beim Laden von '{path}': {ex.Message}");
            throw;
        }
    }

    public static Bitmap LoadRawImageAutoDetect(string path)
    {
        try
        {
            // Prüfe ob die Datei existiert (Cosmos-spezifisch)
            if (!File.Exists(path))
            {
                Console.WriteLine($"FEHLER: Datei '{path}' existiert nicht! Verfügbare Dateien im Verzeichnis:");

                try
                {
                    string[] files = Directory.GetFiles(@"0:\");
                    foreach (string file in files.Take(10)) // Nur erste 10 anzeigen
                    {
                        Console.WriteLine($"  - {file} ({new FileInfo(file).Length} Bytes)");
                    }
                    if (files.Length > 10)
                    {
                        Console.WriteLine($"  ... und {files.Length - 10} weitere Dateien");
                    }
                }
                catch (Exception dirEx)
                {
                    Console.WriteLine($"  (Kann Verzeichnis nicht lesen: {dirEx.Message})");
                }

                // Versuche alternative Pfade
                string[] alternativePaths = new[]
                {
                    @"0:\" + Path.GetFileName(path), // Nur Dateiname mit Drive
                    Path.GetFileName(path),           // Nur Dateiname
                    @"0:\isoFiles\" + Path.GetFileName(path), // Im isoFiles Verzeichnis
                    @"isoFiles\" + Path.GetFileName(path)      // Relativer Pfad
                };

                Console.WriteLine("Versuche alternative Pfade:");
                foreach (string altPath in alternativePaths)
                {
                    Console.WriteLine($"  - {altPath}: {(File.Exists(altPath) ? "EXISTS" : "NOT FOUND")}");
                }

                throw new FileNotFoundException($"Datei '{path}' wurde nicht gefunden!", path);
            }

            byte[] rawData = File.ReadAllBytes(path);
            
            if (rawData.Length == 0)
            {
                throw new Exception("Raw-Datei ist leer oder existiert nicht!");
            }

            Console.WriteLine($"DEBUG: Analysiere RAW-Datei '{path}' ({rawData.Length} Bytes)");

            // Versuche verschiedene Formate
            if (rawData.Length % 4 == 0)
            {
                // RGBA32 Format versuchen
                int pixelCount = rawData.Length / 4;
                if (IsSquareNumber(pixelCount))
                {
                    int size = (int)Math.Sqrt(pixelCount);
                    Console.WriteLine($"DEBUG: RGBA32 Format erkannt ({size}x{size})");
                    return CreateBitmapFromRaw(rawData, size, size, "RGBA32");
                }
            }

            if (rawData.Length % 3 == 0)
            {
                // RGB24 Format versuchen
                int pixelCount = rawData.Length / 3;
                if (IsSquareNumber(pixelCount))
                {
                    int size = (int)Math.Sqrt(pixelCount);
                    Console.WriteLine($"DEBUG: RGB24 Format erkannt ({size}x{size}), konvertiere zu RGBA32");
                    return ConvertRGB24ToRGBA32(rawData, size, size);
                }
            }

            // Fallback: Versuche die Quadratwurzel direkt
            double sqrt = Math.Sqrt(rawData.Length);
            if (Math.Abs(sqrt - Math.Round(sqrt)) < 0.001)
            {
                int size = (int)Math.Round(sqrt);
                Console.WriteLine($"DEBUG: Unbekanntes Format, aber quadratisch ({size}x{size})");
                
                if (rawData.Length == size * size * 4)
                {
                    return CreateBitmapFromRaw(rawData, size, size, "RGBA32-like");
                }
                else if (rawData.Length == size * size * 3)
                {
                    return ConvertRGB24ToRGBA32(rawData, size, size);
                }
                else if (rawData.Length == size * size)
                {
                    return ConvertGrayscaleToRGBA32(rawData, size, size);
                }
            }

            throw new Exception($"Kann kein unterstütztes Format in der RAW-Datei erkennen. Dateigröße: {rawData.Length} Bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FEHLER beim automatischen Erkennen von '{path}': {ex.Message}");
            throw;
        }
    }

    private static bool IsSquareNumber(int number)
    {
        double sqrt = Math.Sqrt(number);
        return Math.Abs(sqrt - Math.Round(sqrt)) < 0.001;
    }

    private static Bitmap CreateBitmapFromRaw(byte[] rawData, int width, int height, string format)
    {
        Console.WriteLine($"DEBUG: Erstelle Bitmap {width}x{height} aus {format}");
        
        Bitmap bmp = new Bitmap((uint)width, (uint)height, ColorDepth.ColorDepth32);
        
        if (bmp.rawData.Length != rawData.Length)
        {
            throw new Exception($"Bitmap-Puffergröße ({bmp.rawData.Length}) stimmt nicht mit RAW-Daten ({rawData.Length}) überein!");
        }

        Array.Copy(rawData, bmp.rawData, rawData.Length);
        return bmp;
    }

    private static Bitmap ConvertRGB24ToRGBA32(byte[] rgbData, int width, int height)
    {
        int pixelCount = width * height;
        byte[] rgbaData = new byte[pixelCount * 4];

        for (int i = 0; i < pixelCount; i++)
        {
            rgbaData[i * 4 + 0] = rgbData[i * 3 + 2]; // R -> B
            rgbaData[i * 4 + 1] = rgbData[i * 3 + 1]; // G -> G
            rgbaData[i * 4 + 2] = rgbData[i * 3 + 0]; // B -> R
            rgbaData[i * 4 + 3] = 255; // Alpha = 255
        }

        return CreateBitmapFromRaw(rgbaData, width, height, "RGB24->RGBA32");
    }

    private static Bitmap ConvertGrayscaleToRGBA32(byte[] grayData, int width, int height)
    {
        int pixelCount = width * height;
        byte[] rgbaData = new byte[pixelCount * 4];

        for (int i = 0; i < pixelCount; i++)
        {
            byte gray = grayData[i];
            rgbaData[i * 4 + 0] = gray; // R
            rgbaData[i * 4 + 1] = gray; // G
            rgbaData[i * 4 + 2] = gray; // B
            rgbaData[i * 4 + 3] = 255; // Alpha
        }

        return CreateBitmapFromRaw(rgbaData, width, height, "Grayscale->RGBA32");
    }
}
