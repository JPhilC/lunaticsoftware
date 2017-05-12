using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lunatic.Core.Geometry;
using System.Collections.Generic;
using ASCOM.Utilities;
using ASCOM.Astrometry.Transform;

namespace Lunatic.Core.Tests
{
   [TestClass]
   public class AffineTests
   {
      DateTime _localTime = new DateTime(2017, 3, 17, 9, 9, 38);
      double _tolerance = 1.0E-6;
      string _longitude = "W1°18'23.7\"";
      string _latitude = "N52°36'11.69\"";
      double _elevation = 175.5;


      [TestMethod]
      public void OneXOneRectangleTest()
      {
         DateTime now = DateTime.Now;
         // Theoretical = a 1 x 1 rectangle
         // Observed - > scaled x 2, rotated 45 degrees and translated
         double[][] from = new double[4][];
         from[0] = new double[] { 1.0, 1.0 };
         from[1] = new double[] { 1.0, 2.0 };
         from[2] = new double[] { 2.0, 2.0 };
         from[3] = new double[] { 2.0, 1.0 };
         double[][] to = new double[4][];
         to[0] = new double[] { 4.0, 4.0 };
         to[1] = new double[] { 6.0, 6.0 };
         to[2] = new double[] { 8.0, 4.0 };
         to[3] = new double[] { 6.0, 2.0 };

         Affine affineSolver = new Affine(from, to);
         bool testPass = true;
         for (int i = 0; i < from.Length; i++) {
            double[] newPosition = affineSolver.Transform(from[i]);
            testPass = testPass && ((Math.Abs(newPosition[0] - to[i][0]) <= _tolerance)
               && (Math.Abs(newPosition[1] - to[i][1]) <= _tolerance));
            System.Diagnostics.Debug.WriteLine("({0:R},{1:R}) => ({2:R},{3:R}) ~= ({4:R},{5:R})",
               from[i][0],
               from[i][1],
               newPosition[0],
               newPosition[1],
               to[i][0],
               to[i][1]);
         }

         Assert.IsTrue(testPass);
      }

      [TestMethod]
      public void AffineAltAzXY()
      {
         MountCoordinate mirphac = new MountCoordinate("3h25m34.77s", "49°55'12.0\"");
         mirphac.AltAzimuth = AltAzCoordinate.FromCartesean(166.35854122, 117.31180100);
         MountCoordinate almaak = new MountCoordinate("2h04m58.83s", "42°24'41.1\"");
         almaak.AltAzimuth = AltAzCoordinate.FromCartesean(128.55624013, 167.82625637);
         MountCoordinate ruchbah = new MountCoordinate("1h26m58.39s", "60°19'33.3\"");
         ruchbah.AltAzimuth = AltAzCoordinate.FromCartesean(173.53560437, 141.50092003);
         MountCoordinate gper = new MountCoordinate("2h03m28.89s", "54°34'10.9\"");
         gper.AltAzimuth = AltAzCoordinate.FromCartesean(161.76649179, 145.00573319);

         // Create a list of observed catalog Mount Coordinates and initialise the solver.
         List<MountCoordinate> coordinates = new List<MountCoordinate>() {
            mirphac,
            almaak,
            ruchbah
         };

         bool testPass = true;
         AltAzCoordinate sugPosition;
         AltAzCoordinate obsPosition;
         AltAzCoordinate newPosition;

         using (Util util = new Util())
         using (Transform transform = new Transform()) {
            transform.SiteLatitude = new Angle(_latitude);
            transform.SiteLongitude = new Angle(_longitude);
            transform.SiteElevation = _elevation;
            transform.JulianDateUTC = util.DateLocalToJulian(_localTime.AddSeconds(Constants.UTIL_LOCAL2JULIAN_TIME_CORRECTION));

            Affine affineSolver = new Affine(coordinates, transform);

            System.Diagnostics.Debug.WriteLine("Reverse test the initialisation set.");
            foreach (MountCoordinate mc in coordinates) {
               sugPosition = mc.GetAltAzimuth(transform);
               obsPosition = mc.AltAzimuth;
               newPosition = affineSolver.Transform(sugPosition);
               System.Diagnostics.Debug.WriteLine("({0},{1}) => ({2},{3}) ~= ({4},{5})",
                  sugPosition.X,
                  sugPosition.Y,
                  newPosition.X,
                  newPosition.Y,
                  obsPosition.X,
                  obsPosition.Y);
               testPass = testPass && ((Math.Abs(obsPosition.X - newPosition.X) <= _tolerance)
                  && (Math.Abs(obsPosition.Y - newPosition.Y) <= _tolerance));
            }

            // Now test gPer which was not included in the initialisation set.
            System.Diagnostics.Debug.WriteLine("Test contained point.");
            sugPosition = gper.GetAltAzimuth(transform);
            obsPosition = gper.AltAzimuth;
            newPosition = affineSolver.Transform(sugPosition);
            System.Diagnostics.Debug.WriteLine("({0},{1}) => ({2},{3}) ~= ({4},{5})",
               sugPosition.X,
               sugPosition.Y,
               newPosition.X,
               newPosition.Y,
               obsPosition.X,
               obsPosition.Y);
            testPass = testPass && ((Math.Abs(obsPosition.X - newPosition.X) <= _tolerance)
               && (Math.Abs(obsPosition.Y - newPosition.Y) <= _tolerance));
         }
         Assert.IsTrue(testPass);
      }

