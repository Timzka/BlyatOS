using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlyatOS.Library.Helpers;
using Cosmos.System.FileSystem;
using Cosmos.System.FileSystem.VFS;
using Cosmos.System.Graphics;
using static BlyatOS.Library.Configs.UsersConfig;

namespace BlyatOS.Library.Configs;

internal class InitSystem
{
    public static bool IsSystemCompleted(string syspath, CosmosVFS fs)
    {
        try
        {
            if (!Directory.Exists(syspath))
            {
                return false;
            }
            if(!File.Exists(Path.Combine(syspath, "usersconfig.nahui")))
            {
                return false;
            }
            if(!File.Exists(Path.Combine(syspath, "systemconfig.nahui")))
            {
                return false;
            }
            if(!File.Exists(Path.Combine(syspath, "blyatlogo.bmp")))
            {
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fehler beim Überprüfen des Systems: ");
            Console.WriteLine(ex.ToString());
            return false;
        }
    }

    public static bool InitSystemData(string syspath, CosmosVFS fs)
    {
        
        try 
        { 
            if(!Directory.Exists(syspath))
            {
                Directory.CreateDirectory(syspath);
                Console.WriteLine("System Verzeichnis erstellt");
            }

            if (!File.Exists(Path.Combine(syspath, "usersconfig.nahui"))) //not finished, doesnt work
            {
                File.Create(Path.Combine(syspath, "usersconfig.nahui")).Close();
            }
            WriteDefaultUsers(fs,Path.Combine(syspath, "usersconfig.nahui"));
            Console.WriteLine("usersconfig.nahui erstellt");

            if (!File.Exists(Path.Combine(syspath, "systemconfig.nahui"))) //not finished
            {
                File.Create(Path.Combine(syspath, "systemconfig.nahui")).Close();
            }
            File.WriteAllText(Path.Combine(syspath, "systemconfig.nahui"), "");
            Console.WriteLine("systemconfig.nahui erstellt");

            if (!File.Exists(Path.Combine(syspath, "blyatlogo.bmp")))
            {
                BitMaps.BlyatLogo.Save(Path.Combine(syspath, "blyatlogo.bmp"));
                Console.WriteLine("blyatlogo.bmp erstellt");
            }

            Console.WriteLine("Init is done!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fehler beim Initialisieren des Systems: ");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("Viele dinge werden möglicherweise nicht funktionieren,");
            Console.WriteLine("das benutzen ist möglich, bitte melden sie jedoch diesen Fehler!");
            return false;
        }
    }
    public static void WriteDefaultUsers(CosmosVFS fs, string usersPath)
    {
        var defaultUsers = new List<User>
    {
        new User("BlyatMan", 1, "1234", URoles.SuperAdmin),
        new User("admin", 2, "admin", URoles.Admin)
    };

        var sb = new StringBuilder();
        foreach (var u in defaultUsers)
        {
            string passwordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(u.Password));
            sb.AppendLine($"{u.Username}:{u.UId}:{passwordBase64}:{u.Role}");
        }

        try
        {
            if(!File.Exists(usersPath))
            {
                throw new Exception(usersPath + " existiert nicht!");
            }
            var entry = VFSManager.GetFile(usersPath);// DirectoryEntry
            var stream = entry.GetFileStream();
            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());

            stream.Write(bytes, 0, bytes.Length);
            stream.Close(); // sehr wichtig in Cosmos
            Console.WriteLine("Standard-Users erfolgreich geschrieben (Base64-Passwörter).");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fehler beim Schreiben der Users-Datei:");
            Console.WriteLine(ex.ToString());
        }
    }



}

