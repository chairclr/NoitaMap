using Silk.NET.Core;

namespace NoitaMap;

public static class PathService
{
    public static string SavePath { get; private set; } = null!;

    public static string WorldPath { get; private set; } = null!;

    public static string? DataPath { get; private set; }

    static PathService()
    {
        if (OperatingSystem.IsWindows())
        {
            string localLowPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low";

            SavePath ??= Path.Combine(localLowPath, "Nolla_Games_Noita", "save00");

            WorldPath ??= Path.Combine(SavePath, "world");

            DataPath ??= Path.Combine(localLowPath, "Nolla_Games_Noita", "data");
        }
    }

    public static void SetPaths(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg == "-s" || arg == "--save")
            {
                if (i + 1 >= args.Length)
                {
                    Console.WriteLine("Invalid Command Line Argument");

                    break;
                }

                i++;
                SavePath = args[i];
            }
            else if (arg == "-w" || arg == "--world")
            {
                if (i + 1 >= args.Length)
                {
                    Console.WriteLine("Invalid Command Line Argument");

                    break;
                }

                i++;
                WorldPath = args[i];
            }
            else if (arg == "-d" || arg == "--data")
            {
                if (i + 1 >= args.Length)
                {
                    Console.WriteLine("Invalid Command Line Argument");

                    break;
                }

                i++;
                DataPath = args[i];
            }
            else if (arg == "-h" || arg == "-?" || arg == "--help")
            {
                PrintUsage();
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine($"Unknown argument: {arg}");
                PrintUsage();
                Environment.Exit(-1);
            }
        }

        if (OperatingSystem.IsWindows())
        {
            string localLowPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low";

            SavePath ??= Path.Combine(localLowPath, "Nolla_Games_Noita", "save00");

            WorldPath ??= Path.Combine(SavePath, "world");

            DataPath ??= Path.Combine(localLowPath, "Nolla_Games_Noita", "data");
        }

        if (SavePath is null)
        {
            Console.WriteLine("Please specify a path for your save: --save \"/path/to/your/save\"");
            throw new Exception("No Save Path Specified");
        }
        else
        {
            WorldPath ??= Path.Combine(SavePath, "world");
        }

        if (WorldPath is null)
        {
            Console.WriteLine("Please specify a path for your world: --world \"/path/to/your/save/world\"");
            throw new Exception("No Save Path Specified");
        }

        if (!Directory.Exists(DataPath))
        {
            DataPath = null;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("-w or --world  \"/path/to/your/save/world/\"");
        Console.WriteLine("-s or --save   \"/path/to/your/save/\"");
        Console.WriteLine("Optional:");
        Console.WriteLine("-d or --data   \"/path/to/exported/game/data/folder/\"");
    }
}
