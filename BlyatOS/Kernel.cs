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

    private const string RootPath = @"0:\";
    private string CurrentDirectory = RootPath; 

    protected override void BeforeRun()
    {
        Encoding.RegisterProvider(CosmosEncodingProvider.Instance); //diese 3 zeilen sind für die codepage 437 (box drawing chars), so könnte man theoretisch eine
        Console.InputEncoding = Encoding.GetEncoding(437); //funktion bauen, die UTF8 unterstützt
        Console.OutputEncoding = Encoding.GetEncoding(437);
        OnStartUp.RunLoadingScreenThing();
        fs = new Sys.FileSystem.CosmosVFS();
        Sys.FileSystem.VFS.VFSManager.RegisterVFS(fs);
        Sys.KeyboardManager.SetKeyLayout(new DE_Standard());
        Console.WriteLine($"BlyatOS v{VersionInfo} booted successfully. Type help for a list of valid commands");
        MomentOfStart = DateTime.Now;

        Global.PIT.Wait(1000);
    }

    protected override void Run()
    {
        try
        {
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
                switch (args[0])
                {
                    case "help":
                        {
                            int? page = null;
                            if (args.Length > 1 && int.TryParse(args[1], out int p)) page = p;
                            Console.Clear();
                            BasicFunctions.Help(page, BasicFunctions.ListType.Main);
                            break;
                        }
                    case "version":
                        {
                            Console.WriteLine("Blyat version " + VersionInfo);
                            break;
                        }
                    case "echo":
                        {
                            BasicFunctions.EchoFunction(args);
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
                    case "loadkusche":
                        {

                            byte[] data = File.ReadAllBytes(@"0:\kusche256.raw");

                            Console.WriteLine("Dateigröße: " + data.Length + " Bytes");
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
                            if (args.Length < 2)
                            {
                                Console.WriteLine("Usage: mkdir <name>");
                                break;
                            }
                            var name = args[1];
                            var path = PathCombine(CurrentDirectory, name);
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
                            var path = CurrentDirectory + name;//PathCombine(CurrentDirectory, name);
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
                            string path = IsAbsolute(fileArg) ? fileArg : PathCombine(CurrentDirectory, fileArg).TrimEnd('\\');

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
                                if (IsRoot(CurrentDirectory, RootPath))
                                {
                                    Console.WriteLine("Already at root");
                                    break;
                                }
                                CurrentDirectory = GetParent(CurrentDirectory, RootPath);
                                Console.WriteLine($"Changed directory to '{CurrentDirectory}'");
                                break;
                            }

                            string newPath = IsAbsolute(target) ? EnsureTrailingSlash(target) : PathCombine(CurrentDirectory, target);
                            if (!fsh.DirectoryExists(newPath))
                            {
                                Console.WriteLine($"Directory not found: {newPath}");
                                break;
                            }
                            CurrentDirectory = EnsureTrailingSlash(newPath);
                            Console.WriteLine($"Changed directory to '{CurrentDirectory}'");
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
                    case "readfile":
                        {
                            if (args.Length < 2)
                            {
                                Console.WriteLine("Usage: readfile <path>");
                                break;
                            }

                            string path = IsAbsolute(args[1])
                                ? args[1]
                                : PathCombine(CurrentDirectory, args[1]).TrimEnd('\\');

                            if (!File.Exists(path))
                            {
                                Console.WriteLine($"File not found: {path}");
                                break;
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
                        Console.WriteLine("Unknown command! Type \"help\" for help or \"exit\" to return!");
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
