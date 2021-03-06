﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

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
        /// <summary>
        /// returns the amount of coloumns in the array
        /// </summary>
        public int Coloumns { get { return array.Count; } }
        /// <summary>
        /// Returns the amount of objects in the spesified coloumn
        /// </summary>
        /// <param name="i">the coloumn you want info on</param>
        /// <returns></returns>
        public int ColumnSize(int i) { return array[i].Count; }
        /// <summary>
        /// the length of the coloumn
        /// </summary>
        /// <param name="i">the coloumn you want info on</param>
        /// <returns></returns>
        public double ColumnLength(int i) 
        {
            List<NCFileMerger> l = array[i];
            double d =l[l.Count-1].YPlacement + l[l.Count-1].GetYLength();
            return d;
        }
        CombinedFile cf;

        public FileArray()
        {
            array = new List<List<NCFileMerger>>();
            //array.Add(new List<NCFileMerger>());
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
           // array[i].Add(new NCFileMerger(filename + ".nc"));
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
                    list[j].XPlacement = xOffsett;
                    double temp = list[j].XPlacement + list[j].GetXLength() + spacing;
                    if (temp > newX) newX = temp;
                    if (usedAreaX < newX) usedAreaX = newX;

                    list[j].YPlacement = yOffset;
                    yOffset = list[j].YPlacement + list[j].GetYLength() + spacing;
                    if (usedAreaY < yOffset) usedAreaY = yOffset;
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

        public void CombineFiles()
        {
            cf = new CombinedFile();
            int i = 0;
            while (i < array.Count)
            {
                List<NCFileMerger> list = array[i];
                int j = 0;
                while (j < list.Count)
                {
                    cf.AddFile(list[j]);

                    j++;
                }
                i++;
            }
        }
        /// <summary>
        /// Saves file to disk...
        /// </summary>
        public void SaveFile() { SaveFile("comment"); }
        /// <summary>
        /// Saves file to disk...
        /// </summary>
        /// <param name="comment">comment</param>
        public void SaveFile(string comment)
        {
            //Finds what files already exists in the folder           
            string[] filenames = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.nc");
            List<string> ncFiles = new List<string>();
            int i = 0;
            while (i < filenames.Length)
            {
                int name = 0;
                string fileName = Path.GetFileNameWithoutExtension(filenames[i]);
                if (int.TryParse(fileName, out name) && ((0 < name)) && (name < 3000))
                {
                    ncFiles.Add(Path.GetFileNameWithoutExtension(filenames[i]));
                }
                i++;
            }

            //Gets user input regarding filename
            string saveName=""; bool foundName = false;
            while (!foundName)
            {
                Console.WriteLine("Saving file... Please type in a name, or press enter for autogenerated name");
                string input = Console.ReadLine(); int number;
                if (input == "") //Use deafult name
                {
                    i = 1001;
                    while (i < 2000)
                    {
                        if (!ncFiles.Contains(Convert.ToString(i))) { saveName = Convert.ToString(i); foundName = true; break; }
                        i++;
                    }
                    if (foundName) break;
                }
                else if ((input.Length == 4) && (int.TryParse(input, out number))) //Use custom name
                {
                    if (ncFiles.Contains(input)) { Console.WriteLine("Error, file already exists... Try again!"); continue; }
                    else { saveName = input; break; }
                }
                else Console.WriteLine("Incompatible name. Name must be from 1 to 2000"); //You are bad at naming :(
            }

            //Saves the file
            Console.WriteLine("Saving file as "+saveName+".nc");
            CombineFiles();
            string text = cf.ReturnCombinedFile(comment);
            StreamWriter sr = new StreamWriter(saveName + ".nc");
            sr.Write(text);
            sr.Dispose();
            sr = null;
            Console.WriteLine("Successfully saved file!");

        }
    }

    class CombinedFile
    {
        List<string> toolComments = new List<string>();
        List<Tool> tools = new List<Tool>();

        public void AddFile(NCFileMerger file)
        {
            //Finds commens about tools used at top of tile
            int i = 2;
            while (i < file.AmountLines())
            {
                if (file.CheckLineBlock(i)) break;
                else { string line = file.GetLine(i); if (line.Contains("(") || line.Contains(")")) toolComments = ToolComments(toolComments, line); }
                i++;
            }
            //Adds tools
            while (i < file.AmountLines())
            {
                if (file.CheckLineBlock(i))
                {
                    string line = file.GetCode(i);
                    if (line.Contains("T"))
                    {
                        line = line.Substring(line.IndexOf("T"));
                        int t = Convert.ToInt32(line.Substring(1, line.IndexOf(" ")));
                        int j = 0;
                        bool toolExists = false;
                        while (j < tools.Count)
                        {
                            if (t == tools[j].ToolID)
                            {
                                i = tools[j].AddTool(file, i + 1);
                                toolExists = true;
                            }
                            j++;
                        }
                        if (!toolExists)
                        {
                            tools.Add(new Tool(t));
                            i = tools[j].AddTool(file, i + 1);
                        }
                    }
                }
                i++;
            }
        }

        /// <summary>
        /// Finds what tool the new line contains, and checks if it is already in the tool list. Adds it if not, or updates the list if it is already present but new line shows tool going deeper
        /// </summary>
        /// <param name="list">the existing tool list</param>
        /// <param name="newLine">new line to check/add</param>
        /// <returns></returns>
        List<string> ToolComments(List<string> list, string newLine)
        {
            string newTool = newLine.Substring(newLine.IndexOf("T"), newLine.IndexOf(" "));
            int i = 0;
            while (i < list.Count)
            {
                string oldTool = list[i];
                oldTool = oldTool.Substring(oldTool.IndexOf("T"), oldTool.IndexOf(" "));
                if (oldTool == newTool)
                {
                    string temp = newLine.Substring(newLine.IndexOf("ZMIN") + 5);
                    double newToolDebth = double.Parse(temp.Substring(0, temp.IndexOf(" ")), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"));
                    temp = list[i]; temp = temp.Substring(newLine.IndexOf("ZMIN") + 5);
                    double oldToolDebth = double.Parse(temp.Substring(0, temp.IndexOf(" ")), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"));
                    if (newToolDebth > oldToolDebth)
                    {
                        list[i] = newLine;
                        return list;
                    }
                    else return list;
                }
                i++;
            }
            list.Add(newLine);
            return list;
        }

        public string ReturnCombinedFile(string comment)
        {
            string newline = "\n";
            string text = "(" + comment + ")" + newline;
            text += "O1001" + newline;
            int i = 0;
            while (i < toolComments.Count)
            {
                text += toolComments[i] + newline;
                i++;
            }
            //G-codes for program start
            text += "N10 G90 G94 G17 G49 G40 G80" + newline;
            text += "N15 G28 G91 Z0." + newline;
            text += "N20 G90" + newline + newline;
            //Makes a list with the order if the tools in tools from 1 to 25
            List<int> toolOrdering = new List<int>();
            i = 0;
            while (i < 25)
            {
                int j = 0;
                while (j < tools.Count)
                {
                    if (tools[j].ToolID == (i + 1)) toolOrdering.Add(j);
                    j++;
                }
                i++;
            }
            //Adds the tools
            i = 0;
            while (i < toolOrdering.Count)
            {
                if (i == 0) text += tools[toolOrdering[i]].ReturnTool(25);
                else text += tools[toolOrdering[i]].ReturnTool(tools[toolOrdering[i - 1]].CodeBlock);
                i++;
            }
            return text;
        }
    }

    class Tool
    {
        int toolID; public int ToolID { get { return toolID; } }
        int codeBlock; public int CodeBlock { get { return codeBlock; } }
        string toolOffset = "";
        string toolHeightOffset = "";
        string toolStart = "";
        List<string> Gcode = new List<string>();

        public Tool(int t)
        {
            toolID = t;
        }

        public int AddTool(NCFileMerger file, int i)
        {
            while (i < file.AmountLines())
            {
                if (file.CheckLineBlock(i))
                {
                    string line = file.GetCode(i);
                    if (line.StartsWith("S")) toolStart = line;
                    else if (line.StartsWith("G54") || line.StartsWith("G56")) toolOffset = line;
                    else if (line.StartsWith("G43")) toolHeightOffset = line;
                    else if (line.StartsWith("M05")) return i;
                    else if (line.StartsWith("G28") || line.StartsWith("G90") || line.StartsWith("G49")) { i++; continue; }
                    else if (line.StartsWith("T")) return (i - 1);
                    else Gcode.Add(file.ReturnActualXYZ(i));
                }
                i++;
            }
            return i;
        }

        /// <summary>
        /// Returns all the code associated with a tool as a string
        /// </summary>
        /// <param name="cb">Current Codeblock number</param>
        /// <returns></returns>
        public string ReturnTool(int cb)
        {
            codeBlock = cb + 5; string tool; string newline = "\n"; int i = 0;
            tool = "N" + codeBlock + " T" + toolID + " M06" + newline; codeBlock += 5;
            tool = tool + "N" + codeBlock + " " + toolStart + newline; codeBlock += 5;
            tool = tool + "N" + codeBlock + " " + toolOffset + newline; codeBlock += 5;
            tool = tool + "N" + codeBlock + " " + Gcode[i] + newline; codeBlock += 5; i++;
            tool = tool + "N" + codeBlock + " " + toolHeightOffset + newline; codeBlock += 5;
            
            while (i < Gcode.Count)
            {
                tool = tool + "N" + codeBlock + " " + Gcode[i] + newline; codeBlock += 5;
                i++;
            }
            tool = tool + "N" + codeBlock + " M05" + newline; codeBlock += 5;
            tool = tool + "N" + codeBlock + " G28 G91 Z0." + newline; codeBlock += 5;
            tool = tool + "N" + codeBlock + " G90" + newline; codeBlock += 5;
            tool = tool + "N" + codeBlock + " G49" + newline; codeBlock += 5;
            tool = tool + newline;
            return tool;
        }
    }
}
