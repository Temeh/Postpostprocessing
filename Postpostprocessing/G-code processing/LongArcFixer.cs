using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Postpostprocessing
{
    /// <summary>
    /// CNC router have a bug where it will spin the knife the wrong way if an arc is longer than 180degrees. 
    /// This class fixes the problem by inserting more points on the arc so that no single arc is more than 180 degrees.
    /// </summary>
    class LongArcFixer : GCodeReader
    {
        NCFile file;
        public LongArcFixer(NCFile[] files)
        {
            int i = 0;
            while (i < files.Length)
            {
                if (!files[i].PreviousPPP)
                    file = files[i];
                file = DetectLongArc(file);
                i++;
            }
        }

        NCFile DetectLongArc(NCFile file)
        {
            int i = 0; double[][] whereAmINow = new double[5][]; //[][0]=X, [][1]=Y, [][2]=Z, [][3]=F, [][4]=A, [][5]=R, [][6]=movement type, [][7]=line number in the file
            while (i < file.AmountLines())
            {
                if (file.CheckLineBlock(i))
                {
                    string line = file.GetCode(i);
                    whereAmINow = GetLocation(line, whereAmINow, i);
                    if (line.Contains("A"))
                    {
                        double startA = DetectStartingDirectionOfArc(whereAmINow);
                        double endA = GetValue(line, 'A');
                        double changed;
                        if (whereAmINow[0][6] == 2)
                        {   // turn clockwise
                            changed = startA - endA;
                            if (changed < 0) changed = startA + (360 - endA);
                        }
                        else
                        {   // turn counter clockwise
                            changed = endA - startA;
                            if (changed < 0) changed = endA + (360 - startA);
                        }
                        string newLine = ""; int blockNumber = file.GetLineBlock(i - 1); blockNumber++;
                        double[] d = DetectArcSenter(whereAmINow);//[0]=x, [1]=y
                        while (true)
                        {

                            if (changed > 179)
                            {
                                if (whereAmINow[0][6] == 2)
                                {   // turn clockwise
                                    double newAngle; double x=d[0]; double y=d[1];
                                    if (startA < 90) { newAngle = 0; y += whereAmINow[0][5]; }
                                    else if (startA < 180) { newAngle = 90; x -= whereAmINow[0][5]; }
                                    else if (startA < 270) { newAngle = 180; y -= whereAmINow[0][5]; }
                                    else { newAngle = 270; x += whereAmINow[0][5]; }
                                    newLine = newLine+"\n"+"N" + blockNumber + " X" + file.DoubleToString(d[0]) + " Y" + file.DoubleToString(d[1]) + " A" + file.DoubleToString(newAngle) +
                                        " R" + file.DoubleToString(whereAmINow[0][5]) +"\n";
                                    blockNumber++; changed -= 90;
                                }
                                else
                                {   // turn counter clockwise

                                }
                            }
                            else break;
                        }
                    }
                }
                i++;
            }
            return file;
        }

        /// <summary>
        /// Detects the start direction of an arc
        /// </summary>
        /// <param name="whereAmINow">array of arrays holding the most recent points</param>
        /// <returns></returns>
        double DetectStartingDirectionOfArc(double[][] whereAmINow)
        {
            /*
            //right triangle stuff
            double destinationAngle = whereAmINow[0][4]; double directionofArcCenter;
            if (whereAmINow[0][6] == 2) directionofArcCenter = destinationAngle - 90;  //turn clockwise
            else directionofArcCenter = destinationAngle + 90;  //Turn counter clockwise
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
            double[] s = new double[2] { xArcCenter, yArcCenter };*/
            double[] s = DetectArcSenter(whereAmINow);
            double destinationAngle = file.CheckDirection(s, whereAmINow[1]);
            if (whereAmINow[0][6] == 2) destinationAngle -= 90;
            else destinationAngle += 90;
            if (destinationAngle < 0) destinationAngle += 360;
            else if (destinationAngle > 360) destinationAngle -= 360;
            return destinationAngle;
        }

        /// <summary>
        /// Finds the center of an arc
        /// </summary>
        /// <param name="whereAmINow">array that holds the locations of the last few point</param>
        /// <returns></returns>
        double[] DetectArcSenter(double[][] whereAmINow)
        {

            //right triangle stuff
            double destinationAngle = whereAmINow[0][4]; double directionofArcCenter;
            if (whereAmINow[0][6] == 2) directionofArcCenter = destinationAngle - 90;  //turn clockwise
            else directionofArcCenter = destinationAngle + 90;  //Turn counter clockwise
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
            double[] center = new double[] { xArcCenter, yArcCenter };
            return center;
        }
    }
}
