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
using Cosmos.System.FileSystem;

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
                foreach (var dir in dirs)
                {
                    Console.WriteLine(dir);
                }
                Console.WriteLine();
                foreach (var file in files)
                {
                    Console.WriteLine(file);
                }
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
                    case "pwd":
                        Console.WriteLine(CurrentDirectory);
                        break;

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