using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Helpers;
using static BlyatOS.Library.Configs.UsersConfig;

namespace BlyatOS.Library.Functions;

internal class UserManagement
{

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

    public static void DeleteUser(UsersConfig conf, int currUser)
    {
        ConsoleHelpers.WriteLine("valid Ids:"); //missing check for Admin to not delete admins
        var current = conf.Users.FirstOrDefault(u => u.UId == currUser);

        var validIds = current.Role switch
        {
            URoles.SuperAdmin => conf.Users.Where(u => u.UId != currUser && u.Role != URoles.SuperAdmin).Select(u => u.UId),
            URoles.Admin => conf.Users.Where(u => u.Role == URoles.User).Select(u => u.UId), //order user ids
            _ => Enumerable.Empty<int>()
        };

        if (validIds.Count() == 0)
        {
            ConsoleHelpers.WriteLine("No valid IDs to delete.");
            return;
        }

        foreach (var id in validIds)
        {
            ConsoleHelpers.WriteLine(id.ToString());
        }

        ConsoleHelpers.WriteLine("Enter the ID of the user you want to delete: ");
        string idInput = ConsoleHelpers.ReadLine();
        if (!int.TryParse(idInput, out int idToDelete))
        {
            ConsoleHelpers.WriteLine("Invalid input");
            return;
        }
        if (!validIds.Contains(idToDelete))
        {
            ConsoleHelpers.WriteLine("Invalid ID");
            return;
        }
        else
        {
            conf.Users.RemoveAll(u => u.UId == idToDelete);
            ConsoleHelpers.WriteLine($"User with ID " + idToDelete + "'");
        }
    }
    public static void CreateUser(UsersConfig conf, int currUser) //Kreiert einen Nutzer
    {
        ConsoleHelpers.WriteLine("Enter username: ");
        string uName = ConsoleHelpers.ReadLine();
        if (conf.Users.Any(u => u.Username == uName))
        {
            ConsoleHelpers.WriteLine("Users may not have the same Username!");
            return;
        }
        ConsoleHelpers.WriteLine("Enter password: ");
        string password = ConsoleHelpers.ReadLine();
        ConsoleHelpers.WriteLine("Enter role (0 - User" + (CheckPermissions(currUser, conf, Permissions.SuperAdmin) ? ", 1 - Admin" : "") + ")");
        string roleInput = ConsoleHelpers.ReadLine();
        int[] validRolesToCreate = GetValidRoles(currUser, conf);
        if (!int.TryParse(roleInput, out int roleInt))
        {
            ConsoleHelpers.WriteLine("You cannot delete yourself.");
            return;
        }

        if (!validRolesToCreate.Contains(roleInt))
        {
            ConsoleHelpers.WriteLine("Invalid role");
            return;
        }
        UsersConfig.URoles role = (UsersConfig.URoles)roleInt;
        var newId = 1;
        while (conf.Users.Any(u => u.UId == newId))
        {
            newId++;
        }
        conf.Users.Add(new UsersConfig.User(uName, newId, password, role));
        ConsoleHelpers.WriteLine($"User " + uName + " created with ID " + newId + " and role " + RoleToString(role)); //role.Tostring geht nicht
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

    public static bool ChangePassword(int currUser, UsersConfig conf, PasswordPolicy policy)
    {
        policy.WritePolicy();
        var current = conf.Users.FirstOrDefault(u => u.UId == currUser);
        ConsoleHelpers.WriteLine("Enter current password: ");
        string currPassword = ConsoleHelpers.ReadPassword();
        if (current.Password != currPassword)
        {
            ConsoleHelpers.WriteLine("Incorrect password.");
            return false;
        }
        ConsoleHelpers.WriteLine("Enter new password: ");
        string newPassword = ConsoleHelpers.ReadPassword();
        if (!policy.CheckPassword(newPassword))
        {
            ConsoleHelpers.WriteLine("Password does not meet policy requirements.");
            return false;
        }
        ConsoleHelpers.WriteLine("Confirm Password by typing it again");
        string confirmPassword = ConsoleHelpers.ReadPassword();
        if (newPassword != confirmPassword)
        {
            ConsoleHelpers.WriteLine("Passwords do not match.");
            return false;
        }
        conf.Users.RemoveAll(u => u.UId == currUser);
        conf.Users.Add(new User(current.Username, current.UId, newPassword, current.Role));
        return true;
    }
}
