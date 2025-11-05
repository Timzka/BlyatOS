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

    // Display settings are now managed in DisplaySettings.cs

    Sys.FileSystem.CosmosVFS fs;
    FileSystemHelpers fsh = new FileSystemHelpers();

    public const string RootPath = @"0:\";
    public const string SYSTEMPATH = RootPath + @"BlyatOS\"; //path where system files are stored, inaccessable to ALL users via normal commands
    private string CurrentDirectory = RootPath;



    private bool LOCKED; //if system isnt complete, lock system, make user run an INIT command

    public static void InitializeGraphics()
    {
        DisplaySettings.InitializeGraphics();
    }

    protected override void BeforeRun()
    {
        // Initialize display settings and graphics
        DisplaySettings.ScreenWidth = 640;
        DisplaySettings.ScreenHeight = 480;
        Global.PIT.Wait(10);
        InitializeGraphics();
        fs = new Sys.FileSystem.CosmosVFS();
        Sys.FileSystem.VFS.VFSManager.RegisterVFS(fs);
        LOCKED = !InitSystem.IsSystemCompleted(SYSTEMPATH, fs);
        Global.PIT.Wait(10);

        //OnStartUp.RunLoadingScreenThing();
        Global.PIT.Wait(1);
        StartupScreen.Show();

        Sys.KeyboardManager.SetKeyLayout(new DE_Standard());

        // Clear the console and display welcome message
        ConsoleHelpers.ClearConsole();
        ConsoleHelpers.WriteLine($"BlyatOS v{VersionInfo}", Color.Cyan);
        ConsoleHelpers.WriteLine("Type 'help' for a list of commands\n", Color.White);

        MomentOfStart = DateTime.Now;
        Global.PIT.Wait(500);
    }

    protected override void Run() // && to chain commands --> if(&&) do the switch again
    {
        try
        {
            // Handle login if not already logged in

            if (!Logged_In)
            {
                HandleLogin();
            }

            if (LOCKED)
            {
                ConsoleHelpers.WriteLine("WARNING: System is locked!", Color.Red);
                ConsoleHelpers.WriteLine("Some system files are missing or corrupted.", Color.Yellow);
                ConsoleHelpers.WriteLine("Running system initialization...\n", Color.White);

                // TODO: Uncomment and implement actual system initialization
                //if (InitSystem.InitSystemData(SYSTEMPATH, fs))
                //{
                //    ConsoleHelpers.WriteLine("System initialized successfully!", Color.Green);
                //    ConsoleHelpers.WriteLine("Press any key to reboot...", Color.White);
                //    ConsoleHelpers.ReadKey();
                //    Cosmos.System.Power.Reboot();
                //}
                LOCKED = false;
                Global.PIT.Wait(1000);
            }

            // Verzeichnislisten immer aktuell holen
            string[] dirs = fsh.GetDirectories(CurrentDirectory);
            string[] files = fsh.GetFiles(CurrentDirectory);

            // Display current directory and prompt
            string prompt = $"input> ";
            var input = ConsoleHelpers.ReadLine(prompt);

            if (string.IsNullOrWhiteSpace(input))
                return;
            string[] args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Process commands, handling && chaining
            for (int i = 0; i < args.Length;)
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
                            ConsoleHelpers.ClearConsole();
                            BasicFunctions.Help(page, BasicFunctions.ListType.Main);
                            break;
                        }
                    case "changecolor":
                        {
                            DisplaySettings.ChangeColorSet();
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
                                ConsoleHelpers.WriteLine("System initialized. Press any key to reboot");
                                ConsoleHelpers.ReadKey();
                                Cosmos.System.Power.Reboot();
                            }
                            break;
                        }
                    case "version":
                    case "v":
                        {
                            ConsoleHelpers.WriteLine("Blyat version " + VersionInfo);
                            break;
                        }
                    case "echo":
                        {
                            BasicFunctions.EchoFunction(commandArgs.ToArray());
                            break;
                        }
                    case "runtime":
                        {
                            ConsoleHelpers.WriteLine(BasicFunctions.RunTime(MomentOfStart));
                            break;
                        }
                    case "reboot":
                        {
                            ConsoleHelpers.WriteLine("rebooting");
                            Global.PIT.Wait(1000);
                            Cosmos.System.Power.Reboot();
                            break;
                        }
                    case "clearScreen":
                    case "cls":
                    case "clear":
                        {
                            ConsoleHelpers.ClearConsole();
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
                            ConsoleHelpers.WriteLine("Logging out...");
                            Global.PIT.Wait(1000);
                            Logged_In = false;
                            break;
                        }
                    case "exit":
                        {
                            ConsoleHelpers.WriteLine("exitting");
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
                                    ConsoleHelpers.WriteLine("Disk host unavailable");
                                    continue;
                                }

                                ConsoleHelpers.WriteLine(fsh.BlockDeviceTypeToString(disk.Host.Type));

                                foreach (var partition in disk.Partitions)
                                {
                                    ConsoleHelpers.WriteLine($"{partition.Host}");
                                    ConsoleHelpers.WriteLine(partition.RootPath);
                                    ConsoleHelpers.WriteLine($"{partition.MountedFS.Size}");
                                }
                            }
                            break;
                        }

                    case "dir":
                        {
                            foreach (var d in dirs)
                            {
                                ConsoleHelpers.WriteLine(TrimPath(d) + "/");
                            }
                            break;
                        }

                    case "ls":
                        {
                            const string dirMarker = "[D]";
                            const string fileMarker = "[F]";
                            foreach (var d in dirs)
                            {
                                ConsoleHelpers.WriteLine($"{dirMarker} {TrimPath(d)}/", Color.Cyan);
                            }
                            foreach (var f in files)
                            {
                                ConsoleHelpers.WriteLine($"{fileMarker} {TrimPath(f)}", Color.Gray);
                            }
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
                            ConsoleHelpers.WriteLine($"Directory '{name}' created at '{path}'");
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
                            ConsoleHelpers.WriteLine($"File '{name}' created at '{path}'");
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
                                ConsoleHelpers.WriteLine($"Changed directory to '{CurrentDirectory}'");
                            }
                            else
                            {
                                string newPath = IsAbsolute(target) ? EnsureTrailingSlash(target) : PathCombine(CurrentDirectory, target);
                                if (!fsh.DirectoryExists(newPath))
                                {
                                    throw new GenericException($"Directory not found: {newPath}");
                                }
                                CurrentDirectory = EnsureTrailingSlash(newPath);
                                ConsoleHelpers.WriteLine($"Changed directory to '{CurrentDirectory}'");
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
                            ConsoleHelpers.WriteLine($"File '{name}' deleted from '{path}'");
                            break;
                        }
                    case "pwd":
                        ConsoleHelpers.WriteLine(CurrentDirectory);
                        break;

                    case "findkusche":
                        {
                            ConsoleHelpers.ClearConsole();
                            ConsoleHelpers.WriteLine($"=== Searching for  {args[1]}===");
                            ConsoleHelpers.WriteLine();

                            var disks = fs.Disks;
                            foreach (var disk in disks)
                            {
                                if (disk?.Host == null) continue;

                                ConsoleHelpers.WriteLine($"Disk Type: {fsh.BlockDeviceTypeToString(disk.Host.Type)}");

                                foreach (var partition in disk.Partitions)
                                {
                                    ConsoleHelpers.WriteLine($"  Checking: {partition.RootPath}");
                                    SearchFileRecursive(partition.RootPath, args[1]);
                                }
                            }

                            ConsoleHelpers.WriteLine();
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
                                    ConsoleHelpers.WriteLine(ReadDisplay.ReadTextFile(path));
                                }
                                else
                                {
                                    // For larger files, read in chunks
                                    ReadDisplay.ReadTextFileInChunks(path);
                                }
                            }
                            break;
                        }
                    case "write":
                        {
                            if (commandArgs.Count < 3)
                                throw new GenericException("Usage: write <mode> <filename> <content>");

                            string modeToken = commandArgs[0].ToLower();
                            string filename = commandArgs[1];
                            string content = string.Join(" ", commandArgs.Skip(2));
                            if (content[0] == '"' && content[^1] == '"')
                            {
                                content = content[1..^1];
                            }
                            else if (content.Contains(" "))
                            {
                                throw new GenericException("Usage: write <mode> <filename> <content>");
                            }
                            string mode = modeToken switch
                            {
                                "append" => "append",
                                "add" => "append",
                                "overwrite" => "overwrite",
                                "ovr" => "overwrite",
                                _ => throw new GenericException($"Unknown write mode '{modeToken}'. Use append|add or overwrite|ovr.")
                            };

                            // Build absolute path (avoid trailing slash)
                            string path = IsAbsolute(filename) ? filename : PathCombine(CurrentDirectory, filename);
                            if (path.EndsWith("\\") || path.EndsWith("/"))
                                path = path.TrimEnd('\\', '/');

                            try
                            {
                                // Ensure file exists
                                if (!VFSManager.FileExists(path))
                                {
                                    VFSManager.CreateFile(path);
                                }

                                var vfsFile = VFSManager.GetFile(path);
                                var stream = vfsFile.GetFileStream();

                                if (!stream.CanWrite)
                                    throw new GenericException($"Stream not writable for '{path}'", "write", "filesystem");
                                string toWrite = ConsoleHelpers.ProcessEscapeSequences(content);
                                byte[] bytes = Encoding.ASCII.GetBytes(toWrite);

                                if (mode == "overwrite")
                                {
                                    // Truncate: recreate simple by setting Position=0 and (if supported) Length=0.
                                    // If Length set is not implemented, delete & recreate.
                                    try
                                    {
                                        stream.Position = 0;
                                        // Some Cosmos versions allow setting length:
                                        if (stream.CanSeek)
                                        {
                                            // Attempt truncate by writing zero length (if SetLength exists)
                                            if (stream.Length > 0)
                                            {
                                                // If SetLength not available, fall back to delete-recreate
                                                bool setLengthWorked = true;
                                                try { stream.SetLength(0); }
                                                catch { setLengthWorked = false; }
                                                if (!setLengthWorked)
                                                {
                                                    stream.Close();
                                                    VFSManager.DeleteFile(path);
                                                    VFSManager.CreateFile(path);
                                                    vfsFile = VFSManager.GetFile(path);
                                                    stream = vfsFile.GetFileStream();
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        stream.Close();
                                        VFSManager.DeleteFile(path);
                                        VFSManager.CreateFile(path);
                                        vfsFile = VFSManager.GetFile(path);
                                        stream = vfsFile.GetFileStream();
                                    }

                                    stream.Write(bytes, 0, bytes.Length);
                                }
                                else // append
                                {
                                    if (stream.CanSeek)
                                    {
                                        stream.Seek(0, SeekOrigin.End);
                                        stream.Write(bytes, 0, bytes.Length);
                                    }
                                    else
                                    {
                                        // Fallback: read existing, then rewrite whole file
                                        byte[] existing;
                                        if (stream.CanRead)
                                        {
                                            stream.Position = 0;
                                            existing = new byte[stream.Length];
                                            stream.Read(existing, 0, existing.Length);
                                        }
                                        else
                                        {
                                            existing = Array.Empty<byte>();
                                        }

                                        byte[] combined = new byte[existing.Length + bytes.Length];
                                        existing.CopyTo(combined, 0);
                                        bytes.CopyTo(combined, existing.Length);

                                        stream.Close();
                                        VFSManager.DeleteFile(path);
                                        VFSManager.CreateFile(path);
                                        vfsFile = VFSManager.GetFile(path);
                                        stream = vfsFile.GetFileStream();
                                        stream.Write(combined, 0, combined.Length);
                                    }
                                }

                                stream.Close();

                                ConsoleHelpers.WriteLine(mode == "append"
                                    ? $"Appended {bytes.Length} bytes to '{filename}'"
                                    : $"Overwrote '{filename}' with {bytes.Length} bytes");
                            }
                            catch (GenericException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                throw new GenericException($"Write failed: {ex.Message}", "write", "filesystem");
                            }

                            break;
                        }
                        case "neofetch":
                            {
                                Neofetch.Show(VersionInfo, MomentOfStart, UsersConf, CurrentUser, CurrentDirectory, fs);
                                break;
                            }

                    default:
                        throw new GenericException($"Unknown command '{command}'! Type \"help\" for help or \"exit\" to return!");
                }
            }

        }
        catch (GenericException ex)
        {
            ConsoleHelpers.WriteLine();
            ConsoleHelpers.WriteLine($"Message: {ex.EMessage}" + (ex.Label != "" ? $",\nLabel: {ex.Label}" : "") + (ex.ComesFrom != "" ? $",\nSource: {ex.ComesFrom}" : ""));
            ConsoleHelpers.WriteLine();
        }
        catch (Exception ex)
        {
            ConsoleHelpers.WriteLine("An error occured: " + ex.Message);
        }
    }

    private void HandleLogin()
    {
        if (Logged_In) return;

        ConsoleHelpers.ClearConsole();
        ConsoleHelpers.WriteLine("=== BlyatOS Login ===\n", Color.Cyan);

        while (!Logged_In)
        {
            string username = ConsoleHelpers.ReadLine("Username: ", Color.White);
            string password = ConsoleHelpers.ReadPassword("Password: ", Color.White, '•');

            // Try to find user
            var user = UsersConf.Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user != null && user.Password == password) // In a real system, use proper password hashing!
            {
                CurrentUser = UsersConf.Users.IndexOf(user);
                Logged_In = true;
                ConsoleHelpers.WriteLine("\nLogin successful!\n", Color.Green);
                Global.PIT.Wait(500);
            }
            else
            {
                ConsoleHelpers.WriteLine("\nInvalid username or password.\n", Color.Red);
                Global.PIT.Wait(1000);
            }
        }
        ConsoleHelpers.ClearConsole();
    }
}
