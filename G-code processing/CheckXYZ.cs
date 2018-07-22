using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Configuration;

namespace Postpostprocessing
{
    /// <summary>
    /// This class checks for negative XYZ values, and stops those files from being moved to the machine.
    /// </summary>
    class CheckXYZ
    {
        public CheckXYZ(NCFile[] files)
        {
            int i = 0;
            while (i < files.Length)
            {
                if (!files[i].PreviousPPP) files[i] = Xyz(files[i]);
                i++;
            }
        }

        NCFile Xyz(NCFile file)
        {
            int i = 0;
            while (i < file.AmountLines())
            {
                if (file.CheckLineBlock(i))
                {
                    string line = file.GetCode(i);
                    double value = 0;
                    if (line.IndexOf("X") > -1)
                    {
                        string xblock = line.Substring(line.IndexOf("X") + 1);
                        if (xblock.IndexOf(" ") > -1) xblock = xblock.Substring(0, xblock.IndexOf(" "));
                        if (double.TryParse(xblock, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value))
                            if (value < double.Parse(ConfigurationManager.AppSettings["maxNegativeX"]))
                                file.negativeXYZ('X');
                    }
                    if (line.IndexOf("Y") > -1)
                    {
                        string yblock = line.Substring(line.IndexOf("Y") + 1);
                        if (yblock.IndexOf(" ") > -1) yblock = yblock.Substring(0, yblock.IndexOf(" "));
                        if (double.TryParse(yblock, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value))
                            if (value < double.Parse(ConfigurationManager.AppSettings["maxNegativeY"]))
                                file.negativeXYZ('Y');
                    }
                    if (line.IndexOf("Z") > -1)
                    {
                        string zblock = line.Substring(line.IndexOf("Z") + 1);
                        if (zblock.IndexOf(" ") > -1) zblock = zblock.Substring(0, zblock.IndexOf(" "));
                        if (double.TryParse(zblock, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("en-US"), out value))
                            if (value < double.Parse(ConfigurationManager.AppSettings["maxNegativeZ"]))
                                file.negativeXYZ('Z');
                    }
                }

                i++;
            }

            //Adds a comment to the top line indicating if xyz values are ok
            {
                string status;
                if (file.CheckNegativeXYZ()) status = "Warning! negative XYZ values detected";
                else status = "XYZok";

                string line = file.GetLine(0);
                if (line.IndexOf("(") > -1)
                {
                    status = line.Substring(0, line.IndexOf("(") + 1) + status + " " + line.Substring(line.IndexOf("(")+1);
                    file.UpdateLine(0, status);
                }
                else
                {
                    status = "(" + status + ")";
                    file.UpdateLine(0, status);
                }
            }
            return file;
        }
    }
}
