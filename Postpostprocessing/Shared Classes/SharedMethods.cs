using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Postpostprocessing
{
    static class SharedMethods
    {
        /// <summary>
        /// Detects the start direction of an arc
        /// </summary>
        /// <param name="whereAmINow">array of arrays holding the most recent points</param>
        /// <returns></returns>
        public static double DetectStartingDirectionOfArc(double[][] whereAmINow)
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
            double destinationAngle = CheckDirection(s, whereAmINow[1]);
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
        public static double[] DetectArcSenter(double[][] whereAmINow)
        {

            //finds the direction of the arc's senter
            double destinationAngle = whereAmINow[0][4]; double directionofArcCenter;
            if (whereAmINow[0][6] == 2) directionofArcCenter = destinationAngle - 90;  //turn clockwise
            else directionofArcCenter = destinationAngle + 90;  //Turn counter clockwise
            if (directionofArcCenter < 0) directionofArcCenter += 360;

            //finds the angle towards the center relative to the angle of the y-axis
            double angle = 0;
            {   
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

        /// <summary>
        /// takes a string of doubles with coordinates of start x/y and end x/y and checks what direction  movement is happening in
        /// </summary>
        /// <param name="start">start location</param>
        /// <param name="end">end location</param>
        /// <returns>the angle</returns>
        public static double CheckDirection(double[] start, double[] end)
        {
            double x = end[0] - start[0];
            double y = end[1] - start[1];
            double hypotenus = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
            double angle = -1;
            if (x == 0 || y == 0)
            {
                if (x > 0) angle = 0;
                else if (x < 0) angle = 180;
                else if (y > 0) angle = 90;
                else if (y < 0) angle = 270;
            }
            else
            {
                angle = Math.Acos((x / hypotenus)) * 180 / Math.PI;
                if (y < 0) angle = 360 - angle;
                angle = Math.Round(angle, 3);
            }
            return angle;
        }
    }
}