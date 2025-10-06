using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using BlyatOS.Library.Configs;
using Cosmos.HAL;

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

    public static void CreateUser(UsersConfig conf)
    {
        Console.WriteLine("Enter username: ");
        string uName = Console.ReadLine();
        Console.WriteLine("Enter password: ");
        string password = Console.ReadLine();
        Console.WriteLine("Enter role (0 - User, 1 - Admin): ");
        string roleInput = Console.ReadLine();

        if (!int.TryParse(roleInput, out int roleInt))
        {
            Console.WriteLine("Invalid input, must be a number");
            return;
        }

        if (roleInt < 0 || roleInt > 1)
        {
            if (roleInt == 2) Console.WriteLine("SuperAdmin(2) Cannot be created during Runtime!");
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
        Console.WriteLine($"User {uName} created with ID {newId.ToString()} and role {role.ToString()}"); //role.Tostring geht nicht
    }
}
