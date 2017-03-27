using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lunatic.Core.Geometry;

namespace Lunatic.Core.Tests
{
   [TestClass]
   public class MountCoordinateTests
   {
      private DateTime _localTime = new DateTime(2017, 3, 14, 9, 21, 27);
      [TestMethod]
      public void MountCoordinateRAToAltAz()
      {
         MountCoordinate deneb = new MountCoordinate(new EquatorialCoordinate("+20h42m1.78s", "+45°20'40.6\"", "-1°20'20.54\"", _localTime),
            new AxisPosition(0.0, 0.0),
            new Angle("52°40'6.38\""));
         deneb.ObservedAltAzimuth = new AltAzCoordinate("+82°39'42.0\"", "+183°53'10.5\"");
         AltAzCoordinate altAz = deneb.SuggestedAltAzimuth;

         System.Diagnostics.Debug.WriteLine(string.Format("Az/Alt (Suggested) = {0}, Expecting {1}",
            deneb.SuggestedAltAzimuth,
            deneb.ObservedAltAzimuth));

         bool testResult = (deneb.SuggestedAltAzimuth == deneb.ObservedAltAzimuth);
         Assert.IsTrue(testResult);
      }
   }
}
