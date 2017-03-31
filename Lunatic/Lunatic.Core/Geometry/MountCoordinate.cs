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
      private EquatorialCoordinate _Equatorial;
      private AltAzCoordinate _ObservedAltAz;
      private AxisPosition _SuggestedAxes;
      private AxisPosition _ObservedAxes;
      private DateTime _ObservationTime;
      private DateTime _AxisObservationTime;
      //private Angle _SiteLatitude;
      //private Angle _SiteLongitude;
      //private double _SiteElevation;

      public EquatorialCoordinate Equatorial
      {
         get
         {
            return _Equatorial;
         }
      }

      public AltAzCoordinate ObservedAltAzimuth
      {
         get
         {
            return _ObservedAltAz;
         }
         set
         {
            _ObservedAltAz = value;
         }
      }

      //public AxisPosition SuggestedAxes
      //{
      //   get
      //   {
      //      return _SuggestedAxes;
      //   }
      //}

      public AxisPosition ObservedAxes
      {
         get
         {
            return _ObservedAxes;
         }
         //set
         //{
         //   _ObservedAxes = value;
         //}
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
         //set
         //{
         //   _ObservedAxes = value;
         //}
      }

      //public Angle SiteLongitude
      //{
      //   get
      //   {
      //      return _SiteLongitude;
      //   }
      //}

      //public Angle SiteLatitude
      //{
      //   get
      //   {
      //      return _SiteLatitude;
      //   }
      //}

      public DateTime ObservationTime
      {
         get
         {
            return _ObservationTime;
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
      }

      /// <summary>
      /// Simple initialisation with an equatorial coordinate
      /// </summary>
      /// <param name="altAz">The AltAzimuth coordinate for the mount</param>
      /// <param name="suggested">The suggested position for the axes (e.g. via a star catalogue lookup)</param>
      /// <param name="localTime">The local time of the observation</param>
      public MountCoordinate(EquatorialCoordinate equatorial) 
      {
         _Equatorial = equatorial;
         //_ObservationTime = observationTime;
         //_SiteLatitude = latitude;
         //_SiteLongitude = longitude;
         //_SiteElevation = siteElevation;
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
         _ObservedAxes = new AxisPosition(axis1Radians, axis2Radians);
         _AxisObservationTime = observationTime;
      }

      /// <summary>
      /// Initialise the mount with and equatorial and suggested axis position.
      /// Used in Taki transformations.
      /// </summary>
      /// <param name="altAz">The AltAzimuth coordinate for the mount</param>
      /// <param name="suggested">The suggested position for the axes (e.g. via a star catalogue lookup)</param>
      /// <param name="localTime">The local time of the observation</param>
      public MountCoordinate(EquatorialCoordinate equatorial, AxisPosition observedAxis, DateTime axisObservationTime)  // , Angle latitude, Angle longitude, double siteElevation, DateTime observationTime)
      {
         _Equatorial = equatorial;
         _ObservedAxes = observedAxis;
         _AxisObservationTime = axisObservationTime;
         //_ObservationTime = observationTime;
         //_SiteLatitude = latitude;
         //_SiteLongitude = longitude;
         //_SiteElevation = siteElevation;
      }

      ///// <summary>
      ///// Initialise the mount coordinate using suggested and observed
      ///// alt-az coordinates. (Primarily for unit testing Affine transformations)
      ///// </summary>
      ///// <param name="suggested">Theoretical axis positions</param>
      ///// <param name="observed">Corresponding observed axis positions</param>
      //public MountCoordinate(AltAzCoordinate observedAltAz)
      //{
      //   // _SuggestedAltAz = suggestedAltAz;
      //   _ObservedAltAz = observedAltAz;
      //}


      /// <summary>
      /// Returns the AltAzimuth coordinate for the equatorial using the values
      /// currently set in the passed Transform instance.
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public AltAzCoordinate GetAltAzimuth(Transform transform)
      {
         transform.SetTopocentric(_Equatorial.RightAcension, _Equatorial.Declination);
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
      public EquatorialCoordinate GetRADec(Transform transform)
      {
         transform.SetAzimuthElevation(_ObservedAltAz.Azimuth, _ObservedAltAz.Altitude);
         transform.Refresh();
         EquatorialCoordinate coord = new EquatorialCoordinate(transform.RATopocentric, transform.DECTopocentric);
         return coord;
      }


      public void SetObservedAxis(AxisPosition axisPosition, DateTime observationTime)
      {
         _ObservedAxes = axisPosition;
         _AxisObservationTime = observationTime;
      }
   }

}
