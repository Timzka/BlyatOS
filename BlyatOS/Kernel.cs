using BadTetrisCS;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Functions;
using BlyatOS.Library.Startupthings;
using Cosmos.HAL;
using System;
using System.IO;
using Cosmos.System.ScanMaps;
using Sys = Cosmos.System;
using BlyatOS.Library.BlyatFileSystem;

namespace BlyatOS;

public class Kernel : Sys.Kernel
{
    DateTime momentOfStart;
    string versionString = "0.9";
    private UsersConfig UsersConf = new UsersConfig();
    private int CurrentUser;
    bool logged_in = false;
    Random Rand = new Random(DateTime.Now.Millisecond);

    Sys.FileSystem.CosmosVFS fs;
    FileSystemHelpers fsh = new FileSystemHelpers();

    private const string RootPath = @"0:\";
    private string current_directory = RootPath; // immer mit abschlie�endem Backslash

    protected override void BeforeRun()
    {
        fs = new Sys.FileSystem.CosmosVFS();
        Sys.FileSystem.VFS.VFSManager.RegisterVFS(fs);

        Sys.KeyboardManager.SetKeyLayout(new DE_Standard());
        Console.WriteLine($"BlyatOS v{versionString} booted successfully. Type help for a list of valid commands");
        momentOfStart = DateTime.Now;
        Global.PIT.Wait(1000);
    }

