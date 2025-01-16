using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Specialized;
using System.IO;

namespace FileManager
{
    public class FileSystem
    {
        public int CopyFilesToClipboard(StringCollection filePaths)
        {
            if (filePaths.Count != 0)
            {
                Clipboard.SetFileDropList(filePaths);
                return 1;
            }
            else
            {
                return 0;
            }
        }
        public int CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = System.IO.Path.GetFileName(file);
                var destFile = System.IO.Path.Combine(destDir, fileName);
                try
                {
                    File.Copy(file, destFile, true);
                }
                catch (Exception )
                {
                    return -1;
                    
                }
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = System.IO.Path.GetFileName(subDir);
                var destSubDir = System.IO.Path.Combine(destDir, dirName);
                CopyDirectory(subDir, destSubDir);
            }
            return 1;
        }
        public int MoveToRecycleBin(string pathFile)
        {
            try
            {
                const int ssfBITBUCKET = 0xa;
                dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                var recycleBin = shell.Namespace(ssfBITBUCKET);
                recycleBin.MoveHere(pathFile);
            }
            catch
            {
                return -1;
            }
            return 1;
        }
        public int CreateNewFile(string itemType, string newPath)
        {
            try
            {
                if (itemType == "Каталог")
                {
                    Directory.CreateDirectory(newPath);
                }
                else if (itemType == "Файл")
                {
                    File.Create(newPath).Dispose();
                }
                return 1;
            }
            catch
            {
                return -1;
            }

        }
        public int RenameFile(string oldFullPath,string newFullPath)
        {
            if (File.Exists(oldFullPath))
            {
                File.Move(oldFullPath, newFullPath);
            }
            else if (Directory.Exists(oldFullPath))
            {
                Directory.Move(oldFullPath, newFullPath);
            }
            else
            {
                return -1;
            }
            return 1;
        }
        public long MoveFile(FileStream destStream, byte[] buffer, int bytesReadInCurrentOperation)
        {
            long bytesRead=0;
            destStream.Write(buffer, 0, bytesReadInCurrentOperation);
            bytesRead += bytesReadInCurrentOperation;
            return bytesRead;
        }
    }
}
