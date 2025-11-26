using System;
using System.Collections.Generic;
using System.Linq;
using BlyatOS.Library.Helpers;

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
        int commandsPerPage = 5;

        var commands = GetCommandsForListType(listType);

        int totalCommands = commands.Count;
        int totalPages = (int)Math.Ceiling((double)totalCommands / commandsPerPage);

        int currentPage = Math.Max(1, Math.Min(page ?? 1, totalPages));

        ConsoleHelpers.WriteLine($"--- Help Page " + currentPage + "/" + totalPages + GetListTypeName(listType) + " ---");
        ConsoleHelpers.WriteLine($"--- | = command alias, <> = optional, [] = mandatory                   ---");

        var wantedCommandsPage = commands.Skip((currentPage - 1) * commandsPerPage).Take(commandsPerPage);

        if (currentPage == 1)
        {
            ConsoleHelpers.WriteLine("---");
            ConsoleHelpers.WriteLine("command: help <page>");
            ConsoleHelpers.WriteLine("description: show help pages (optional page number)");
        }

        foreach (var command in wantedCommandsPage)
        {
            ConsoleHelpers.WriteLine("---");
            ConsoleHelpers.WriteLine("command: " + command.Command);
            ConsoleHelpers.WriteLine("description: " + command.Description);
        }

        ConsoleHelpers.WriteLine();
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
                    new Commands("initsystem", "re-initialize critical system files !they will be reset!"),
                    new Commands("runtime", "show runtime"),
                    new Commands("reboot", "reboot system"),
                    new Commands("getdriver", "get audio driver"),
                    new Commands("showbmpbig [filepath] [width] [height]", "display a bmp image in BIG"),
                    new Commands("clearScreen | clear | cls", "clear the console"),
                    new Commands("userManagement", "access user management functions"),
                    new Commands("blyatgames", "access games (tetris, wiseman, OOGA)"),
                    new Commands("lock | logout", "return to login"),
                    new Commands("exit", "shutdown kernel"),
                    new Commands("pwd", "show current directory"),
                    new Commands("ls", "show all directories and files in Currentpath"),
                    new Commands("dir", "show all directories in Currentpath"),
                    new Commands("cd [dir]", "change currentpath to a directory withing the previous. Use \"cd ..\" to go back"),
                    new Commands("cdisk", "change current disk if multiple are available"),
                    new Commands("pwd", "show current directory"),
                    new Commands("findkusche [filename]", "finds a file for you, giving you its path on any disk and any directory, example: \"findkusche kudzu.txt\""),
                    new Commands("fsinfo", "get information about the file system"),
                    new Commands("mkdir [name]", "create a new directory with [name]"),
                    new Commands("rmdir [name]" , "delete a directory with [name] in currentpath with everything in it, there is no safety at the moment!"),
                    new Commands("touch [filename]", "create a new file with [name], if no ending is given, it will automatically assume .blyat"),
                    new Commands("delfile [filename]", "delete a file with [filename] in currentpath, there is no safety at the moment!"),
                    new Commands("readfile | cat [file]", "read a file, if it is an image, display it, else write the text. if root isnt given, go from currentpath"),
                    new Commands("neofetch", "system overview (logo, user, runtime, disks, etc.)"),
                    new Commands("changecolor", "change console text and background color"),

                };

            case ListType.Blyatgames:
                return new List<Commands>
                {
                    new Commands("tetris", "starts a game of tetris"),
                    new Commands("highscores", "shows tetris highscores"),
                    new Commands("resethighscores", "resets tetris highscores"),
                    new Commands("wiseman", "get a motivational message"),
                    new Commands("TRAKTOR", "you might guess what it does"),
                    new Commands("screensave <number> <filepath>", "animated screensaver with multiple bouncing images (default: 1)"),
                    new Commands("mainMenu | exit", "return to main menu")
                };

            case ListType.UserManagement:
                return new List<Commands>
                {
                    new Commands("vodka", "admin command (not implemented)"),
                    new Commands("createUser", "create new user (admin only)"),
                    new Commands("deleteUser", "delete a user (admin only)"),
                    new Commands("changePassword", "change your password"),
                    new Commands("changePolicy", "change password policy (admin only)"),
                    new Commands("checkPolicy", "check the currently set Password Policy (admin only)"),
                    new Commands("mainMenu | exit", "return to main menu")
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
        if (payload.Length > 0)
        {
            string input = "";
            for (int i = 0; i < payload.Length; i++)
            {
                input += (payload[i] + " ");
            }
            string output = ConsoleHelpers.ProcessEscapeSequences(input);
            ConsoleHelpers.WriteLine(output);
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
