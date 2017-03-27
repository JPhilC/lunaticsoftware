using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.Astrometry.Transform;
using ASCOM.Utilities;

namespace Lunatic.Core.Geometry
{
   /// <summary>
   /// A class for determining the Azimuth and Altitude of Equatorial 
   /// coordinate at a given time and observing location. 
   /// </summary>
   public class AstroCoordinate : IDisposable
   {
      private enum InitialisedWith
      {
         RADec,
         AzAlt
      }

      private EquatorialCoordinate _Equatorial;
      private AltAzCoordinate _AltAz;
      private HourAngle _Ha;
      private Transform _Transform;
      private Util _Util;
      private InitialisedWith _InitialisedWith;
      private DateTime _LocalTime;

      public EquatorialCoordinate Equatorial
      {
         set
         {
            _Equatorial = value;
            _Transform.SetTopocentric(_Equatorial.RightAcension, _Equatorial.Declination);
            _Transform.Refresh();
            _InitialisedWith = InitialisedWith.RADec;
         }
         get
         {
            if (_InitialisedWith == InitialisedWith.AzAlt) {
               _Equatorial = GetEquatorial();
            }
            return _Equatorial;
         }
      }
      public AltAzCoordinate AltAz
      {
         set
         {
            _AltAz = value;
            _Transform.SetAzimuthElevation(_AltAz.Azimuth, _AltAz.Altitude);
            _Transform.Refresh();
            _InitialisedWith = InitialisedWith.AzAlt;
         }
         get
         {
            if (_InitialisedWith == InitialisedWith.RADec) {
               _AltAz = GetAltAz();
            }
            return _AltAz;
         }
      }

      public HourAngle HourAngle
      {
         get
         {
            //TODO: Code Hour Angle
            // Sidereal time at Greenwich - Longitude - Ra

            return _Ha;
         }
      }

      public Angle Latitude
      {
         get
         {
            return _Transform.SiteLatitude;
         }
      }

      public Angle Longitude
      {
         get
         {
            return _Transform.SiteLongitude;
         }
      }

      /// <summary>
      /// Site elevation in metres
      /// </summary>
      public double Elevation
      {
         get
         {
            return _Transform.SiteElevation;
         }
      }


      public DateTime LocalTime
      {
         get
         {
            if (_Transform.JulianDateUTC == 0) {
               return DateTime.Now;
            }
            else {
               return _Util.DateJulianToLocal(_Transform.JulianDateUTC).AddSeconds(-0.2);
            }
         }
         set
         {
            _Transform.JulianDateUTC = _Util.DateLocalToJulian(value.AddSeconds(0.2));
            _Transform.Refresh();
         }
      }

      private AstroCoordinate()
      {
         _Transform = new Transform();
         _Util = new Util();
      }

      public static AstroCoordinate FromRADec(string rightAcension, string declination, string latitude, string longitude, double elev)
      {
         AstroCoordinate coordinate = new AstroCoordinate(latitude, longitude, elev);
         coordinate.Equatorial = new EquatorialCoordinate(rightAcension, declination, longitude);
         return coordinate;
      }

      public static AstroCoordinate FromRADec(double rightAcension, double declination, double latitude, double longitude, double elev)
      {
         AstroCoordinate coordinate = new AstroCoordinate(latitude, longitude, elev);
         coordinate.Equatorial = new EquatorialCoordinate(rightAcension, declination, longitude);
         return coordinate;
      }

      public static AstroCoordinate FromAltAz(string altitude, string azimuth, string latitude, string longitude, double elev)
      {
         AstroCoordinate coordinate = new AstroCoordinate(latitude, longitude, elev);
         coordinate.AltAz = new AltAzCoordinate(altitude, azimuth);
         return coordinate;
      }

      public static AstroCoordinate FromAltAz(double altitude, double azimuth, double latitude, double longitude, double elev)
      {
         AstroCoordinate coordinate = new AstroCoordinate(latitude, longitude, elev);
         coordinate.AltAz = new AltAzCoordinate(altitude, azimuth);
         return coordinate;
      }

      public AstroCoordinate(string latitude, string longitude, double elev) : this()
      {
         _Transform.SiteLatitude = new Angle(latitude);
         _Transform.SiteLongitude = new Angle(longitude);
         _Transform.SiteElevation = elev;
      }

      public AstroCoordinate(double latitude, double longitude, double elev) : this()
      {
         _Transform.SiteLatitude = new Angle(latitude);
         _Transform.SiteLongitude = new Angle(longitude);
         _Transform.SiteElevation = elev;
      }


      public AstroCoordinate(string rightAcension, string declination, string latitude, string longitude, double elev) : this(latitude, longitude, elev)
      {
         _InitialisedWith = InitialisedWith.RADec;
         _Equatorial = new EquatorialCoordinate(rightAcension, declination, longitude);
      }

      public AstroCoordinate(double rightAcension, double declination, double latitude, double longitude, double elev) : this(latitude, longitude, elev)
      {

         _InitialisedWith = InitialisedWith.RADec;
         _Equatorial = new EquatorialCoordinate(rightAcension, declination, longitude);
      }

      ~AstroCoordinate()
      {
         // Do not re-create Dispose clean-up code here. 
         // Calling Dispose(false) is optimal in terms of 
         // readability and maintainability.
         Dispose(false);
      }

      #region Utility code ...

      private AltAzCoordinate GetAltAz()
      {
         _Transform.Refresh();
         return new AltAzCoordinate(_Transform.ElevationTopocentric, _Transform.AzimuthTopocentric);
      }

      private EquatorialCoordinate GetEquatorial()
      {
         _Transform.Refresh();
         return new EquatorialCoordinate(_Transform.RATopocentric, _Transform.DECTopocentric, _Transform.SiteLongitude);
      }

      #endregion

      #region IDisposable support ...
      // Track whether Dispose has been called. 
      private bool disposed = false;

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      // Dispose(bool disposing) executes in two distinct scenarios. 
      // If disposing equals true, the method has been called directly 
      // or indirectly by a user's code. Managed and unmanaged resources 
      // can be disposed. 
      // If disposing equals false, the method has been called by the 
      // runtime from inside the finalizer and you should not reference 
      // other objects. Only unmanaged resources can be disposed. 
      protected virtual void Dispose(bool disposing)
      {
         // Check to see if Dispose has already been called. 
         if (!this.disposed) {
            // If disposing equals true, dispose all managed 
            // and unmanaged resources. 
            if (disposing) {
               // Dispose managed resources.
               _Util.Dispose();
               _Transform.Dispose();
            }

            // Note disposing has been done.
            disposed = true;

         }
      }
      #endregion
   }
}
