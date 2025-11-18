using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using BlyatOS.Library.BlyatFileSystem;
using BlyatOS.Library.Helpers;
using Cosmos.System.FileSystem;
using Cosmos.System.FileSystem.VFS;
using static BlyatOS.PathHelpers;

namespace BlyatOS.Library.Functions
{
    public static class FileFunctions
    {
        public static void EnsureRawFileNameIsValid(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (name.IndexOfAny(invalidChars) >= 0)
            {
                throw new GenericException("Invalid characters in file name: " + name);
            }
            string fileNameOnly = Path.GetFileName(name);

            string baseName = Path.GetFileNameWithoutExtension(fileNameOnly);

            if (baseName.Length <= 3)
            {
                throw new GenericException("File name '" + baseName + "' is too short. Must be longer than 3 characters.");
            }
        }

        public static void ListDirectories(string[] dirs)
        {
            var sb = new StringBuilder(128);
            foreach (var d in dirs)
            {
                sb.Clear();
                sb.Append(TrimPath(d));
                sb.Append("/");
                ConsoleHelpers.WriteLine(sb.ToString());
            }
            sb.Clear(); // StringBuilder aufräumen
        }

        public static void ListAll(string[] dirs, string[] files)
        {
            var sb = new StringBuilder(128);

            foreach (var d in dirs)
            {
                sb.Clear();
                sb.Append("[D] ");
                sb.Append(TrimPath(d));
                sb.Append("/");
                ConsoleHelpers.WriteLine(sb.ToString(), Color.Cyan);
            }
            foreach (var f in files)
            {
                sb.Clear();
                sb.Append("[F] ");
                sb.Append(TrimPath(f));
                ConsoleHelpers.WriteLine(sb.ToString(), Color.Gray);
            }

            // StringBuilder aufräumen
            sb.Clear();
        }

        public static void MakeDirectory(string CurrentDirectory, string name)
        {
            var path = PathCombine(CurrentDirectory, name);
            Directory.CreateDirectory(path);
            if (Directory.Exists(path))
            {
                throw new GenericException("Directory '" + name + "' already exists at '" + path + "'");
            }
            ConsoleHelpers.WriteLine("Directory '" + name + "' created at '" + path + "'");
        }

        public static void DeleteDirectory(string CurrentDirectory, string name)
        {
            string path = PathCombine(CurrentDirectory, name);
            if (!Directory.Exists(path))
            {
                throw new GenericException("Directory not found: " + path);
            }
            VFSManager.DeleteDirectory(path, true);
        }

        public static void CreateFile(string CurrentDirectory, string name, CosmosVFS fs)
        {
            EnsureRawFileNameIsValid(name);
            if (!name.Contains('.'))
            {
                name += ".blyat";
            }
            else if (name.EndsWith("."))
            {
                name += "blyat";
            }
            string path = IsAbsolute(name) ? name : PathCombine(CurrentDirectory, name);
            if (path.EndsWith("\\") || path.EndsWith("/"))
                path = path.TrimEnd('\\', '/');
            if (File.Exists(path))
            {
                throw new GenericException("File '" + name + "' already exists at '" + path + "'");
            }
            VFSManager.CreateFile(path);
            ConsoleHelpers.WriteLine("File '" + name + "' created at '" + path + "'");
        }

        public static string ChangeDirectory(string CurrentDirectory, string RootPath, string target, FileSystemHelpers fsh)
        {
            if (target == "..")
            {
                if (IsRoot(CurrentDirectory, RootPath))
                {
                    throw new GenericException("Already at root");
                }
                return GetParent(CurrentDirectory, RootPath);

            }
            else
            {
                string newPath = IsAbsolute(target) ? EnsureTrailingSlash(target) : PathCombine(CurrentDirectory, target);
                if (!fsh.DirectoryExists(newPath))
                {
                    throw new GenericException("Directory not found: " + newPath);
                }
                return newPath;
            }
        }

