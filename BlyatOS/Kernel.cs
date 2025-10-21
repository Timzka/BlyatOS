using System;
using System.Collections.Generic;
using System.Text;
using Cosmos.HAL;
using Sys = Cosmos.System;
using BlyatOS.Library.Startupthings;
using BlyatOS.Library.Functions;
using BadTetrisCS;
using BlyatOS.Library.Configs;
using Microsoft.VisualBasic.FileIO;
using System.Linq;
using System.ComponentModel.Design;
using BlyatOS.Library;

namespace BlyatOS;

public class Kernel : Sys.Kernel
{
    DateTime momentOfStart;
    string versionString = "0.9";
    private UsersConfig UsersConf = new UsersConfig();
    private int CurrentUser;
    bool logged_in = false;
    Random Rand = new Random(DateTime.Now.Millisecond); //universal random so it doesnt need to be set all the time(for functions that need it)
    protected override void BeforeRun()
    {
        OnStartUp.RunLoadingScreenThing(); //could be removed, but it is cool
        //VGACursorFix.EnableDebug();
        Console.WriteLine($"BlyatOS v{versionString} booted successfully. Type help for a list of valid commands");
        momentOfStart = DateTime.Now;
        Global.PIT.Wait(1000);
    }

    protected override void Run()
    {
        if (logged_in)
        {
            //Console.WriteLine("Enter 'help', if you need help");
            //Console.WriteLine("Enter 'basic' to use basic functions");
            //Console.WriteLine("Enter 'userManagment' to get access to user managment");
            //Console.WriteLine("Enter 'tetris' to play a game");
            //Console.WriteLine("Enter 'exit' to exit the OS");
            Console.WriteLine("Input: ");

            var input = Console.ReadLine();

            string[] args = input.Split(' ');

            switch (args[0])
            {
                case "help": 
                    {
                        int? page = null;
                        if (args.Length > 1)
                        {
                            if (int.TryParse(args[1], out int p)) page = p;
                        }
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
                default:
                    {
                        Console.WriteLine("Unknown command! Enter \"help\" for more information!");
                        break;
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
            logged_in = true;
            Console.Clear();
        }
    }
}