    protected override void Run()
    {
        try
        {
            // Verzeichnislisten immer aktuell holen
            string[] dirs = fsh.GetDirectories(current_directory);
            string[] files = fsh.GetFiles(current_directory);

            if (logged_in)
            {
                Console.Write("Input: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    return;

                string[] args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                switch (args[0])
                {
                    case "help":
                        {
                            int? page = null;
                            if (args.Length > 1 && int.TryParse(args[1], out int p)) page = p;
                            Console.Clear();
                            BasicFunctions.Help(page);
                            break;
                        }
                    case "basic":
                        {

                            bool exitProgram = false;
                            Console.Clear();
                            //Console.WriteLine("Enter 'mainMenu'");
                            do
                            {
                                Console.WriteLine("Basic");
                                Console.WriteLine("Input: ");

                                var userInput = Console.ReadLine();

                                string[] arr = userInput.Split(' ');

                                switch (arr[0])
                                {
                                    case "version":
                                        {
                                            Console.WriteLine("Blyat version " + versionString);
                                            break;
                                        }
                                    case "echo":
                                        {
                                            BasicFunctions.EchoFunction(args);
                                            break;
                                        }
                                    case "runtime":
                                        {
                                            Console.WriteLine(BasicFunctions.RunTime(momentOfStart));
                                            break;
                                        }
                                    case "wiseman":
                                        {
                                            Console.WriteLine(BasicFunctions.GenerateWiseManMessage(Rand));
                                            break;
                                        }
                                    case "reboot":
                                        {
                                            Console.WriteLine("rebooting");
                                            Global.PIT.Wait(1000);
                                            Cosmos.System.Power.Reboot();
                                            break;
                                        }
                                    case "clearScreen":
                                        {
                                            Console.Clear();
                                            break;
                                        }
                                    case "mainMenu":
                                        {
                                            exitProgram = true;
                                            break;
                                        }
                                    default:
                                        {
                                            Console.WriteLine("Unknown command!  Enter \"help\" for more information!");
                                            break;
                                        }
                                }
                            } while (!exitProgram);
                            break;

                        }
                    case "userManagment":
                        {
                            bool exitProgram = false;
                            Console.Clear();
                            do
                            {

                                Console.WriteLine("User Managment");
                                Console.WriteLine("Input: ");

                                var userInput = Console.ReadLine();

                                string[] arr = userInput.Split(' ');

                                switch (arr[0])
                                {
                                    case "lock":
                                        {
                                            Console.WriteLine("Logging out...");
                                            Global.PIT.Wait(1000);
                                            logged_in = false;
                                            break;
                                        }
                                    case "vodka":
                                        {
                                            if (!UserManagement.CheckPermissions(CurrentUser, UsersConf, UsersConfig.Permissions.Admin))
                                            {
                                                Console.WriteLine("Missing Permissions");
                                                break;
                                            }
                                            Console.WriteLine("Nyet, no vodka for you! //not implemented");
                                            break;
                                        }
                                    case "createUser":
                                        {
                                            Console.Clear();
                                            if (!UserManagement.CheckPermissions(CurrentUser, UsersConf, UsersConfig.Permissions.Admin))
                                            {
                                                Console.WriteLine("Missing Permissions");
                                                break;
                                            }
                                            UserManagement.CreateUser(UsersConf, CurrentUser);
                                            break;
                                        }
                                    case "deleteUser":
                                        {
                                            Console.Clear();
                                            if (!UserManagement.CheckPermissions(CurrentUser, UsersConf, UsersConfig.Permissions.Admin))
                                            {
                                                Console.WriteLine("Missing Permissions");
                                                break;
                                            }
                                            UserManagement.DeleteUser(UsersConf, CurrentUser);
                                            break;
                                        }
                                    case "mainMenu":
                                        {
                                            exitProgram = true;
                                            break;
                                        }
                                    default:
                                        {
                                            Console.WriteLine("Unknown command!  Enter \"help\" for more information!");
                                            break;
                                        }
                                }
                            } while (!exitProgram && logged_in);
                            break;
                        }

                    case "exit":
                        {
                            Console.WriteLine("exitting");
                            Global.PIT.Wait(1000);
                            Cosmos.System.Power.Shutdown();
                            break;
                        }

                    case "tetris":
                        {
                            BadTetris game = new BadTetris();
                            game.Run();
                            break;
                        }

                    case "fsinfo":
                        {
                            var disks = fs.Disks;
                            foreach (var disk in disks)
                            {
                                if (disk?.Host == null)
                                {
                                    Console.WriteLine("Disk host unavailable");
                                    continue;
                                }

                                Console.WriteLine(fsh.BlockDeviceTypeToString(disk.Host.Type));

                                foreach (var partition in disk.Partitions)
                                {
                                    Console.WriteLine(partition.Host);
                                    Console.WriteLine(partition.RootPath);
                                    Console.WriteLine(partition.MountedFS.Size);
                                }
                            }
                            break;
                        }

                    case "dir":
                        {
                            foreach (var d in dirs)
                            {
                                Console.WriteLine(TrimPath(d) + "/");
                            }
                            break;
                        }

                    case "ls":
                        {
                            const string dirMarker = "[D]";
                            const string fileMarker = "[F]";
                            foreach (var d in dirs)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"{dirMarker} {TrimPath(d)}/");
                            }
                            foreach (var f in files)
                            {
                                Console.ForegroundColor = ConsoleColor.Gray;
                                Console.WriteLine($"{fileMarker} {TrimPath(f)}");
                            }
                            Console.ResetColor();
                            break;
                        }

                    case "mkdir":
                        {
                            if (args.Length < 2)
                            {
                                Console.WriteLine("Usage: mkdir <name>");
                                break;
                            }
                            var name = args[1];
                            var path = PathCombine(current_directory, name);
                            fs.CreateDirectory(path);
                            Console.WriteLine($"Directory '{name}' created at '{path}'");
                            break;
                        }

                    case "touch":
                        {
                            if (args.Length < 2)
                            {
                                Console.WriteLine("Usage: touch <name>");
                                break;
                            }
                            var name = args[1];
                            var path = PathCombine(current_directory, name);
                            fs.CreateFile(path);
                            Console.WriteLine($"File '{name}' created at '{path}'");
                            break;
                        }

                    case "cat":
                        {
                            if (args.Length < 2)
                            {
                                Console.WriteLine("Usage: cat <filename>");
                                break;
                            }
                            var fileArg = args[1];
                            string path = IsAbsolute(fileArg) ? fileArg : PathCombine(current_directory, fileArg).TrimEnd('\\');

                            if (!fsh.FileExists(path))
                            {
                                Console.WriteLine($"File not found: {path}");
                                break;
                            }

                            try
                            {
                                Console.WriteLine(fsh.ReadAllText(path));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error reading file: " + ex.Message);
                            }
                            break;
                        }

                    case "cd":
                        {
                            if (args.Length < 2)
                            {
                                Console.WriteLine("Usage: cd <directory>|..");
                                break;
                            }
                            var target = args[1];
                            if (target == "..")
                            {
                                if (IsRoot(current_directory))
                                {
                                    Console.WriteLine("Already at root");
                                    break;
                                }
                                current_directory = GetParent(current_directory);
                                Console.WriteLine($"Changed directory to '{current_directory}'");
                                break;
                            }

                            string newPath = IsAbsolute(target) ? EnsureTrailingSlash(target) : PathCombine(current_directory, target);
                            if (!fsh.DirectoryExists(newPath))
                            {
                                Console.WriteLine($"Directory not found: {newPath}");
                                break;
                            }
                            current_directory = EnsureTrailingSlash(newPath);
                            Console.WriteLine($"Changed directory to '{current_directory}'");
                            break;
                        }

                    case "pwd":
                        Console.WriteLine(current_directory);
                        break;

                    default:
                        Console.WriteLine("Unknown command! Enter \"help\" for more information!");
                        break;
                }
            }
            else
            {
                Console.Clear();
                int c = 0;
                while (true)
                {
                    if (c >= 5) Cosmos.System.Power.Shutdown();
                    else if (c > 0) Console.WriteLine($"You have {5 - c} tries left until shutdown");
                    int uid = UserManagement.Login(UsersConf);
                    if (uid != -1)
                    {
                        CurrentUser = uid;
                        break;
                    }
                    c++;
                    Console.Clear();
                }
                logged_in = true;
                Console.Clear();
            }
            }
        catch (GenericException ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Message: {ex.EMessage}" + (ex.Label != "" ? $",\nLabel: {ex.Label}" : "") + (ex.ComesFrom != "" ? $",\nSource: {ex.ComesFrom}" : ""));
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occured: " + ex.Message);
        }
    }

    // Helpers

    private static string TrimPath(string full)
    {
        if (string.IsNullOrEmpty(full)) return full;
        var sep = full.TrimEnd('\\').LastIndexOf('\\');
        if (sep >= 0 && sep < full.Length - 1)
            return full.TrimEnd('\\').Substring(sep + 1);
        return full.TrimEnd('\\');
    }

    private static bool IsAbsolute(string path) => path.Contains(":\\");
    private static bool IsRoot(string path) => string.Equals(path, RootPath, StringComparison.Ordinal);

    private static string EnsureTrailingSlash(string path)
    {
        if (!path.EndsWith("\\"))
            return path + "\\";
        return path;
    }

    private static string PathCombine(string baseDir, string child)
    {
        // Basis immer mit Backslash
        baseDir = EnsureTrailingSlash(baseDir);
        // Path.Combine entfernt doppelte Backslashes korrekt
        var combined = Path.Combine(baseDir, child);
        return EnsureTrailingSlash(combined);
    }

    private static string GetParent(string path)
    {
        path = EnsureTrailingSlash(path);
        if (IsRoot(path)) return path;
        // entfernt letzten Segment-Backslash
        var trimmed = path.TrimEnd('\\');
        var idx = trimmed.LastIndexOf('\\');
        if (idx <= 2) // z.B. "0:\" -> Index 2
            return RootPath;
        var parent = trimmed.Substring(0, idx + 1);
        return EnsureTrailingSlash(parent);
    }
}