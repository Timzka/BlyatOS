using BadTetrisCS;
using BlyatOS.Library.Functions;
using BlyatOS.Library.Helpers;
using Cosmos.HAL;
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
        Console.Clear();

        do
        {
            Console.WriteLine("You are now in Blyatgames, write \"mainMenu\" to go back or \"help\" for available commands");
            Console.Write("Input: ");

            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            string[] arr = userInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (arr[0])
            {
                case "tetris":
                    {
                        Console.Clear();
                        BadTetris game = new BadTetris();
                        game.Run();
                        Console.Clear();
                        break;
                    }
                case "wiseman":
                    {
                        Console.Clear();
                        Console.WriteLine(BasicFunctions.GenerateWiseManMessage(rand));
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        Console.Clear();
                        break;
                    }
                case "OOGA": 
                    {

                        Console.Clear();
                        Canvas canvas;
                        canvas = FullScreenCanvas.GetFullScreenCanvas(new Mode(640, 480, ColorDepth.ColorDepth32));
                        canvas.Clear(Color.Black); 
                        Console.ReadKey();
                        Bitmap kusche = RawToBitMap.LoadRawImageAutoDetect(@"0:\kusche256.raw");
                        while (true)
                        {
                            if (Cosmos.System.KeyboardManager.TryReadKey(out var keyInfo))
                            {
                                break;
                            }
                            canvas.Clear(Color.Black);
                            canvas.DrawImage(kusche, rand.Next(0, 512), rand.Next(0, 360));
                            canvas.Display();
                            Global.PIT.Wait(500);
                        }
                        canvas.Disable();
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

                        Console.Clear();
                        Canvas canvas;
                        int canvasWidth = 640;
                        int canvasHeight = 480;
                        canvas = FullScreenCanvas.GetFullScreenCanvas(new Mode(canvasWidth, canvasHeight, ColorDepth.ColorDepth32));
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
                                canvas.DrawImage(BitMaps.Bitmaps["barie"], imageX[i], imageY[i]);
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
                        canvas.Disable();
                        break;
                    }
                case "help":
                    {
                        Console.Clear();
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
                        Console.WriteLine("Unknown command! Type \"help\" for help or \"exit\" to return!");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        Console.Clear();
                        break;
                    }
            }
        } while (!exitGames);
    }
}
