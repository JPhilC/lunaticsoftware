using System;

namespace Lunatic.Core
{
   public static class LunaticMath
   {
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

      public static double LocalSiderealTime(double longitude)
      {
         return LocalSiderealTime(longitude, DateTime.Now);
         //// get greenwich sidereal time: https://en.wikipedia.org/wiki/Sidereal_time
         ////double siderealTime = (18.697374558 + 24.065709824419081 * (utilities.DateUTCToJulian(DateTime.UtcNow) - 2451545.0));

         //// alternative using NOVAS 3.1
         //double siderealTime = 0.0;
         //using (var novas = new ASCOM.Astrometry.NOVAS.NOVAS31()) {
         //   // var jd = utilities.DateUTCToJulian(DateTime.UtcNow);
         //   var jd = DateTime.UtcNow.ToOADate() + 2415018.5;      // Taken from ASCOM.Util.DateUTCToJulian
         //   novas.SiderealTime(jd, 0, novas.DeltaT(jd),
         //       ASCOM.Astrometry.GstType.GreenwichApparentSiderealTime,
         //       ASCOM.Astrometry.Method.EquinoxBased,
         //       ASCOM.Astrometry.Accuracy.Reduced, ref siderealTime);
         //}
         //// allow for the longitude
         //siderealTime += longitude / 360.0 * 24.0;
         //// reduce to the range 0 to 24 hours
         //siderealTime = siderealTime % 24.0;
         //return siderealTime;
      }

      public static double LocalSiderealTime(double longitude, DateTime localTime)
      {
         // get greenwich sidereal time: https://en.wikipedia.org/wiki/Sidereal_time
         //double siderealTime = (18.697374558 + 24.065709824419081 * (utilities.DateUTCToJulian(DateTime.UtcNow) - 2451545.0));

         // alternative using NOVAS 3.1
         double siderealTime = 0.0;
         using (var novas = new ASCOM.Astrometry.NOVAS.NOVAS31()) {
            var jd = localTime.ToUniversalTime().ToOADate() +2415018.5;      // Taken from ASCOM.Util.DateUTCToJulian
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
         double hourAngle = raHours - LocalSiderealTime(longitude);     // Not sure how this is derived from H = LST - ɑ
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

      #region Astro32 stuff ...
            
      #endregion


      public static double DeltaRAMap(double raAxisPosition)      // Delta_RA_MAP
      {
         throw new NotImplementedException();
      }

      public static double DeltaDECMap(double raAxisPosition)      // Delta_DEC_MAP
      {
         throw new NotImplementedException();
      }

      public static Coordt DeltaMatrixMap(double raAxisPosition, double decAxisPosition) // Delta_Matrix_Map
      {
         throw new NotImplementedException();
      }

      public static Coordt DeltaMatrixReverseMap(double raAxisPosition, double decAxisPosition) //Delta_Matrix_Reverse_Map
      {
         throw new NotImplementedException();
      }

      public static Coordt DeltaSyncMatrixMap(double raAxisPosition, double decAxisPosition)    // DeltaSync_Matrix_Map
      {
         throw new NotImplementedException();
      }

      public static Coordt DeltaSyncReverseMatrixMap(double raAxisPosition, double decAxisPosition)      // DeltaSyncReverse_Matrix_Map
      {
         throw new NotImplementedException();
      }
   }
}
