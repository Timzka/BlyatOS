using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using BadTetrisCS;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Functions;
using BlyatOS.Library.Helpers;
using BlyatOS.Library.Ressources;
using Cosmos.HAL;
using Cosmos.System.Graphics;

namespace BlyatOS
{
    public class BlyatgamesApp
    {
        // Statische Highscore-Liste für Tetris (Top 5)
        private static List<int> tetrisHighScores = new List<int>() { 0, 0, 0, 0, 0 };

        public static void Run(Random rand)
        {
            bool exitGames = false;
            ConsoleHelpers.ClearConsole();

            ConsoleHelpers.WriteLine("You are now in Blyatgames, write \"mainMenu\" to go back or \"help\" for available commands");

            do
            {
                ConsoleHelpers.WriteLine();

                var userInput = ConsoleHelpers.ReadLine("BlyatGames>");

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
                            BadTetris game = new BadTetris(tetrisHighScores);
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

                    case "TRAKTOR":
                        {

                            var canvas = DisplaySettings.Canvas;
                            canvas.Clear(Color.Black);

                            Bitmap bitmap = BMP.Traktor;
                            AudioHandler.Play(Audio.BlyatTraktor);
                            int screenWidth = (int)DisplaySettings.ScreenWidth;
                            int screenHeight = (int)DisplaySettings.ScreenHeight;
                            int x = (screenWidth - (int)bitmap.Width) / 2;
                            int y = (screenHeight - (int)bitmap.Height) / 2;
                            canvas.DrawImage(bitmap, x, y);
                            canvas.Display();
                            while(AudioHandler.IsPlaying)
                            {
                                Global.PIT.Wait(1000);
                            }
                            AudioHandler.Stop();
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
                                bitmap = BMP.BlyatLogo;

                            Console.Clear();
                            var canvas = DisplaySettings.Canvas;
                            int canvasWidth = (int)DisplaySettings.ScreenWidth;
                            int canvasHeight = (int)DisplaySettings.ScreenHeight;

                            canvas.Clear(Color.Black);

                            int[] imageX = new int[numberOfImages];
                            int[] imageY = new int[numberOfImages];
                            int[] velocityX = new int[numberOfImages];
                            int[] velocityY = new int[numberOfImages];
                            int imageSizeW = (int)bitmap.Width;
                            int imageSizeH = (int)bitmap.Height;

                            // Startpositionen und Geschwindigkeiten initialisieren
                            for (int i = 0; i < numberOfImages; i++)
                            {
                                imageX[i] = rand.Next(imageSizeW, canvasWidth - imageSizeW);
                                imageY[i] = rand.Next(imageSizeH, canvasHeight - imageSizeH);

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
                                    if (imageX[i] <= 0 || imageX[i] >= canvasWidth - imageSizeW)
                                        velocityX[i] = -velocityX[i];

                                    if (imageY[i] <= 0 || imageY[i] >= canvasHeight - imageSizeH)
                                        velocityY[i] = -velocityY[i];
                                }

                                Global.PIT.Wait(25); // ca. 40 FPS
                            }

                            canvas.Clear(DisplaySettings.BackgroundColor);
                            break;
                        }

                    case "highscores":
                        {
                            ConsoleHelpers.ClearConsole();
                            ConsoleHelpers.WriteLine("=== TETRIS TOP SCORES ===");
                            ConsoleHelpers.WriteLine();

                            if (tetrisHighScores.Count == 0)
                            {
                                ConsoleHelpers.WriteLine("No high scores yet. Play Tetris to set a record!");
                            }
                            else
                            {
                                for (int i = 0; i < tetrisHighScores.Count; i++)
                                {
                                    ConsoleHelpers.Write((i + 1).ToString());
                                    ConsoleHelpers.Write(". ");
                                    ConsoleHelpers.WriteLine(tetrisHighScores[i].ToString());
                                }
                            }

                            ConsoleHelpers.WriteLine();
                            ConsoleHelpers.WriteLine("Press any key to continue...");
                            ConsoleHelpers.ReadKey();
                            ConsoleHelpers.ClearConsole();
                            break;
                        }

                    case "resethighscores":
                        {
                            ConsoleHelpers.ClearConsole();
                            ConsoleHelpers.Write("Are you sure you want to reset all Tetris high scores? (y/n): ");
                            var confirm = ConsoleHelpers.ReadLine();

                            if (confirm != null && confirm.ToLower() == "y")
                            {
                                tetrisHighScores.Clear();
                                ConsoleHelpers.WriteLine("High scores have been reset.");
                            }
                            else
                            {
                                ConsoleHelpers.WriteLine("Reset cancelled.");
                            }

                            ConsoleHelpers.WriteLine("Press any key to continue...");
                            ConsoleHelpers.ReadKey();
                            ConsoleHelpers.ClearConsole();
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