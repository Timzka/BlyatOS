using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Cosmos.System.FileSystem;
using Cosmos.System.FileSystem.VFS;
using static BlyatOS.Library.Configs.UsersConfig;
using BlyatOS.Library.Helpers;

namespace BlyatOS.Library.Configs;

public class UsersConfig
{
    public List<User> Users { get; set; } = new()
    {
        new User("BlyatMan", 1, "1234", URoles.SuperAdmin),
        new User("admin", 2, "admin", URoles.Admin),
        new User("user", 3, "user", URoles.User)
    };

    public class User
    {
        public string Username { get; init; }
        public int UId { get; init; }
        public string Password { get; init; }
        public URoles Role { get; init; }
        public User(string username, int uid, string password, URoles role)
        {
            Username = username;
            UId = uid;
            Password = password;
            Role = role;
        }
    }

    public void WriteUsers(string usersPath)
    {
        List<string> lines = new List<string>();
        foreach (var u in Users)
        {
            string passwordBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(u.Password));
            lines.Add($"{u.Username}:{u.UId}:{passwordBase64}:{u.Role}");
        }

        File.WriteAllLines(usersPath, lines);
        ConsoleHelpers.WriteLine("Users erfolgreich gespeichert (Passwörter Base64).");
    }
    public void LoadUsers(string usersPath)
    {
        if (!File.Exists(usersPath))
        {
            ConsoleHelpers.WriteLine("Users-Datei existiert nicht, Standard-Users werden geladen.");
            return;
        }

        var lines = File.ReadAllLines(usersPath);
        Users = new List<User>();

        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length < 4) continue;

            string username = parts[0];
            int uid = int.Parse(parts[1]);

            // Passwort aus Base64 dekodieren
            string password = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));

            URoles role = (URoles)Enum.Parse(typeof(URoles), parts[3]);

            Users.Add(new User(username, uid, password, role));
        }

        ConsoleHelpers.WriteLine("Users erfolgreich geladen (Passwörter Base64 dekodiert).");
    }


    public enum URoles
    {
        User,
        Admin,
        SuperAdmin,
    }
    public static class Permissions
    {
        public static readonly UsersConfig.URoles[] User = new[] { URoles.User, URoles.Admin, URoles.SuperAdmin };
        public static readonly UsersConfig.URoles[] Admin = new[] { URoles.Admin, URoles.SuperAdmin };
        public static readonly UsersConfig.URoles[] SuperAdmin = new[] { URoles.SuperAdmin };
    }
}


