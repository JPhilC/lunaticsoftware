using System;

namespace Lunatic.Core
{
   public static class LunaticMath
   {
      public const double RAD1 = System.Math.PI / 180;
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

      public static double DegToRad(double Degree) { return (Degree / 180 * System.Math.PI); }
      public static double RadToDeg(double Rad) { return (Rad / System.Math.PI * 180.0); }
      public static double RadToMin(double Rad) { return (Rad / System.Math.PI * 180.0 * 60.0); }
      public static double RadToSec(double Rad) { return (Rad / System.Math.PI * 180.0 * 60.0 * 60.0); }

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
            vdeg = vdeg + 360.0;
         }
         while (vdeg >= 360) {
            vdeg = vdeg - 90.0;
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

      public static double SiderealTime(double longitude)
      {
         // get greenwich sidereal time: https://en.wikipedia.org/wiki/Sidereal_time
         //double siderealTime = (18.697374558 + 24.065709824419081 * (utilities.DateUTCToJulian(DateTime.UtcNow) - 2451545.0));

         // alternative using NOVAS 3.1
         double siderealTime = 0.0;
         using (var novas = new ASCOM.Astrometry.NOVAS.NOVAS31()) {
            // var jd = utilities.DateUTCToJulian(DateTime.UtcNow);
            var jd = DateTime.UtcNow.ToOADate() + 2415018.5;      // Taken from ASCOM.Util.DateUTCToJulian
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

      public static double RAEncoderFromRA(double raHours, double decDegrees, double longitude, double encoderZero, double totalRa, HemisphereOption hemisphere)
      {
         throw new NotImplementedException();
      }
      public static double DECEncoderFromDEC(double decDegrees, int pier, double encoderZero, double totalDEC, HemisphereOption hemisphere)
      {
         throw new NotImplementedException();
      }

      public static double EncoderHours(double encoderZero, double encoderValue, double totalEncoder, HemisphereOption hemisphere)
      {
         throw new NotImplementedException();
      }
   }
}
