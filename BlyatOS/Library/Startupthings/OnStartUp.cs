using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmos.HAL;

namespace BlyatOS.Library.Startupthings;

public class OnStartUp
{
    public static void RunLoadingScreenThing() //unneccessary, but i think it is really cool
    {
        int maxLength = 37;
        int filterLength = 10;
        Random rand = new Random();

        string[] smokePatterns =
        {
        "  ~~~     ",
        "  ~~      ",
        " ~~~      ",
        "  ~~      ",
        "  ~       "
        };
        Console.CursorVisible = false;
        Console.Clear();

        for (int len = maxLength; len >= 0; len--)
        {
            int smokePos = filterLength + len + 2;
            uint randTime = (uint)(50 + rand.Next(0, 250)); // in ms

            // smoke on top
            Console.SetCursorPosition(0, 2);
            Console.Write(new string(' ', smokePos));
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(smokePatterns[len % smokePatterns.Length]);
            Console.ResetColor();

            // draw cigarette filter + case + fire + smoke
            Console.SetCursorPosition(0, 3);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            for (int i = 0; i < filterLength; i++) Console.Write("#");

            Console.ForegroundColor = ConsoleColor.White;
            if (len > 0)
            {
                for (int i = 0; i < len - 1; i++) Console.Write("#");

                // "animation"
                Console.ForegroundColor = len % 3 == 0 ? ConsoleColor.Red :
                                          len % 3 == 1 ? ConsoleColor.DarkYellow :
                                                          ConsoleColor.Yellow;
                Console.Write("#");
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("~~~~      ");
            Console.ResetColor();

            Global.PIT.Wait(randTime); // Delay
        }

        Console.Clear(); 
        Console.SetCursorPosition(0, 0);
        Console.CursorVisible = true;
    }
}
