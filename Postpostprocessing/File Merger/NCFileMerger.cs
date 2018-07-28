﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Postpostprocessing
{
    class NCFileMerger : NCFile
    {
        double xMin; bool xSet = false;
        double xMax;
        double yMin; bool ySet = false;
        double yMax;
        double xOffset;
        double yOffset;
        double xArrayOffset; public double XArrayOffset { get { return xArrayOffset; } set { xArrayOffset = value; } }
        double yArrayOffset; public double YArrayOffset { get { return yArrayOffset; } set { yArrayOffset = value; } }

        public NCFileMerger(string filename)
            : base(filename)
        {
            FindMinMaxValues();
        }
        /// <summary>
        /// Finds and sets the min/max x/y values, also gets the files offset from x0,y0
        /// </summary>
        public void FindMinMaxValues()
        {
            int i = 0;
            while (i < lines.Length)
            {
                if (CheckLineBlock(i))
                {
                    string line = GetCode(i);
                    if (line.Contains("X") && xSet == false)
                    {
                        xMin = GetValue(line, 'X');
                        xMax = xMin; xSet = true;
                    }
                    if (line.Contains("Y") && ySet == false)
                    {
                        yMin = GetValue(line, 'Y');
                        yMax = yMin; ySet = true;
                    }
                    if (xSet && ySet) break;
                }
                i++;
            }
            i++;
            while (i < lines.Length)
            {
                if (CheckLineBlock(i))
                {
                    string line = GetCode(i);
                    if (line.Contains("X"))
                    {
                        double x = GetValue(line, 'X');
                        if (x < xMin) xMin = x;
                        else if (x > xMax) xMax = x;
                    }
                    if (line.Contains("Y"))
                    {
                        double y = GetValue(line, 'Y');
                        if (y < yMin) yMin = y;
                        else if (y > yMax) yMax = y;
                    }
                }
                i++;
            }
            xOffset = xMin; yOffset = yMin;
        }

        /// <summary>
        /// Returns the spesified value from a line
        /// </summary>
        /// <param name="line">the line containing the value</param>
        /// <param name="c">char of the axis's value you are looking for</param>
        /// <returns></returns>
        double GetValue(string line, char c)
        {
            line = line.Substring(line.IndexOf(c) + 1);
            double value;
            if (line.IndexOf(" ") != -1) line = line.Substring(0, line.IndexOf(" "));
            value = double.Parse(line);
            return value;
        }

        public double GetXLength() { return xMax - xMin; }
        public double GetYLength() { return yMax - yMin; }

        public void Rotate(char r)
        {
            //r values: l=turn 90 degree's left, r=turn 90 degree's right, t=turn 180 degrees
            int i = 0;
            while (i < lines.Length)
            {
                if (CheckLineBlock(i))
                {
                    string line = GetCode(i);
                    if (line.Contains("X") || line.Contains("Y"))
                    {
                        double x; string newX = "";
                        double y; string newY = "";

                        if (line.Contains("X"))
                        {
                            x = GetValue(line, 'X');
                            switch (r)
                            {
                                case 'l': newX = " Y" + DoubleToString(x); break;
                                case 'r': newX = " Y" + DoubleToString((-x)); break;
                                case 't': newX = " X" + DoubleToString((-x)); break;
                            }
                        }
                        if (line.Contains("Y"))
                        {
                            y = GetValue(line, 'Y');
                            switch (r)
                            {
                                case 'l': newY = " X" + DoubleToString((-y)); break;
                                case 'r': newY = " X" + DoubleToString(y); break;
                                case 't': newY = " Y" + DoubleToString((-y)); break;
                            }
                        }
                        string newline = "N" + (GetLineBlock(i));
                        if (line.Contains("G00")) newline = newline + " G00";
                        if (line.Contains("G01")) newline = newline + " G01";
                        if (line.Contains("G02")) newline = newline + " G02";
                        if (line.Contains("G03")) newline = newline + " G03";
                        if (newX.Contains("Y")) { string temp = newX; newX = newY; newY = temp; } //sorts newX and newY, so the x value will always come first
                        newline = newline + newX + newY;
                        if (line.Contains("A"))
                        {
                            double a = GetValue(line, 'A');
                            switch (r)
                            {
                                case 'l': a += 90; break;
                                case 'r': a -= 90; break;
                                case 't': a -= 180; break;
                            }
                            if (a >= 360) a = a - 360;
                            else if (a < 0) a = a + 360;
                            newline = newline + " A" + DoubleToString(a);
                        }
                        if (line.Contains("R")) newline = newline + " R" + DoubleToString(GetValue(line, 'R'));
                        if (line.Contains("F")) newline = newline + " F" + DoubleToString(GetValue(line, 'F'));
                    }
                }
                i++;
            }
        }

    }
}
