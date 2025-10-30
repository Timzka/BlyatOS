using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Cosmos.System.FileSystem.VFS;
using static BlyatOS.Library.Configs.UsersConfig;

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

    public void LoadUsers(string path)
    {
        throw new GenericException("Not Implemented");
        //get file data from path
        //make USERS list out of it
        //set it to Users.
    }
    public void WriteUsers(string path)
    {
        throw new GenericException("Not Implemented");
        //write UserList into the file
        //save it
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


