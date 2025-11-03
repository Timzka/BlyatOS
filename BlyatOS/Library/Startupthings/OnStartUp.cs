using System;
using System.Drawing;
using Cosmos.HAL;
using BlyatOS.Library.Helpers;
using BlyatOS.Library.Configs;

namespace BlyatOS.Library.Startupthings;

public class OnStartUp
{
    static char block = (char)219;
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
        ConsoleHelpers.ClearConsole();

        for (int len = maxLength; len >= 0; len--)
        {
            int smokePos = filterLength + len + 2;
            uint randTime = (uint)(50 + rand.Next(0, 250)); // in ms

            // smoke on top
            ConsoleHelpers.SetCursorPosition(0, 2);
            ConsoleHelpers.Write(new string(' ', smokePos));
            
            // Draw smoke pattern with gray color
            ConsoleHelpers.Write(smokePatterns[len % smokePatterns.Length], Color.Gray);

            // draw cigarette filter + case + fire + smoke
            ConsoleHelpers.SetCursorPosition(0, 3);

            // Draw filter (dark yellow)
            ConsoleHelpers.Write(new string(block, filterLength), Color.DarkGoldenrod);

            if (len > 0)
            {
                // Draw cigarette (white)
                ConsoleHelpers.Write(new string(block, len), Color.White);

                // Draw burning tip (animated)
                Color tipColor = len % 3 == 0 ? Color.DarkGoldenrod :
                               len % 3 == 1 ? Color.Red : Color.Yellow;
                ConsoleHelpers.Write($"{block}", tipColor);
            }

            // Draw smoke trail
            ConsoleHelpers.Write("~~~~      ", Color.Gray);

            Global.PIT.Wait(randTime); // Delay
        }

        ConsoleHelpers.ClearConsole();
    }
}
