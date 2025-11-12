using BadTetrisCS;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Functions;
using BlyatOS.Library.Helpers;
using Cosmos.Core.Memory;
using Cosmos.HAL;
using Cosmos.System.FileSystem;
using Cosmos.System.Graphics;
using System;
using System.Drawing;
using System.IO;

namespace BlyatOS;

public class BlyatgamesApp
{
    public static void Run(Random rand)
    {
        bool exitGames = false;
        ConsoleHelpers.ClearConsole();

        ConsoleHelpers.WriteLine("You are now in Blyatgames, write \"mainMenu\" to go back or \"help\" for available commands");
        do
        {
            Heap.Collect();
            ConsoleHelpers.WriteLine();
            ConsoleHelpers.Write("BlyatGames> ");

            var userInput = ConsoleHelpers.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            string[] arr = userInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (arr[0])
            {
                case "tetris":
                    {
                        ConsoleHelpers.ClearConsole();
                        ConsoleHelpers.WriteLine("Starting BadTetris...");
                        Global.PIT.Wait(100);
                        BadTetris game = new BadTetris();
                        game.Run();
                        ConsoleHelpers.ClearConsole();
                        break;
                    }
                case "wiseman":
                    {
                        ConsoleHelpers.ClearConsole();
                        ConsoleHelpers.WriteLine(BasicFunctions.GenerateWiseManMessage(rand));
                        ConsoleHelpers.WriteLine("Press any key to continue...");
                        ConsoleHelpers.ReadKey();
                        ConsoleHelpers.ClearConsole();
                        break;
                    }
                case "OOGA":
                    {
                        string path = "";
                        if (arr.Length > 1)
                        {
                            path = arr[1];
                        }
                        ConsoleHelpers.ClearConsole();
                        var canvas = DisplaySettings.Canvas;
                        canvas.Clear(Color.Black);
                        Bitmap bitmap;
                        if (path != "" && File.Exists(path))
                        {
                            bitmap = ImageHelpers.LoadBMP(path);
                        }
                        else
                        {
                            bitmap = ImageHelpers.LoadBMP(@"0:\Blyatos\blyatlogo.bmp");
                        }
                        while (true)
                        {
                            if (Cosmos.System.KeyboardManager.TryReadKey(out var keyInfo))
                            {
                                break;
                            }
                            canvas.DrawImage(bitmap, rand.Next(200, DisplaySettings.ScreenWidth)-200, rand.Next(200, DisplaySettings.ScreenHeight) -200);
                            canvas.Display();
                            Global.PIT.Wait(500);
                        }

                        canvas.Clear(DisplaySettings.BackgroundColor);
                        //Console.ReadKey();
                        break;
                    }
                case "screensave":

                    {
                        // Parameter parsen: screensave [number]
                        int numberOfImages = 1; // Standard: 1 Bild
                        if (arr.Length > 1 && int.TryParse(arr[1], out int numImages))
                        {
                            numberOfImages = Math.Max(1, Math.Min(numImages, 10)); // Max 10 Bilder
                        }
                        string path = "";
                        if(arr.Length > 2)
                        {
                            path = arr[2];
                        }

                        Bitmap bitmap;
                        if(path != "" && File.Exists(path))
                        {
                            bitmap = ImageHelpers.LoadBMP(path);
                        }
                        else
                        {
                            bitmap = ImageHelpers.LoadBMP(@"0:\Blyatos\blyatlogo.bmp");
                        }

                        Console.Clear();
                        var canvas = DisplaySettings.Canvas;
                        int canvasWidth = DisplaySettings.ScreenWidth;
                        int canvasHeight = DisplaySettings.ScreenHeight;
                        canvas.Clear(Color.Black);

                        // Arrays für mehrere Bilder initialisieren
                        int[] imageX = new int[numberOfImages];
                        int[] imageY = new int[numberOfImages];
                        int[] velocityX = new int[numberOfImages];
                        int[] velocityY = new int[numberOfImages];
                        int imageSize = 128;

                        // Startpositionen und Geschwindigkeiten für alle Bilder initialisieren
                        for (int i = 0; i < numberOfImages; i++)
                        {
                            // Zufällige Startpositionen (mit Randabstand)
                            imageX[i] = rand.Next(imageSize, canvasWidth - imageSize);
                            imageY[i] = rand.Next(imageSize, canvasHeight - imageSize);

                            // Zufällige Geschwindigkeiten (-5 bis +5, nicht 0)
                            velocityX[i] = rand.Next(-5, 6);
                            if (velocityX[i] == 0) velocityX[i] = 1;

                            velocityY[i] = rand.Next(-5, 6);
                            if (velocityY[i] == 0) velocityY[i] = 1;
                        }

                        while (true)
                        {
                            // Tastatur prüfen zum Beenden
                            if (Cosmos.System.KeyboardManager.TryReadKey(out var keyInfo))
                            {
                                break;
                            }

                            canvas.Clear(Color.Black);

                            // Alle Bilder zeichnen
                            for (int i = 0; i < numberOfImages; i++)
                            {
                                canvas.DrawImage(bitmap, imageX[i], imageY[i]);
                            }
                            canvas.Display();

                            // Positionen aller Bilder aktualisieren
                            for (int i = 0; i < numberOfImages; i++)
                            {
                                imageX[i] += velocityX[i];
                                imageY[i] += velocityY[i];
                            }

                            // Rand-Kollision für alle Bilder prüfen und Richtung umkehren
                            for (int i = 0; i < numberOfImages; i++)
                            {
                                // Linker Rand
                                if (imageX[i] <= 0)
                                {
                                    imageX[i] = 0;
                                    velocityX[i] = Math.Abs(velocityX[i]); // Nach rechts bewegen
                                }
                                // Rechter Rand (canvasWidth - imageSize)
                                else if (imageX[i] >= canvasWidth - imageSize)
                                {
                                    imageX[i] = canvasWidth - imageSize;
                                    velocityX[i] = -Math.Abs(velocityX[i]); // Nach links bewegen
                                }

                                // Oberer Rand
                                if (imageY[i] <= 0)
                                {
                                    imageY[i] = 0;
                                    velocityY[i] = Math.Abs(velocityY[i]); // Nach unten bewegen
                                }
                                // Unterer Rand (canvasHeight - imageSize)
                                else if (imageY[i] >= canvasHeight - imageSize)
                                {
                                    imageY[i] = canvasHeight - imageSize;
                                    velocityY[i] = -Math.Abs(velocityY[i]); // Nach oben bewegen
                                }
                            }

                            Global.PIT.Wait(25); // 40 FPS für smooth Animation mit mehreren Bildern
                        }
                        canvas.Clear(DisplaySettings.BackgroundColor);
                        break;
                    }
                case "help":
                    {
                        ConsoleHelpers.ClearConsole();
                        BasicFunctions.Help(null, BasicFunctions.ListType.Blyatgames);
                        break;
                    }
                case "mainMenu":
                case "exit":
                    {
                        exitGames = true;
                        break;
                    }
                default:
                    {
                        ConsoleHelpers.WriteLine("Unknown command! Type \"help\" for help or \"exit\" to return!");
                        ConsoleHelpers.WriteLine("Press any key to continue...");
                        ConsoleHelpers.ReadKey();
                        ConsoleHelpers.ClearConsole();
                        break;
                    }
            }
        } while (!exitGames);
    }
}
