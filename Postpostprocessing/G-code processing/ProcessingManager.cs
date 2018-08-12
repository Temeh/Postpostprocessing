using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;


namespace Postpostprocessing
{
    /// <summary>
    /// Main class that acts as the centre of 
    /// </summary>
    class ProcessingManager
    {
        /// <summary>
        /// takes a string[] of filenames to look up
        /// </summary>
        /// <param name="fileNames">names of the files to check/process</param>
        public ProcessingManager()
        {
            Stopwatch sw = new Stopwatch();            sw.Start();
            FileManager fm = new FileManager();
            NCFile[] files = fm.Files;

            KnifeHomeing kh = new KnifeHomeing(files);
            LongArcFixer laf = new LongArcFixer(files);
            KnifeDirection kd = new KnifeDirection(files);
            CheckXYZ checkXYZ = new CheckXYZ(files);
            Console.WriteLine("Checks and modifications complete!");
            Console.WriteLine("Saving files to disk");
            fm.SaveFile();
            Console.WriteLine("Modified and saved all files!");
            Console.WriteLine("Copying files");
            fm.CopyFile();

            sw.Stop();
            Console.WriteLine("Work complete in "+sw.Elapsed.ToString("mm\\:ss\\.fff"));
            Console.WriteLine("All done, press any key to close");
            Console.ReadKey();

        }
    }
}