      [TestMethod]
      public void AffineAltAz()
      {
         MountCoordinate mirphac = new MountCoordinate("3h25m34.77s", "49°55'12.0\"");
         mirphac.AltAzimuth = AltAzCoordinate.FromCartesean(166.35854122, 117.31180100);
         MountCoordinate almaak = new MountCoordinate("2h04m58.83s", "42°24'41.1\"");
         almaak.AltAzimuth = AltAzCoordinate.FromCartesean(128.55624013, 167.82625637);
         MountCoordinate ruchbah = new MountCoordinate("1h26m58.39s", "60°19'33.3\"");
         ruchbah.AltAzimuth = AltAzCoordinate.FromCartesean(173.53560437, 141.50092003);
         MountCoordinate gper = new MountCoordinate("2h03m28.89s", "54°34'10.9\"");
         gper.AltAzimuth = AltAzCoordinate.FromCartesean(161.76649179, 145.00573319);

         // Create a list of observed catalog Mount Coordinates and initialise the solver.
         List<MountCoordinate> coordinates = new List<MountCoordinate>() {
            mirphac,
            almaak,
            ruchbah
         };

         bool testPass = true;

         AltAzCoordinate sugPosition;
         AltAzCoordinate obsPosition;
         AltAzCoordinate newPosition;

         using (Util util = new Util())
         using (Transform transform = new Transform()) {
            transform.SiteLatitude = new Angle(_latitude);
            transform.SiteLongitude = new Angle(_longitude);
            transform.SiteElevation = _elevation;
            transform.JulianDateUTC = util.DateLocalToJulian(_localTime.AddSeconds(Constants.UTIL_LOCAL2JULIAN_TIME_CORRECTION));

            Affine affineSolver = new Affine(coordinates, transform);

            System.Diagnostics.Debug.WriteLine("Reverse test the initialisation set.");
            foreach (MountCoordinate mc in coordinates) {
               sugPosition = mc.GetAltAzimuth(transform);
               obsPosition = mc.AltAzimuth;
               newPosition = affineSolver.Transform(sugPosition);
               System.Diagnostics.Debug.WriteLine("({0}) => ({1}) ~= ({2})",
                  sugPosition,
                  newPosition,
                  obsPosition);
               testPass = testPass && ((Math.Abs(obsPosition.Altitude - newPosition.Altitude) <= Constants.TENTH_SECOND)
                  && (Math.Abs(obsPosition.Azimuth.Value - newPosition.Azimuth.Value) <= Constants.TENTH_SECOND));
            }

            // Now test gPer which was not included in the initialisation set.
            System.Diagnostics.Debug.WriteLine("Test contained point.");
            sugPosition = gper.GetAltAzimuth(transform);
            obsPosition = gper.AltAzimuth;
            newPosition = affineSolver.Transform(sugPosition);
            System.Diagnostics.Debug.WriteLine("({0}) => ({1}) ~= ({2})",
               sugPosition,
               newPosition,
               obsPosition);
            testPass = testPass && ((Math.Abs(obsPosition.Altitude.Value - newPosition.Altitude.Value) <= Constants.TENTH_SECOND)
               && (Math.Abs(obsPosition.Azimuth.Value - newPosition.Azimuth.Value) <= Constants.TENTH_SECOND));
         }
         Assert.IsTrue(testPass);
      }

      //[TestMethod]
      //public void GetObservedAxisPositions()
      //{
      //   MountCoordinate mirphac = new MountCoordinate("3h25m34.77s", "49°55'12.0\"");
      //   mirphac.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(166.35854122, 117.31180100);
      //   MountCoordinate almaak = new MountCoordinate("2h04m58.83s", "42°24'41.1\"");
      //   almaak.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(128.55624013, 167.82625637);
      //   MountCoordinate ruchbah = new MountCoordinate("1h26m58.39s", "60°19'33.3\"");
      //   ruchbah.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(173.53560437, 141.50092003);
      //   MountCoordinate gper = new MountCoordinate("2h03m28.89s", "54°34'10.9\"");
      //   gper.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(161.76649179, 145.00573319);

      //   // Create a list of observed catalog Mount Coordinates and initialise the solver.
      //   List<MountCoordinate> coordinates = new List<MountCoordinate>() {
      //      mirphac,
      //      almaak,
      //      ruchbah,
      //      gper
      //   };

      //   EquatorialCoordinate eqPosition;

      //   using (Util util = new Util())
      //   using (Transform transform = new Transform()) {
      //      transform.SiteLatitude = new Angle(_latitude);
      //      transform.SiteLongitude = new Angle(_longitude);
      //      transform.SiteElevation = _elevation;
      //      transform.JulianDateUTC = util.DateLocalToJulian(_localTime.AddSeconds(Constants.UTIL_LOCAL2JULIAN_TIME_CORRECTION));

      //      foreach (MountCoordinate mc in coordinates) {
      //         eqPosition = mc.GetRADec(transform);
      //         System.Diagnostics.Debug.WriteLine("(Catalog {0}:  Observed {1} -> AxisPositions ({2},{3})",
      //            mc.Equatorial,
      //            eqPosition,
      //            AstroConvert.HrsToRad(eqPosition.RightAscention),
      //            AstroConvert.DegToRad(eqPosition.Declination));
      //      }
      //   }
      //      // Now test gPer which was not included in the initialisation set.
      //      Assert.Fail();
      //}

   }
}
