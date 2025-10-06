using System;
using System.Collections.Generic;
using System.Text;
using Cosmos.HAL;
using Sys = Cosmos.System;
using BlyatOS.Library.Startupthings;
using BlyatOS.Library.Functions;
using BadTetrisCS;

namespace BlyatOS;

public class Kernel : Sys.Kernel
{
    DateTime momentOfStart;
    string versionString = "Blyat version 1";
    protected override void BeforeRun()
    {
        OnStartUp.RunLoadingScreenThing(); //could be removed, but it is cool
        Console.WriteLine("BlyatOS booted successfully. Type /help for help");
        momentOfStart = DateTime.Now;
    }

    protected override void Run()
    {
        Console.Write("Input: ");
        var input = Console.ReadLine();

        string[] args = input.Split(' ');

        switch (args[0])
        {
            case "help":
                {
                    BasicFunctions.Help();
                    break;
                }
            case "version":
                {
                    Console.WriteLine(versionString);
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
                {
                    Console.Clear();
                    break;
                }
            case "vodka":
                {
                    Console.WriteLine("Nyet, no vodka for you! //not implemented");
                    break;
                }
            case "tetris":
                {
                    BadTetris game = new BadTetris();
                    game.Run();
                    break;
                }
            default:
                {
                    Console.WriteLine("Unknown command! Enter \"help\" for more information!");
                    break;
                }

        }
    }
}
