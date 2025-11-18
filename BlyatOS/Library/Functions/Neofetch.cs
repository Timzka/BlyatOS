using System;
using System.Collections.Generic;
using System.Drawing;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Helpers;
using Cosmos.Core;
using Cosmos.System.FileSystem;

namespace BlyatOS.Library.Functions;

public static class Neofetch
{
    private struct InfoRow { public string Label; public string Segment; public bool First; public int LabelLen; }

    public static void Show(string version, DateTime startTime, UsersConfig usersConfig, int currentUserId, string currentDirectory, CosmosVFS fs)
    {
        if (usersConfig == null)
        {
            SafeFallback("UsersConfig null");
            return;
        }
        if (fs == null)
        {
            SafeFallback("FileSystem null");
            return;
        }

        try
        {
            string uptime = "n/a";
            try { uptime = BasicFunctions.RunTime(startTime); } catch { }

            UsersConfig.User currentUser = null;
            var list = usersConfig.Users;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var u = list[i];
                    if (u != null && u.UId == currentUserId) { currentUser = u; break; }
                }
            }
            string currentUserName = currentUser != null ? currentUser.Username : "unknown";
            int userCount = list != null ? list.Count : 0;

            ulong totalSize = 0;
            int partitions = 0;
            int diskCount = 0;
            try
            {
                if (fs.Disks != null)
                {
                    diskCount = fs.Disks.Count;
                    for (int d = 0; d < fs.Disks.Count; d++)
                    {
                        var disk = fs.Disks[d];
                        if (disk == null || disk.Host == null || disk.Partitions == null) continue;
                        for (int p = 0; p < disk.Partitions.Count; p++)
                        {
                            var part = disk.Partitions[p];
                            if (part == null) continue;
                            partitions++;
                            try { if (part.MountedFS != null) totalSize += (ulong)part.MountedFS.Size; } catch { }
                        }
                    }
                }
            }
            catch { }
            string totalSizeMB = totalSize == 0 ? "n/a" : (totalSize / (1024UL * 1024UL)).ToString() + " MB";

            string cpuBrand = "Unknown CPU";
            try { cpuBrand = CPU.GetCPUBrandString(); } catch { }

            ulong usedRamBytes = 0;
            ulong totalRamBytes = 0;
            try
            {
                usedRamBytes = GCImplementation.GetUsedRAM();
                ulong totalRamMB = CPU.GetAmountOfRAM();
                totalRamBytes = totalRamMB * 1024UL * 1024UL;
            }
            catch { }

            string memoryString = totalRamBytes == 0 ? "n/a" : FormatBytesMatching(usedRamBytes, totalRamBytes);

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

            string[] labels = { "BlyatOS", "User", " Users", "Uptime", "Directory", " Disks/Parts", "CPU", "   Memory" };
            string versionStr = version ?? "0.0";
            string[] values =
            {
                "v" + versionStr,
                currentUserName + " (id " + currentUserId.ToString() + ")",
                userCount.ToString(),
                uptime,
                currentDirectory ?? "n/a",
                diskCount.ToString() + "/" + partitions.ToString() + " (" + totalSizeMB + ")",
                cpuBrand,
                memoryString
            };

            bool graphicalAvailable = IsGraphicalAvailable();
            if (!graphicalAvailable)
            {
                FallbackConsole(art, labels, values);
                return;
            }

            int artWidth = MaxLength(art);
            int gap = 1;
            int totalWidth = GetConsoleCharWidth();
            int maxAvailable = totalWidth - artWidth - gap - 1;
            if (maxAvailable < 30) maxAvailable = 30;

            List<InfoRow> infoRows = new List<InfoRow>();
            for (int i = 0; i < labels.Length; i++)
            {
                int labelLen = labels[i].Length;
                int valueWidth = maxAvailable - (labelLen + 3);
                if (valueWidth < 5) valueWidth = 5;
                List<string> segs = WrapValue(values[i], valueWidth);
                for (int s = 0; s < segs.Count; s++)
                {
                    infoRows.Add(new InfoRow { Label = s == 0 ? labels[i] : string.Empty, Segment = segs[s], First = s == 0, LabelLen = labelLen });
                }
            }

