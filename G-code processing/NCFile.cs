using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Postpostprocessing
{
    class NCFile
    {
        readonly string filename;
        public string Filename { get { return filename; } }
        string[] lines;
        bool negativeX = false;
        bool negativeY = false;
        bool negativeZ = false;
        bool previousPPP = false;
       public bool PreviousPPP { get { return previousPPP; } }


        public NCFile(string name)
        {
            filename = name;
            StreamReader sr = new StreamReader(filename);
            SplitString(sr.ReadToEnd());
            sr.Dispose();
            if (lines[0].Contains("XYZ")) previousPPP = true;
        }
        /// <summary>
        /// Splits a string into one string for each linebreak
        /// </summary>
        /// <param name="text">the string you want to split</param>
        void SplitString(string text)
        {
            text = text.Replace("\r", "");
            lines = text.Split('\n');
        }
        /// <summary>
        /// Combines all the lines in the file and returns them as a single string
        /// </summary>
        /// <returns>string of the whole file</returns>
       public string CombineLines()
        {

            string text;
            text = lines[0];
            int i = 1;
            while (i < lines.Length)
            {
                text = text + "\r\n" + lines[i];
                i++;
            }
            return text;
        }

        /// <summary>
        /// Returns a spesific line in the file
        /// </summary>
        /// <param name="line">int, of the line you want</param>
        /// <returns></returns>
        public string GetLine(int line)
        {
            return lines[line];
        }
        /// <summary>
        /// Returns the amount of lines in the tile as an int
        /// </summary>
        /// <returns></returns>
        public int AmountLines()
        {
            return lines.Length;
        }
        /// <summary>
        /// Checks if a line contains machine code
        /// </summary>
        /// <param name="i">int of the line in question</param>
        /// <returns></returns>
        public bool CheckLineBlock(int i)
        {
            if (lines[i].StartsWith("N") && (int.TryParse(lines[i].Substring(1, lines[i].IndexOf(" ")), out i))) return true;
            else return false;
        }
        /// <summary>
        /// Returns the line (block) number in the program, should only be used if you know this line contains a lineblock
        /// </summary>
        /// <param name="i">the line in the file you want</param>
        /// <returns>int of the program line number</returns>
        public int GetLineBlock(int i)
        {
            return int.Parse(lines[i].Substring(1, lines[i].IndexOf(" ")));
        }

        /// <summary>
        /// Returns the machine code of the line
        /// </summary>
        /// <param name="i">int indicating the line in question</param>
        /// <returns></returns>
        public string GetCode(int i)
        {
            string machineCode = lines[i].Substring(lines[i].IndexOf(" ") + 1);
            return machineCode;

        }

        /// <summary>
        /// Overwrites a line with a new string
        /// </summary>
        /// <param name="i">the line you want to change</param>
        /// <param name="newline">string containing the new line</param>
        public void UpdateLine(int i, string newline)
        {
            lines[i] = newline;
        }

        /// <summary>
        /// Marks an axis as having negative values above what is tolerated
        /// </summary>
        /// <param name="xyz"></param>
        public void negativeXYZ(char xyz)
        {
            if (xyz == 'X') negativeX = true;
            if (xyz == 'Y') negativeY = true;
            if (xyz == 'Z') negativeZ = true;
        }

        /// <summary>
        /// Returns a bool indicating weather or not negative XYZ values were detected
        /// </summary>
        /// <returns>bool</returns>
        public bool CheckNegativeXYZ()
        {
            if (negativeX) return true;
            if (negativeY) return true;
            if (negativeZ) return true;
            return false;
        }
        /// <summary>
        /// Returns a bool indicating weather or not negative XYZ values were detected on a spesific axis
        /// </summary>
        /// <param name="c">axis you want to check</param>
        /// <returns></returns>
        public bool CheckNegativeXYZ(char c)
        {
            if (c == 'X') return negativeX;
            if (c == 'Y') return negativeY;
            if (c == 'Z') return negativeZ;
            return true;
        }

        /// <summary>
        /// Adds a new line to the file
        /// </summary>
        /// <param name="i">Line nummber of the line preceeding the new line</param>
        /// <param name="newline">string containing the new line</param>
        public void AddNewLine(int i, string newline)
        {
            lines[i] = lines[i] + "\r\n" + newline;
            SplitString(CombineLines());
        }

    }

}
