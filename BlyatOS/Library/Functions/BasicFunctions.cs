using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlyatOS.Library.Functions;

public class BasicFunctions
{
    public static void Help(int? page)
    {
        int commandsPerPage = 6;

        var commands = new List<Commands>//command, description
        {
            new Commands("help [page]", "show help pages (optional page number)"),
            new Commands("version | v", "write version number"),
            new Commands("echo", "echo text"),
            new Commands("runtime", "show runtime"),
            new Commands("tetris", "starts a game of tetris"),
            new Commands("reboot", "reboot system") ,
            new Commands("exit", "shutdown kernel"),
            new Commands("createUser", "create new user"),
            new Commands("lock | logout", "return to login"),
            new Commands("deleteUser", "delete a user") ,
            new Commands("wiseman", "get a motivational message"),
            new Commands("clearScreen | clear | cls", "clear the console"),
        };

        int totalCommands = commands.Count;
        int totalPages = (int)Math.Ceiling((double)totalCommands / commandsPerPage);

        int currentPage = Math.Max(1, Math.Min(page ?? 1, totalPages));

        Console.WriteLine($"--- Help Page {currentPage}/{totalPages} ---");

        var wantedCommandsPage = commands.Skip((currentPage - 1) * commandsPerPage).Take(commandsPerPage);

        foreach (var command in wantedCommandsPage)
        {
            Console.WriteLine("---");
            Console.WriteLine("command: " + command.Command);
            Console.WriteLine("description: " + command.Description);
        }

        Console.WriteLine();
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

public class Commands
{
    public string Command { get; set; }
    public string Description { get; set; }

    public Commands(string command, string description)
    {
        Command = command;
        Description = description;
    }
}
