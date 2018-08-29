using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Postpostprocessing
{
    class FileMerger
    {
        public List<string> targetFiles;
        NCFileMerger[] files;
        FileArray array;

        public FileMerger()
        {
            CheckForFiles();
        }

        /// <summary>
        /// returns the amount of merger candidates found
        /// </summary>
        /// <returns></returns>
        public int CheckMergerCandidates()
        {
            return targetFiles.Count;
        }

        public void OpenMergerConsole()
        {
            InitializeFileList(); // initialising files
     

            //the actual console 
            ListFiles();
            array = new FileArray();
            Console.WriteLine("Starting filemerger...");
            while (true)
            {
                Console.WriteLine("\"h\" for Help, \"r\" to restart merger process, \"a\" add files to array, \"i\" for info about the array" + "\n"
                    + "\"s\" to save files, \"e\" to exit the filemerger.");
                ConsoleKeyInfo c = Console.ReadKey(true);
                if (c.KeyChar == 'h') Help();
                if (c.KeyChar == 'r') Restart();
                if (c.KeyChar == 'a') AddFile();
                if (c.KeyChar == 'i') Info();
                if (c.KeyChar == 's') Save();
                if (c.KeyChar == 'e') break;
            }
        }

        void ListFiles()
        {
            Console.WriteLine(files.Length + " files available");
            int i = 0;
            while (i < files.Length)
            {
                Console.Write("   " + files[i].Filename);
                if (files[i].Comment != "") Console.WriteLine(": " + files[i].Comment);
                else Console.WriteLine();
                Console.WriteLine("     Size: " + files[i].GetXLength() + "x" + files[i].GetYLength());
                i++;
            }
        }
        /// <summary>
        /// Lists the commands available
        /// </summary>
        void Help()
        {
            Console.WriteLine("you asked for help, too bad there is none.");
        }
        /// <summary>
        /// Resets the array making process
        /// </summary>
        void Restart()
        {
            Console.WriteLine("Press c to reset array process, or any other key to abort reset");
            ConsoleKeyInfo c = Console.ReadKey(true);
            if (c.KeyChar == 'c')
            {
                array = new FileArray();
                Console.WriteLine("Reset the array...");
                ListFiles();
            }
            else Console.WriteLine("returning...");

        }
        void AddFile()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("Adding files... e to exit...\nformat: column:firstfilename,secondfilename,etc");
                    string input = Console.ReadLine(); Console.WriteLine();
                    if (input == "exit") { Console.WriteLine("Exiting AddFile..."); break; }
                    int column = int.Parse(input.Substring(0, input.IndexOf(":"))) - 1;
                    List<string[]> list = new List<string[]>();
                    input = input.Substring(input.IndexOf(":") + 1);
                    while (input.Length > 0)
                    {
                        string[] file = new string[2]; // 0=filename, 1=rotation option
                        file[0] = input.Substring(0, 4);
                        if (!targetFiles.Contains(file[0])) Console.WriteLine("Bad file name detected, try again");
                        string temp = "";
                        if (input.Length > 4) temp = input.Substring(4, 1);
                        if (temp == "l" || temp == "r" || temp == "t") file[1] = temp;
                        else file[1] = "";
                        list.Add(file);
                        if (input.IndexOf(",") > -1) input = input.Substring(input.IndexOf(",") + 1);
                        else input = "";
                    }
                    array.AddToArray(list, column);
                    Console.WriteLine("Successfully added files to array.");
                    Info();
                }
                catch (Exception e) { Console.WriteLine("Bad input, try again..(error " + e + ")"); }
            }
        }
        void Info()
        {
            Console.WriteLine("Used area is now " + array.UsedArea() + " using " + array.Coloumns);
            int i = 0;
            while (i < array.Coloumns)
            {
                Console.Write("[" + array.ColumnSize(i) + ":" + array.ColumnLength(i) + "] ");
                i++;
            }
            Console.WriteLine();
        }
        /// <summary>
        /// Saves the current array of files into a single .nc file
        /// </summary>
        void Save()
        {
            array.SaveFile();
            CheckForFiles(); //updates targetFiles list
            ListFiles();
        }

        /// <summary>
        /// Checks for suitable files for merging and adds them to targetFiles
        /// </summary>
        void CheckForFiles()
        {
            string[] filenames = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.nc");
            targetFiles = new List<string>();
            int i = 0;
            while (i < filenames.Length)
            {
                int name = 0;
                string fileName = Path.GetFileNameWithoutExtension(filenames[i]);
                if (int.TryParse(fileName, out name) && ((2000 < name)) && (name < 3000))
                {
                    targetFiles.Add(Path.GetFileNameWithoutExtension(filenames[i]));
                }
                i++;
            }
            InitializeFileList();
        }

        /// <summary>
        /// Initialized the list of merger files
        /// </summary>
        void InitializeFileList()
        {
            files = new NCFileMerger[targetFiles.Count];
            int i = 0;
            foreach (var name in targetFiles)
            {
                files[i] = new NCFileMerger(name + ".nc");
                i++;
            }
        }
    }
}
