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
                {
                    file = files[i];
                    file = DetectLongArc(file);
                }
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
                        double startA = SharedMethods.DetectStartingDirectionOfArc(whereAmINow);
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
                        string newLine = ""; int blockNumber = file.GetLineBlock(i);
                        if (changed > 359) { blockNumber -= 3; } //      changes starting block number to correspond to amount of new lines being added 
                        else if (changed > 269) { blockNumber -= 2; }//  infront of the line being examined
                        else if (changed > 179) { blockNumber -= 1; }

                        double[] d = SharedMethods.DetectArcSenter(whereAmINow);//[0]=x, [1]=y
                        if (changed > 179) while (true)
                            {
                                if (changed > 179)
                                {
                                    double x = d[0]; double y = d[1]; double newAngle;
                                    if (whereAmINow[0][6] == 2)
                                    {   // turn clockwise
                                        if (startA <= 90) { newAngle = 0; y += whereAmINow[0][5]; }
                                        else if (startA <= 180) { newAngle = 90; x -= whereAmINow[0][5]; }
                                        else if (startA <= 270) { newAngle = 180; y -= whereAmINow[0][5]; }
                                        else { newAngle = 270; x += whereAmINow[0][5]; }
                                    }
                                    else
                                    {   // turn counter clockwise
                                        if (startA >= 270) { newAngle = 0; y -= whereAmINow[0][5]; }
                                        else if (startA >= 180) { newAngle = 270; x -= whereAmINow[0][5]; }
                                        else if (startA >= 90) { newAngle = 180; y += whereAmINow[0][5]; }
                                        else { newAngle = 90; x += whereAmINow[0][5]; }
                                    }
                                    newLine = newLine + "N" + blockNumber + " G0" + Convert.ToInt32(whereAmINow[0][6]) + " X" + file.DoubleToString(x) + " Y" +
                                        file.DoubleToString(y) + " A" + file.DoubleToString(newAngle) + " R" + file.DoubleToString(whereAmINow[0][5]);
                                    if (!newLine.Contains("F")) newLine = newLine + " F" + file.DoubleToString(whereAmINow[0][3]);
                                    newLine = newLine + "\n";
                                    blockNumber++; changed -= 90;
                                    startA = newAngle;
                                }
                                else
                                {
                                    string oldLine = "N" + Convert.ToString(file.GetLineBlock(i)) + " X" + file.DoubleToString(whereAmINow[0][0]) + " Y" + file.DoubleToString(whereAmINow[0][1])
                                        + " A" + file.DoubleToString(whereAmINow[0][4]) + " R" + file.DoubleToString(whereAmINow[0][5]);
                                    newLine = newLine + oldLine;
                                    file.UpdateLine(i, newLine);
                                    break;
                                }
                            }
                    }
                }
                i++;
            }
            file.RebuildLines();
            return file;
        }


    }
}
