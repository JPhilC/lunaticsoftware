using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lunatic.Core.Geometry;

namespace Lunatic.Core.Tests
{
   [TestClass]
   public class TakiTests
   {
      DateTime _localTime = new DateTime(2017, 3, 17, 9, 9, 38);
      string _longitude = "W1°18'23.7\"";
      string _latitude = "N52°36'11.69\"";

      [TestMethod]
      public void GetTheoreticalFromEquatorial()
      {

         MountCoordinate mirphac = new MountCoordinate(
            new EquatorialCoordinate("3h25m34.77s", "49°55'12.0\"", _longitude, _localTime),
            new AxisPosition(0.897009787, 0.871268363),
            _latitude);
         MountCoordinate almaak = new MountCoordinate(
            new EquatorialCoordinate("2h04m58.83s", "42°24'41.1\"", _longitude, _localTime),
            new AxisPosition(0.545291764, 0.740218861),
            _latitude);
         MountCoordinate ruchbah = new MountCoordinate(
            new EquatorialCoordinate("1h26m58.39s", "60°19'33.3\"", _longitude, _localTime),
            new AxisPosition(0.37949203, 1.05288587),
            _latitude);
         TakiEQMountMapper taki = new TakiEQMountMapper(mirphac, almaak, ruchbah, _localTime);
         EquatorialCoordinate gPer = new EquatorialCoordinate("2h03m28.89s", "54°34'10.9\"", _longitude, _localTime);
         AxisPosition gPerExpected = new AxisPosition(0.538789685, 0.95242084);
         AxisPosition gPerCalculated = taki.GetAxisPosition(gPer);
         Assert.AreEqual(gPerExpected, gPerCalculated);
      }

      [TestMethod]
      public void AlignmentTest()
      {
         MountCoordinate mirphac = new MountCoordinate(
            new EquatorialCoordinate("3h25m34.77s", "49°55'12.0\"", _longitude, _localTime),
            new AxisPosition(0.897009787, 0.871268363),
            _latitude);
         mirphac.ObservedAxes = new AxisPosition(0.8884478, 0.9392852);

         MountCoordinate almaak = new MountCoordinate(
            new EquatorialCoordinate("2h04m58.83s", "42°24'41.1\"", _longitude, _localTime),
            new AxisPosition(0.545291764, 0.740218861),
            _latitude);
         almaak.ObservedAxes = new AxisPosition(0.5515027, 0.7739144);

         MountCoordinate ruchbah = new MountCoordinate(
            new EquatorialCoordinate("1h26m58.39s", "60°19'33.3\"", _longitude, _localTime),
            new AxisPosition(0.37949203, 1.05288587),
            _latitude);
         ruchbah.ObservedAxes = new AxisPosition(0.37949203, 1.0685469);

         TakiAlignmentMapper taki = new TakiAlignmentMapper(mirphac, almaak, ruchbah);
         AxisPosition gPerTheoretical = new AxisPosition(0.538789685, 0.95242084);

         AxisPosition gPerExpected = new AxisPosition(0.523934, 0.9844184);
         AxisPosition gPerCalculated = taki.GetObservedPosition(gPerTheoretical);
         Assert.AreEqual(gPerExpected, gPerCalculated);
      }

   }
}
