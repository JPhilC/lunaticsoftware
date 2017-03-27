using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lunatic.Core.Geometry;
using System.Diagnostics;

namespace Lunatic.Core.Tests
{
   [TestClass]
   public class AltAzCoordinateTests
   {
      public TestContext TestContext { get; set; }


      [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"Data\AzimuthAltitudeSamples.xml", "Row", DataAccessMethod.Sequential)]
      [TestMethod]
      public void AzAltCateseanRoundTrip()
      {
         // Arrange
         double alt = (double)TestContext.DataRow[0];
         double az = (double)TestContext.DataRow[1];
         double tol = (double)TestContext.DataRow[2];
         string caption = TestContext.DataRow[3].ToString();
         string failMessage = TestContext.DataRow[4].ToString();
         Debug.WriteLine(caption, az, alt);
         AltAzCoordinate coord = new AltAzCoordinate(alt, az);
         AltAzCoordinate coord2 = AltAzCoordinate.FromCartesean(coord.X, coord.Y);
         Assert.AreEqual(coord.Azimuth.Value, coord2.Azimuth.Value, tol, string.Format(failMessage, "Azimuth"));
         Assert.AreEqual(coord.Altitude.Value, coord2.Altitude.Value, tol, string.Format(failMessage, "Altitude"));
      }

      [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", @"Data\AzimuthAltitudeLimits.xml", "Row", DataAccessMethod.Sequential)]
      [ExpectedException(typeof(ArgumentOutOfRangeException))]
      [TestMethod]
      public void AzAltCoordinateLimits()
      {
         // Arrange
         double alt = (double)TestContext.DataRow[0];
         double az = (double)TestContext.DataRow[1];
         string failMessage = TestContext.DataRow[2].ToString();
         AltAzCoordinate coord = new AltAzCoordinate(alt, az);

      }
   }
}
