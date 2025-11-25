using System;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Functions;
using BlyatOS.Library.Helpers;

namespace BlyatOS;

public class UserManagementApp
{
    //return 1 if user should re-login after the command, return 0 if not
    public static int Run(int currentUser, UsersConfig usersConf, PasswordPolicy policy)
    {
        bool exitUserManager = false;
        ConsoleHelpers.ClearConsole();
        do
        {
            ConsoleHelpers.WriteLine("You are now in UserManagement, write \"mainMenu\" to go back or \"help\" for available commands");
            ConsoleHelpers.Write("UserManagement> ");

            var userInput = ConsoleHelpers.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            string[] arr = userInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (arr[0])
            {
                case "vodka":
                    {
                        if (!UserManagement.CheckPermissions(currentUser, usersConf, UsersConfig.Permissions.Admin))
                        {
                            ConsoleHelpers.WriteLine("Missing Permissions");
                            break;
                        }
                        ConsoleHelpers.WriteLine("Nyet, no vodka for you! //not implemented");
                        break;
                    }
                case "createUser":
                    {
                        Console.Clear();
                        if (!UserManagement.CheckPermissions(currentUser, usersConf, UsersConfig.Permissions.Admin))
                        {
                            ConsoleHelpers.WriteLine("Missing Permissions");
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
                            ConsoleHelpers.WriteLine("Missing Permissions");
                            break;
                        }
                        UserManagement.DeleteUser(usersConf, currentUser);
                        break;
                    }
                case "changePassword":
                    {
                        if (UserManagement.ChangePassword(currentUser, usersConf, policy) == true) //true if password changed
                            return 1;
                        else break;
                    }
                case "checkPolicy":
                    {
                        if (!UserManagement.CheckPermissions(currentUser, usersConf, UsersConfig.Permissions.Admin))
                        {
                            ConsoleHelpers.WriteLine("Missing Permissions");
                            break;
                        }
                        policy.WritePolicy();
                        break;
                    }
                case "changePolicy":
                    {
                        if (!UserManagement.CheckPermissions(currentUser, usersConf, UsersConfig.Permissions.Admin))
                        {
                            Console.WriteLine("Missing Permissions");
                            break;
                        }
                        policy.SetPolicy();
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
                        ConsoleHelpers.WriteLine("Unknown command! Type \"help\" for help or \"exit\" to return!");
                        ConsoleHelpers.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        Console.Clear();
                        break;
                    }
            }
        } while (!exitUserManager);
        return 0;
    }
}