            int totalRows = art.Length > infoRows.Count ? art.Length : infoRows.Count;
            for (int r = 0; r < totalRows; r++)
            {
                if (r < art.Length) ConsoleHelpers.Write(art[r], Color.Red); else ConsoleHelpers.Write(new string(' ', artWidth));
                ConsoleHelpers.Write(" ");
                if (r < infoRows.Count)
                {
                    var ir = infoRows[r];
                    if (ir.First) { ConsoleHelpers.Write(ir.Label, Color.Yellow); ConsoleHelpers.Write(" : ", Color.Yellow); }
                    else { ConsoleHelpers.Write(new string(' ', ir.LabelLen + 3), Color.Yellow); }
                    ConsoleHelpers.Write(ir.Segment, Color.Yellow);
                }
                ConsoleHelpers.WriteLine();
            }
            ConsoleHelpers.WriteLine();
        }
        catch (Exception ex)
        {
            if (IsGraphicalAvailable()) ConsoleHelpers.WriteLine("neofetch failed: " + ex.Message, Color.Red); else System.Console.WriteLine("neofetch failed: " + ex.Message);
        }
    }

    private static void FallbackConsole(string[] art, string[] labels, string[] values)
    {
        int artWidth = MaxLength(art); int gap = 1; int rows = art.Length > labels.Length ? art.Length : labels.Length;
        for (int i = 0; i < rows; i++)
        {
            if (i < art.Length) { System.Console.ForegroundColor = ConsoleColor.Red; System.Console.Write(art[i]); System.Console.ResetColor(); }
            else System.Console.Write(new string(' ', artWidth));
            System.Console.Write(new string(' ', gap));
            if (i < labels.Length) { System.Console.ForegroundColor = ConsoleColor.Yellow; System.Console.Write(labels[i] + " : " + values[i]); System.Console.ResetColor(); }
            System.Console.WriteLine();
        }
        System.Console.WriteLine();
    }

    private static void SafeFallback(string msg)
    {
        if (IsGraphicalAvailable()) ConsoleHelpers.WriteLine("neofetch aborted: " + msg, Color.Red); else System.Console.WriteLine("neofetch aborted: " + msg);
    }

    private static bool IsGraphicalAvailable() { try { return DisplaySettings.Canvas != null && DisplaySettings.Font != null; } catch { return false; } }

    private static int MaxLength(string[] arr) { int max = 0; for (int i = 0; i < arr.Length; i++) if (arr[i] != null && arr[i].Length > max) max = arr[i].Length; return max; }

    private static List<string> WrapValue(string value, int maxWidth)
    {
        List<string> result = new List<string>(); if (string.IsNullOrEmpty(value)) { result.Add(""); return result; }
        if (maxWidth <= 0) { result.Add(value); return result; }
        int idx = 0; while (idx < value.Length) { int len = value.Length - idx; if (len > maxWidth) len = maxWidth; if (len == maxWidth && idx + len < value.Length) { int spacePos = LastSpace(value, idx, len); if (spacePos >= idx) len = spacePos - idx + 1; } string segment = value.Substring(idx, len).TrimEnd(); result.Add(segment); idx += len; while (idx < value.Length && value[idx] == ' ') idx++; }
        return result;
    }

    private static int LastSpace(string value, int start, int len) { for (int i = start + len - 1; i >= start; i--) if (value[i] == ' ') return i; return -1; }

    private static int GetConsoleCharWidth() { try { return (int)(DisplaySettings.ScreenWidth / DisplaySettings.Font.Width) - 1; } catch { return 80; } }

    private static string FormatBytesMatching(ulong value1, ulong value2)
    {
        const double KB = 1024.0;
        const double MB = KB * 1024.0;
        const double GB = MB * 1024.0;

        ulong larger = value2 > value1 ? value2 : value1;

        if (larger >= GB)
        {
            return (value1 / GB).ToString("0.00") + " GB / " + (value2 / GB).ToString("0.00") + " GB";
        }
        else if (larger >= MB)
        {
            return (value1 / MB).ToString("0.00") + " MB / " + (value2 / MB).ToString("0.00") + " MB";
        }
        else if (larger >= KB)
        {
            return (value1 / KB).ToString("0.00") + " KB / " + (value2 / KB).ToString("0.00") + " KB";
        }
        else
        {
            return value1.ToString() + " B / " + value2.ToString() + " B";
        }
    }
}