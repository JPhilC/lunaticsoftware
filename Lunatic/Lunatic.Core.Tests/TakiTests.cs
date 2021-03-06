﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lunatic.Core.Geometry;
using ASCOM.Astrometry.Transform;

namespace Lunatic.Core.Tests
{
   [TestClass]
   public class TakiTests
   {
      DateTime _localTime = new DateTime(2017, 3, 17, 9, 9, 38);
      [TestMethod]
      public void TakiExample5_4_4()
      {
         using (Transform transform = new Transform()) {
            Angle longitude = new Angle("-1°20'20.54\"");
            transform.SiteLatitude = new Angle("52°40'6.38\"");
            transform.SiteLongitude = longitude;
            transform.SiteElevation = 175.5;
            double initialSiderealTime = AstroConvert.LocalApparentSiderealTime(longitude, new DateTime(2017, 03, 28, 21, 0, 0));
            double observationSiderealTime = AstroConvert.LocalApparentSiderealTime(longitude, new DateTime(2017, 03, 28, 21, 27, 56));
            MountCoordinate star1 = new MountCoordinate("0h7m54.0s", "29.038°", new AxisPosition(1.732239, 1.463808), transform, observationSiderealTime);
            observationSiderealTime = AstroConvert.LocalApparentSiderealTime(longitude, new DateTime(2017, 03, 28, 21, 37, 02));
            MountCoordinate star2 = new MountCoordinate("2h21m45.0s", "89.222°", new AxisPosition(5.427625, 0.611563), transform, observationSiderealTime);
            TakiEQMountMapper taki = new TakiEQMountMapper(star1, star2, initialSiderealTime);

            EquatorialCoordinate bCet = new EquatorialCoordinate("0h43m07s", "-18.038°");
            double targetSiderealTime = AstroConvert.LocalApparentSiderealTime(longitude, new DateTime(2017, 03, 28, 21, 52, 12));
            AxisPosition bCetExpected = new AxisPosition(2.27695654215, 0.657465529226);  // 130.46°, 37.67°
            AxisPosition bCetCalculated = taki.GetAxisPosition(bCet, targetSiderealTime);

            System.Diagnostics.Debug.WriteLine("Expected: {0}, calculated: {1}", bCetExpected, bCetCalculated);

            double tolerance = 0.5; // degrees.
            bool testResult = ((Math.Abs(bCetExpected.DecAxis - bCetCalculated.DecAxis) < tolerance)
                  && (Math.Abs(bCetExpected.RAAxis - bCetCalculated.RAAxis) < tolerance));
            Assert.IsTrue(testResult);
         }
      }

      [TestMethod]
      public void GetTheoreticalFromEquatorial()
      {
         using (Transform transform = new Transform()) {
            Angle longitude = new Angle("-1°20'20.54\"");
            transform.SiteLatitude = new Angle("52°40'6.38\"");
            transform.SiteLongitude = longitude;
            transform.SiteElevation = 175.5;
            double localSiderealTime = AstroConvert.LocalApparentSiderealTime(longitude, _localTime);
            MountCoordinate mirphac = new MountCoordinate("3h25m34.77s", "49°55'12.0\"", new AxisPosition(1.04551212078025, 0.882804566344625), transform, localSiderealTime);
            MountCoordinate almaak = new MountCoordinate("2h04m58.83s", "42°24'41.1\"", new AxisPosition(0.597795712351665, 0.817146830684098), transform, localSiderealTime);
            MountCoordinate ruchbah = new MountCoordinate("1h26m58.39s", "60°19'33.3\"", new AxisPosition(0.506260233480349, 1.09753088667021), transform, localSiderealTime);
            TakiEQMountMapper taki = new TakiEQMountMapper(mirphac, almaak, ruchbah, localSiderealTime);
            EquatorialCoordinate gPer = new EquatorialCoordinate("2h03m28.89s", "54°34'10.9\"");
            AxisPosition gPerExpected = new AxisPosition(0.649384407012042, 0.998796900509728);
            AxisPosition gPerCalculated = taki.GetAxisPosition(gPer, localSiderealTime);
            System.Diagnostics.Debug.WriteLine("Calculated: {0}, expected: {1}", gPerExpected, gPerCalculated);
            double tolerance = 0.25; // degrees.
            bool testResult = ((Math.Abs(gPerExpected.DecAxis - gPerCalculated.DecAxis) < tolerance)
                  && (Math.Abs(gPerExpected.RAAxis - gPerCalculated.RAAxis) < tolerance));

            Assert.IsTrue(testResult);
         }
      }

      //[TestMethod]
      //public void AlignmentTest()
      //{
      //   MountCoordinate mirphac = new MountCoordinate(
      //      new EquatorialCoordinate("3h25m34.77s", "49°55'12.0\""),
      //      new AxisPosition(0.897009787, 0.871268363));
      //   mirphac.SetObservedAxis(new AxisPosition(0.8884478, 0.9392852), _localTime);

      //   MountCoordinate almaak = new MountCoordinate(
      //      new EquatorialCoordinate("2h04m58.83s", "42°24'41.1\""),
      //      new AxisPosition(0.545291764, 0.740218861));
      //   almaak.SetObservedAxis(new AxisPosition(0.5515027, 0.7739144), _localTime);

      //   MountCoordinate ruchbah = new MountCoordinate(
      //      new EquatorialCoordinate("1h26m58.39s", "60°19'33.3\""),
      //      new AxisPosition(0.37949203, 1.05288587));
      //   ruchbah.SetObservedAxis(new AxisPosition(0.37949203, 1.0685469), _localTime);

      //   TakiAlignmentMapper taki = new TakiAlignmentMapper(mirphac, almaak, ruchbah);
      //   AxisPosition gPerTheoretical = new AxisPosition(0.538789685, 0.95242084);

      //   AxisPosition gPerExpected = new AxisPosition(0.523934, 0.9844184);
      //   AxisPosition gPerCalculated = taki.GetObservedPosition(gPerTheoretical);
      //   Assert.AreEqual(gPerExpected, gPerCalculated);
      //}

   }
}
