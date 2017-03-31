using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lunatic.Core.Geometry;

namespace Lunatic.Core.Tests
{
   [TestClass]
   public class CoordinateOperatorOverloadTests
   {
      [TestMethod]
      public void CarteseanAdd()
      {
         CarteseanCoordinate c1 = new CarteseanCoordinate(5.5, 5.5);
         CarteseanCoordinate c2 = new CarteseanCoordinate(2.5, 2.5);
         CarteseanCoordinate c3 = new CarteseanCoordinate(8.0, 8.0);
         Assert.IsTrue(c1 + c2 == c3);
      }

      [TestMethod]
      public void CarteseanSubstract()
      {
         CarteseanCoordinate c1 = new CarteseanCoordinate(5.5, 5.5);
         CarteseanCoordinate c2 = new CarteseanCoordinate(2.5, 2.5);
         CarteseanCoordinate c3 = new CarteseanCoordinate(3.0, 3.0);
         Assert.IsTrue(c1 - c2 == c3);
      }

      [TestMethod]
      public void AxisPositionAdd()
      {
         AxisPosition c1 = new AxisPosition(1.55, 1.55);
         AxisPosition c2 = new AxisPosition(1.25, 1.25);
         AxisPosition c3 = new AxisPosition(2.80, 2.80);
         Assert.IsTrue(c1 + c2 == c3);
      }

      [TestMethod]
      public void AxisPositionSubstract()
      {
         AxisPosition c1 = new AxisPosition(1.55, 1.55);
         AxisPosition c2 = new AxisPosition(1.25, 1.25);
         AxisPosition c3 = new AxisPosition(0.30, 0.30);
         AxisPosition c4 = (c1 - c2);
         Assert.IsTrue(c4.Equals(c3, 0.000001));
      }

      [TestMethod]
      public void AltAzCoordinateAdd()
      {
         AltAzCoordinate c1 = new AltAzCoordinate(30.0, 225.0);
         AltAzCoordinate c2 = new AltAzCoordinate(5.0, 5.0);
         AltAzCoordinate c3 = new AltAzCoordinate(35.0, 230.0);
         Assert.IsTrue(c1 + c2 == c3);
      }

      [TestMethod]
      public void AltAzCoordinateSubstract()
      {
         AltAzCoordinate c1 = new AltAzCoordinate(30.0, 270.0);
         AltAzCoordinate c2 = new AltAzCoordinate(90.0, 70.0);
         AltAzCoordinate c3 = new AltAzCoordinate(-60.0, 200.0);
         Assert.IsTrue(c1 - c2 == c3);
      }

      [TestMethod]
      public void EquatorialCoordinateAdd()
      {
         EquatorialCoordinate c1 = new EquatorialCoordinate(4.0, 30.0);
         EquatorialCoordinate c2 = new EquatorialCoordinate(5.0, 5.0);
         EquatorialCoordinate c3 = new EquatorialCoordinate(9.0, 35.0);
         Assert.IsTrue(c1 + c2 == c3);
      }

      [TestMethod]
      public void EquatorialCoordinateSubstract()
      {
         EquatorialCoordinate c1 = new EquatorialCoordinate(23.5, 30.0);
         EquatorialCoordinate c2 = new EquatorialCoordinate(3.5, 90.0);
         EquatorialCoordinate c3 = new EquatorialCoordinate(20.0, -60.0);
         Assert.IsTrue(c1 - c2 == c3);
      }

      //TODO: Add Out of range tests.
   }
}
