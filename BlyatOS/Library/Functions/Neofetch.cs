using System;
using Cosmos.System.FileSystem;
using BlyatOS.Library.Configs;
using Cosmos.Core;
using BlyatOS.Library.Helpers;
using System.Drawing;
using System.Collections.Generic;

namespace BlyatOS.Library.Functions;

public static class Neofetch
{
    private struct InfoRow { public string Label; public string Segment; public bool First; public int LabelLen; }

    public static void Show(string version, DateTime startTime, UsersConfig usersConfig, int currentUserId, string currentDirectory, CosmosVFS fs)
    {
        // Grundlegende Null-Prüfungen (verhindert Crash direkt nach Boot)
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
            // Uptime (falls BasicFunctions noch nicht bereit -> Fallback)
            string uptime = SafeString(() => BasicFunctions.RunTime(startTime), "n/a");

            // User lookup ohne LINQ
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

            // Disk / Partition Info (robust)
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

            // RAM / CPU
            string cpuBrand = SafeString(() => CPU.GetCPUBrandString(), "Unknown CPU");
            ulong usedRamBytes = SafeUlong(SafeUInt(() => GCImplementation.GetUsedRAM(), 0));
            ulong totalRamMB = SafeUlong(SafeUInt(() => CPU.GetAmountOfRAM(), 0));
            ulong totalRamBytes = totalRamMB > 0 ? totalRamMB * 1024UL * 1024UL : 0;
            string memoryString = (totalRamBytes == 0) ? "n/a" : FormatBytes(usedRamBytes) + "/" + FormatBytes(totalRamBytes);

            // ASCII Art
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

            // Labels + Values (mit Fallback-Werten)
            string[] labels = { "BlyatOS","User"," Users","Uptime","Directory"," Disks/Parts","CPU","   Memory" };
            string[] values =
            {
                "v" + (version ?? "0.0"),
                currentUserName + " (id " + currentUserId + ")",
                userCount.ToString(),
                uptime,
                currentDirectory ?? "n/a",
                diskCount + "/" + partitions + " (" + totalSizeMB + ")",
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

    // Fallback reine Text-Konsole
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
        List<string> result = new List<string>(); if (string.IsNullOrEmpty(value)) { result.Add(""); return result; } if (maxWidth <= 0) { result.Add(value); return result; }
        int idx = 0; while (idx < value.Length) { int len = value.Length - idx; if (len > maxWidth) len = maxWidth; if (len == maxWidth && idx + len < value.Length) { int spacePos = LastSpace(value, idx, len); if (spacePos >= idx) len = spacePos - idx + 1; } string segment = value.Substring(idx, len).TrimEnd(); result.Add(segment); idx += len; while (idx < value.Length && value[idx] == ' ') idx++; }
        return result;
    }

    private static int LastSpace(string value, int start, int len) { for (int i = start + len - 1; i >= start; i--) if (value[i] == ' ') return i; return -1; }

    private static int GetConsoleCharWidth() { try { return (int)(DisplaySettings.ScreenWidth / DisplaySettings.Font.Width) - 1; } catch { return 80; } }

    private static string FormatBytes(ulong bytes)
    { const double KB = 1024.0; const double MB = KB * 1024.0; const double GB = MB * 1024.0; if (bytes >= GB) return (bytes / GB).ToString("0.00") + " GB"; if (bytes >= MB) return (bytes / MB).ToString("0.00") + " MB"; if (bytes >= KB) return (bytes / KB).ToString("0.00") + " KB"; return bytes + " B"; }

    private static ulong SafeUlong(uint value) => value;
    private static uint SafeUInt(System.Func<uint> getter, uint fallback)
    { try { return getter(); } catch { return fallback; } }
    private static string SafeString(System.Func<string> getter, string fallback)
    { try { var v = getter(); return string.IsNullOrEmpty(v) ? fallback : v; } catch { return fallback; } }
}