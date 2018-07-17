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
            bool knifeActive = false;
            bool rapidMovement = false;
            double[] whereAmINow = new double[2]; //0=X, 1=Y
            int i = 0;
            while (i < file.AmountLines())
            {
                if (file.CheckLineBlock(i))
                {
                    string line = file.GetCode(i);

                    if (knifeActive)
                    {
                        whereAmINow = GetLocation(line, whereAmINow);
                        if (line.StartsWith("G00")) rapidMovement = true;
                    }
                    if (rapidMovement)
                    {
                        while (rapidMovement && i < file.AmountLines())
                        {
                            line = file.GetCode(i);
                            if (line.Contains("X") || line.Contains("Y"))
                            {
                                whereAmINow = GetLocation(line, whereAmINow);
                                int j = i + 1;
                                while (j < file.AmountLines())
                                {
                                    line = file.GetCode(j);
                                    if (line.Contains('X') || line.Contains('Y'))
                                    {
                                        double[] nextPoint = new double[2];
                                        nextPoint[0] = whereAmINow[0];
                                        nextPoint[1] = whereAmINow[1];
                                        nextPoint = GetLocation(line, nextPoint);
                                        InsertNewPoint(nextPoint[0], nextPoint[1], whereAmINow[0], whereAmINow[1], i);
                                        i = j-1; rapidMovement = false;
                                        break;
                                    }
                                    j++;
                                }
                            }
                            i++;
                        }
                        continue;
                    }
                    if (line.StartsWith("T"))//checks for tool change and checks if new tool is knife or not
                    {
                        int value;
                        if (int.TryParse(line.Substring(1, line.IndexOf(" ") - 1), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value))
                        {
                            if (value == 25) knifeActive = true;
                            else knifeActive = false;
                            i++; continue;
                        }
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

            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            string line = "N" + file.GetLineBlock(i) + " G00 X";
            double temp = Math.Round((x1 + relativeX), 3);
            line = line + temp.ToString(nfi) + " Y";
            temp = Math.Round((y1 + relativeY), 3);
            line = line + temp.ToString(nfi);

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
        /// Updates the coordinate array with new locations in the string(if any)
        /// </summary>
        /// <param name="subline">the string you want to check</param>
        /// <param name="c">array of current coordinates</param>
        /// <returns>returns the coordinate array taken as input with updated values</returns>
        double[] GetLocation(string line, double[] whereAmINow)
        {
            double value;
            if (line.Contains("X"))
            {
                string subline = line.Substring(line.IndexOf("X") + 1);
                if (subline.IndexOf(" ") != -1) subline = subline.Substring(0, subline.IndexOf(" "));
                double.TryParse(subline, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value);
                whereAmINow[0] = value;
            }
            if (line.Contains("Y"))
            {
                string subline = line.Substring(line.IndexOf("Y") + 1);
                if (subline.IndexOf(" ") != -1) subline = subline.Substring(0, subline.IndexOf(" "));
                double.TryParse(subline, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value);
                whereAmINow[1] = value;
            }
            /* //not bothering with Z values for now
            if (line.Contains("Z"))
            {
                string subline = line.Substring(line.IndexOf("Z") + 1);
                if (subline.IndexOf(" ") != -1) subline = subline.Substring(0, subline.IndexOf(" "));
                double.TryParse(subline, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value);
                whereAmINow[2] = value;
            }
             */
            return whereAmINow;
        }
    }
}
