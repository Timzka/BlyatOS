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
using static Cosmos.HAL.BlockDevice.ATA_PIO;

namespace BlyatOS.Library.Configs;

internal class InitSystem
{
    public static bool IsSystemCompleted(string syspath, CosmosVFS fs)
    {
        try
        {
            if (!VFSManager.DirectoryExists(syspath))
            {
                return false;
            }
            if(!VFSManager.FileExists(Path.Combine(syspath, "usersconfig.nahui")))
            {
                return false;
            }
            if(!VFSManager.FileExists(Path.Combine(syspath, "systemconfig.nahui")))
            {
                return false;
            }
            if(!VFSManager.FileExists(Path.Combine(syspath, "blyatlogo.bmp")))
            {
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelpers.WriteLine("Fehler beim Überprüfen des Systems: ");
            ConsoleHelpers.WriteLine(ex.ToString());
            return false;
        }
    }

    public static bool InitSystemData(string syspath, CosmosVFS fs)
    {

        try
        {
            ConsoleHelpers.ClearConsole();
            ConsoleHelpers.WriteLine("Initializing Files");
            if (!VFSManager.DirectoryExists(syspath))
            {
                VFSManager.CreateDirectory(syspath);
                ConsoleHelpers.WriteLine("System Verzeichnis erstellt");
            }

            if (!VFSManager.FileExists(Path.Combine(syspath, "usersconfig.nahui"))) //not finished, doesnt work
            {
                VFSManager.CreateFile(Path.Combine(syspath, "usersconfig.nahui"));
            }
            //WriteDefaultUsers(Path.Combine(syspath, "usersconfig.nahui"));
            ConsoleHelpers.WriteLine("usersconfig.nahui erstellt");

            if (!VFSManager.FileExists(Path.Combine(syspath, "systemconfig.nahui"))) //not finished
            {
                VFSManager.CreateFile(Path.Combine(syspath, "systemconfig.nahui"));
            }
            //File.WriteAllText(Path.Combine(syspath, "systemconfig.nahui"), "");
            ConsoleHelpers.WriteLine("systemconfig.nahui erstellt");

            if (!VFSManager.FileExists(Path.Combine(syspath, "blyatlogo.bmp")))
            {
                BitMaps.BlyatLogo.Save(Path.Combine(syspath, "blyatlogo.bmp"));
                ConsoleHelpers.WriteLine("blyatlogo.bmp erstellt");
            }

            ConsoleHelpers.WriteLine("Init is done!");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelpers.WriteLine("Fehler beim Initialisieren des Systems: ");
            ConsoleHelpers.WriteLine(ex.ToString());
            ConsoleHelpers.WriteLine("Viele dinge werden möglicherweise nicht funktionieren,");
            ConsoleHelpers.WriteLine("das benutzen ist möglich, bitte melden sie jedoch diesen Fehler!");
            return false;
        }
    }
    public static void WriteDefaultUsers( string usersPath)
    {
        var defaultUsers = new List<User>
    {
        new User("BlyatMan", 1, "1234", URoles.SuperAdmin),
        new User("admin", 2, "admin", URoles.Admin)
    };

        string data = "";
        foreach (var u in defaultUsers)
        {
            string passwordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(u.Password));
            data += ($"{u.Username}:{u.UId}:{passwordBase64}:{u.Role}");
            data += "\n";
        }

        try
        {

            var helloFile = VFSManager.GetFile(usersPath);
            var helloFileStream = helloFile.GetFileStream();

            if (helloFileStream.CanWrite)
            {
                byte[] textToWrite = Encoding.ASCII.GetBytes(data);
                helloFileStream.Write(textToWrite, 0, textToWrite.Length);
            }
            else
            {
                throw new Exception("Cannot write to " + usersPath);
            }
        }
        catch(Exception ex)
        {
            ConsoleHelpers.WriteLine(ex.ToString());
        }
    }



}

