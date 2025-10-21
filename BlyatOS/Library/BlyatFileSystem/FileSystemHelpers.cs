using Cosmos.HAL.BlockDevice;
using System.IO;

namespace BlyatOS.Library.BlyatFileSystem;

public class FileSystemHelpers
{
    public  string[] GetDirectories(string path)
    {
        return Directory.GetDirectories(path);
    }

    public  string BlockDeviceTypeToString(BlockDeviceType type) =>
        type switch
        {
            BlockDeviceType.HardDrive => "Hard Drive",
            BlockDeviceType.Removable => "Removable Drive",
            BlockDeviceType.RemovableCD => "Removable CD Drive",
            _ => type.ToString()
        };
}