using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Functions;
using BlyatOS.Library.Helpers;
using BlyatOS.Library.Ressources;
using Cosmos.Core.Memory;
using Cosmos.HAL;
using Cosmos.System.Graphics;
using static BlyatOS.PathHelpers;

namespace BlyatOS;

public class KernelParser
{
    private Kernel kernel;

    public KernelParser(Kernel kernel)
    {
        this.kernel = kernel;
    }

    public void HandleCommand(string input, ref string[] dirs, ref string[] files)
    {
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
                case "getdriver":
                    {
                        AudioHandler.GetDriverInfo();
                        break;
                    }
                case "hardbass":
                    {
                        AudioHandler.Play(Audio.NarkotikKal);
                        break;
                    }
                case "showbmpbig": //temp --> way to resize bitmaps easily
                    {
                        if(commandArgs.Count != 3)
                            throw new GenericException("Usage: showbmpbig <path> <width> <height>");
                        if(!uint.TryParse(commandArgs[1], out _) || !uint.TryParse(commandArgs[2], out _))
                            throw new GenericException("Width and Height must be valid unsigned integers.");
                        string path = IsAbsolute(commandArgs[0])
                            ? commandArgs[0]
                            : PathCombine(kernel.CurrentDirectory, commandArgs[0]).TrimEnd('\\');

                        var data = DisplaySettings.ResizeBitmap(ImageHelpers.LoadBMP(path), uint.Parse(commandArgs[1]), uint.Parse(commandArgs[2]), true);
                        var bmp = new Bitmap(uint.Parse(commandArgs[1]), uint.Parse(commandArgs[2]), ColorDepth.ColorDepth32);
                        bmp.RawData = data;
                        DisplaySettings.Canvas.DrawImage(bmp, 0, 0);
                        DisplaySettings.Canvas.Display();
                        ConsoleHelpers.ReadKey();
                        ConsoleHelpers.ClearConsole();
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
                        FileFunctions.DeleteDirectory(kernel.CurrentDirectory, commandArgs[0]);
                        break;
                    }
                case "initsystem":
                    {
                        if (InitSystem.InitSystemData(Kernel.SYSTEMPATH, kernel.fs))
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
                        ConsoleHelpers.WriteLine("Blyat version " + kernel.VersionInfo);
                        break;
                    }
                case "echo":
                    {
                        BasicFunctions.EchoFunction(commandArgs.ToArray());
                        break;
                    }
                case "runtime":
                    {
                        ConsoleHelpers.WriteLine(BasicFunctions.RunTime(kernel.MomentOfStart));
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
                        int result = UserManagementApp.Run(kernel.CurrentUser, kernel.UsersConf, kernel.Policy);
                        if(result == 1)
                            kernel.Logged_In = false; // Force re-login if needed
                        break;
                    }
                case "blyatgames":
                    {
                        BlyatgamesApp.Run(kernel.Rand);
                        break;
                    }
                case "lock":
                case "logout":
                    {
                        ConsoleHelpers.WriteLine("Logging out...");
                        Global.PIT.Wait(1000);
                        kernel.Logged_In = false;
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
                        FileFunctions.FsInfo(kernel.fs, kernel.fsh);
                        break;
                    }

                case "dir":
                    {
                        // Lazy load directory listings only when needed
                        if (dirs == null) dirs = kernel.fsh.GetDirectories(kernel.CurrentDirectory);

                        FileFunctions.ListDirectories(dirs);
                        break;
                    }

                case "ls":
                    {
                        // Lazy load directory listings only when needed
                        if (dirs == null) dirs = kernel.fsh.GetDirectories(kernel.CurrentDirectory);
                        if (files == null) files = kernel.fsh.GetFiles(kernel.CurrentDirectory);

                        FileFunctions.ListAll(dirs, files);
                        break;
                    }

                case "mkdir":
                    {
                        if (commandArgs.Count == 0)
                        {
                            throw new GenericException("Usage: mkdir <name>");
                        }
                        FileFunctions.MakeDirectory(kernel.CurrentDirectory, commandArgs[0]);
                        break;
                    }

                case "touch":
                    {
                        if (commandArgs.Count == 0)
                        {
                            throw new GenericException("Usage: touch <name>");
                        }
                        FileFunctions.CreateFile(kernel.CurrentDirectory, commandArgs[0], kernel.fs);
                        break;
                    }
                case "cd":
                    {
                        if (commandArgs.Count == 0)
                        {
                            throw new GenericException("Usage: cd <directory>|..");
                        }
                        kernel.CurrentDirectory = EnsureTrailingSlash(FileFunctions.ChangeDirectory(kernel.CurrentDirectory, Kernel.RootPath, commandArgs[0], kernel.fsh));
                        ConsoleHelpers.WriteLine($"Changed directory to '" + kernel.CurrentDirectory + "'");
                        break;
                    }
                case "cdisk":
                    {
                        kernel.CurrentDirectory = FileFunctions.ChangeRoot(kernel.fsh);
                        ConsoleHelpers.WriteLine("Changed directory to disk: " + kernel.CurrentDirectory);
                        break;
                    }
                case "delfile":
                    {
                        if (commandArgs.Count == 0)
                        {
                            throw new GenericException("Usage: delfile <filename>");
                        }
                        FileFunctions.DeleteFile(kernel.CurrentDirectory, commandArgs[0]);
                        break;
                    }
                case "pwd":
                    {
                        ConsoleHelpers.WriteLine(kernel.CurrentDirectory);
                        break;
                    }

                case "findkusche":
                    {
                        FileFunctions.FindKusche(commandArgs[0], kernel.fs, kernel.fsh);
                        break;
                    }
                case "cat":
                case "readfile":
                    {
                        if (commandArgs.Count == 0)
                        {
                            throw new GenericException("Usage: readfile <path>");
                        }
                        FileFunctions.ReadFile(commandArgs[0], kernel.CurrentDirectory);
                        break;
                    }
                case "write":
                    {
                        if (commandArgs.Count < 3)
                            throw new GenericException("Usage: write <mode> <filename> <content>");
                        FileFunctions.WriteFile(kernel.CurrentDirectory, commandArgs);
                        break;
                    }
                case "neofetch":
                    {
                        Neofetch.Show(kernel.VersionInfo, kernel.MomentOfStart, kernel.UsersConf, kernel.CurrentUser, kernel.CurrentDirectory, kernel.fs);
                        break;
                    }

                default:
                    throw new GenericException($"Unknown command '" + command + "'! Type \"help\" for help or \"exit\" to return!");
            }

            // Clear commandArgs after each command to free memory
            commandArgs.Clear();

            Heap.Collect();
        }
    }
}
