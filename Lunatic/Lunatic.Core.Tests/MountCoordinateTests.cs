using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lunatic.Core.Geometry;
using ASCOM.Utilities;
using ASCOM.Astrometry.Transform;

namespace Lunatic.Core.Tests
{
   [TestClass]
   public class MountCoordinateTests
   {
      private DateTime _localTime = new DateTime(2017, 3, 14, 9, 21, 27);
      [TestMethod]
      public void MountCoordinateRAToAltAz()
      {
         double localTimeZoneOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalHours;   // Taken from Util.GetTimeZoneOffset()
         DateTime testTime = _localTime.AddHours(localTimeZoneOffset).AddSeconds(Constants.UTIL_LOCAL2JULIAN_TIME_CORRECTION);     // Fix for daylight saving 0.2 seconds
         using (Util util = new Util())
         using (Transform transform = new Transform()) {
            Angle longitude = new Angle("-1°20'20.54\"");
            double last = AstroConvert.LocalApparentSiderealTime(longitude, testTime);
            System.Diagnostics.Debug.WriteLine(string.Format("Local Sidereal Time = {0}, Expecting 20:44:51.7", util.HoursToHMS(last, ":", ":", "", 1)));
            transform.SiteLatitude = new Angle("52°40'6.38\"");
            transform.SiteLongitude = longitude;
            transform.SiteElevation = 175.5;
            transform.JulianDateUTC = util.DateLocalToJulian(testTime);

            MountCoordinate deneb = new MountCoordinate("+20h42m1.78s", "+45°20'40.6\"");
            deneb.ObservedAltAzimuth = new AltAzCoordinate("+82°39'42.0\"", "+183°53'10.5\"");
            AltAzCoordinate suggestedAltAz = deneb.GetAltAzimuth(transform);

            System.Diagnostics.Debug.WriteLine(string.Format("{0} (Suggested), Expecting {1}",
               suggestedAltAz,
               deneb.ObservedAltAzimuth));
            double tolerance = 5.0 / 3600; // 5 seconds.
            bool testResult = ((Math.Abs(suggestedAltAz.Altitude.Value-deneb.ObservedAltAzimuth.Altitude.Value) < tolerance) 
                  && (Math.Abs(suggestedAltAz.Azimuth.Value - deneb.ObservedAltAzimuth.Azimuth.Value) < tolerance));
            Assert.IsTrue(testResult);
         }
      }
   }
}
