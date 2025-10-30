using BadTetrisCS;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Functions;
using BlyatOS.Library.Startupthings;
using Cosmos.HAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cosmos.System.ScanMaps;
using Sys = Cosmos.System;
using BlyatOS.Library.BlyatFileSystem;
using Cosmos.System.ExtendedASCII;
using System.Text;
using static BlyatOS.PathHelpers;
using static BlyatOS.Library.Configs.UsersConfig;
using Cosmos.System.FileSystem.VFS;
using Cosmos.System.Graphics;
using System.Drawing;
using BlyatOS.Library.Helpers;

namespace BlyatOS;

public class Kernel : Sys.Kernel
{
    DateTime MomentOfStart;
    string VersionInfo = "0.9";
    private UsersConfig UsersConf = new UsersConfig();
    private int CurrentUser;
    bool Logged_In = false;
    Random Rand = new Random(DateTime.Now.Millisecond);

    Sys.FileSystem.CosmosVFS fs;
    FileSystemHelpers fsh = new FileSystemHelpers();

    public const string RootPath = @"0:\";
    public const string SYSTEMPATH = RootPath + @"BlyatOS\"; //path where system files are stored, inaccessable to ALL users via normal commands
    private string CurrentDirectory = RootPath;

    private bool LOCKED; //if system isnt complete, lock system, make user run an INIT command

    protected override void BeforeRun()
    {
        Encoding.RegisterProvider(CosmosEncodingProvider.Instance); //diese 3 zeilen sind für die codepage 437 (box drawing chars), so könnte man theoretisch eine
        Console.InputEncoding = Encoding.GetEncoding(437); //funktion bauen, die UTF8 unterstützt
        Console.OutputEncoding = Encoding.GetEncoding(437);

        OnStartUp.RunLoadingScreenThing();

        fs = new Sys.FileSystem.CosmosVFS();
        Sys.FileSystem.VFS.VFSManager.RegisterVFS(fs);
        LOCKED = !InitSystem.IsSystemCompleted(SYSTEMPATH, fs);

        Sys.KeyboardManager.SetKeyLayout(new DE_Standard());

        Console.WriteLine($"BlyatOS v{VersionInfo} booted successfully. Type help for a list of valid commands");
        MomentOfStart = DateTime.Now;
        Global.PIT.Wait(1000);
    }

