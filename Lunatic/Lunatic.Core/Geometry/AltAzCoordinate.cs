using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core.Geometry
{
   /// <summary>
   /// A structure to represent an Altitude Azimuth coordinate
   /// </summary>
   public struct AltAzCoordinate
   {
      private Angle _Alt;
      private Angle _Az;
      private double _X;
      private double _Y;
      public const double ALT_OFFSET = 180;  // Used to allow us to encode altitudes of when determing equivalent cartesean coordinates.

      public Angle Altitude
      {
         get
         {
            return _Alt;
         }
      }


      public Angle Azimuth
      {
         get
         {
            return _Az;
         }
      }

      /// <summary>
      /// Returns the cartesean X component of the coordinate
      /// Cos(Az)*Alt
      /// </summary>
      public double X
      {
         get
         {
            return _X;
         }
      }

      /// <summary>
      /// Returns the cartesean Y component of the coordinate
      /// Sin(Az)*Alt
      /// </summary>
      public double Y
      {
         get
         {
            return _Y;
         }
      }

      public AltAzCoordinate(string altitude, string azimuth)
      {

         _Alt = new Angle(altitude);
         _Az = new Angle(azimuth);
         _X = Math.Cos(_Az.Radians) * (_Alt + ALT_OFFSET);
         _Y = Math.Sin(_Az.Radians) * (_Alt + ALT_OFFSET);
      }
      public AltAzCoordinate(double altitude,double azimuth)
      {
         if (azimuth < 0 || azimuth >= 360) {
            throw new ArgumentOutOfRangeException("Azimuth must be >= 0 and < 360");
         }
         if (altitude < -90 || altitude > 90) {
            throw new ArgumentOutOfRangeException("Altitude must be between -90 and 90.");
         }
         _Alt = new Angle(altitude);
         _Az = new Angle(azimuth);
         _X = Math.Cos(_Az.Radians) * (_Alt + ALT_OFFSET);
         _Y = Math.Sin(_Az.Radians) * (_Alt + ALT_OFFSET);
      }

      public AltAzCoordinate(Angle altitude, Angle azimuth)
      {
         if (azimuth.Value < 0 || azimuth.Value >= 360) {
            throw new ArgumentOutOfRangeException("Azimuth must be >= 0 and < 360");
         }
         if (altitude.Value < -90 || altitude.Value > 90) {
            throw new ArgumentOutOfRangeException("Altitude must be between -90 and 90.");
         }
         _Alt = altitude;
         _Az = azimuth;
         _X = Math.Cos(_Az.Radians) * (_Alt + ALT_OFFSET);
         _Y = Math.Sin(_Az.Radians) * (_Alt + ALT_OFFSET);
      }

      /// <summary>
      /// Index used during Affine transformations
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      public double this[int index]
      {
         get
         {
            if (index < 0 || index > 1) {
               throw new ArgumentOutOfRangeException();
            }
            return (index == 0 ? _X : _Y);
         }
         //set
         //{
         //   if (index < 0 || index > 1) {
         //      throw new ArgumentOutOfRangeException();
         //   }
         //   if (index == 0) {
         //      _RAAxis.Radians = value;
         //   }
         //   else {
         //      _DecAxis.Radians = value;
         //   }
         //}
      }

      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(AltAzCoordinate pos1, AltAzCoordinate pos2)
      {
         return (pos1.Altitude.Value == pos2.Altitude.Value && pos1.Azimuth.Value == pos2.Azimuth.Value);
      }

      public static bool operator !=(AltAzCoordinate pos1, AltAzCoordinate pos2)
      {
         return !(pos1 == pos2);
      }

      public override int GetHashCode()
      {
         unchecked // Overflow is fine, just wrap
         {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + _Az.GetHashCode();
            hash = hash * 23 + _Alt.GetHashCode();
            return hash;
         }
      }

      public override bool Equals(object obj)
      {
         return (obj is AltAzCoordinate
                 && this == (AltAzCoordinate)obj);
      }
      public static AltAzCoordinate operator -(AltAzCoordinate pos1, AltAzCoordinate pos2)
      {
         return new AltAzCoordinate(pos1.Altitude - pos2.Altitude, pos1.Azimuth - pos2.Azimuth);
      }

      public static AltAzCoordinate operator +(AltAzCoordinate pos1, AltAzCoordinate pos2)
      {
         return new AltAzCoordinate(pos1.Altitude + pos2.Altitude, pos1.Azimuth + pos2.Azimuth);
      }

      public override string ToString()
      {
         return string.Format("Alt/Az = {0}/{1}", 
            Altitude.ToString(AngularFormat.DegreesMinutesSeconds, false), 
            Azimuth.ToString(AngularFormat.DegreesMinutesSeconds, false));
      }

      /// <summary>
      /// Decodes an AzAlt Coordinate from it's cartesean equivalent
      /// Note: This method should ONLY be used to decode cartesean coordinates
      /// that were originally generated from an AzAltCoordinate of from values
      /// interpolated from those originally generated from AzAltCoordinates.
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <returns></returns>
      public static AltAzCoordinate FromCartesean(double x, double y)
      {
         double az = 0.0;
         double alt = Math.Sqrt((x * x) + (y * y));
         if (x > 0) {
            az = Math.Atan(y / x);
         }

         if (x < 0) {
            if (y >= 0) {
               az = Math.Atan(y / x) + Math.PI;
            }
            else {
               az = Math.Atan(y / x) - Math.PI;
            }
         }
         if (x == 0) {
            if (y > 0) {
               az = Math.PI / 2.0;
            }
            else {
               az = -1 * (Math.PI / 2.0);
            }
         }
         return new AltAzCoordinate((alt - ALT_OFFSET), AstroConvert.Range360(AstroConvert.RadToDeg(az)));
      }

   }

}
