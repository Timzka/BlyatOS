using System;
using System.Linq;
using Cosmos.System.FileSystem;
using BlyatOS.Library.Configs;
using Cosmos.Core;
using BlyatOS.Library.Helpers;
using System.Drawing;
using System.Collections.Generic;

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

            ulong usedRamBytes = SafeUlong(GCImplementation.GetUsedRAM());
            ulong totalRamMB = SafeUlong(CPU.GetAmountOfRAM());
            ulong totalRamBytes = totalRamMB * 1024UL * 1024UL;
            string memoryString = FormatBytes(usedRamBytes) + "/" + FormatBytes(totalRamBytes);

            string[] art =
            {
                " /$$$$$$$  /$$                       /$$      ",
                "| $$__  $$| $$                      | $$      ",
                "| $$  \\ $$| $$ /$$   /$$  /$$$$$$  /$$$$$$   ",
                "| $$$$$$$ | $$| $$  | $$ |____  $$|_  $$_/    ",
                "| $$__  $$| $$| $$  | $$  /$$$$$$$  | $$      ",
                "| $$  \\ $$| $$| $$  | $$ /$$__  $$  | $$ /$$ ",
                "| $$$$$$$/| $$|  $$$$$$$|  $$$$$$$  |  $$$$/  ",
                "|_______/ |__/ \\____  $$ \\_______/   \\___/ ",
                "               /$$  | $$                      ",
                "              |  $$$$$$/                      ",
                "               \\______/                      ",

                "  /$$$$$$   /$$$$$$                           ",
                " /$$__  $$ /$$__  $$                          ",
                "| $$  \\ $$| $$  \\__/                        ",
                "| $$  | $$|  $$$$$$                           ",
                "| $$  | $$ \\____  $$                         ",
                "| $$  | $$ /$$  \\ $$                         ",
                "|  $$$$$$/|  $$$$$$/                          ",
                "\\______/  \\______/                          "
            };

            string[] labels =
            {
                "BlyatOS","User"," Users","Uptime","Directory"," Disks/Parts","CPU","Memory"
            };
            string[] values =
            {
                $"v{version}",
                $"{currentUserName} (id {currentUserId})",
                userCount.ToString(),
                uptime,
                currentDirectory,
                $"{fs.Disks.Count}/{partitions} ({totalSizeMB})",
                CPU.GetCPUBrandString(),
                memoryString
            };

            int artWidth = MaxLength(art);
            int gap = 1; // möglichst direkt nach Art
            int totalWidth = GetConsoleCharWidth();

            // Wir berechnen Wrap pro Eintrag abhängig von Restbreite hinter Art + Gap + Label + " : "
            // Für Einfachheit nutzen wir maximale Restbreite (ab größter Art-Linie)
            int maxAvailable = totalWidth - artWidth - gap - 1; // -1 Puffer
            if (maxAvailable < 20) maxAvailable = 20;

            // Flatten info entries
            List<(string Label, string ValueSeg, bool First, int LabelLen)> infoRows = new();
            for (int i = 0; i < labels.Length; i++)
            {
                int labelLen = labels[i].Length;
                int valueWidth = maxAvailable - (labelLen + 3); // Platz für Value hinter "Label : "
                if (valueWidth < 5) valueWidth = 5;
                var segs = WrapValue(values[i], valueWidth);
                for (int s = 0; s < segs.Count; s++)
                {
                    bool first = s == 0;
                    infoRows.Add((first ? labels[i] : string.Empty, segs[s], first, labelLen));
                }
            }

            int totalRows = art.Length > infoRows.Count ? art.Length : infoRows.Count;
            for (int r = 0; r < totalRows; r++)
            {
                if (r < art.Length)
                    ConsoleHelpers.Write(art[r], Color.Red);
                else
                    ConsoleHelpers.Write(new string(' ', artWidth));

                // Gap (nur 1 Space)
                ConsoleHelpers.Write(" ");

                if (r < infoRows.Count)
                {
                    var row = infoRows[r];
                    if (row.First)
                    {
                        // Kein Padding, direkt Label ausgeben
                        ConsoleHelpers.Write(row.Label, Color.Yellow);
                        ConsoleHelpers.Write(" : ", Color.Yellow);
                    }
                    else
                    {
                        // Fortsetzungszeile: gleiche Startposition für Value wie in erster Zeile
                        ConsoleHelpers.Write(new string(' ', row.LabelLen + 3), Color.Yellow);
                    }
                    ConsoleHelpers.Write(row.ValueSeg, Color.Yellow);
                }
                ConsoleHelpers.WriteLine();
            }

            ConsoleHelpers.WriteLine();
        }
        catch (Exception ex)
        {
            ConsoleHelpers.WriteLine($"neofetch failed: {ex.Message}");
        }
    }

    private static int MaxLength(string[] arr)
    {
        int max = 0;
        for (int i = 0; i < arr.Length; i++) if (arr[i].Length > max) max = arr[i].Length;
        return max;
    }

    private static List<string> WrapValue(string value, int maxWidth)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrEmpty(value)) { result.Add(""); return result; }
        if (maxWidth <= 0) { result.Add(value); return result; }

        int idx = 0;
        while (idx < value.Length)
        {
            int len = value.Length - idx;
            if (len > maxWidth) len = maxWidth;

            if (len == maxWidth && idx + len < value.Length)
            {
                int spacePos = LastSpace(value, idx, len);
                if (spacePos >= idx)
                {
                    len = spacePos - idx + 1;
                }
            }

            string segment = value.Substring(idx, len).TrimEnd();
            result.Add(segment);
            idx += len;
            while (idx < value.Length && value[idx] == ' ') idx++;
        }
        return result;
    }

    private static int LastSpace(string value, int start, int len)
    {
        for (int i = start + len - 1; i >= start; i--)
        {
            if (value[i] == ' ') return i;
        }
        return -1;
    }

    private static int GetConsoleCharWidth()
    {
        try { return (int)(DisplaySettings.ScreenWidth / DisplaySettings.Font.Width) - 1; }
        catch { return 80; }
    }

    private static string FormatBytes(ulong bytes)
    {
        const double KB = 1024.0;
        const double MB = KB * 1024.0;
        const double GB = MB * 1024.0;
        if (bytes >= GB) return (bytes / GB).ToString("0.00") + " GB";
        if (bytes >= MB) return (bytes / MB).ToString("0.00") + " MB";
        if (bytes >= KB) return (bytes / KB).ToString("0.00") + " KB";
        return bytes + " B";
    }

    private static ulong SafeUlong(uint value) => value;
}
