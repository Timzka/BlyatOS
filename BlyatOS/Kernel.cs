using BadTetrisCS;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Functions;
using BlyatOS.Library.Startupthings;
using Cosmos.HAL;
using Cosmos.HAL.BlockDevice;
using System;
using System.IO;
using System.Security.Principal;
using Cosmos.System.ScanMaps;
using Sys = Cosmos.System;
using SysFS = Cosmos.System.FileSystem;
using SysVFS = Cosmos.System.FileSystem.VFS;
using BlyatOS.Library.BlyatFileSystem;

namespace BlyatOS;

public class Kernel : Sys.Kernel
{
    DateTime momentOfStart;
    string versionString = "0.9";
    private UsersConfig UsersConf = new UsersConfig();
    private int CurrentUser;
    bool logged_in = false;
    Random Rand = new Random(DateTime.Now.Millisecond);

    Sys.FileSystem.CosmosVFS fs;
    FileSystemHelpers fsh = new FileSystemHelpers();
    string current_directory = "0:\\";

    protected override void BeforeRun()
    {
        fs = new Sys.FileSystem.CosmosVFS();
        Sys.FileSystem.VFS.VFSManager.RegisterVFS(fs);

        Sys.KeyboardManager.SetKeyLayout(new DE_Standard());
        //OnStartUp.RunLoadingScreenThing();
        Console.WriteLine($"BlyatOS v{versionString} booted successfully. Type help for a list of valid commands");
        momentOfStart = DateTime.Now;
        Global.PIT.Wait(1000);
    }

    protected override void Run()
    {
        string[] dirs = fsh.GetDirectories(current_directory);
        if (logged_in)
        {
            Console.Write("Input: ");
            var input = Console.ReadLine();
            string[] args = input.Split(' ');

            switch (args[0])
            {
                case "help":
                    {
                        int? page = null;
                        if (args.Length > 1 && int.TryParse(args[1], out int p)) page = p;
                        Console.Clear();
                        BasicFunctions.Help(page);
                        break;
                    }
                case "version":
                case "v":
                    {
                        Console.WriteLine("Blyat version " + versionString);
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
                case "wiseman":
                    {
                        Console.WriteLine(BasicFunctions.GenerateWiseManMessage(Rand));
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
                case "clear":
                case "clearScreen":
                    {
                        Console.Clear();
                        break;
                    }
                case "logout":
                case "lock":
                    {
                        Console.WriteLine("Logging out...");
                        Global.PIT.Wait(1000);
                        logged_in = false;
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
                        if (!UserManagement.CheckPermissions(CurrentUser, UsersConf, UsersConfig.Permissions.Admin))
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
                        if (!UserManagement.CheckPermissions(CurrentUser, UsersConf, UsersConfig.Permissions.Admin))
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
                case "fsinfo":
                    {
                        var disks = fs.Disks;
                        foreach (var disk in disks)
                        {
                            if (disk?.Host == null)
                            {
                                Console.WriteLine("Disk host unavailable");
                                continue;
                            }

                            Console.WriteLine(fsh.BlockDeviceTypeToString(disk.Host.Type));

                            foreach (var partition in disk.Partitions)
                            {
                                Console.WriteLine(partition.Host);
                                Console.WriteLine(partition.RootPath);
                                Console.WriteLine(partition.MountedFS.Size);
                            }
                        }
                        break;
                    }
                case "dir":
                    {
                        foreach (var item in dirs)
                        {
                            Console.WriteLine(item);
                        }

                        break;
                    }
                case "mkdir":
                    {
                        string? name = null;
                        if (args.Length > 1) name = args[1];
                        var path = current_directory + name;
                        fs.CreateDirectory(path);
                        Console.WriteLine($"Directory '{name}' created at '{path}'");
                        break;
                    }
                case "touch":
                    {
                        string? name = null;
                        if (args.Length > 1) name = args[1];
                        var path = current_directory + name;
                        fs.CreateFile(path);
                        Console.WriteLine($"File '{name}' created at '{path}'");
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Unknown command! Enter \"help\" for more information!");
                        break;
                    }
            }
        }
        else
        {
            Console.Clear();
            int c = 0;
            while (true)
            {
                if (c >= 5) Cosmos.System.Power.Shutdown();
                else if (c > 0) Console.WriteLine($"You have {5 - c} tries left until shutdown");
                int uid = UserManagement.Login(UsersConf);
                if (uid != -1)
                {
                    CurrentUser = uid;
                    break;
                }
                c++;
                Console.Clear();
            }
            logged_in = true;
            Console.Clear();
        }
    }
}
