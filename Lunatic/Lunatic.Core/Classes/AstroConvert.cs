/*
MIT License

Copyright (c) 2017 Phil Crompton

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using Lunatic.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{

   public class AstroConvert
   {
      #region Unit convertsions ...
      public static double AngleDistance(double ang1, double ang2)
      {
         ang1 = UniformAngle(ang1);
         ang2 = UniformAngle(ang2);

         double d = ang2 - ang1;

         return UniformAngle(d);
      }
      public static double UniformAngle(double Source)
      {
         Source = Source % (System.Math.PI * 2);
         if (Source > System.Math.PI)
            return Source - 2 * System.Math.PI;
         if (Source < -System.Math.PI)
            return Source + 2 * System.Math.PI;
         return Source;
      }

      public static double DegToRad(double degrees) { return (degrees * Constants.DEG_RAD); }
      public static double HrsToRad(double hours) { return (hours * Constants.HRS_RAD); }

      public static double RadToDeg(double Rad) { return (Rad * Constants.RAD_DEG); }
      public static double RadToMin(double Rad) { return (Rad * Constants.RAD_MIN); }
      public static double RadToSec(double Rad) { return (Rad * Constants.RAD_SEC); }
      public static double Range2Pi(double rad)
      {
         while (rad < 0.0) {
            rad = rad + Constants.TWO_PI;
         }
         while (rad >= Constants.TWO_PI) {
            rad = rad - Constants.TWO_PI;
         }
         return rad;
      }

      public static double Range24(double vha)
      {
         while (vha < 0.0) {
            vha = vha + 24.0;
         }
         while (vha >= 24) {
            vha = vha - 24.0;
         }
         return vha;
      }

      public static double Range360(double vdeg)
      {

         while (vdeg < 0) {
            vdeg = vdeg + 360.0;
         }
         while (vdeg >= 360.0) {
            vdeg = vdeg - 360.0;
         }
         return vdeg;
      }

      public static double Range90(double vdeg)
      {

         while (vdeg < -90.0) {
            vdeg = vdeg + 360;
         }
         while (vdeg >= 360.0) {
            vdeg = vdeg - 90;
         }
         return vdeg;
      }

      public static double RangeHA(double ha)
      {

         while (ha < -12.0) {
            ha = ha + 24.0;
         }
         while (ha >= 12.0) {
            ha = ha - 24.0;
         }
         return ha;
      }

      /// <summary>
      /// Ensure the value lies between zero and the ceiling.
      /// </summary>
      /// <param name="value"></param>
      /// <param name="ceiling"></param>
      /// <returns></returns>
      public static double Range(double value, double ceiling)
      {
         double rangedValue = value;
         rangedValue -= value * Math.Floor(value / ceiling);
         return rangedValue;
      }

      #endregion

      #region Sidereal time ...
      public static double LocalApparentSiderealTime(double longitude)
      {
         return LocalApparentSiderealTime(longitude, DateTime.Now);
      }

      public static double LocalApparentSiderealTime(double longitude, DateTime localTime)
      {
         // get greenwich sidereal time: https://en.wikipedia.org/wiki/Sidereal_time
         //double siderealTime = (18.697374558 + 24.065709824419081 * (utilities.DateUTCToJulian(DateTime.UtcNow) - 2451545.0));

         // alternative using NOVAS 3.1
         double siderealTime = 0.0;
         using (var novas = new ASCOM.Astrometry.NOVAS.NOVAS31()) {
            var jd = localTime.ToUniversalTime().ToOADate() + 2415018.5;      // Taken from ASCOM.Util.DateUTCToJulian
            //var jd = DateTime.UtcNow.ToOADate() + 2415018.5;      // Taken from ASCOM.Util.DateUTCToJulian
            novas.SiderealTime(jd, 0, novas.DeltaT(jd),
                ASCOM.Astrometry.GstType.GreenwichApparentSiderealTime,
                ASCOM.Astrometry.Method.EquinoxBased,
                ASCOM.Astrometry.Accuracy.Reduced, ref siderealTime);
         }
         // allow for the longitude
         siderealTime += longitude / 360.0 * 24.0;
         // reduce to the range 0 to 24 hours
         siderealTime = siderealTime % 24.0;
         return siderealTime;
      }

      #endregion

      #region Axis positions ...

      /// <summary>
      /// Returns an hour value for a given axis position in Radians
      /// </summary>
      /// <param name="zeroPosition">Axis position for zero hours in Radians</param>
      /// <param name="valuePosition">Axis position for value in Radians</param>
      /// <param name="hemisphere">Which hemisphere are you in.</param>
      /// <returns></returns>
      public static double AxisHours(double zeroPosition, double valuePosition, HemisphereOption hemisphere)   // Get_EncoderHours
      {
         // Compute in Hours the encoder value based on 0 position value (RAOffset0)
         // and Total 360 degree rotation microstep count (Tot_Enc

         double hours;
         if (valuePosition > zeroPosition) {
            hours = 24 - (((valuePosition - zeroPosition) / Constants.TWO_PI) * 24.0);
         }
         else {
            hours = ((zeroPosition - valuePosition) / Constants.TWO_PI) * 24;
         }
         if (hemisphere == HemisphereOption.Northern) {
            hours = Range24(hours + 6.0);     // Set to true Hours which is perpendicular to RA Axis
         }
         else {
            hours = Range24((24.0 - hours) + 6.0);
         }
         return hours;
      }

      public static double AxisPositionFromHours(double zeroPosition, double hourValue, HemisphereOption hemisphere) //Get_EncoderfromHours
      {
         double hours = Range24(hourValue - 6.0);
         double axisPosition;
         if (hemisphere == HemisphereOption.Northern) {
            if (hours < 12.0) {
               axisPosition = zeroPosition - ((hours / 24.0) * Constants.TWO_PI);
            }
            else {
               axisPosition = (((24.0 - hours) / 24.0) * Constants.TWO_PI) + zeroPosition;
            }
         }
         else {
            if (hours < 12) {
               axisPosition = ((hours / 24.0) * Constants.TWO_PI) + zeroPosition;
            }
            else {
               axisPosition = zeroPosition - (((24.0 - hours) / 24.0) * Constants.TWO_PI);
            }
         }
         return axisPosition;
      }


      /// <summary>
      /// Returns the Axis Position in Radians for a given value in degrees.
      /// </summary>
      /// <param name="zeroPosition"></param>
      /// <param name="degreesValue"></param>
      /// <param name="pier"></param>
      /// <param name="hemisphere"></param>
      /// <returns></returns>
      public static double AxisPositionFromDegrees(double zeroPosition, double degreesValue, int pier, HemisphereOption hemisphere)  //  Get_EncoderfromDegrees
      {
         double axisPosition;
         if (hemisphere == HemisphereOption.Southern) {
            degreesValue = 360.0 - degreesValue;
         }
         if (degreesValue > 180.0 && pier == 0) {
            axisPosition = zeroPosition - DegToRad(360.0 - degreesValue);
         }
         else {
            axisPosition = DegToRad(degreesValue) + zeroPosition;
         }
         return axisPosition;
      }

      /// <summary>
      /// Returns a degrees value for a given axis position
      /// </summary>
      /// <param name="zeroPosition">Axis positon for zero degrees in radians.</param>
      /// <param name="valuePosition">Axis position for value in radians</param>
      /// <param name="hemisphere">Northern or Southern hemisphere</param>
      /// <returns></returns>
      public static double AxisDegrees(double zeroPosition, double valuePosition, HemisphereOption hemisphere)     // Get_EncoderDegrees
      {
         double degrees;
         if (valuePosition > zeroPosition) {
            degrees = RadToDeg(valuePosition - zeroPosition);
         }
         else {
            degrees = 360.0 - RadToDeg(zeroPosition - valuePosition);
         }
         if (hemisphere == HemisphereOption.Northern) {
            degrees = Range360(degrees);
         }
         else {
            degrees = Range360(360.0 - degrees);
         }
         return degrees;
      }

      public static double RAAxisPositionFromRA(double raHours, double decDegrees, double longitude, double zeroPosition, HemisphereOption hemisphere) // Get_RAEncoderfromRA
      {
         double hourAngle = raHours - AstroConvert.LocalApparentSiderealTime(longitude);     // Not sure how this is derived from H = LST - ɑ
         if (hemisphere == HemisphereOption.Northern) {
            if (decDegrees > 90 && decDegrees <= 270) {
               hourAngle -= 12.0;
            }
         }
         else {
            if (decDegrees > 90 && decDegrees <= 270) {
               hourAngle += 12.0;
            }
         }
         hourAngle = Range24(hourAngle);
         return AxisPositionFromHours(zeroPosition, hourAngle, hemisphere);
      }

      public static double DECAxisPositionFromDEC(double decDegrees, int pier, double zeroPosition, HemisphereOption hemisphere)  // Get_DECEncoderfromDEC
      {
         if (pier == 1) {
            decDegrees = 180.0 - decDegrees;
         }
         return AxisPositionFromDegrees(zeroPosition, decDegrees, pier, hemisphere);
      }

      public static double RAAxisPositionFromAltAz(double altDegrees, double azDegrees, double longDegrees, double latDegrees, double zeroPosition, HemisphereOption hemisphere) // Get_RAEncoderfromAltAz
      {
         throw new NotImplementedException();
         /*
            Public Function Get_RAEncoderfromAltAz(Alt_in_deg As Double, Az_in_deg As Double, pLongitude As Double, pLatitude As Double, encOffset0 As Double, Tot_enc As Double, hmspr As Long) As Long

            Dim i As Double
            Dim ttha As Double
            Dim ttdec As Double

                aa_hadec (pLatitude * DEG_RAD), (Alt_in_deg * DEG_RAD), ((360# - Az_in_deg) * DEG_RAD), ttha, ttdec
                i = (ttha * RAD_HRS)
                i = Range24(i)
                Get_RAEncoderfromAltAz = Get_EncoderfromHours(encOffset0, i, Tot_enc, hmspr)
   
            End Function
          */
      }

      public static double DECAxisPositionFromAltAz(double altDegrees, double azDegrees, double longDegrees, double latDegrees, double zeroPosition, int pier, HemisphereOption hemisphere) // Get_DECEncoderfromAltAz
      {
         throw new NotImplementedException();
         /*
            Public Function Get_DECEncoderfromAltAz(Alt_in_deg As Double, Az_in_deg As Double, pLongitude As Double, pLatitude As Double, encOffset0 As Double, Tot_enc As Double, Pier As Double, hmspr As Long) As Long

            Dim i As Double
            Dim ttha As Double
            Dim ttdec As Double

                aa_hadec (pLatitude * DEG_RAD), (Alt_in_deg * DEG_RAD), ((360# - Az_in_deg) * DEG_RAD), ttha, ttdec
                i = ttdec * RAD_DEG ' tDec was in Radians
                If Pier = 1 Then i = 180 - i
                Get_DECEncoderfromAltAz = Get_EncoderfromDegrees(encOffset0, i, Tot_enc, Pier, hmspr)
   
            End Function
          */
      }
      #endregion

      #region AA-HADEC ...
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


      public static EquatorialCoordinate GetEquatorial(AltAzCoordinate altAz, Angle latitude, Angle longitude, DateTime localTime)
      {
         double hourAngle = 0.0;
         double declination = 0.0;
         aaha_aux(latitude.Radians, altAz.Azimuth.Radians, altAz.Altitude.Radians, ref hourAngle, ref declination);
         if (hourAngle > Math.PI)
            hourAngle -= 2 * Math.PI;

         // LHA = LST - Ra
         double lst = AstroConvert.LocalApparentSiderealTime(longitude.Value, localTime);
         double rightAscension = lst - hourAngle;
         return new EquatorialCoordinate(new HourAngle(rightAscension, true), new Angle(declination, true));
      }


      /* given geographical (n+, radians), lt, hour angle (radians), ha, and
       * declination (radians), dec, return altitude (up+, radians), alt, and
       * azimuth (angle round to the east from north+, radians),
      */
      public static void HaDecToAltAz(double latitude, double hourAngle, double declination, ref double altitude, ref double azimuth)
      {
         aaha_aux(latitude, hourAngle, declination, ref azimuth, ref altitude);
      }

      public static AltAzCoordinate GetAltAz(EquatorialCoordinate equatorial, Angle latitude)
      {
         double alt = 0.0;
         double az = 0.0;
         aaha_aux(latitude.Radians, equatorial.RightAcension.Radians, equatorial.Declination.Radians, ref alt, ref az);
         return new AltAzCoordinate(new Angle(alt, true), new Angle(az, true));
      }

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

         Bp = AstroConvert.Range(B, 2 * Math.PI);
      }
      #endregion

      #region Delta Sync stuff ...
      public static double DeltaRAMap(double raAxisPosition)      // Delta_RA_MAP
      {
         throw new NotImplementedException();
      }

      public static double DeltaDECMap(double raAxisPosition)      // Delta_DEC_MAP
      {
         throw new NotImplementedException();
      }

      public static CarteseanCoordinate DeltaMatrixMap(double raAxisPosition, double decAxisPosition) // Delta_Matrix_Map
      {
         throw new NotImplementedException();
      }

      public static CarteseanCoordinate DeltaMatrixReverseMap(double raAxisPosition, double decAxisPosition) //Delta_Matrix_Reverse_Map
      {
         throw new NotImplementedException();
      }

      public static CarteseanCoordinate DeltaSyncMatrixMap(double raAxisPosition, double decAxisPosition)    // DeltaSync_Matrix_Map
      {
         throw new NotImplementedException();
      }

      public static CarteseanCoordinate DeltaSyncReverseMatrixMap(double raAxisPosition, double decAxisPosition)      // DeltaSyncReverse_Matrix_Map
      {
         throw new NotImplementedException();
      }

      //      Public Function Delta_Matrix_Map(ByVal RA As Double, ByVal DEC As Double) As Coordt
      //Dim i As Integer
      //Dim obtmp As Coord
      //Dim obtmp2 As Coord

      //    If(RA >= &H1000000) Or(DEC >= &H1000000) Then
      //      Delta_Matrix_Map.X = RA
      //      Delta_Matrix_Map.Y = DEC

      //      Delta_Matrix_Map.z = 1

      //      Delta_Matrix_Map.F = 0

      //      Exit Function

      //  End If


      //  obtmp.X = RA

      //  obtmp.Y = DEC

      //  obtmp.z = 1

      //    ' re transform based on the nearest 3 stars

      //  i = EQ_UpdateTaki(RA, DEC)      // Find the nearest 3 points and they update the Taki transformation matrix


      //  obtmp2 = EQ_plTaki(obtmp)


      //  Delta_Matrix_Map.X = obtmp2.X

      //  Delta_Matrix_Map.Y = obtmp2.Y

      //  Delta_Matrix_Map.z = 1

      //  Delta_Matrix_Map.F = i


      //End Function


      //Public Function Delta_Matrix_Reverse_Map(ByVal RA As Double, ByVal DEC As Double) As Coordt

      //Dim i As Integer
      //Dim obtmp As Coord
      //Dim obtmp2 As Coord

      //    If(RA >= &H1000000) Or(DEC >= &H1000000) Then
      //      Delta_Matrix_Reverse_Map.X = RA
      //      Delta_Matrix_Reverse_Map.Y = DEC

      //      Delta_Matrix_Reverse_Map.z = 1

      //      Delta_Matrix_Reverse_Map.F = 0

      //      Exit Function

      //  End If


      //  obtmp.X = RA + gRASync01

      //  obtmp.Y = DEC + gDECSync01

      //  obtmp.z = 1

      //    ' re transform using the 3 nearest stars

      //  i = EQ_UpdateAffine(obtmp.X, obtmp.Y)    // Gets the nearest 3 points then uses these to generate the Affine transformation

      //  obtmp2 = EQ_plAffine(obtmp)


      //  Delta_Matrix_Reverse_Map.X = obtmp2.X

      //  Delta_Matrix_Reverse_Map.Y = obtmp2.Y

      //  Delta_Matrix_Reverse_Map.z = 1

      //  Delta_Matrix_Reverse_Map.F = i


      //  gSelectStar = 0


      //End Function


      //Public Function DeltaSync_Matrix_Map(ByVal RA As Double, ByVal DEC As Double) As Coordt
      //Dim i As Long

      //    If(RA >= &H1000000) Or(DEC >= &H1000000) Then GoTo HandleError

      //  i = GetNearest(RA, DEC)
      //    If i<> -1 Then
      //        gSelectStar = i
      //        DeltaSync_Matrix_Map.X = RA + (ct_Points(i).X - my_Points(i).X) + gRASync01
      //        DeltaSync_Matrix_Map.Y = DEC + (ct_Points(i).Y - my_Points(i).Y) + gDECSync01
      //        DeltaSync_Matrix_Map.z = 1
      //        DeltaSync_Matrix_Map.F = 0
      //    Else
      //HandleError:
      //        DeltaSync_Matrix_Map.X = RA
      //        DeltaSync_Matrix_Map.Y = DEC
      //        DeltaSync_Matrix_Map.z = 0
      //        DeltaSync_Matrix_Map.F = 0
      //    End If
      //End Function


      //Public Function DeltaSyncReverse_Matrix_Map(ByVal RA As Double, ByVal DEC As Double) As Coordt
      //Dim i As Long

      //    If(RA >= &H1000000) Or(DEC >= &H1000000) Or gAlignmentStars_count = 0 Then GoTo HandleError

      //  i = GetNearest(RA, DEC)


      //    If i<> -1 Then
      //        gSelectStar = i
      //        DeltaSyncReverse_Matrix_Map.X = RA - (ct_Points(i).X - my_Points(i).X)
      //        DeltaSyncReverse_Matrix_Map.Y = DEC - (ct_Points(i).Y - my_Points(i).Y)
      //        DeltaSyncReverse_Matrix_Map.z = 1
      //        DeltaSyncReverse_Matrix_Map.F = 0
      //    Else
      //HandleError:
      //        DeltaSyncReverse_Matrix_Map.X = RA
      //        DeltaSyncReverse_Matrix_Map.Y = DEC
      //        DeltaSyncReverse_Matrix_Map.z = 1
      //        DeltaSyncReverse_Matrix_Map.F = 0
      //    End If

      //End Function
      #endregion

   }
}
