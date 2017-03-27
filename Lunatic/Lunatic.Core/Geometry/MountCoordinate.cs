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
      private AltAzCoordinate _SuggestedAltAz;
      private AltAzCoordinate _ObservedAltAz;
      private AxisPosition _SuggestedAxes;
      private AxisPosition _ObservedAxes;
      private DateTime _ObservedWhen;
      private Angle _Latitude;
      private Angle _Longitude;

      public EquatorialCoordinate Equatorial
      {
         get
         {
            return _Equatorial;
         }
      }

      public AltAzCoordinate SuggestedAltAzimuth
      {
         get
         {
            return _SuggestedAltAz;
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

      public AxisPosition SuggestedAxes
      {
         get
         {
            return _SuggestedAxes;
         }
      }

      public AxisPosition ObservedAxes
      {
         get
         {
            return _ObservedAxes;
         }
         set
         {
            _ObservedAxes = value;
         }
      }

      public Angle SiteLongitude
      {
         get
         {
            return _Longitude;
         }
      }

      public Angle SiteLatitude
      {
         get
         {
            return _Latitude;
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
      /// Initialise the mount with and equatorial and suggested axis position.
      /// Used in Taki transformations.
      /// </summary>
      /// <param name="altAz">The AltAzimuth coordinate for the mount</param>
      /// <param name="suggested">The suggested position for the axes (e.g. via a star catalogue lookup)</param>
      /// <param name="localTime">The local time of the observation</param>
      public MountCoordinate(EquatorialCoordinate equatorial, AxisPosition suggested, Angle latitude)
      {
         _Equatorial = equatorial;
         _SuggestedAxes = suggested;
         _ObservedAxes = suggested;
         _ObservedWhen = _Equatorial.ObservedWhen;
         _Latitude = latitude;
         _Longitude = _Equatorial.SiteLongitude;
         _SuggestedAltAz = AstroConvert.GetAltAz(_Equatorial, latitude);
      }

      /// <summary>
      /// Initialise the mount coordinate using suggested and observed
      /// alt-az coordinates. (Primarily for unit testing Affine transformations)
      /// </summary>
      /// <param name="suggested">Theoretical axis positions</param>
      /// <param name="observed">Corresponding observed axis positions</param>
      public MountCoordinate(AltAzCoordinate suggestedAltAz, AltAzCoordinate observedAltAz)
      {
         _SuggestedAltAz = suggestedAltAz;
         _ObservedAltAz = observedAltAz;
      }
   }

}