    protected override void Run() // && to chain commands --> if(&&) do the switch again
    {
        try
        {
            if(LOCKED)
            {
                Console.WriteLine("Locked, there are system files missing!");
                Console.WriteLine("running initsystem to initialize the files");
                //if (InitSystem.InitSystemData(SYSTEMPATH, fs))
                //{
                //    Console.WriteLine("System initialized. Press any key to reboot");
                //    Console.ReadKey();
                //    Cosmos.System.Power.Reboot();
                //}
                LOCKED = false;
            }
            // Verzeichnislisten immer aktuell holen
            string[] dirs = fsh.GetDirectories(CurrentDirectory);
            string[] files = fsh.GetFiles(CurrentDirectory);

            if (Logged_In)
            {
                Console.Write("Input: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    return;
                string[] args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                // Process commands, handling && chaining
                for (int i = 0; i < args.Length; )
                {
                    // Skip any leading &&
                    while (i < args.Length && args[i] == "&&") i++;
                    if (i >= args.Length) break;
                    
                    // Get the command
                    string command = args[i++];
                    
                    // Collect arguments until next && or end of input
                    var commandArgs = new List<string>();
                    while (i < args.Length && args[i] != "&&")
                    {
                        commandArgs.Add(args[i++]);
                    }
                    
                    // Process the command with its arguments
                    switch (command)
                    {
                        case "help":
                            {
                                int? page = null;
                                if (commandArgs.Count > 0 && int.TryParse(commandArgs[0], out int p)) page = p;
                                Console.Clear();
                                BasicFunctions.Help(page, BasicFunctions.ListType.Main);
                                break;
                            }
                        case "rmsys":
                            {
                                string path = @"0:\BlyatOS";
                                VFSManager.DeleteDirectory(path, true);
                                break;
                            }
                        case "initsystem":
                            {
                                if (InitSystem.InitSystemData(SYSTEMPATH, fs))
                                {
                                    Console.WriteLine("System initialized. Press any key to reboot");
                                    Console.ReadKey();
                                    Cosmos.System.Power.Reboot();
                                }
                                break;
                            }
                        case "version":
                        case "v":
                            {
                                Console.WriteLine("Blyat version " + VersionInfo);
                                break;
                            }
                        case "echo":
                            {
                                BasicFunctions.EchoFunction(commandArgs.ToArray());
                                break;
                            }
                        case "runtime":
                            {
                                Console.WriteLine(BasicFunctions.RunTime(MomentOfStart));
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
                        case "cls":
                        case "clear":
                            {
                                Console.Clear();
                                break;
                            }
                        case "userManagement":
                            {
                                UserManagementApp.Run(CurrentUser, UsersConf);
                                break;
                            }
                        case "blyatgames":
                            {
                                BlyatgamesApp.Run(Rand);
                                break;
                            }
                        case "lock":
                            {
                                Console.WriteLine("Logging out...");
                                Global.PIT.Wait(1000);
                                Logged_In = false;
                                break;
                            }
                        case "exit":
                            {
                                Console.WriteLine("exitting");
                                Global.PIT.Wait(1000);
                                Cosmos.System.Power.Shutdown();
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
                                if (commandArgs.Count == 0)
                                {
                                    throw new GenericException("Usage: mkdir <name>");
                                }
                                var name = commandArgs[0];
                                var path = PathCombine(CurrentDirectory, name);
                                Directory.CreateDirectory(path);
                                Console.WriteLine($"Directory '{name}' created at '{path}'");
                                break;
                            }

                        case "touch":
                            {
                                if (commandArgs.Count == 0)
                                {
                                    throw new GenericException("Usage: touch <name>");
                                }
                                var name = commandArgs[0];

                                if (!name.Contains('.'))
                                {
                                    name += ".blyat";
                                }
                                if (name.EndsWith("."))
                                {
                                    name += "blyat";
                                }
                                var path = CurrentDirectory + name;//PathCombine(CurrentDirectory, name);
                                if (File.Exists(path))
                                {
                                    throw new GenericException($"File '{name}' already exists at '{path}'");
                                }
                                fs.CreateFile(path);
                                Console.WriteLine($"File '{name}' created at '{path}'");
                                break;
                            }
                        case "cd":
                            {
                                if (commandArgs.Count == 0)
                                {
                                    throw new GenericException("Usage: cd <directory>|..");
                                }
                                var target = commandArgs[0];
                                
                                if (target == "..")
                                {
                                    if (IsRoot(CurrentDirectory, RootPath))
                                    {
                                        throw new GenericException("Already at root");
                                    }
                                    CurrentDirectory = GetParent(CurrentDirectory, RootPath);
                                    Console.WriteLine($"Changed directory to '{CurrentDirectory}'");
                                }
                                else
                                {
                                    string newPath = IsAbsolute(target) ? EnsureTrailingSlash(target) : PathCombine(CurrentDirectory, target);
                                    if (!fsh.DirectoryExists(newPath))
                                    {
                                        throw new GenericException($"Directory not found: {newPath}");
                                    }
                                    CurrentDirectory = EnsureTrailingSlash(newPath);
                                    Console.WriteLine($"Changed directory to '{CurrentDirectory}'");
                                }
                                break;
                            }
                        case "delfile":
                            {
                                if (commandArgs.Count == 0)
                                {
                                    throw new GenericException("Usage: delfile <filename>");
                                }

                                var name = commandArgs[0];
                                var path = IsAbsolute(name) ? name : PathCombine(CurrentDirectory, name);
                                if (!File.Exists(path))
                                {
                                    throw new GenericException($"File not found: {path}");
                                }
                                File.Delete(path);
                                Console.WriteLine($"File '{name}' deleted from '{path}'");
                                break;
                            }
                        case "pwd":
                            Console.WriteLine(CurrentDirectory);
                            break;

                        case "findkusche":
                            {
                                Console.WriteLine($"=== Searching for  {args[1]}===");
                                Console.WriteLine();

                                var disks = fs.Disks;
                                foreach (var disk in disks)
                                {
                                    if (disk?.Host == null) continue;

                                    Console.WriteLine($"Disk Type: {fsh.BlockDeviceTypeToString(disk.Host.Type)}");

                                    foreach (var partition in disk.Partitions)
                                    {
                                        Console.WriteLine($"  Checking: {partition.RootPath}");
                                        SearchFileRecursive(partition.RootPath, args[1]);
                                    }
                                }

                                Console.WriteLine();
                                break;
                            }
                        case "cat":
                        case "readfile":
                            {
                                if (commandArgs.Count == 0)
                                {
                                    throw new GenericException("Usage: readfile <path>");
                                }

                                string path = IsAbsolute(commandArgs[0])
                                    ? commandArgs[0]
                                    : PathCombine(CurrentDirectory, commandArgs[0]).TrimEnd('\\');

                                if (!File.Exists(path))
                                {
                                    throw new GenericException($"File not found: {path}");
                                }

                                if (path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                                {
                                    ReadDisplay.DisplayBitmap(path);
                                }
                                else
                                {
                                    // For small files, read all at once
                                    if (new FileInfo(path).Length < 1024 * 1024) // 1MB
                                    {
                                        Console.WriteLine(ReadDisplay.ReadTextFile(path));
                                    }
                                    else
                                    {
                                        // For larger files, read in chunks
                                        ReadDisplay.ReadTextFileInChunks(path);
                                    }
                                }
                                break;
                            }

                        default:
                            throw new GenericException($"Unknown command '{command}'! Type \"help\" for help or \"exit\" to return!");
                    }
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
                Logged_In = true;
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
}
