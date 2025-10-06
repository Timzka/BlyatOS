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

namespace BlyatOS;

public class Kernel : Sys.Kernel
{
    DateTime momentOfStart;
    string versionString = "Blyat version 0.9";
    private UsersConfig UsersConf = new UsersConfig();
    private int CurrentUser;
    protected override void BeforeRun()
    {
        Console.Clear();
        int c = 0;
        while (true)
        {
            if(c >= 5) Cosmos.System.Power.Shutdown();
            int uid = UserManagement.Login(UsersConf);
            if (uid != -1)
            {
                CurrentUser = uid;
                break;
            }
            c++;
        }
        //OnStartUp.RunLoadingScreenThing(); //could be removed, but it is cool
        Console.WriteLine("BlyatOS booted successfully. Type /help for help");
        momentOfStart = DateTime.Now;
    }

    protected override void Run()
    {
        Console.Write("Input: ");
        var input = Console.ReadLine();

        string[] args = input.Split(' ');

        switch (args[0])
        {
            case "help":
                {
                    BasicFunctions.Help();
                    break;
                }
            case "version":
                {
                    Console.WriteLine(versionString);
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
            case "reboot":
                {
                    Console.WriteLine("rebooting");
                    Global.PIT.Wait(1000);
                    Cosmos.System.Power.Reboot();
                    break;
                }
            case "exit":
                {
                    Console.WriteLine("exitting");
                    Global.PIT.Wait(1000);
                    Cosmos.System.Power.Shutdown();
                    break;
                }
            case "cls":
                {
                    Console.Clear();
                    break;
                }
            case "logout":
            case "lock":
                {
                    Console.WriteLine("Logging out...");
                    Global.PIT.Wait(1000);
                    int c = 0;
                    while (true)
                    {
                        if (c >= 5) Cosmos.System.Power.Shutdown();
                        int uid = UserManagement.Login(UsersConf);
                        if (uid != -1)
                        {
                            CurrentUser = uid;
                            break;
                        }
                        c++;
                    }
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
                    // Admins and SuperAdmins allowed (SuperAdmin may also create Admins)
                    var allowed = UsersConfig.Permissions.Admin; // includes Admin + SuperAdmin
                    if (!UserManagement.CheckPermissions(CurrentUser, UsersConf, allowed))
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
                    // Admins and SuperAdmins allowed (rules enforced inside method)
                    var allowed = UsersConfig.Permissions.Admin;
                    if (!UserManagement.CheckPermissions(CurrentUser, UsersConf, allowed))
                    {
                        Console.WriteLine("Missing Permissions");
                        break;
                    }
                    UserManagement.DeleteUser(UsersConf, CurrentUser);
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
}
