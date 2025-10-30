using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlyatOS.Library.Helpers;
using Cosmos.System.FileSystem;
using Cosmos.System.Graphics;

namespace BlyatOS.Library.Configs;

internal class InitSystem
{
    public static bool IsSystemCompleted(string syspath, CosmosVFS fs)
    {
        try
        {
            if (fs.GetDirectory(syspath) == null)
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
            if(fs.GetDirectory(syspath) == null)
            {
                fs.CreateDirectory(syspath);
            }

            if(!File.Exists(Path.Combine(syspath, "usersconfig.nahui"))) //not finished
            {
                File.Create(Path.Combine(syspath, "usersconfig.nahui")).Close();
            }
            File.WriteAllText(Path.Combine(syspath, "usersconfig.nahui"), "admin:admin:True");
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

}

