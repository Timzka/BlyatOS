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

}