using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;

namespace Postpostprocessing
{
    class FileManager
    {
        NCFile[] files;
        public NCFile[] Files { get { return files; } }
        /// <summary>
        /// Gets a list of all the *.nc files in the apps directory, for further processing
        /// </summary>
        public FileManager()
        {
            Console.WriteLine("Finding files...");
            String[] fileList = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.nc");
            Console.WriteLine("Found " + fileList.Length + " .nc files:");
            int i = 0;
            while (i < fileList.Length)
            {
                fileList[i] = Path.GetFileName(fileList[i]);
                Console.WriteLine("   " + fileList[i]);
                i++;
            }
            files = new NCFile[fileList.Length];
            i = 0;
            while (i < fileList.Length)
            {
                files[i] = new NCFile(fileList[i]);
                i++;
            }
        }

        public void SaveFile()
        {
            int i = 0;
            while (i < files.Length)
            {
                if (!files[i].PreviousPPP)
                {
                    Console.WriteLine("..Saving " + files[i].Filename);
                    File.WriteAllText(files[i].Filename, files[i].CombineLines());
                }
                else Console.WriteLine(files[i].Filename + " was already checked and will not be updated");
                i++;
            }
        }

        public void CopyFile()
        {
            int i = 0;
            while (i < files.Length)
            {
                if (!files[i].CheckNegativeXYZ())
                {
                    Console.WriteLine("..Copying " + files[i].Filename);
                    File.WriteAllText(ConfigurationManager.AppSettings["savePath"] + files[i].Filename, files[i].CombineLines());
                }
                else Console.WriteLine(files[i].Filename + " contains negative XYZ vallues and was not copied");
                i++;
            }
        }
    }

}
