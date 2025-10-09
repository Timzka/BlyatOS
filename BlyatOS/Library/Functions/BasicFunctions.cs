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
        int padding = 30; //make page system for Help, so that like.. 6 commands are shown at a time or so

        var commands = new Dictionary<string, string> //command, description
        {
            { "version | v", "write version number" },
            { "echo", "echo text" },
            { "runtime", "show runtime" },
            { "tetris", "starts a game of tetris" },
            { "reboot", "reboot system" },
            { "exit", "shutdown kernel" },
            { "createUser", "create new user" },
            { "lock | logout", "return to login" },
            { "deleteUser", "delete a user" },
            { "wiseman", "get a motivational message" },
            { "clearScreen | clear", "clear the console" },
            { "cls", "" },
            { "help", "show this help" }
        };

        foreach (var command in commands)
        {
            Console.WriteLine(command.Key.PadRight(padding) + command.Value);
        }
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

    private static string[] WiseManMessages = new string[]
    {
        "Sometimes you have to let your goofy shine side through.",
        "The best way to get something done is to begin, do it for 5 minutes and quit without saving",
        "If you think your are too small to make a difference, go gambling at SlotsOS",
        "It is better to just use BlyatOS forever instead of returning to Windows",
        //"Lieber arm dran als Arm ab!" //german thing
        //add more whatever you want ;)))
    };
    public static string GenerateWiseManMessage(Random rand)
    {
        int index = rand.Next(0, WiseManMessages.Length); //however much messages there are it will work!
        return WiseManMessages[index];
    }
}
