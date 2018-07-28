using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Globalization;

namespace Postpostprocessing
{
    class NCFile
    {
        readonly string filename;
        public string Filename { get { return filename; } }
        string comment;
        public string Comment { get { return comment; } }
        protected string[] lines;
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
            FixComment();
        }
        /// <summary>
        /// Fixes comments so that if comments exists on the second line, it will be added as a comment on the first line
        /// If first line has a comment already, the second line comment as added at the end
        /// </summary>
        void FixComment()
        {
            string firstComment = "";
            string secondComment = "";
            if (lines[0].IndexOf("(") > -1)
            {
                firstComment = lines[0].Substring(lines[0].IndexOf("(") + 1);
                if (firstComment.Contains(")")) firstComment = firstComment.Substring(0, firstComment.IndexOf(")"));
                else firstComment = "";
            }
            if (lines[1].IndexOf("(") > -1)
            {
                secondComment = lines[1].Substring(lines[1].IndexOf("(") + 1);
                if (secondComment.Contains(")"))
                {
                    secondComment = secondComment.Substring(0, secondComment.IndexOf(")"));
                    lines[1] = lines[1].Substring(0, lines[1].IndexOf("("));
                }
                else secondComment = "";
            }
            firstComment = firstComment +" "+ secondComment;
            if (!(firstComment == " "))
            {
                lines[0] = "(" + firstComment + ")";
                comment = firstComment;
            }
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
            //   SplitString(CombineLines());
        }

        /// <summary>
        /// checks the distance bewteen 2 points on the x/y surface
        /// </summary>
        /// <param name="first">double with the location of the first point</param>
        /// <param name="second">double with the location of the second point</param>
        /// <returns></returns>
        public double CheckDistance(double[] first, double[] second)
        {
            double xLength = Math.Abs(first[0] - second[0]);
            double yLenght = Math.Abs(first[1] - second[1]);
            if (xLength == 0) return yLenght;
            if (yLenght == 0) return xLength;
            double length = Math.Sqrt(Math.Pow(xLength, 2) + Math.Pow(yLenght, 2));
            return length;
        }

        /// <summary>
        /// takes a string of doubles with coordinates of start x/y and end x/y and checks what direction  movement is happening in
        /// </summary>
        /// <param name="start">start location</param>
        /// <param name="end">end location</param>
        /// <returns>the angle</returns>
        public double CheckDirection(double[] start, double[] end)
        {
            double x = end[0] - start[0];
            double y = end[1] - start[1];
            double hypotenus = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
            double angle = -1;
            if (x == 0 || y == 0)
            {
                if (x > 0) angle = 0;
                else if (x < 0) angle = 180;
                else if (y > 0) angle = 90;
                else if (y < 0) angle = 270;
            }
            else
            {
                angle = Math.Acos((x / hypotenus)) * 180 / Math.PI;
                if (y < 0) angle = 360 - angle;
                angle = Math.Round(angle, 3);
            }
            return angle;
        }

        /// <summary>
        /// Takes a double and converts it to a string with upto 3 decimals, and always having a decimal separator even if double is a whole number.
        /// </summary>
        /// <param name="number">the double you want to turn to a string</param>
        /// <returns></returns>
        public string DoubleToString(double number)
        {
            number = Math.Round(number, 3);
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            string stringy = number.ToString(nfi);
            if (stringy.IndexOf(".") < 0)
            {
                stringy = stringy + ".";
            }
            return stringy;
        }
    }


}