        public static void ReadFile(string rawpath, string CurrentDirectory)
        {
            string path = IsAbsolute(rawpath)
                                ? rawpath
                                : PathCombine(CurrentDirectory, rawpath).TrimEnd('\\');

            if (!File.Exists(path))
            {
                throw new GenericException("File not found: " + path);
            }

            if (path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
            {
                ReadDisplay.DisplayBitmap(path);
            }
            else
            {
                // For small files, read all at once
                if (new FileInfo(path).Length < 1024 * 1024) // 1MB
                {
                    ConsoleHelpers.WriteLine(ReadDisplay.ReadTextFile(path));
                }
                else
                {
                    // For larger files, read in chunks
                    ReadDisplay.ReadTextFileInChunks(path);
                }
            }
        }

        public static void FindKusche(string filename, CosmosVFS fs, FileSystemHelpers fsh)
        {
            ConsoleHelpers.ClearConsole();
            ConsoleHelpers.WriteLine("=== Searching for  " + filename + "===");
            ConsoleHelpers.WriteLine();

            var disks = fs.Disks;
            foreach (var disk in disks)
            {
                if (disk?.Host == null) continue;

                ConsoleHelpers.WriteLine("Disk Type: " + fsh.BlockDeviceTypeToString(disk.Host.Type));

                foreach (var partition in disk.Partitions)
                {
                    ConsoleHelpers.WriteLine("  Checking: " + partition.RootPath);
                    SearchFileRecursive(partition.RootPath, filename);
                }
            }

            ConsoleHelpers.WriteLine();
        }

        public static void DeleteFile(string CurrentDirectory, string name)
        {
            var path = IsAbsolute(name) ? name : PathCombine(CurrentDirectory, name);
            if (!File.Exists(path))
            {
                throw new GenericException("File not found: " + path);
            }
            File.Delete(path);
            ConsoleHelpers.WriteLine("File '" + name + "' deleted from '" + CurrentDirectory + "'");
        }
        public static void WriteFile(string CurrentDirectory, List<string> commandArgs)
        {
            string modeToken = commandArgs[0].ToLower();
            string filename = commandArgs[1];
            string content = string.Join(" ", commandArgs.Skip(2));
            EnsureRawFileNameIsValid(filename);
            if (content[0] == '"' && content[^1] == '"')
            {
                content = content[1..^1];
            }
            else if (content.Contains(" "))
            {
                throw new GenericException("Usage: write <mode> <filename> <content>");
            }
            string mode = modeToken switch
            {
                "append" => "append",
                "add" => "append",
                "overwrite" => "overwrite",
                "ovr" => "overwrite",
                _ => throw new GenericException("Unknown write mode '" + modeToken + "'. Use append|add or overwrite|ovr.")
            };

            // Build absolute path (avoid trailing slash)
            string path = IsAbsolute(filename) ? filename : PathCombine(CurrentDirectory, filename);
            if (path.EndsWith("\\") || path.EndsWith("/"))
                path = path.TrimEnd('\\', '/');

            try
            {
                // Ensure file exists
                if (!VFSManager.FileExists(path))
                {
                    VFSManager.CreateFile(path);
                }

                var vfsFile = VFSManager.GetFile(path);
                var stream = vfsFile.GetFileStream();

                if (!stream.CanWrite)
                    throw new GenericException("Stream not writable for '" + path + "'", "write", "filesystem");
                string toWrite = ConsoleHelpers.ProcessEscapeSequences(content);
                byte[] bytes = Encoding.ASCII.GetBytes(toWrite);

                if (mode == "overwrite")
                {
                    // Truncate: recreate simple by setting Position=0 and (if supported) Length=0.
                    // If Length set is not implemented, delete & recreate.
                    try
                    {
                        stream.Position = 0;
                        // Some Cosmos versions allow setting length:
                        if (stream.CanSeek)
                        {
                            // Attempt truncate by writing zero length (if SetLength exists)
                            if (stream.Length > 0)
                            {
                                // If SetLength not available, fall back to delete-recreate
                                bool setLengthWorked = true;
                                try { stream.SetLength(0); }
                                catch { setLengthWorked = false; }
                                if (!setLengthWorked)
                                {
                                    stream.Close();
                                    VFSManager.DeleteFile(path);
                                    VFSManager.CreateFile(path);
                                    vfsFile = VFSManager.GetFile(path);
                                    stream = vfsFile.GetFileStream();
                                }
                            }
                        }
                    }
                    catch
                    {
                        stream.Close();
                        VFSManager.DeleteFile(path);
                        VFSManager.CreateFile(path);
                        vfsFile = VFSManager.GetFile(path);
                        stream = vfsFile.GetFileStream();
                    }

                    stream.Write(bytes, 0, bytes.Length);
                }
                else // append
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.End);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        // Fallback: read existing, then rewrite whole file
                        byte[] existing;
                        if (stream.CanRead)
                        {
                            stream.Position = 0;
                            existing = new byte[stream.Length];
                            stream.Read(existing, 0, existing.Length);
                        }
                        else
                        {
                            existing = Array.Empty<byte>();
                        }

                        byte[] combined = new byte[existing.Length + bytes.Length];
                        existing.CopyTo(combined, 0);
                        bytes.CopyTo(combined, existing.Length);

                        stream.Close();
                        VFSManager.DeleteFile(path);
                        VFSManager.CreateFile(path);
                        vfsFile = VFSManager.GetFile(path);
                        stream = vfsFile.GetFileStream();
                        stream.Write(combined, 0, combined.Length);
                    }
                }

                stream.Close();

                string successMsg = mode == "append"
                    ? "Appended " + bytes.Length.ToString() + " bytes to '" + filename + "'"
                    : "Overwrote '" + filename + "' with " + bytes.Length.ToString() + " bytes";
                ConsoleHelpers.WriteLine(successMsg);
            }
            catch (GenericException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GenericException("Write failed: " + ex.Message, "write", "filesystem");
            }
        }

        public static void FsInfo(CosmosVFS fs, FileSystemHelpers fsh)
        {
            var disks = fs.Disks;
            foreach (var disk in disks)
            {
                if (disk?.Host == null)
                {
                    ConsoleHelpers.WriteLine("Disk host unavailable");
                    continue;
                }

                ConsoleHelpers.WriteLine(fsh.BlockDeviceTypeToString(disk.Host.Type));

                foreach (var partition in disk.Partitions)
                {
                    ConsoleHelpers.WriteLine(partition.Host.ToString());
                    ConsoleHelpers.WriteLine(partition.RootPath);
                    ConsoleHelpers.WriteLine(partition.MountedFS.Size.ToString());
                }
            }
        }
    }
}