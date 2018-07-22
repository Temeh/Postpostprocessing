using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Postpostprocessing
{
    /// <summary>
    /// This class checks that the knife home is set to G56, and fixes if that is not the case
    /// </summary>
    class KnifeHomeing
    {
        public KnifeHomeing(NCFile[] files)
        {
            int i = 0;
            while (i < files.Length)
            {
                if (!files[i].PreviousPPP) SetKnifeHome(files[i]);
                i++;
            }
        }

        /// <summary>
        /// Takes a file and updates the home for the knife to G56
        /// </summary>
        /// <param name="file">File in question</param>
        /// <returns></returns>
        NCFile SetKnifeHome(NCFile file)
        {

            int amountLines = file.AmountLines();
            int i = 0;
            while (i < amountLines)
            {
                if (file.CheckLineBlock(i)) //checks if the line actually contains machien code
                {
                    string line = file.GetCode(i);
                    if (line.StartsWith("T25")) // if the line fetches T25(knife), then it starts looking for a following line setting the WorkCoordinateSystem, and makeing sure its set to G56
                    {
                        int j = i + 1;
                        while (j < i + 3)
                        {
                            line = file.GetCode(j);
                            if (line.StartsWith("G54")) //Found a line that sets the wrong WCS and changes it to "G56"
                            {
                                line = file.GetLine(j);
                                line = line.Substring(0, line.IndexOf(" ")) + " G56";
                                file.UpdateLine(j, line);
                            }
                            j++;
                        }
                    }
                }
                i++;
            }
            return file;
        }

    }
}
