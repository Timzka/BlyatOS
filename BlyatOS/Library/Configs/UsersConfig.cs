using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BlyatOS.Library.Configs.UsersConfig;

namespace BlyatOS.Library.Configs;

public class UsersConfig
{
    public List<User> Users = new()
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


