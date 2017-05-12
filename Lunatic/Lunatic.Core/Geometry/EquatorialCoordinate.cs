using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core.Geometry
{
   /// <summary>
   /// A structure to represent an EquatorialCoordinate
   /// </summary>
   public struct EquatorialCoordinate
   {
      private HourAngle _RA;
      private Angle _Dec;
      //private DateTime _ObservedWhen;
      //private Angle _Longitude;

      public HourAngle RightAscention
      {
         get
         {
            return _RA;
         }
      }
      public Angle Declination
      {
         get
         {
            return _Dec;
         }
      }

      public EquatorialCoordinate(double rightAscention, double declination)   // , double longitude, DateTime observedTime)
      {
         if (rightAscention < 0 || rightAscention > 24.0) { throw new ArgumentOutOfRangeException("Right Ascension must be between 0 and 24."); }
         if (declination < -90 || declination > 90) { throw new ArgumentOutOfRangeException("Declination must be between -90 and 90."); }
         _RA = new HourAngle(rightAscention);
         _Dec = new Angle(declination);
         //_Longitude = new Angle(longitude);
         //_ObservedWhen = observedTime;
      }

      //public EquatorialCoordinate(HourAngle rightAscention, Angle declination)    // , Angle longitude)
      //   :this(rightAscention, declination, longitude, DateTime.Now)
      //{
      //}

      public EquatorialCoordinate(HourAngle rightAscention, Angle declination)    // , Angle longitude, DateTime observedTime)
      {
         if (rightAscention.Value < 0 || rightAscention.Value > 24.0) { throw new ArgumentOutOfRangeException("Right Ascension must be between 0 and 24."); }
         if (declination.Value < -90 || declination.Value > 90) { throw new ArgumentOutOfRangeException("Declination must be between -90 and 90."); }
         _RA = rightAscention;
         _Dec = declination;
         //_Longitude = longitude;
         //_ObservedWhen = observedTime;
      }

      #region Operator overloads ...
      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return (pos1.RightAscention.Value == pos2.RightAscention.Value && pos1.Declination.Value == pos2.Declination.Value);
      }

      public static bool operator !=(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return !(pos1 == pos2);
      }

      public override int GetHashCode()
      {
         unchecked // Overflow is fine, just wrap
         {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + _RA.GetHashCode();
            hash = hash * 23 + _Dec.GetHashCode();
            return hash;
         }
      }

      public override bool Equals(object obj)
      {
         return (obj is EquatorialCoordinate
                 && this == (EquatorialCoordinate)obj);
      }

      public static EquatorialCoordinate operator -(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return new EquatorialCoordinate(pos1.RightAscention - pos2.RightAscention, pos1.Declination - pos2.Declination);
      }

      public static EquatorialCoordinate operator +(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return new EquatorialCoordinate(pos1.RightAscention + pos2.RightAscention, pos1.Declination + pos2.Declination);
      }


      public override string ToString()
      {
         return string.Format("{0}/{1}", _RA, _Dec);
      }
      #endregion

      public CarteseanCoordinate ToCartesean(Angle latitude, bool affineTaki = true)
      {
         CarteseanCoordinate cartCoord;
         if (affineTaki) {
            // Get Polar (or should than be get AltAzimuth) from Equatorial coordinate (formerly call to EQ_SphericalPolar)
            AltAzCoordinate polar = AstroConvert.GetAltAz(this, latitude);
            // Get  Cartesean from Polar (formerly call to EQ_Polar2Cartes)
            cartCoord = polar.ToCartesean();
         }
         else {
            cartCoord = new CarteseanCoordinate(this.RightAscention.Radians, this.Declination.Radians, 1.0);
         }
         return cartCoord;
      }


   }

}
