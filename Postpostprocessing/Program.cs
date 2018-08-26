using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Postpostprocessing
{
    class Program
    {
        static void Main(string[] args)
        {
            FileMerger fm =new FileMerger();
            int filesFound=fm.CheckMergerCandidates();
            if (filesFound>0)
            {
                Console.WriteLine("Found " + filesFound + " merger candidates...");
                while (true)
                {
                    Console.WriteLine("press m for merger console or press space to continue to post processing");
                    ConsoleKeyInfo c = Console.ReadKey();
                    if (c.KeyChar == 'm')
                    {
                        fm.OpenMergerConsole();

                    }
                    else if (c.KeyChar == ' ')
                    {
                        break;
                    }
                }
            }
            ProcessingManager pm = new ProcessingManager();
        }
    }
}
