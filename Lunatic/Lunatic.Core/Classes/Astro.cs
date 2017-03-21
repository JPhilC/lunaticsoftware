using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   public class Astro
   {
      static double lastLatitide;
      static double sinLatitude = 0.0;
      static double cosLatitude = 0.0;

      /* given geographical latitude (n+, radians), lt, altitude (up+, radians),
       * alt, and azimuth (angle round to the east from north+, radians),
       * return hour angle (radians), ha, and declination (radians), dec.
       */
      public static void AltAzToHaDec(double latitude, double altitude, double azimuth, ref double hourAngle, ref double declination)
      {
         aaha_aux(latitude, azimuth, altitude, ref hourAngle, ref declination);
         if (hourAngle > Math.PI)
            hourAngle -= 2 * Math.PI;
      }

      /* given geographical (n+, radians), lt, hour angle (radians), ha, and
          * declination (radians), dec, return altitude (up+, radians), alt, and
         * azimuth (angle round to the east from north+, radians),
      */
      public static void HaDecToAltAz(double latitude, double hourAngle, double declination, ref double altitude, ref double azimuth)
      {
         aaha_aux(latitude, hourAngle, declination, ref azimuth, ref altitude);
      }

      /* the actual formula is the same for both transformation directions so
         * do it here once for each way.
      * N.B.all arguments are in radians.
      */
      static void aaha_aux(double latitude, double x, double y, ref double p, ref double q)
      {
         lastLatitide = double.MinValue;
         double cap = 0.0;
         double B = 0.0;

         if (latitude != lastLatitide) {
            sinLatitude = Math.Sin(latitude);
            cosLatitude = Math.Cos(latitude);
            lastLatitide = latitude;
         }

         solve_sphere(-x, Math.PI / 2 - y, sinLatitude, cosLatitude, ref cap, ref B);
         p = B;
         q = Math.PI / 2 - Math.Acos(cap);
      }

      /* solve a spherical triangle:
 *           A
 *          /  \
 *         /    \
 *      c /      \ b
 *       /        \
 *      /          \
 *    B ____________ C
 *           a
 *
 * given A, b, c find B and a in range 0..B..2PI and 0..a..PI, respectively..
 * cap and Bp may be NULL if not interested in either one.
 * N.B. we pass in cos(c) and sin(c) because in many problems one of the sides
 *   remains constant for many values of A and b.
 */
      static void solve_sphere(double A, double b, double cc, double sc, ref double cap, ref double Bp)
      {
         double cb = Math.Cos(b), sb = Math.Sin(b);
         double sA, cA = Math.Cos(A);
         double x, y;
         double ca;
         double B;

         ca = cb * cc + sb * sc * cA;
         if (ca > 1.0) {
            ca = 1.0;
         }
         if (ca < -1.0) {
            ca = -1.0;
         }
            cap = ca;

         if (sc < 1e-7) {
            B = cc < 0 ? A : Math.PI - A;
         }
         else {
            sA = Math.Sin(A);
            y = sA * sb * sc;
            x = cb - ca * cc;
            B = Math.Atan2(y, x);
         }

         Bp = LunaticMath.Range(B, 2 * Math.PI);
      }

   }
}
