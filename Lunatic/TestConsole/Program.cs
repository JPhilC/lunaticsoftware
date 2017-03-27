using ASCOM.Astrometry.Transform;
using ASCOM.Utilities;
using Lunatic.Core;
using Lunatic.Core.Geometry;
using System;

namespace TestConsole
{
   class Program
   {
      [STAThread]
      static void Main(string[] args)
      {
         try {
            //using (JoyStickService js = new JoyStickService()) {
            //   Console.WriteLine("Testing Joystick Service");
            //   Console.WriteLine("Press a key to exit.");
            //   Console.ReadKey();
            //}
            // TestAstro();
            // TestTransforms();
            TestAstroCoordinate();

            Console.WriteLine("Press <Enter> to Exit");
            Console.ReadLine();
         }
         catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine(ex.Message);
         }


      }

      static void TestDriver()
      {
         Console.WriteLine("Press <Enter> to choose a driver.");
         Console.ReadLine();

         ASCOM.Lunatic.Telescope.Telescope driver = new ASCOM.Lunatic.Telescope.Telescope();
         //driver.SetupDialog();
         driver.Connected = true;

         //Type ProgIdType = Type.GetTypeFromProgID("ASCOM.Lunatic.TelescopeDriver.Telescope");
         //Object oDrv = Activator.CreateInstance(ProgIdType);

         //string driverId = ASCOM.DriverAccess.Telescope.Choose("");
         //if (!string.IsNullOrWhiteSpace(driverId)) {
         //   ASCOM.DriverAccess.Telescope driver = new ASCOM.DriverAccess.Telescope(driverId);

         //   Console.WriteLine("Press <Enter> to Connect");
         //   Console.ReadLine();
         //   driver.Connected = true;

         //   Console.WriteLine("Press <Enter> to Dispose");
         //   Console.ReadLine();

         //   driver.Dispose();
         //}
      }

      static void TestTransforms()
      {
         using (Transform transform = new Transform())
         using (Util util = new Util()) {
            double latitude = util.DMSToDegrees("52:40:6.38");
            double longitude = util.DMSToDegrees("-1:20:20.54");
            transform.SiteElevation = 175.50;
            transform.SiteLatitude = latitude;
            transform.SiteLongitude = longitude;

            // Check we are in the same time zone
            double localTimeZoneOffset = -TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalHours;   // Taken from Util.GetTimeZoneOffset()
            Console.WriteLine(string.Format("Local timezone offset = {0}", localTimeZoneOffset));

            DateTime testTime = new DateTime(2017, 3, 14, 9, 21, 27).AddSeconds(0.2);
            //  JD: 2457826.88990 + 1.3 seonds
            double julianTime = util.DateLocalToJulian(testTime);
            Console.WriteLine(string.Format("Julian date: {0}, Expecting 2457826.88990 + 1.3 seconds", julianTime));

            double lst = LocalSiderealTime(longitude, testTime);
            Console.WriteLine(string.Format("Local Siderial Time = {0}, Expecting 20:44:51.7", util.HoursToHMS(lst, ":", ":", "", 1)));


            //transform.JulianDateTT = julianTime;
            transform.JulianDateUTC = julianTime;

            // Deneb RA/DEC (on date) 20h42m1.78s/+45d20'40.6"
            double rA = util.HMSToHours("+20:42:1.78");
            double dEC = util.DMSToDegrees("+45:20:40.6");
            transform.SetTopocentric(rA, dEC);

            double raTopo = transform.RATopocentric;
            double decTopo = transform.DECTopocentric;
            Console.WriteLine(string.Format("RA/Dec (Topocentric) = {0} / {1}, Expecting +20:42:1.78 / +45:20:40.6", util.HoursToHMS(raTopo, ":", ":", "", 2), util.DegreesToDMS(decTopo, ":", ":", "", 1)));

            double raApparent = transform.RAApparent;
            double decApparent = transform.DECApparent;
            Console.WriteLine(string.Format("RA/Dec (Apparent) = {0} / {1}, Expecting +20:42:1.78 / +45:20:40.6", util.HoursToHMS(raApparent, ":", ":", "", 2), util.DegreesToDMS(decApparent, ":", ":", "", 1)));

            double raJ2000 = transform.RAJ2000;
            double decJ2000 = transform.DecJ2000;
            // Expecting RA/Dec (J2000.0): 20h41m25.92s/+45d16'49.3
            Console.WriteLine(string.Format("RA/Dec (J2000) = {0} / {1}, Expecting +20:41:25.92 / +45:16:49.3", util.HoursToHMS(raJ2000, ":", ":", "", 2), util.DegreesToDMS(decJ2000, ":", ":", "", 1)));

            double azTopo = transform.AzimuthTopocentric;
            double altTopo = transform.ElevationTopocentric;
            // Expecting Az/Alt: +183d53'10.5"/+82d39'42.0"
            Console.WriteLine(string.Format("Az/Alt (Topocentric) = {0} / {1}, Expecting +183:53:10.5 / +82:39:42.0", util.DegreesToDMS(azTopo, ":", ":", "", 1), util.DegreesToDMS(altTopo, ":", ":", "", 1)));

         }
      }

