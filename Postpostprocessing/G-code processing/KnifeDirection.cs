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
    class KnifeDirection :GCodeReader
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
            double[][] whereAmINow = new double[5][]; //[][0]=X, [][1]=Y, [][2]=Z, [][3]=F, [][4]=A, [][5]=R, [][6]=movement type
            //double[] startPoint = new double[5]; //0=X, 1=Y, 2=Z, 3=F, 4=A, 5=R, 6=movement type
            //double[] entryPoint = new double[5]; //0=X, 1=Y, 2=Z, 3=F, 4=A, 5=R, 6=movement type
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
                        whereAmINow = GetLocation(line, whereAmINow, i);
                        if (line.StartsWith("G00") && !foundEntryPoint)
                        {
                            rapidMovement = true;
                            //Array.Copy(whereAmINow[0], startPoint, whereAmINow[0].Length);
                        }
                    }
                    else { i++; continue; }

                    if (rapidMovement && knifeActive)
                    {
                        if (line.Contains("X") || line.Contains("Y"))
                        {
                            //Array.Copy(whereAmINow[0], entryPoint, whereAmINow[0].Length);
                            foundEntryPoint = true;
                            rapidMovement = false; locationInFile = i;
                            i++; continue;
                        }
                    }

                    if (foundEntryPoint)
                    {
                        if (line.Contains("X") || line.Contains("Y"))
                        {
                            //  double[] nextPoint = new double[whereAmINow[0].Length];
                            //  Array.Copy(whereAmINow[0], nextPoint, whereAmINow[0].Length);
                            InsertKnifeTurning(whereAmINow, locationInFile);
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

        /// <summary>
        /// Changes G00 movement to G02/G03 to make the knife arrive, facing the correct destination
        /// </summary>
        /// <param name="whereAmINow">array holding points around the current location</param>
        /// <param name="i"></param>
        void InsertKnifeTurning(double[][] whereAmINow, int i)
        {
            /*
            if (whereAmINow[2] == null || // if we dont know where we are coming from, we fix the knife direction when it arrives at insertion point.
                file.CheckDistance(whereAmINow[1], whereAmINow[2]) > double.Parse(ConfigurationManager.AppSettings["minDistForRapidMove"]))
            {
                InsertNewPoint(whereAmINow[1], whereAmINow[0], i);
                return;
            }
             */
            double newAngle;

            if (whereAmINow[0][4] >= 0) // detects starting direction of a knife if its inserted into an arc
            {
                newAngle = DetectDirectioninArc(whereAmINow);
            }
            else // detects starting direction if knife is inserted on a straight line
            {
                newAngle =SharedMethods.CheckDirection(whereAmINow[1], whereAmINow[0]);
            }

            double oldAngle; string turnDirection;
            if (!(whereAmINow[3] == null))
            {
                oldAngle = SharedMethods.CheckDirection(whereAmINow[3], whereAmINow[2]);
                double degreesTurned = oldAngle - newAngle;

                if (degreesTurned < 0) degreesTurned = degreesTurned + 360;
                if (degreesTurned < 180) turnDirection = "G02";
                else turnDirection = "G03";
            }
            else turnDirection = "G02";

            newAngle = Math.Round(newAngle, 3);
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            string line = "N" + file.GetLineBlock(i) + " " + turnDirection.ToString(nfi) + " X" + file.DoubleToString(whereAmINow[1][0]);
            line = line + " Y" + file.DoubleToString(whereAmINow[1][1]) + " F" +
                ConfigurationManager.AppSettings["aboveMaterialRadiusSpeed"] + " A" + file.DoubleToString(newAngle) + " R" + ConfigurationManager.AppSettings["radiusAboveMaterial"];
            line = line + "\r\nN" + (file.GetLineBlock(i) + 1) + " F2000.";
            file.UpdateLine(i, line);
        }

        /// <summary>
        /// Detects the start direction of an arc
        /// </summary>
        /// <param name="whereAmINow">array of arrays holding the most recent points</param>
        /// <returns></returns>
        double DetectDirectioninArc(double[][] whereAmINow)
        {
            double destinationAngle = whereAmINow[0][4]; double directionofArcCenter;
            if (whereAmINow[0][6] == 2)  //turn clockwise
            {
                directionofArcCenter = destinationAngle - 90;
                if (directionofArcCenter < 0) directionofArcCenter += 360;
            }
            else  //Turn counter clockwise
            {
                directionofArcCenter = destinationAngle + 90;
                if (directionofArcCenter >= 360) directionofArcCenter -= 360;
            }
            //holds position of the center of the arc, relative to the end point
            double angle = 0;
            {   //Finds cosV
                if (directionofArcCenter <= 90) angle = 90 - directionofArcCenter;
                else if (directionofArcCenter <= 180) angle = directionofArcCenter - 90;
                else if (directionofArcCenter <= 270) angle = 270 - directionofArcCenter;
                else if (directionofArcCenter <= 360) angle = directionofArcCenter - 270;
                angle = angle * (Math.PI / 180); 
                double t = Math.Sin(angle);
            }
            double xArcCenter = Math.Abs(Math.Sin(angle) * whereAmINow[0][5]);
            double yArcCenter = Math.Abs(Math.Cos(angle) * whereAmINow[0][5]);
            if (directionofArcCenter > 180) yArcCenter = -yArcCenter;
            if (directionofArcCenter > 90 && directionofArcCenter < 270) xArcCenter = -xArcCenter;
            xArcCenter += whereAmINow[0][0]; yArcCenter += whereAmINow[0][1];
            double[] s = new double[2] { xArcCenter, yArcCenter };
            destinationAngle =SharedMethods.CheckDirection(s, whereAmINow[1]);
            if (whereAmINow[0][6] == 2) destinationAngle -= 90;
            else destinationAngle += 90;
            if (destinationAngle < 0) destinationAngle += 360;
            else if (destinationAngle > 360) destinationAngle -= 360;
            return destinationAngle;                   
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

    }
}
