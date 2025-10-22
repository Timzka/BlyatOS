using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlyatOS.Library.Functions;

public class BasicFunctions
{
    public enum ListType
    {
        Main,
        Blyatgames,
        UserManagement
    }

    public static void Help(int? page, ListType listType = ListType.Main)
    {
        int commandsPerPage = 6;

        var commands = GetCommandsForListType(listType);

        int totalCommands = commands.Count;
        int totalPages = (int)Math.Ceiling((double)totalCommands / commandsPerPage);

        int currentPage = Math.Max(1, Math.Min(page ?? 1, totalPages));

        Console.WriteLine($"--- Help Page {currentPage}/{totalPages} ({GetListTypeName(listType)}) ---");

        var wantedCommandsPage = commands.Skip((currentPage - 1) * commandsPerPage).Take(commandsPerPage);

        if (currentPage == 1)
        {
            Console.WriteLine("---");
            Console.WriteLine("command: help [page]");
            Console.WriteLine("description: show help pages (optional page number)");
        }

        foreach (var command in wantedCommandsPage)
        {
            Console.WriteLine("---");
            Console.WriteLine("command: " + command.Command);
            Console.WriteLine("description: " + command.Description);
        }

        Console.WriteLine();
    }

    private static List<Commands> GetCommandsForListType(ListType listType)
    {
        switch (listType)
        {
            case ListType.Main:
                return new List<Commands>
                {
                    new Commands("version | v", "write version number"),
                    new Commands("echo", "echo text"),
                    new Commands("runtime", "show runtime"),
                    new Commands("reboot", "reboot system"),
                    new Commands("clearScreen | clear | cls", "clear the console"),
                    new Commands("userManagement", "access user management functions"),
                    new Commands("blyatgames", "access games (tetris, wiseman, OOGA)"),
                    new Commands("lock | logout", "return to login"),
                    new Commands("exit", "shutdown kernel"),
                    new Commands("pwd", "show current directory")
                };

            case ListType.Blyatgames:
                return new List<Commands>
                {
                    new Commands("tetris", "starts a game of tetris"),
                    new Commands("wiseman", "get a motivational message"),
                    new Commands("OOGA", "jumpscare"),
                    new Commands("screensave [number]", "animated screensaver with multiple bouncing images (default: 1)"),
                    new Commands("mainMenu", "return to main menu"),
                    new Commands("help [page]", "show this help page")
                };

            case ListType.UserManagement:
                return new List<Commands>
                {
                    new Commands("vodka", "admin command (not implemented)"),
                    new Commands("createUser", "create new user (admin only)"),
                    new Commands("deleteUser", "delete a user (admin only)"),
                    new Commands("mainMenu", "return to main menu"),
                    new Commands("help [page]", "show this help page")
                };

            default:
                return new List<Commands>();
        }
    }

    private static string GetListTypeName(ListType listType)
    {
        switch (listType)
        {
            case ListType.Main:
                return "Main Menu";
            case ListType.Blyatgames:
                return "Blyatgames";
            case ListType.UserManagement:
                return "User Management";
            default:
                return "Unknown";
        }
    }

    public static void EchoFunction(string[] payload)
    {
        if (payload.Length > 1)
        {
            for (int i = 1; i < payload.Length; i++)
            {
                Console.Write(payload[i] + " ");
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
    };

    public static string GenerateWiseManMessage(Random rand)
    {
        int index = rand.Next(0, WiseManMessages.Length);
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
