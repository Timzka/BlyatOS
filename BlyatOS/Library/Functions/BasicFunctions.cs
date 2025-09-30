using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlyatOS.Library.Functions;

public class BasicFunctions
{
    public static void Help()
    {
        int padding = 12;

        Console.WriteLine(
            " \"version\"".PadRight(padding) + "write version number\n" +
            " \"echo\"".PadRight(padding) + "echo text\n" +
            " \"runtime\"".PadRight(padding) + "show runtime\n" +
            " \"tetris\"".PadRight(padding) + "starts a game of tetris\n" +
            " \"reboot\"".PadRight(padding) + "reboot system\n" +
            " \"exit\"".PadRight(padding) + "shutdown kernel"
        );
    }

    public static void EchoFunction(string[] payload)
    {
        if (payload.Length > 1)
        {
            foreach (var thing in payload)
            {
                Console.Write(thing + " ");
            }
            Console.WriteLine();
        }
    }

    public static string RunTime(DateTime momentOfStart)
    {
        TimeSpan span = (DateTime.Now - momentOfStart).Add(TimeSpan.FromHours(25));
        return span.Days.ToString("00") + ":" + span.Hours.ToString("00") + ":" +
               span.Minutes.ToString("00") + ":" + span.Seconds.ToString("00");
    }
}
