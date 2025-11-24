using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using BlyatOS.Library.BlyatFileSystem;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Functions;
using BlyatOS.Library.Helpers;
using BlyatOS.Library.Ressources;
using BlyatOS.Library.Startupthings;
using Cosmos.Core.Memory;
using Cosmos.HAL;
using Cosmos.System.FileSystem.VFS;
using Cosmos.System.Graphics;
using Cosmos.System.ScanMaps;
using CosmosAudioInfrastructure.HAL.Drivers.PCI.Audio;
using Sys = Cosmos.System;

namespace BlyatOS;

public class Kernel : Sys.Kernel
{
    internal DateTime MomentOfStart;
    internal string VersionInfo = "0.9";
    internal UsersConfig UsersConf = new UsersConfig();
    internal int CurrentUser;
    internal bool Logged_In = false;
    internal Random Rand = new Random(DateTime.Now.Millisecond);


    public Sys.FileSystem.CosmosVFS fs;
    internal FileSystemHelpers fsh = new FileSystemHelpers();

    public const string RootPath = @"0:\";
    public const string SYSTEMPATH = RootPath + @"BlyatOS\"; //path where system files are stored, inaccessable to ALL users via normal commands
    internal string CurrentDirectory = RootPath;

    internal bool LOCKED; //if system isnt complete, lock system, make user run INIT

    internal KernelParser parser;

    public static void InitializeGraphics()
    {
        DisplaySettings.InitializeGraphics();
    }

    protected override void BeforeRun()
    {
        InitializeGraphics();
        AudioHandler.Initialize(AudioDriverType.AC97, debug: false);

        // Initialize display settings and graphics1024x768
        Global.PIT.Wait(10);
        Ressourceloader.InitRessources();
        Global.PIT.Wait(1000);
        fs = new Sys.FileSystem.CosmosVFS();
        Sys.FileSystem.VFS.VFSManager.RegisterVFS(fs);
        Global.PIT.Wait(1000);
        //LOCKED = !InitSystem.IsSystemCompleted(SYSTEMPATH, fs);
        //if (LOCKED)
        //{
        //    ConsoleHelpers.WriteLine("WARNING: System is locked!", Color.Red);
        //    ConsoleHelpers.WriteLine("Some system files are missing or corrupted.", Color.Yellow);
        //    ConsoleHelpers.WriteLine("Running system initialization...\n", Color.White);
        //    Global.PIT.Wait(1000);
        //    if (InitSystem.InitSystemData(SYSTEMPATH, fs))
        //    {
        //        ConsoleHelpers.WriteLine("System initialized successfully!", Color.Green);
        //        ConsoleHelpers.WriteLine("Press any key to reboot...", Color.White);
        //        ConsoleHelpers.ReadKey();
        //        Cosmos.System.Power.Reboot();
        //    }
        //    LOCKED = false;
        //    ConsoleHelpers.WriteLine("Press any key to continue...", Color.White);
        //    ConsoleHelpers.ReadKey();
        //    Global.PIT.Wait(1000);
        //}
        Global.PIT.Wait(10);

        OnStartUp.RunLoadingScreenThing();
        Global.PIT.Wait(1);
        StartupScreen.Show();

        Sys.KeyboardManager.SetKeyLayout(new DEStandardLayout());

        // Clear the console and display welcome message
        ConsoleHelpers.ClearConsole();
        ConsoleHelpers.WriteLine("BlyatOS v" + VersionInfo, Color.Cyan);
        ConsoleHelpers.WriteLine("Type 'help' for a list of commands\n", Color.White);

        MomentOfStart = DateTime.Now;
        Global.PIT.Wait(500);

        // Initialize parser
        parser = new KernelParser(this);
    }

    protected override void Run()
    {
        try
        {
            // Handle login if not already logged in
            if (!Logged_In)
            {
                HandleLogin();
            }

            // Verzeichnislisten nur einmal pro Command-Loop holen
            string[] dirs = null;
            string[] files = null;

            // Display current directory and prompt - avoid string interpolation
            string prompt = CurrentDirectory + "> ";
            var input = ConsoleHelpers.ReadLine(prompt);

            // Delegate command handling to parser
            parser.HandleCommand(input, ref dirs, ref files);

            // Explicitly clear arrays to help GC
            dirs = null;
            files = null;
            Heap.Collect();
        }
        catch (GenericException ex)
        {
            ConsoleHelpers.WriteLine();
            ConsoleHelpers.WriteLine($"Message: " + ex.EMessage + (ex.Label != "" ? $",\nLabel: " + ex.Label : "") + (ex.ComesFrom != "" ? $",\nSource: " + ex.ComesFrom : ""));
            ConsoleHelpers.WriteLine();
        }
        catch (Exception ex)
        {
            ConsoleHelpers.WriteLine("An error occured: " + ex.Message);
        }
    }

    private void HandleLogin()
    {
        if (Logged_In) return;

        ConsoleHelpers.ClearConsole();
        ConsoleHelpers.WriteLine("=== BlyatOS Login ===\n", Color.Cyan);

        while (!Logged_In)
        {
            string username = ConsoleHelpers.ReadLine("Username: ", Color.White);
            string password = ConsoleHelpers.ReadPassword("Password: ", Color.White, '•');

            // Try to find user
            var user = UsersConf.Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user != null && user.Password == password) // In a real system, use proper password hashing!
            {
                CurrentUser = user.UId;
                Logged_In = true;
                ConsoleHelpers.WriteLine("\nLogin successful!\n", Color.Green);
                Global.PIT.Wait(500);
            }
            else
            {
                ConsoleHelpers.WriteLine("\nInvalid username or password.\n", Color.Red);
                Global.PIT.Wait(1000);
            }
        }
        ConsoleHelpers.ClearConsole();
    }
}
