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
using Cosmos.Core.Memory;

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



    private bool LOCKED; //if system isnt complete, lock system, make user run INIT

    public static void InitializeGraphics()
    {
        DisplaySettings.InitializeGraphics();
    }

    protected override void BeforeRun()
    {

        // Initialize display settings and graphics1024x768
        DisplaySettings.ScreenWidth = 1024;
        DisplaySettings.ScreenHeight = 768;
        Global.PIT.Wait(10);
        InitializeGraphics();
        fs = new Sys.FileSystem.CosmosVFS();
        Sys.FileSystem.VFS.VFSManager.RegisterVFS(fs);
        LOCKED = !InitSystem.IsSystemCompleted(SYSTEMPATH, fs); if (LOCKED)
        {
            ConsoleHelpers.WriteLine("WARNING: System is locked!", Color.Red);
            ConsoleHelpers.WriteLine("Some system files are missing or corrupted.", Color.Yellow);
            ConsoleHelpers.WriteLine("Running system initialization...\n", Color.White);

            if (InitSystem.InitSystemData(SYSTEMPATH, fs))
            {
                ConsoleHelpers.WriteLine("System initialized successfully!", Color.Green);
                ConsoleHelpers.WriteLine("Press any key to reboot...", Color.White);
                ConsoleHelpers.ReadKey();
                Cosmos.System.Power.Reboot();
            }
            LOCKED = false;
            Global.PIT.Wait(1000);
        }
        Global.PIT.Wait(10);

        OnStartUp.RunLoadingScreenThing();
        Global.PIT.Wait(1);
        StartupScreen.Show();

        Sys.KeyboardManager.SetKeyLayout(new DEStandardLayout());

        // Clear the console and display welcome message
        ConsoleHelpers.ClearConsole();
        ConsoleHelpers.WriteLine("BlyatOS v" + VersionInfo, Color.Cyan);
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
            // Verzeichnislisten nur einmal pro Command-Loop holen
            string[] dirs = null;
            string[] files = null;

            // Display current directory and prompt - avoid string interpolation
            string prompt = CurrentDirectory + "> ";
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
                    case "memtest":
                        {
                            // Lazy load directory listings only when needed
                            if (dirs == null) dirs = fsh.GetDirectories(CurrentDirectory);
                            if (files == null) files = fsh.GetFiles(CurrentDirectory);

                            for (int m = 0; m < 30; m++)
                            {
                                FileFunctions.ListAll(dirs, files);
                            }
                            break;
                        }
                    case "changecolor":
                        {
                            DisplaySettings.ChangeColorSet();
                            break;
                        }
                    case "meminfo":
                        {
                            // Get RAM values (returns uint, not ulong)
                            uint usedRAM = Cosmos.Core.GCImplementation.GetUsedRAM();
                            ulong availableRAM = Cosmos.Core.GCImplementation.GetAvailableRAM();

                            // Use single WriteLine to minimize allocations
                            ConsoleHelpers.Write("Used RAM: ", Color.Cyan);
                            ConsoleHelpers.Write((usedRAM / 1000000).ToString(), Color.White);
                            ConsoleHelpers.Write(" MB / ", Color.Cyan);
                            ConsoleHelpers.Write(availableRAM.ToString(), Color.White);
                            ConsoleHelpers.WriteLine(" MB", Color.Cyan);
                            break;
                        }
                    case "rmdir":
                        {
                            FileFunctions.DeleteDirectory(CurrentDirectory, commandArgs[0]);
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
                            FileFunctions.FsInfo(fs, fsh);
                            break;
                        }

                    case "dir":
                        {
                            // Lazy load directory listings only when needed
                            if (dirs == null) dirs = fsh.GetDirectories(CurrentDirectory);

                            FileFunctions.ListDirectories(dirs);
                            break;
                        }

                    case "ls":
                        {
                            // Lazy load directory listings only when needed
                            if (dirs == null) dirs = fsh.GetDirectories(CurrentDirectory);
                            if (files == null) files = fsh.GetFiles(CurrentDirectory);

                            FileFunctions.ListAll(dirs, files);
                            break;
                        }

                    case "mkdir":
                        {
                            if (commandArgs.Count == 0)
                            {
                                throw new GenericException("Usage: mkdir <name>");
                            }
                            FileFunctions.MakeDirectory(CurrentDirectory, commandArgs[0]);
                            break;
                        }

                    case "touch":
                        {
                            if (commandArgs.Count == 0)
                            {
                                throw new GenericException("Usage: touch <name>");
                            }
                            FileFunctions.CreateFile(CurrentDirectory, commandArgs[0], fs);
                            break;
                        }
                    case "cd":
                        {
                            if (commandArgs.Count == 0)
                            {
                                throw new GenericException("Usage: cd <directory>|..");
                            }
                            CurrentDirectory = EnsureTrailingSlash(FileFunctions.ChangeDirectory(CurrentDirectory, RootPath, commandArgs[0], fsh));
                            ConsoleHelpers.WriteLine($"Changed directory to '" + CurrentDirectory + "'");
                            break;
                        }
                    case "delfile":
                        {
                            if (commandArgs.Count == 0)
                            {
                                throw new GenericException("Usage: delfile <filename>");
                            }
                            FileFunctions.DeleteFile(CurrentDirectory, commandArgs[0]);
                            break;
                        }
                    case "pwd":
                        {
                            ConsoleHelpers.WriteLine(CurrentDirectory);
                            break;
                        }

                    case "findkusche":
                        {
                            FileFunctions.FindKusche(commandArgs[0], fs, fsh);
                            break;
                        }
                    case "cat":
                    case "readfile":
                        {
                            if (commandArgs.Count == 0)
                            {
                                throw new GenericException("Usage: readfile <path>");
                            }
                            FileFunctions.ReadFile(commandArgs[0], CurrentDirectory);
                            break;
                        }
                    case "write":
                        {
                            if (commandArgs.Count < 3)
                                throw new GenericException("Usage: write <mode> <filename> <content>");
                            FileFunctions.WriteFile(CurrentDirectory, commandArgs);
                            break;
                        }
                    case "neofetch":
                        {
                            Neofetch.Show(VersionInfo, MomentOfStart, UsersConf, CurrentUser, CurrentDirectory, fs);
                            break;
                        }

                    default:
                        throw new GenericException($"Unknown command '" + command + "'! Type \"help\" for help or \"exit\" to return!");
                }

                // Clear commandArgs after each command to free memory
                commandArgs.Clear();

                // Aggressive GC after each command (NclearOS 2 pattern)
                Heap.Collect();
            }

            // Explicitly clear arrays to help GC
            dirs = null;
            files = null;
            args = null;
            Heap.Collect();
        }
        catch (GenericException ex)
        {
            ConsoleHelpers.WriteLine();
            ConsoleHelpers.WriteLine($"Message: " + ex.EMessage + (ex.Label != "" ? $",\nLabel: " + ex.Label : "") + (ex.ComesFrom != "" ? $",\nSource: " + ex.ComesFrom : ""));
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
                CurrentUser = user.UId;
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
