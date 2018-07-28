using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Configuration;

namespace Postpostprocessing
{
    /// <summary>
    /// Updates the G code to prevent the knife form entering the material before it has turned to face the correct direction
    /// </summary>
    class KnifeDirection
    {
        NCFile file;

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
            bool foundEntryPoint = false;
            double[][] whereAmINow = new double[5][]; //[][0]=X, [][1]=Y, [][2]=Z, [][3]=F, [][4]=A
            double[] startPoint = new double[5]; //0=X, 1=Y, 2=Z, 3=F
            double[] entryPoint = new double[5]; //0=X, 1=Y, 2=Z, 3=F 
            int locationInFile = -1; // line# in the file, of the entry point
            int i = 0;
            while (i < file.AmountLines())
            {
                if (file.CheckLineBlock(i))
                {
                    string line = file.GetCode(i);

                    if (line.StartsWith("T"))//checks for tool change and checks if new tool is knife or not
                    {
                        int value;
                        if (int.TryParse(line.Substring(1, line.IndexOf(" ") - 1), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value))
                        {
                            if (value == 25) knifeActive = true;
                            else { knifeActive = false; rapidMovement = false; foundEntryPoint = false; }
                        }
                    }

                    if (knifeActive)
                    {
                        whereAmINow = GetLocation(line, whereAmINow);
                        if (line.StartsWith("G00") && !foundEntryPoint)
                        {
                            rapidMovement = true;
                            Array.Copy(whereAmINow[0], startPoint, whereAmINow[0].Length);
                        }
                    }
                    else { i++; continue; }

                    if (rapidMovement && knifeActive)
                    {
                        if (line.Contains("X") || line.Contains("Y"))
                        {
                            Array.Copy(whereAmINow[0], entryPoint, whereAmINow[0].Length);
                            foundEntryPoint = true;
                            rapidMovement = false; locationInFile = i;
                            i++; continue;
                        }
                    }

                    if (foundEntryPoint)
                    {
                        if (line.Contains("X") || line.Contains("Y"))
                        {
                            double[] nextPoint = new double[whereAmINow[0].Length];
                            Array.Copy(whereAmINow[0], nextPoint, whereAmINow[0].Length);
                            InsertKnifeTurning(whereAmINow, locationInFile);
                            //InsertNewPoint(entryPoint, nextPoint, locationInFile);
                            foundEntryPoint = false;
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
        /// <param name="entry">Array holding the entry point coordinates</param>
        /// <param name="next">Array holding the next point after entry point</param>
        void InsertNewPoint(double[] entry, double[] next, int i)
        {
            double x1 = entry[0]; double y1 = entry[1];
            double x2 = next[0]; double y2 = next[1];
            //adds a new line with the original x/y coordinates
            string newline = "N" + (file.GetLineBlock(i) + 1) + " " + file.GetCode(i);

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
            line = line + file.DoubleToString(temp) + " Y";
            temp = Math.Round((y1 + relativeY), 3);
            line = line + file.DoubleToString(temp);

            line = line + "\r\n" + newline;
            file.UpdateLine(i, line);
        }

        void InsertKnifeTurning(double[][] whereAmINow, int i)
        {

            if (whereAmINow[2] == null || file.CheckDistance(whereAmINow[1], whereAmINow[2]) > double.Parse(ConfigurationManager.AppSettings["minDistForRapidMove"]))
            {
                InsertNewPoint(whereAmINow[1], whereAmINow[0], i);
            }
            else
            {
                double newAngle = file.CheckDirection(whereAmINow[1], whereAmINow[0]);
                double oldAngle = file.CheckDirection(whereAmINow[3], whereAmINow[2]);
                double degreesTurned = oldAngle - newAngle;
                if (degreesTurned < 0) degreesTurned = degreesTurned + 360;
                string turnDirection;
                if (degreesTurned < 180) turnDirection = "G02";
                else turnDirection = "G03";

                newAngle = Math.Round(newAngle, 3);
                NumberFormatInfo nfi = new NumberFormatInfo();
                nfi.NumberDecimalSeparator = ".";
                string line = "N" + file.GetLineBlock(i) + " " + turnDirection.ToString(nfi) + " X" + file.DoubleToString(whereAmINow[1][0]);
                line = line + " Y" + file.DoubleToString(whereAmINow[1][1]) + " F" + ConfigurationManager.AppSettings["aboveMaterialRadiusSpeed"] + " A" + file.DoubleToString(newAngle) + " R" + ConfigurationManager.AppSettings["radiusAboveMaterial"];
                line = line + "\r\nN" + (file.GetLineBlock(i) + 1) + " F2000.";
                file.UpdateLine(i, line);
            }
        }

        /// <summary>
        /// Checks if there is a change of tool, and returns true if new tool is knife(T25), false if any other tool, and returns the original tool status if no change
        /// </summary>
        /// <param name="line">int of current line number</param>
        /// /// <param name="knifeActive">bool of knife's previous status</param>
        /// <returns>bool indicating if knife is active or not</returns>
        bool CheckKnife(string line, bool knifeActive)
        {
            if (line.StartsWith("T"))//checks for tool change and checks if new tool is knife or not
            {
                int value;
                if (int.TryParse(line.Substring(1, line.IndexOf(" ") - 1), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value))
                {
                    if (value == 25) return true;
                    else return false;
                }
                else return knifeActive;
            }
            else return knifeActive;
        }
        /// <summary>
        /// Updates the coordinate array with new locations in the string(if any)
        /// </summary>
        /// <param name="subline">the string you want to check</param>
        /// <param name="c">array of current coordinates</param>
        /// <returns>returns the coordinate array taken as input with updated values</returns>
        double[][] GetLocation(string line, double[][] whereAmINow)
        {
            if (line.Contains("X") || line.Contains("Y"))
            {
                if (!(whereAmINow[3] == null)) whereAmINow[4] = whereAmINow[3].Clone() as double[];
                if (!(whereAmINow[2] == null)) whereAmINow[3] = whereAmINow[2].Clone() as double[];
                if (!(whereAmINow[1] == null)) whereAmINow[2] = whereAmINow[1].Clone() as double[];
                if (!(whereAmINow[0] == null)) whereAmINow[1] = whereAmINow[0].Clone() as double[];
                if (whereAmINow[0] == null) whereAmINow[0] = new double[5];
            }
            double value;
            if (line.Contains("X"))
            {
                string subline = line.Substring(line.IndexOf("X") + 1);
                if (subline.IndexOf(" ") != -1) subline = subline.Substring(0, subline.IndexOf(" "));
                double.TryParse(subline, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value);
                whereAmINow[0][0] = value;
            }
            if (line.Contains("Y"))
            {
                string subline = line.Substring(line.IndexOf("Y") + 1);
                if (subline.IndexOf(" ") != -1) subline = subline.Substring(0, subline.IndexOf(" "));
                double.TryParse(subline, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value);
                whereAmINow[0][1] = value;
            }
            /*
            if (line.Contains("Z"))
            {
                string subline = line.Substring(line.IndexOf("Z") + 1);
                if (subline.IndexOf(" ") != -1) subline = subline.Substring(0, subline.IndexOf(" "));
                double.TryParse(subline, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value);
                whereAmINow[0][2] = value;
            }
            if (line.Contains("F"))
            {
                string subline = line.Substring(line.IndexOf("F") + 1);
                if (subline.IndexOf(" ") != -1) subline = subline.Substring(0, subline.IndexOf(" "));
                double.TryParse(subline, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value);
                whereAmINow[0][3] = value;
            }
             */
            if (line.Contains("A") && (line.Contains("X") || line.Contains("Y")))
            {
                string subline = line.Substring(line.IndexOf("A") + 1);
                if (subline.IndexOf(" ") != -1) subline = subline.Substring(0, subline.IndexOf(" "));
                double.TryParse(subline, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value);
                whereAmINow[0][4] = value;

            }
            else if (line.Contains("X") || line.Contains("Y")) whereAmINow[0][4] = -1;
            return whereAmINow;
        }
    }
}
