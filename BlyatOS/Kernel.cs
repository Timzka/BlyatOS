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
                            var path = PathCombine(CurrentDirectory, name);
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

                    case "loadiso":
                        {
                            Console.WriteLine("=== Testing direct ISO access ===");
                            try
                            {
                                // Versuche kusche256.raw direkt von der ISO zu laden
                                var data = BlyatOS.Library.Helpers.ISO9660Reader.LoadFileFromISO("kusche256.raw");
                                
                                if (data != null && data.Length > 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"SUCCESS! Loaded {data.Length} bytes from ISO!");
                                    Console.ResetColor();
                                    
                                    // Optinal: Schreibe ins VFS
                                    Console.WriteLine("Schreibe nach 0:\\kusche256.raw...");
                                    File.WriteAllBytes(@"0:\kusche256.raw", data);
                                    Console.WriteLine("Fertig! Datei ist jetzt im VFS verfügbar.");
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("FAILED: Could not load file from ISO");
                                    Console.ResetColor();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error: {ex.Message}");
                            }
                            break;
                        }

                    case "mountcd":
                        {
                            Console.WriteLine("Attempting to mount CD-ROM...");
                            try
                            {
                                var disks = fs.Disks;
                                foreach (var disk in disks)
                                {
                                    if (disk?.Host == null) continue;
                                    
                                    if (disk.Host.Type == Cosmos.HAL.BlockDevice.BlockDeviceType.RemovableCD)
                                    {
                                        Console.WriteLine("Found CD-ROM drive, mounting...");
                                        disk.Mount();
                                        Console.WriteLine("Mount attempted. Run 'findkusche' to check.");
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error: {ex.Message}");
                            }
                            break;
                        }

                    case "findkusche":
                        {
                            Console.WriteLine("=== Searching for kusche256.raw ===");
                            Console.WriteLine();
                            
                            // Zeige alle verfügbaren Laufwerke
                            Console.WriteLine("Available drives:");
                            var disks = fs.Disks;
                            foreach (var disk in disks)
                            {
                                if (disk?.Host == null) continue;
                                
                                Console.WriteLine($"  Type: {fsh.BlockDeviceTypeToString(disk.Host.Type)}");
                                foreach (var partition in disk.Partitions)
                                {
                                    Console.WriteLine($"    Drive: {partition.RootPath}");
                                    
                                    // Prüfe auf kusche256.raw in diesem Laufwerk
                                    try
                                    {
                                        string[] searchPaths = new[] { "", "isoFiles\\" };
                                        foreach (var subPath in searchPaths)
                                        {
                                            string testPath = Path.Combine(partition.RootPath, subPath, "kusche256.raw");
                                            if (File.Exists(testPath))
                                            {
                                                FileInfo fi = new FileInfo(testPath);
                                                Console.ForegroundColor = ConsoleColor.Green;
                                                Console.WriteLine($"      FOUND: {testPath} ({fi.Length} bytes)");
                                                Console.ResetColor();
                                            }
                                            else
                                            {
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine(" Not found in: " + Path.Combine(partition.RootPath, subPath));
                                                Console.ResetColor();
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                            Console.WriteLine();
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