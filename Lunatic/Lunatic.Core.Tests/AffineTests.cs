using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lunatic.Core.Geometry;
using System.Collections.Generic;

namespace Lunatic.Core.Tests
{
   [TestClass]
   public class AffineTests
   {
      DateTime _localTime = new DateTime(2017, 3, 17, 9, 9, 38);
      string _longitude = "W1°18'23.7\"";
      string _latitude = "N52°36'11.69\"";
      double _tolerance = 1.0E-6;



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
         MountCoordinate mirphac = new MountCoordinate(
            new EquatorialCoordinate("3h25m34.77s", "49°55'12.0\"", _longitude, _localTime),
            new AxisPosition(0.897009787, 0.871268363),
            _latitude);
         mirphac.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(200.23040678, 119.09737314);
         MountCoordinate almaak = new MountCoordinate(
            new EquatorialCoordinate("2h04m58.83s", "42°24'41.1\"", _longitude, _localTime),
            new AxisPosition(0.545291764, 0.740218861),
            _latitude);
         almaak.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(204.95895082, 147.09524972);
         MountCoordinate ruchbah = new MountCoordinate(
            new EquatorialCoordinate("1h26m58.39s", "60°19'33.3\"", _longitude, _localTime),
            new AxisPosition(0.37949203, 1.05288587),
            _latitude);
         ruchbah.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(153.23810784, 181.55679225);
         MountCoordinate gper = new MountCoordinate(
            new EquatorialCoordinate("2h03m28.89s", "54°34'10.9\"", _longitude, _localTime),
            new AxisPosition(0.0, 0.0),
            _latitude);
         gper.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(175.97337739, 164.42061332);

         // Create a list of observed catalog Mount Coordinates and initialise the solver.
         List<MountCoordinate> coordinates = new List<MountCoordinate>() {
            mirphac,
            almaak,
            ruchbah
         };
         Affine affineSolver = new Affine(coordinates);

         bool testPass = true;

         AltAzCoordinate sugPosition;
         AltAzCoordinate obsPosition;
         AltAzCoordinate newPosition;
         System.Diagnostics.Debug.WriteLine("Reverse test the initialisation set.");
         foreach (MountCoordinate mc in coordinates) {
            sugPosition = mc.SuggestedAltAzimuth;
            obsPosition = mc.ObservedAltAzimuth;
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
         sugPosition = gper.SuggestedAltAzimuth;
         obsPosition = gper.ObservedAltAzimuth;
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

         Assert.IsTrue(testPass);
      }

      [TestMethod]
      public void AffineAltAz()
      {
         MountCoordinate mirphac = new MountCoordinate(
            new EquatorialCoordinate("3h25m34.77s", "49°55'12.0\"", _longitude, _localTime),
            new AxisPosition(0.897009787, 0.871268363),
            _latitude);
         mirphac.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(200.23040678, 119.09737314);
         MountCoordinate almaak = new MountCoordinate(
            new EquatorialCoordinate("2h04m58.83s", "42°24'41.1\"", _longitude, _localTime),
            new AxisPosition(0.545291764, 0.740218861),
            _latitude);
         almaak.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(204.95895082, 147.09524972);
         MountCoordinate ruchbah = new MountCoordinate(
            new EquatorialCoordinate("1h26m58.39s", "60°19'33.3\"", _longitude, _localTime),
            new AxisPosition(0.37949203, 1.05288587),
            _latitude);
         ruchbah.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(153.23810784, 181.55679225);
         MountCoordinate gper = new MountCoordinate(
            new EquatorialCoordinate("2h03m28.89s", "54°34'10.9\"", _longitude, _localTime),
            new AxisPosition(0.0, 0.0),
            _latitude);
         gper.ObservedAltAzimuth = AltAzCoordinate.FromCartesean(175.97337739, 164.42061332);

         // Create a list of observed catalog Mount Coordinates and initialise the solver.
         List<MountCoordinate> coordinates = new List<MountCoordinate>() {
            mirphac,
            almaak,
            ruchbah
         };
         Affine affineSolver = new Affine(coordinates);

         bool testPass = true;

         AltAzCoordinate sugPosition;
         AltAzCoordinate obsPosition;
         AltAzCoordinate newPosition;
         System.Diagnostics.Debug.WriteLine("Reverse test the initialisation set.");
         foreach (MountCoordinate mc in coordinates) {
            sugPosition = mc.SuggestedAltAzimuth;
            obsPosition = mc.ObservedAltAzimuth;
            newPosition = affineSolver.Transform(sugPosition);
            System.Diagnostics.Debug.WriteLine("({0}) => ({1}) ~= ({2})",
               sugPosition,
               newPosition,
               obsPosition);
            testPass = testPass && ((Math.Abs(obsPosition.Altitude.Value - newPosition.Altitude.Value) <= _tolerance)
               && (Math.Abs(obsPosition.Azimuth.Value - newPosition.Azimuth.Value) <= _tolerance));
         }

         // Now test gPer which was not included in the initialisation set.
         System.Diagnostics.Debug.WriteLine("Test contained point.");
         sugPosition = gper.SuggestedAltAzimuth;
         obsPosition = gper.ObservedAltAzimuth;
         newPosition = affineSolver.Transform(sugPosition);
         System.Diagnostics.Debug.WriteLine("({0}) => ({1}) ~= ({2})",
            sugPosition,
            newPosition,
            obsPosition);
         testPass = testPass && ((Math.Abs(obsPosition.Altitude.Value - newPosition.Altitude.Value) <= _tolerance)
            && (Math.Abs(obsPosition.Azimuth.Value - newPosition.Azimuth.Value) <= _tolerance));

         Assert.IsTrue(testPass);
      }
   }
}
