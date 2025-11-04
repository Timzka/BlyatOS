using System;
using System.Linq;
using Cosmos.System.FileSystem; // For CosmosVFS
using BlyatOS.Library.Configs;
using Cosmos.Core;

namespace BlyatOS.Library.Functions;

public static class Neofetch
{
    public static void Show(string version, DateTime startTime, UsersConfig usersConfig, int currentUserId, string currentDirectory, CosmosVFS fs)
    {
        try
        {
            string uptime = BasicFunctions.RunTime(startTime);
            var currentUser = usersConfig.Users.FirstOrDefault(u => u.UId == currentUserId);
            string currentUserName = currentUser?.Username ?? "unknown";
            int userCount = usersConfig.Users.Count;

            // Disk / partition info
            ulong totalSize = 0;
            int partitions = 0;
            try
            {
                foreach (var disk in fs.Disks)
                {
                    if (disk?.Host == null) continue;
                    foreach (var part in disk.Partitions)
                    {
                        partitions++;
                        try { totalSize += (ulong)part.MountedFS.Size; } catch { }
                    }
                }
            }
            catch { }

            string totalSizeMB = totalSize == 0 ? "n/a" : (totalSize / (1024 * 1024)).ToString() + " MB";

            // RAM Werte holen
            ulong usedRamBytes = SafeUlong(GCImplementation.GetUsedRAM());
            ulong totalRamMB = SafeUlong(CPU.GetAmountOfRAM());
            ulong totalRamBytes = totalRamMB * 1024UL * 1024UL;
            string memoryString = FormatBytes(usedRamBytes) + "/" + FormatBytes(totalRamBytes);

            string[] art =
            {
                " /$$$$$$$  /$$                       /$$",
                "| $$__  $$| $$                      | $$",
                "| $$  \\ $$| $$ /$$   /$$  /$$$$$$  /$$$$$$",
                "| $$$$$$$ | $$| $$  | $$ |____  $$|_  $$_/",
                "| $$__  $$| $$| $$  | $$  /$$$$$$$  | $$",
                "| $$  \\ $$| $$| $$  | $$ /$$__  $$  | $$ /$$",
                "| $$$$$$$/| $$|  $$$$$$$|  $$$$$$$  |  $$$$/",
                "|_______/ |__/ \\____  $$ \\_______/   \\___/",
                "               /$$  | $$",
                "               |  $$$$$$/",
                "               \\______/",
                "  /$$$$$$   /$$$$$$",
                " /$$__  $$ /$$__  $$",
                "| $$  \\ $$| $$  \\__/",
                "| $$  | $$|  $$$$$$",
                "| $$  | $$ \\____  $$",
                "| $$  | $$ /$$  \\ $$",
                "|  $$$$$$/|  $$$$$$/",
                "\\______/  \\______/"

            };

            string[] infoLines =
            {
                $"BlyatOS v{version}",
                $"User       : {currentUserName} (id {currentUserId})",
                $"Users      : {userCount}",
                $"Uptime     : {uptime}",
                $"Directory  : {currentDirectory}",
                $"Disks/Parts: {fs.Disks.Count}/{partitions} ({totalSizeMB})",
                $"CPU        : {CPU.GetCPUBrandString()}",
                $"Memory     : {memoryString}"
            };

            int startTop = SafeCursorTop(); // Startposition merken
            int artWidth = art.Max(l => l.Length);
            int gap = 3; // Abstand zwischen Spalten
            int infoStartCol = artWidth + gap; // feste Startspalte rechte Spalte

            // Linke Spalte zuerst komplett zeichnen
            foreach (var line in art)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine(line);
                System.Console.ResetColor();
            }

            int artEndTop = SafeCursorTop(); // Untere Grenze der Art

            // Rechte Spalte unabhängig platzieren
            int bufferWidth = GetBufferWidthSafe();
            int maxRightWidth = bufferWidth - infoStartCol - 1;
            if (maxRightWidth < 10) maxRightWidth = 10;

            int row = startTop; // Start direkt neben oberster Art-Zeile
            foreach (var info in infoLines)
            {
                row = WriteInfoWrapped(infoStartCol, row, info, maxRightWidth);
            }

            // Cursor unter den größeren Block setzen
            int finalRow = Math.Max(row, artEndTop);
            TrySetCursorPosition(0, finalRow);
            System.Console.WriteLine();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"neofetch failed: {ex.Message}");
        }
    }

    // Schreibt Text mit Wrap an (column,row). Gibt nächste freie Zeile zurück.
    private static int WriteInfoWrapped(int column, int row, string text, int maxWidth)
    {
        int idx = 0;
        bool first = true;
        System.Console.ForegroundColor = ConsoleColor.Yellow;
        while (idx < text.Length)
        {
            int take = Math.Min(maxWidth, text.Length - idx);
            string part = text.Substring(idx, take);
            TrySetCursorPosition(column, row);
            System.Console.Write(part);
            idx += take;
            row++; // nächste Zeile nur für rechte Spalte
            first = false;
        }
        System.Console.ResetColor();
        return row; // freie Zeile unterhalb
    }

    private static int GetBufferWidthSafe()
    {
        try
        {
            int w = System.Console.BufferWidth;
            if (w > 0) return w;
        }
        catch { }
        return 80; // Fallback
    }

    private static int SafeCursorTop()
    {
        try { return System.Console.CursorTop; } catch { return 0; }
    }

    private static void TrySetCursorPosition(int left, int top)
    {
        try { System.Console.SetCursorPosition(left, top); } catch { }
    }

    private static string FormatBytes(ulong bytes)
    {
        const double KB = 1024.0;
        const double MB = KB * 1024.0;
        const double GB = MB * 1024.0;
        if (bytes >= GB)
            return (bytes / GB).ToString("0.00") + " GB";
        if (bytes >= MB)
            return (bytes / MB).ToString("0.00") + " MB";
        if (bytes >= KB)
            return (bytes / KB).ToString("0.00") + " KB";
        return bytes + " B";
    }

    private static ulong SafeUlong(uint value) => value;
}
