using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Postpostprocessing
{
    /// <summary>
    /// Holds various methods, usefull for reading GCode
    /// </summary>
    class GCodeReader
    {
        public GCodeReader()
        {

        }

        /// <summary>
        /// Updates the coordinate array with new locations in the string(if any)
        /// </summary>
        /// <param name="subline">the string you want to check</param>
        /// <param name="c">array of current coordinates</param>
        /// <returns>returns the coordinate array taken as input with updated values</returns>
        protected double[][] GetLocation(string line, double[][] whereAmINow, int lineNumber)
        {
            if (line.Contains("X") || line.Contains("Y"))
            {
                if (!(whereAmINow[3] == null)) whereAmINow[4] = whereAmINow[3].Clone() as double[];
                if (!(whereAmINow[2] == null)) whereAmINow[3] = whereAmINow[2].Clone() as double[];
                if (!(whereAmINow[1] == null)) whereAmINow[2] = whereAmINow[1].Clone() as double[];
                if (!(whereAmINow[0] == null)) whereAmINow[1] = whereAmINow[0].Clone() as double[];
                if (whereAmINow[0] == null) whereAmINow[0] = new double[8];
                //[][0]=X, [][1]=Y, [][2]=Z, [][3]=F, [][4]=A, [][5]=R, [][6]=movement type, [][7], line number in the file
                whereAmINow[0][7] = lineNumber;
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

            if (line.Contains("R") && (line.Contains("X") || line.Contains("Y")))
            {
                string subline = line.Substring(line.IndexOf("R") + 1);
                if (subline.IndexOf(" ") != -1) subline = subline.Substring(0, subline.IndexOf(" "));
                double.TryParse(subline, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value);
                whereAmINow[0][5] = value;
            }

            if (line.StartsWith("G00")) whereAmINow[0][6] = 0;
            else if (line.StartsWith("G01")) whereAmINow[0][6] = 1;
            else if (line.StartsWith("G02")) whereAmINow[0][6] = 2;
            else if (line.StartsWith("G03")) whereAmINow[0][6] = 3;
            return whereAmINow;
        }

        /// <summary>
        /// Returns the spesified value from a line
        /// </summary>
        /// <param name="line">the line containing the value</param>
        /// <param name="c">char of the axis's value you are looking for</param>
        /// <returns></returns>
        protected double GetValue(string line, char c)
        {
            line = line.Substring(line.IndexOf(c) + 1);
            double value;
            if (line.IndexOf(" ") != -1) line = line.Substring(0, line.IndexOf(" "));
            value = double.Parse(line, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"));
            return value;
        }
    }
}
