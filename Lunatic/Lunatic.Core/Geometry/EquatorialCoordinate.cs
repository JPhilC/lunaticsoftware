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
      private DateTime _ObservedWhen;
      private Angle _Longitude;

      public HourAngle RightAcension
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
      public Angle SiteLongitude
      {
         get
         {
            return _Longitude;
         }
      }

      public DateTime ObservedWhen
      {
         get
         {
            return _ObservedWhen;
         }
      }

      /// <summary>
      /// Return the Hour angle of the coordiate at the time of observation.
      /// </summary>
      public HourAngle Ha
      {
         get
         {
            return GetHa(_ObservedWhen);
         }
      }

      public EquatorialCoordinate(string rightAcension, string declination, string longitude)
         :this(rightAcension, declination, longitude, DateTime.Now)
      {
      }

      public EquatorialCoordinate(string rightAcension, string declination, string longitude,  DateTime observedTime )
      {
         _RA = new HourAngle(rightAcension);
         _Dec = new Angle(declination);
         _Longitude = new Angle(longitude);
         _ObservedWhen = observedTime;
      }

      public EquatorialCoordinate(double rightAcension, double declination, double longitude)
         :this(rightAcension, declination, longitude, DateTime.Now)
      {
      }

      public EquatorialCoordinate(double rightAcension, double declination, double longitude, DateTime observedTime)
      {
         if (rightAcension < 0 || rightAcension > 24.0) { throw new ArgumentOutOfRangeException("Right Ascension must be between 0 and 24."); }
         if (declination < -90 || declination > 90) { throw new ArgumentOutOfRangeException("Declination must be between -90 and 90."); }
         _RA = new HourAngle(rightAcension);
         _Dec = new Angle(declination);
         _Longitude = new Angle(longitude);
         _ObservedWhen = observedTime;
      }

      public EquatorialCoordinate(HourAngle rightAcension, Angle declination, Angle longitude)
         :this(rightAcension, declination, longitude, DateTime.Now)
      {
      }

      public EquatorialCoordinate(HourAngle rightAcension, Angle declination, Angle longitude, DateTime observedTime)
      {
         if (rightAcension.Value < 0 || rightAcension.Value > 24.0) { throw new ArgumentOutOfRangeException("Right Ascension must be between 0 and 24."); }
         if (declination.Value < -90 || declination.Value > 90) { throw new ArgumentOutOfRangeException("Declination must be between -90 and 90."); }
         _RA = rightAcension;
         _Dec = declination;
         _Longitude = longitude;
         _ObservedWhen = observedTime;
      }


      /// <summary>
      /// Returns the HA for a given local time.
      /// </summary>
      /// <param name="time"></param>
      /// <returns></returns>
      public HourAngle GetHa(DateTime time)
      {
         return LunaticMath.LocalSiderealTime(_Longitude.Value, time) - _RA.Value;
      }

      #region Operator overloads ...
      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return (pos1.RightAcension.Value == pos2.RightAcension.Value && pos1.Declination.Value == pos2.Declination.Value);
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

      public override string ToString()
      {
         return string.Format("{0}/{1}", _RA, _Dec);
      }
      #endregion
   }

}
