using BlyatOS.Library.Configs;
using BlyatOS.Library.Functions;
using Cosmos.HAL;
using System;

namespace BlyatOS;

public class UserManagementApp
{
    public static void Run(int currentUser, UsersConfig usersConf)
    {
        bool exitUserManager = false;
        Console.Clear();
        do
        {
            Console.WriteLine("You are now in UserManagement, write \"mainMenu\" to go back or \"help\" for available commands");
            Console.Write("Input: ");

            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            string[] arr = userInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (arr[0])
            {
                case "vodka":
                    {
                        if (!UserManagement.CheckPermissions(currentUser, usersConf, UsersConfig.Permissions.Admin))
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
                        if (!UserManagement.CheckPermissions(currentUser, usersConf, UsersConfig.Permissions.Admin))
                        {
                            Console.WriteLine("Missing Permissions");
                            break;
                        }
                        UserManagement.CreateUser(usersConf, currentUser);
                        break;
                    }
                case "deleteUser":
                    {
                        Console.Clear();
                        if (!UserManagement.CheckPermissions(currentUser, usersConf, UsersConfig.Permissions.Admin))
                        {
                            Console.WriteLine("Missing Permissions");
                            break;
                        }
                        UserManagement.DeleteUser(usersConf, currentUser);
                        break;
                    }
                case "help":
                    {
                        Console.Clear();
                        BasicFunctions.Help(null, BasicFunctions.ListType.UserManagement);
                        break;
                    }
                case "mainMenu":
                case "exit":
                    {
                        exitUserManager = true;
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Unknown command! Type \"help\" for help or \"exit\" to return!");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        Console.Clear();
                        break;
                    }
            }
        } while (!exitUserManager); 
    }
}
