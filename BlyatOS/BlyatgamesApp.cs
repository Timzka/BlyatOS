using System;
using System.Drawing;
using System.IO;
using BadTetrisCS;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Functions;
using BlyatOS.Library.Helpers;
using Cosmos.HAL;
using Cosmos.System.Graphics;

namespace BlyatOS
{
    public class BlyatgamesApp
    {
        public static void Run(Random rand)
        {
            bool exitGames = false;
            ConsoleHelpers.ClearConsole();

            ConsoleHelpers.WriteLine("You are now in Blyatgames, write \"mainMenu\" to go back or \"help\" for available commands");

            do
            {
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

                    case "pacman":
                    case "pac-man":
                        {
                            ConsoleHelpers.ClearConsole();
                            ConsoleHelpers.WriteLine("Starting Pac-Man...");
                            Global.PIT.Wait(100);
                            PacMan game = new PacMan();
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
                            string path = arr.Length > 1 ? arr[1] : "";
                            ConsoleHelpers.ClearConsole();

                            var canvas = DisplaySettings.Canvas;
                            canvas.Clear(Color.Black);

                            Bitmap bitmap;
                            if (path != "" && File.Exists(path))
                                bitmap = ImageHelpers.LoadBMP(path);
                            else
                                bitmap = ImageHelpers.LoadBMP(@"0:\Blyatos\blyatlogo.bmp");

                            int screenWidth = (int)DisplaySettings.ScreenWidth;
                            int screenHeight = (int)DisplaySettings.ScreenHeight;

                            while (true)
                            {
                                if (Cosmos.System.KeyboardManager.TryReadKey(out var _))
                                    break;

                                int x = rand.Next(0, Math.Max(1, screenWidth - 200));
                                int y = rand.Next(0, Math.Max(1, screenHeight - 200));

                                canvas.DrawImage(bitmap, x, y);
                                canvas.Display();
                                Global.PIT.Wait(500);
                            }

                            canvas.Clear(DisplaySettings.BackgroundColor);
                            break;
                        }

                    case "screensave":
                        {
                            // Parameter parsen: screensave [number] [optional path]
                            int numberOfImages = 1;
                            if (arr.Length > 1 && int.TryParse(arr[1], out int numImages))
                                numberOfImages = Math.Max(1, Math.Min(numImages, 10));

                            string path = arr.Length > 2 ? arr[2] : "";

                            Bitmap bitmap;
                            if (path != "" && File.Exists(path))
                                bitmap = ImageHelpers.LoadBMP(path);
                            else
                                bitmap = ImageHelpers.LoadBMP(@"0:\Blyatos\blyatlogo.bmp");

                            Console.Clear();
                            var canvas = DisplaySettings.Canvas;
                            int canvasWidth = (int)DisplaySettings.ScreenWidth;
                            int canvasHeight = (int)DisplaySettings.ScreenHeight;

                            canvas.Clear(Color.Black);

                            int[] imageX = new int[numberOfImages];
                            int[] imageY = new int[numberOfImages];
                            int[] velocityX = new int[numberOfImages];
                            int[] velocityY = new int[numberOfImages];
                            int imageSize = 128;

                            // Startpositionen und Geschwindigkeiten initialisieren
                            for (int i = 0; i < numberOfImages; i++)
                            {
                                imageX[i] = rand.Next(imageSize, canvasWidth - imageSize);
                                imageY[i] = rand.Next(imageSize, canvasHeight - imageSize);

                                velocityX[i] = rand.Next(-5, 6);
                                if (velocityX[i] == 0) velocityX[i] = 1;

                                velocityY[i] = rand.Next(-5, 6);
                                if (velocityY[i] == 0) velocityY[i] = 1;
                            }

                            while (true)
                            {
                                if (Cosmos.System.KeyboardManager.TryReadKey(out var _))
                                    break;

                                canvas.Clear(Color.Black);

                                // Bilder zeichnen
                                for (int i = 0; i < numberOfImages; i++)
                                {
                                    canvas.DrawImage(bitmap, imageX[i], imageY[i]);
                                }

                                canvas.Display();

                                // Bewegung aktualisieren
                                for (int i = 0; i < numberOfImages; i++)
                                {
                                    imageX[i] += velocityX[i];
                                    imageY[i] += velocityY[i];

                                    // Rand-Kollisionen prüfen
                                    if (imageX[i] <= 0 || imageX[i] >= canvasWidth - imageSize)
                                        velocityX[i] = -velocityX[i];

                                    if (imageY[i] <= 0 || imageY[i] >= canvasHeight - imageSize)
                                        velocityY[i] = -velocityY[i];
                                }

                                Global.PIT.Wait(25); // ca. 40 FPS
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
            }
            while (!exitGames);
        }
    }
}
