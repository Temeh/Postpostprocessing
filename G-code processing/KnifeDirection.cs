using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Postpostprocessing
{
    /// <summary>
    /// Updates the G code to prevent the knife form entering the material before it has turned to face the correct direction
    /// </summary>
    class KnifeDirection
    {
        NCFile file;
        double currentX;
        double currentY;
        double currentZ;

        public KnifeDirection(NCFile[] files)
        {
            int i = 0;
            while (i < files.Length)
            {
                file = files[i];
                if (!files[i].PreviousPPP) DetectRapidMovement();
                i++;
            }
        }
        /// <summary>
        /// Attempts to find new cuts outs by looking for rapid movement
        /// </summary>
        /// <returns></returns>
        NCFile DetectRapidMovement()
        {
            int i = 0;
            while (i < file.AmountLines())
            {
                if (file.CheckLineBlock(i)) if (checkATC(i) == 25)
                    {
                        i++;
                        while (i < file.AmountLines())
                        {
                            if (checkATC(i) == 25) break;
                            string line = file.GetCode(i);
                            if (line.StartsWith("G00")) if (line.Contains(" X") || line.Contains(" Y")) //Checks for rapid movement on x/y axis("G00")
                                {
                                    double newX; double newY;
                                    double nextX; double nextY;
                                    newX = GetLocation(line, 'X'); newY = GetLocation(line, 'Y'); int j = i + 1;
                                    while (j < file.AmountLines())
                                    {
                                        line = file.GetCode(j);
                                        if (line.Contains('X') || line.Contains('Y'))
                                        {
                                            if (line.Contains('X')) nextX = GetLocation(line, 'X'); else nextX = newX;
                                            if (line.Contains('Y')) nextY = GetLocation(line, 'Y'); else nextY = newY;
                                            InsertNewPoint(nextX, nextY, newX, newY, i);
                                            i = j;
                                            break;
                                        }
                                        j++;
                                    }
                                }
                            i++;
                        }
                    }
                i++;
            }



            return file;
        }
        /// <summary>
        /// changes the new x/y to be a point .1mm away from the original location, away from the next location, to force the knife to turn the right direction before entering the material
        /// </summary>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        void InsertNewPoint(double x2, double y2, double x1, double y1, int i)
        {
            //adds a new line with the original x/y coordinates
            string newline = "N" + (file.GetLineBlock(i) + 1) + " " + file.GetCode(i);
            file.AddNewLine(i, newline);

            //Finds how much relative x/y should change for newX/newY
            double relativeX = -(x2 - x1);
            double relativeY = -(y2 - y1);
            double changeX; if (relativeX < 0) changeX = relativeX / -1; else changeX = relativeX;
            double changeY; if (relativeY < 0) changeY = relativeY / -1; else changeY = relativeY;
            if (changeX > changeY)
            {
                relativeY = relativeY / Math.Abs(relativeX) * .1;
                relativeX = relativeX / Math.Abs(relativeX) * .1;
            }
            else
            {
                relativeX = relativeX / Math.Abs(relativeY) * .1;
                relativeY = relativeY / Math.Abs(relativeY) * .1;
            }
            string line = "N" + file.GetLineBlock(i) + " G00 X" + Math.Round((x1 + relativeX), 3) + " Y" + Math.Round((y1 + relativeY), 3);
            file.UpdateLine(i, line);

        }

        /// <summary>
        /// Checks if there is a change of tool
        /// </summary>
        /// <param name="i">int of current line number</param>
        /// <returns>int of new tool number, 0 for no tool change</returns>
        int checkATC(int i)
        {
            string line = file.GetCode(i);
            if (line.IndexOf("T") == 0)
            {
                int value;
                if (int.TryParse(line.Substring(1, line.IndexOf(" ") - 1), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value))
                    return value;
            }
            return 0;
        }
        /// <summary>
        /// Finds a spesified axis new coordinate in a string
        /// </summary>
        /// <param name="line">the string you want to check</param>
        /// <param name="c">the axis you want</param>
        /// <returns></returns>
        double GetLocation(string line, char c)
        {
            double value;
            line = line.Substring(line.IndexOf(c) + 1);
            if (line.IndexOf(" ") != -1) line = line.Substring(0, line.IndexOf(" "));
            double.TryParse(line, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value);
            return value;
        }
    }
}
