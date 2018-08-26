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
            String[] fl = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.nc");
            List<string> fileList = new List<string>();
            int i = 0;
            while (i < fl.Length)
            {
                string s = Path.GetFileNameWithoutExtension(fl[i]);
                int number;
                if (int.TryParse(s, out number))
                {
                    if (number < 2000) fileList.Add(fl[i]);
                }
                i++;
            }

            if (fileList.Count > 0)
            {
                Console.WriteLine("Found " + fileList.Count + " .nc files:");
                i = 0;
                while (i < fileList.Count)
                {
                    fileList[i] = Path.GetFileName(fileList[i]);
                    Console.Write(fileList[i] + ", ");
                    i++;
                }
                Console.WriteLine();
                files = new NCFile[fileList.Count];
                i = 0;
                while (i < fileList.Count)
                {
                    files[i] = new NCFile(fileList[i]);
                    i++;
                }
            }
        }

        public void SaveFile()
        {
            int i = 0; List<string> alreadyChecked = new List<string>();
            Console.Write("Saving file(s)");
            while (i < files.Length)
            {
                if (!files[i].PreviousPPP)
                {
                    Console.Write(", " + files[i].Filename);
                    File.WriteAllText(files[i].Filename, files[i].CombineLines());
                }
                else
                {
                    alreadyChecked.Add(files[i].Filename);
                }
                i++;
            }
            Console.WriteLine();

            if (alreadyChecked.Count > 0)
            {
                i = 0;
                while (i < alreadyChecked.Count)
                {
                    Console.WriteLine(alreadyChecked[i] + " was already checked and will not be updated");
                    i++;
                }
            }
            if (files.Length > 0) Console.WriteLine("Saved all files");
        }

        public void CopyFile()
        {
            int i = 0;

            while (true) //Checks that the target directory exists before copying files
            {
                if (Directory.Exists(ConfigurationManager.AppSettings["savePath"])) break;
                else
                {
                    Console.WriteLine("Warning, output directory appears to not exist");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey(true);
                }
            }

            while (i < files.Length) // saves files
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
