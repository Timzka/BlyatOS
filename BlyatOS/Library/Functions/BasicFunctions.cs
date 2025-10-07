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
        int padding = 20;

        Console.WriteLine(
            " \"version\"| \"v\"".PadRight(padding) + "write version number\n" +
            " \"echo\"".PadRight(padding) + "echo text\n" +
            " \"runtime\"".PadRight(padding) + "show runtime\n" +
            " \"tetris\"".PadRight(padding) + "starts a game of tetris\n" +
            " \"reboot\"".PadRight(padding) + "reboot system\n" +
            " \"exit\"".PadRight(padding) + "shutdown kernel\n" +
            " \"createUser\"".PadRight(padding) + "create new user\n" +
            " \"lock\" | \"logout\"".PadRight(padding) + "return to login\n" +
            " \"clearScreen\" | \"clear\" | \"cls\"".PadRight(padding) + "clear the console\n"
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
        TimeSpan span = (DateTime.Now - momentOfStart);
        
        string days = span.Days < 10 ? "0" + span.Days.ToString() : span.Days.ToString();
        string hours = span.Hours < 10 ? "0" + span.Hours.ToString() : span.Hours.ToString();
        string minutes = span.Minutes < 10 ? "0" + span.Minutes.ToString() : span.Minutes.ToString();
        string seconds = span.Seconds < 10 ? "0" + span.Seconds.ToString() : span.Seconds.ToString();
        
        return days + ":" + hours + ":" + minutes + ":" + seconds;
    }
}
