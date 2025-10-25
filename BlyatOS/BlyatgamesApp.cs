using BadTetrisCS;
using BlyatOS.Library.Functions;
using Cosmos.HAL;
using System;

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
                        Console.WriteLine("ZIGARETTEN");
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