      static void TestAstroCoordinate()
      {
         AstroCoordinate aCoord = AstroCoordinate.FromRADec("+20:42:1.78", "+45:20:40.6", "52:40:6.38", "-1:20:20.54", 175.5);
         aCoord.LocalTime = new DateTime(2017, 3, 14, 9, 21, 27);
         AltAzCoordinate altAz = aCoord.AltAz;
         Console.WriteLine(string.Format("Az/Alt (Topocentric) = {0} / {1}, Expecting +183:53:10.5 / +82:39:42.0", 
            altAz.Azimuth.ToString(AngularFormat.DegreesMinutesSeconds), 
            altAz.Altitude.ToString(AngularFormat.DegreesMinutesSeconds)));
      }

      private static double LocalSiderealTime(double longitude)
      {
         return LocalSiderealTime(longitude, DateTime.Now);
      }

      private static double LocalSiderealTime(double longitude, DateTime localTime)
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

      static void TestTransforms_Old()
      {
         using (Transform transform = new Transform())
         using (Util util = new Util()) {
            double latitude = util.DMSToDegrees("52:40:6.38");
            double longitude = util.DMSToDegrees("-1:20:20.54");
            transform.SiteElevation = 175.50;
            transform.SiteLatitude = latitude;
            transform.SiteLongitude = longitude;

            DateTime testTime = new DateTime(2017, 3, 14, 9, 21, 27).AddSeconds(32.184 + 37.0);
            //  JD: 2457826.88990
            double julianTime = util.DateLocalToJulian(testTime);
            Console.WriteLine(string.Format("Julian date: {0}, Expecting 2457826.88990 + 1.3 seconds", julianTime));
            transform.JulianDateTT = julianTime;
            // transform.JulianDateUTC = julianTime;

            // Deneb RA/DEC (on date) 20h42m1.78s/+45d20'40.6"
            double rA = util.HMSToHours("+20:42:1.78");
            double dEC = util.DMSToDegrees("+45:20:40.6");
            transform.SetTopocentric(rA, dEC);


            double raTopo = transform.RATopocentric;
            double decTopo = transform.DECTopocentric;
            Console.WriteLine(string.Format("RA/Dec (Topocentric) = {0}/{1}", util.HoursToHMS(raTopo), util.DegreesToDMS(decTopo)));

            double raApparent = transform.RAApparent;
            double decApparent = transform.DECApparent;
            Console.WriteLine(string.Format("RA/Dec (Apparent) = {0}/{1}", util.HoursToHMS(raApparent), util.DegreesToDMS(decApparent)));

            double raJ2000 = transform.RAJ2000;
            double decJ2000 = transform.DecJ2000;
            // Expecting RA/Dec (J2000.0): 20h41m25.92s/+45d16'49.3
            Console.WriteLine(string.Format("RA/Dec (J2000) = {0}/{1}", util.HoursToHMS(raJ2000), util.DegreesToDMS(decJ2000)));

            double azTopo = transform.AzimuthTopocentric;
            double altTopo = transform.ElevationTopocentric;
            Console.WriteLine("Expecting Az/Alt: +183d53'10.5\"/+82d39'42.0\"");
            Console.WriteLine(string.Format("Az/Alt (Topocentric) = {0}/{1}", util.DegreesToDMS(azTopo), util.DegreesToDMS(altTopo)));
            Console.WriteLine();
            //}
         }
      }

      static void TestAstro()
      {
         using (Util util = new Util()) {
            double latitude = util.DMSToDegrees("51:30:30.00");
            double longitude = util.DMSToDegrees("-0:7:32.00");

            DateTime testTime = new DateTime(2017, 3, 14, 7, 21, 00);
            //  JD: 2457826.88990
            double julianTime = util.DateLocalToJulian(testTime);

            double lst = LunaticMath.LocalSiderealTime(longitude, testTime);
            // Deneb RA/DEC (on date) 20h42m1.78s/+45d20'40.6"
            double rA = util.HMSToHours("+20:42:1.78");
            double dEC = util.DMSToDegrees("+45:20:40.6");
            // H = LST - ɑ
            double hA = lst - rA;


            double az = 0.0;
            double alt = 0.0;
            AstroConvert.HaDecToAltAz(LunaticMath.DegToRad(latitude), LunaticMath.HrsToRad(hA), LunaticMath.DegToRad(dEC), ref alt, ref az);
            double azimuth = LunaticMath.RadToDeg(az);
            double altitude = LunaticMath.RadToDeg(alt);
            Console.WriteLine("Expecting Az/Alt: +97d08'28.9\"/+70d23'31.4\"");
            Console.WriteLine(string.Format("Az/Alt: {0}/{1}", util.DegreesToDMS(azimuth, "°","'","\"",2 ), util.DegreesToDMS(altitude, "°", "'", "\"", 2)));
            Console.WriteLine(string.Format("Local Sidereal Time: {0}", util.HoursToHMS(lst)));
            Console.WriteLine();
            //}
         }
      }

   }
}
