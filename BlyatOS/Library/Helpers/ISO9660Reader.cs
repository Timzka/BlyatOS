using System;
using System.IO;
using Cosmos.HAL.BlockDevice;
using Cosmos.System.FileSystem;
using Cosmos.System.FileSystem.ISO9660;
using Cosmos.System.FileSystem.Listing;

namespace BlyatOS.Library.Helpers;

/// <summary>
/// Direkter Zugriff auf ISO9660-Dateien vom CD-ROM, auch wenn nicht gemountet
/// </summary>
public static class ISO9660Reader
{
    /// <summary>
    /// Lädt eine Datei direkt vom CD-ROM-BlockDevice
    /// </summary>
    public static byte[] LoadFileFromISO(string fileName)
{
    try
    {
        // Suche nach dem CD-ROM-Laufwerk
        BlockDevice cdrom = null;
        foreach (var device in BlockDevice.Devices)
        {
            if (device.Type == BlockDeviceType.RemovableCD)
            {
                cdrom = device;
                break;
            }
        }

        if (cdrom == null)
        {
            Console.WriteLine("Kein CD-ROM-Laufwerk gefunden!");
            return null;
        }

        Console.WriteLine($"CD-ROM gefunden: BlockSize={cdrom.BlockSize}, BlockCount={cdrom.BlockCount}");

        // Versuche, die Datei direkt über das Dateisystem zu lesen
        var fs = new CosmosVFS();
        var disks = fs.GetDisks();
        
        foreach (var disk in disks)
        {
            if (disk.Host == cdrom)
            {
                Console.WriteLine("CD-ROM gefunden, durchsuche Dateien...");
                var partitions = disk.Partitions;
                foreach (var partition in partitions)
                {
                    try
                    {
                        var root = partition.RootPath;
                        Console.WriteLine($"Durchsuche Partition: {root}");

                        // Prüfe im Root-Verzeichnis
                        if (File.Exists(Path.Combine(root, fileName)))
                        {
                            string filePath = Path.Combine(root, fileName);
                            Console.WriteLine($"Datei gefunden: {filePath}");
                            return File.ReadAllBytes(filePath);
                        }

                        // Prüfe im isoFiles-Verzeichnis
                        string isoFilesPath = Path.Combine(root, "isoFiles", fileName);
                        if (File.Exists(isoFilesPath))
                        {
                            Console.WriteLine($"Datei gefunden: {isoFilesPath}");
                            return File.ReadAllBytes(isoFilesPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Fehler beim Durchsuchen der Partition: {ex.Message}");
                    }
                }
            }
        }

        Console.WriteLine($"Datei '{fileName}' nicht gefunden!");
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Fehler beim Lesen von ISO: {ex.Message}");
        return null;
    }
}

    /// <summary>
    /// Liest Dateidaten direkt vom BlockDevice basierend auf DirectoryEntry
    /// </summary>
    private static byte[] ReadFileData(BlockDevice device, DirectoryEntry entry)
    {
        try
        {
            uint fileSize = (uint)entry.mSize;
            byte[] data = new byte[fileSize];
            
            // Verwende Reflection um auf DataSector zuzugreifen, da ISO9660DirectoryEntry internal ist
            var entryType = entry.GetType();
            var dataSectorProperty = entryType.GetProperty("DataSector");
            
            if (dataSectorProperty != null)
            {
                var dataSectorValue = dataSectorProperty.GetValue(entry);
                uint startSector = Convert.ToUInt32(dataSectorValue);
                uint sectorsToRead = (uint)((fileSize + device.BlockSize - 1) / device.BlockSize);
                
                Console.WriteLine($"Lese von Sektor {startSector}, {sectorsToRead} Sektoren...");
                
                byte[] buffer = device.NewBlockArray(sectorsToRead);
                device.ReadBlock(startSector, sectorsToRead, ref buffer);
                
                // Kopiere nur die benötigten Bytes
                Array.Copy(buffer, 0, data, 0, fileSize);
                
                Console.WriteLine("Datei erfolgreich gelesen!");
                return data;
            }
            else
            {
                Console.WriteLine("Fehler: Kann DataSector-Property nicht finden!");
                Console.WriteLine($"Entry-Typ: {entryType.FullName}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Lesen der Dateidaten: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Schreibt Datei vom CD-ROM ins VFS
    /// </summary>
    public static bool ExtractFileToVFS(string fileName, string targetPath)
    {
        try
        {
            var data = LoadFileFromISO(fileName);
            if (data == null || data.Length == 0)
                return false;

            File.WriteAllBytes(targetPath, data);
            Console.WriteLine($"Datei erfolgreich nach {targetPath} extrahiert ({data.Length} bytes)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Extrahieren: {ex.Message}");
            return false;
        }
    }
}
