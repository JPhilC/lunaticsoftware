using ASCOM.Astrometry.Transform;
using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core.Geometry
{

   /// <summary>
   /// A class to tie together an equatorial coordinate, 
   /// calculated/theoretical mount axis positions at a given time
   /// and optionally the actual observered axis positions.
   /// </summary>
   public class MountCoordinate
   {
      public enum MasterCoordinate
      {
         Equatorial,
         AltAzimuth
      }

      private EquatorialCoordinate _Equatorial;
      private AltAzCoordinate _AltAzimuth;
      private AxisPosition _AxesPosition;
      private DateTime _AxisObservationTime;
      private double _LocalJulianTimeUTC;

      /// <summary>
      /// Held for reference so that when a refresh is requested we know which coordinate
      /// is the master.
      /// </summary>
      private MasterCoordinate _MasterCoordinate;

      public EquatorialCoordinate Equatorial
      {
         get
         {
            return _Equatorial;
         }
      }

      public AltAzCoordinate AltAzimuth
      {
         get
         {
            return _AltAzimuth;
         }
         set
         {
            _AltAzimuth = value;
         }
      }


      public AxisPosition ObservedAxes
      {
         get
         {
            return _AxesPosition;
         }
      }

      /// <summary>
      /// The time at which the axis values were determined.
      /// </summary>
      public DateTime AxisObservationTime
      {
         get
         {
            return _AxisObservationTime;
         }
      }

      /// <summary>
      /// Julian UTC time of last update/synch
      /// </summary>
      public double LocalJulianTimeUTC
      {
         get
         {
            return _LocalJulianTimeUTC;
         }
      }

      /// <summary>
      /// Initialise a mount coordinate with Ra/Dec strings 
      /// </summary>
      /// <param name="ra">A right ascension string</param>
      /// <param name="dec">declination string</param>
      /// <param name="localTime">The local time of the observation</param>
      public MountCoordinate(string ra, string dec):this(new EquatorialCoordinate(ra, dec))
      {
         _MasterCoordinate = MasterCoordinate.Equatorial;
      }

      /// <summary>
      /// Simple initialisation with an equatorial coordinate
      /// </summary>
      public MountCoordinate(EquatorialCoordinate equatorial) 
      {
         _Equatorial = equatorial;
         _MasterCoordinate = MasterCoordinate.Equatorial;
      }

      /// <summary>
      /// Simple initialisation with an altAzimuth coordinate
      /// </summary>
      public MountCoordinate(AltAzCoordinate altAz)
      {
         _AltAzimuth = altAz;
         _MasterCoordinate = MasterCoordinate.AltAzimuth;
      }

      /// <summary>
      /// Initialisation with an equatorial coordinate, a transform instance and the local julian time (corrected)
      /// which then means that the AltAzimunth at the time is available.
      /// </summary>
      public MountCoordinate(EquatorialCoordinate equatorial, Transform transform, double localJulianTimeUTC):this(equatorial)
      {
         _AltAzimuth = this.GetAltAzimuth(transform, localJulianTimeUTC);
         _LocalJulianTimeUTC = localJulianTimeUTC;
      }

      /// <summary>
      /// Initialisation with an equatorial coordinate, a transform instance and the local julian time (corrected)
      /// which then means that the AltAzimunth at the time is available.
      /// </summary>
      public MountCoordinate(EquatorialCoordinate equatorial, AxisPosition axisPosition, Transform transform, double localJulianTimeUTC) : this(equatorial)
      {
         _AltAzimuth = this.GetAltAzimuth(transform, localJulianTimeUTC);
         _LocalJulianTimeUTC = localJulianTimeUTC;
         _AxesPosition = axisPosition;

      }

      /// <summary>
      /// Initialisation with an equatorial coordinate, a transform instance and the local julian time (corrected)
      /// which then means that the AltAzimunth at the time is available.
      /// </summary>
      public MountCoordinate(AltAzCoordinate altAz, Transform transform, double localJulianTimeUTC) : this(altAz)
      {
         _LocalJulianTimeUTC = localJulianTimeUTC;
         _Equatorial = this.GetEquatorial(transform, localJulianTimeUTC);
      }

      public MountCoordinate(AltAzCoordinate altAz, AxisPosition axisPosition, Transform transform, double localJulianTimeUTC) : this(altAz)
      {
         _LocalJulianTimeUTC = localJulianTimeUTC;
         _Equatorial = this.GetEquatorial(transform, localJulianTimeUTC);
         _AxesPosition = axisPosition;
      }

      /// <summary>
      /// Initialise a mount coordinate with Ra/Dec strings and axis positions in radians.
      /// </summary>
      /// <param name="altAz">The AltAzimuth coordinate for the mount</param>
      /// <param name="suggested">The suggested position for the axes (e.g. via a star catalogue lookup)</param>
      /// <param name="localTime">The local time of the observation</param>
      public MountCoordinate(string ra, string dec, double axis1Radians, double axis2Radians, DateTime observationTime)
      {
         _Equatorial = new EquatorialCoordinate(ra, dec);
         _AxesPosition = new AxisPosition(axis1Radians, axis2Radians);
         _AxisObservationTime = observationTime;
         _MasterCoordinate = MasterCoordinate.Equatorial;
      }


      /// <summary>
      /// Returns the AltAzimuth coordinate for the equatorial using the values
      /// currently set in the passed Transform instance.
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public AltAzCoordinate GetAltAzimuth(Transform transform)
      {
         transform.SetTopocentric(_Equatorial.RightAscention, _Equatorial.Declination);
         transform.Refresh();
         AltAzCoordinate coord = new AltAzCoordinate(transform.ElevationTopocentric, transform.AzimuthTopocentric);
         return coord;
      }

      /// <summary>
      /// Returns the AltAzimuth coordinate for the equatorial using the values
      /// currently set in the passed Transform instance.
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public AltAzCoordinate GetAltAzimuth(Transform transform, double julianDateUTC)
      {
         transform.JulianDateUTC = julianDateUTC;
         transform.SetTopocentric(_Equatorial.RightAscention, _Equatorial.Declination);
         transform.Refresh();
         AltAzCoordinate coord = new AltAzCoordinate(transform.ElevationTopocentric, transform.AzimuthTopocentric);
         return coord;
      }

      /// <summary>
      /// Returns the RADec coordinate for the observed AltAzimuth using the values
      /// currently set in the passed Transform instance.
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public EquatorialCoordinate GetEquatorial(Transform transform, double julianDateUTC)
      {
         transform.JulianDateUTC = julianDateUTC;
         transform.SetAzimuthElevation(_AltAzimuth.Azimuth, _AltAzimuth.Altitude);
         transform.Refresh();
         EquatorialCoordinate coord = new EquatorialCoordinate(transform.RATopocentric, transform.DECTopocentric);
         return coord;
      }


      public void SetObservedAxis(AxisPosition axisPosition, DateTime observationTime)
      {
         _AxesPosition = axisPosition;
         _AxisObservationTime = observationTime;
      }

      public void Refresh(Transform transform, double julianDateUTC)
      {
         transform.JulianDateUTC = julianDateUTC;
         if (_MasterCoordinate == MasterCoordinate.Equatorial) {
            // Update the AltAzimuth
            transform.SetTopocentric(_Equatorial.RightAscention, _Equatorial.Declination);
            transform.Refresh();
            _AltAzimuth = new AltAzCoordinate(transform.ElevationTopocentric, transform.AzimuthTopocentric);
         }
         else {
            // Update the Equatorial
            transform.SetAzimuthElevation(_AltAzimuth.Azimuth, _AltAzimuth.Altitude);
            transform.Refresh();
            _Equatorial = new EquatorialCoordinate(transform.RATopocentric, transform.DECTopocentric);
         }
      }

      public void Refresh(EquatorialCoordinate equatorial, AxisPosition axisPosition, Transform transform, double localJulianTimeUTC)
      {
         _Equatorial = equatorial;
         _AxesPosition = axisPosition;
         _LocalJulianTimeUTC = localJulianTimeUTC;
         _AltAzimuth = this.GetAltAzimuth(transform, localJulianTimeUTC);
         _MasterCoordinate = MasterCoordinate.Equatorial;

      }

      public void Refresh(AltAzCoordinate altAz, AxisPosition axisPosition, Transform transform, double localJulianTimeUTC)
      {
         _AltAzimuth = altAz;
         _AxesPosition = axisPosition;
         _LocalJulianTimeUTC = localJulianTimeUTC;
         _Equatorial = this.GetEquatorial(transform, localJulianTimeUTC);
         _MasterCoordinate = MasterCoordinate.AltAzimuth;
      }

   }

}
