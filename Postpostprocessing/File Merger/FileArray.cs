using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Postpostprocessing
{
    class FileArray
    {
        double spacing = 10;
        public double Spacing { get { return spacing; } set { spacing = value; } }
        double edgeSpacing = 20;
        public double EdgeSpacing { get { return edgeSpacing; } set { edgeSpacing = value; } }
        double usedAreaX;
        double usedAreaY;

        List<List<NCFileMerger>> array;

        public FileArray()
        {
            array = new List<List<NCFileMerger>>();
            array.Add(new List<NCFileMerger>());
        }

        void AddArray()
        {
            array.Add(new List<NCFileMerger>());
        }
        /// <summary>
        /// Adds a new file to the array spesified array, adds a new array if array doesnt exist
        /// </summary>
        /// <param name="i">the array you want to add to</param>
        /// <param name="filename">name of file being added</param>
        public void AddToArray(int i, string filename)
        {
            if (i >= array.Count) i = array.Count; AddArray();
            array[i].Add(new NCFileMerger(filename + ".nc"));
        }
        public void AddToArray(List<string[]> files, int i)
        {
            foreach (string[] file in files)
            {
                if (i >= array.Count) { i = array.Count; AddArray(); }
                array[i].Add(new NCFileMerger(file[0] + ".nc"));
                if (file[1] != "")
                {
                    List<NCFileMerger> l = array[i];
                    int location = l.Count - 1;
                    l[location].Rotate(Convert.ToChar(file[1]));
                }
            }
            UpdateOffsets();
        }

        public void UpdateOffsets()
        {
            int i = 0; usedAreaX = 0; usedAreaY = 0;
            double xOffsett = EdgeSpacing;
            double newX = 0;
            while (i < array.Count)
            {
                List<NCFileMerger> list = array[i];
                int j = 0;
                double yOffset = edgeSpacing;
                while (j < list.Count)
                {
                    list[j].XArrayOffset = xOffsett;
                    double temp = list[j].XArrayOffset + list[j].GetXLength() + spacing;
                    if (temp > newX) newX = temp;
                    if (usedAreaX < newX) usedAreaX = newX;

                    list[j].YArrayOffset = yOffset;
                    yOffset = list[j].YArrayOffset + list[j].GetYLength() + spacing;
                    if (usedAreaY<yOffset) usedAreaY = yOffset;
                    j++;
                }
                xOffsett = newX;
                i++;
            }
        }

        public string UsedArea()
        {
            return "X" + Math.Round(usedAreaX, 1) + " x Y" + Math.Round(usedAreaY, 1);
        }
    }
}
