using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BlyatOS.Library.Configs;
using Cosmos.HAL;
using static BlyatOS.Library.Configs.UsersConfig;

namespace BlyatOS.Library.Functions;

internal class UserManagement
{
    public static int Login(UsersConfig conf)
    {
        Console.WriteLine("Username: ");
        string name = Console.ReadLine();
        Console.WriteLine("Password: ");
        string password = Console.ReadLine();

        var user = conf.Users.FirstOrDefault(u => u.Username == name && u.Password == password);
        if (user != null)
        {
            Console.WriteLine($"Login successful. Welcome, {user.Username}!");
            Global.PIT.Wait(1000);
            return user.UId;
        }
        else
        {
            Console.WriteLine("Login failed go to gulag. Invalid username or password.");
            return -1; 
        }
    }

    public static bool CheckPermissions(int userId, UsersConfig usersConfig, UsersConfig.URoles[] requiredPermissions)
    {
        var user = usersConfig.Users.FirstOrDefault(u => u.UId == userId);
        if (user == null) return false;

        return requiredPermissions.Any(r => r == user.Role);
    }

    public static int[] GetValidRoles(int userId, UsersConfig usersConfig) //Sagt, welche Rollen(int werte) im CreateUsers möglich sind für den jeweiligen Account
    {
        List<int> validRoles = new();

        if (CheckPermissions(userId, usersConfig, Permissions.SuperAdmin))
        {
            validRoles.Add((int)URoles.User);
            validRoles.Add((int)URoles.Admin);
        }
        else if (CheckPermissions(userId, usersConfig, Permissions.Admin))
        {
            validRoles.Add((int)URoles.User);
        }
        return validRoles.ToArray();
    }
    public static void CreateUser(UsersConfig conf, int currUser) //Kreiert einen Nutzer
    {
        Console.WriteLine("Enter username: ");
        string uName = Console.ReadLine();
        if(conf.Users.Any(u => u.Username == uName))
        {
            Console.WriteLine("Users may not have the same Username!");
            return;
        }
        Console.WriteLine("Enter password: ");
        string password = Console.ReadLine(); 
        Console.WriteLine($"Enter role (0 - User{(CheckPermissions(currUser, conf, Permissions.SuperAdmin) ? ", 1 - Admin" : "")}): ");
        string roleInput = Console.ReadLine();
        int[] validRolesToCreate = GetValidRoles(currUser,conf);
        if (!int.TryParse(roleInput, out int roleInt))
        {
            Console.WriteLine("You cannot delete yourself.");
            return;
        }

        if (!validRolesToCreate.Contains(roleInt))
        {
            Console.WriteLine("Invalid role");
            return;
        }
        UsersConfig.URoles role = (UsersConfig.URoles)roleInt;
        var newId = 1;
        while (conf.Users.Any(u => u.UId == newId))
        {
            newId++;
        }
        conf.Users.Add(new UsersConfig.User(uName, newId, password, role));
        Console.WriteLine($"User {uName} created with ID {newId} and role {RoleToString(role)}"); //role.Tostring geht nicht
    }

    static string RoleToString(URoles role)
    {
        switch (role)
        {
            case URoles.Admin: return "Admin";
            case URoles.User: return "User";
            case URoles.SuperAdmin: return "SuperAdmin";
            default: return "Unknown";
        }
    }
}
