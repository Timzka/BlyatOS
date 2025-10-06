using System;
using System.Linq;
using BlyatOS.Library.Configs;
using Cosmos.HAL;

namespace BlyatOS.Library.Functions;

internal class UserManagement
{
    private static string RoleName(UsersConfig.URoles role)
    {
        return role switch
        {
            UsersConfig.URoles.SuperAdmin => "SuperAdmin",
            UsersConfig.URoles.Admin => "Admin",
            UsersConfig.URoles.User => "User",
            _ => "Unknown"
        };
    }

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

    // Updated: include currentUserId to enforce fine-grained creation rules
    public static void CreateUser(UsersConfig conf, int currentUserId)
    {
        var actingUser = conf.Users.FirstOrDefault(u => u.UId == currentUserId);
        if (actingUser == null)
        {
            Console.WriteLine("Invalid current user.");
            return;
        }
        if (actingUser.Role == UsersConfig.URoles.User)
        {
            Console.WriteLine("No permission to create users.");
            return;
        }

        Console.WriteLine("Enter username: ");
        string uName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(uName))
        {
            Console.WriteLine("Username must not be empty.");
            return;
        }
        if (conf.Users.Any(u => u.Username.Equals(uName, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"Username {uName} already exists.");
            return;
        }

        Console.WriteLine("Enter password: ");
        string password = Console.ReadLine();
        if (string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Password must not be empty.");
            return;
        }

        UsersConfig.URoles roleToCreate;
        if (actingUser.Role == UsersConfig.URoles.SuperAdmin)
        {
            while (true)
            {
                Console.Write("Enter role (user/admin) [user]: ");
                var roleInputRaw = Console.ReadLine();
                var roleInput = (roleInputRaw ?? string.Empty).Trim().ToLower();
                if (string.IsNullOrEmpty(roleInput) || roleInput == "user")
                {
                    roleToCreate = UsersConfig.URoles.User;
                    break;
                }
                if (roleInput == "admin")
                {
                    roleToCreate = UsersConfig.URoles.Admin;
                    break;
                }
                Console.WriteLine("Invalid role. Allowed: user, admin");
            }
        }
        else // actingUser.Role == Admin
        {
            roleToCreate = UsersConfig.URoles.User;
            Console.WriteLine("Admins can only create normal users. Role 'User' is enforced.");
        }

        // ID assignment
        int newId = 1;
        while (conf.Users.Any(u => u.UId == newId)) newId++;

        conf.Users.Add(new UsersConfig.User(uName, newId, password, roleToCreate));
        var roleName = RoleName(roleToCreate);
        Console.WriteLine($"User {uName} created with ID {newId} and role {roleName}.");
    }

    // Updated: include currentUserId to enforce fine-grained deletion rules
    public static void DeleteUser(UsersConfig conf, int currentUserId)
    {
        var actingUser = conf.Users.FirstOrDefault(u => u.UId == currentUserId);
        if (actingUser == null)
        {
            Console.WriteLine("Invalid current user.");
            return;
        }
        if (actingUser.Role == UsersConfig.URoles.User)
        {
            Console.WriteLine("No permission to delete users.");
            return;
        }

        Console.WriteLine("Enter username to delete: ");
        string uName = Console.ReadLine();
        var target = conf.Users.FirstOrDefault(u => u.Username.Equals(uName, StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            Console.WriteLine($"User {uName} not found.");
            return;
        }

        if (actingUser.UId == target.UId)
        {
            Console.WriteLine("You cannot delete yourself.");
            return;
        }

        if (actingUser.Role == UsersConfig.URoles.Admin)
        {
            if (target.Role != UsersConfig.URoles.User)
            {
                Console.WriteLine("Admins can only delete normal users.");
                return;
            }
        }
        else if (actingUser.Role == UsersConfig.URoles.SuperAdmin)
        {
            if (target.Role == UsersConfig.URoles.SuperAdmin)
            {
                Console.WriteLine("SuperAdmins cannot delete other SuperAdmins.");
                return;
            }
        }

        conf.Users.Remove(target);
        Console.WriteLine($"User {uName} deleted.");
    }
}
