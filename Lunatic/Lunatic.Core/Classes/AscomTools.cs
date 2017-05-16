using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.Astrometry.Transform;
using ASCOM.Utilities;

namespace Lunatic.Core
{
   public class AscomTools: IDisposable
   {
      //#region Singleton ...
      //private static AscomTools _Instance = null;
      //public static AscomTools Instance
      //{
      //   get
      //   {
      //      if (_Instance == null) {
      //         _Instance = new AscomTools();
      //      }
      //      return _Instance;
      //   }
      //}

      //public static void DisposeInstance()
      //{
      //   if (_Instance != null) {
      //      _Instance.Dispose();
      //      _Instance = null;
      //   }
      //}
      //#endregion

      private Transform _Transform;
      public Transform Transform
      {
         get
         {
            return _Transform;
         }
      }

      private Util _Util;
      public Util Util
      {
         get
         {
            return _Util;
         }
      }

      /// <summary>
      /// Returns the local julian time corrected for daylight saving and a minor adjustment
      /// to get a more accurate result when converting from RA/Dec to AltAz using the Transform instance.
      /// </summary>
      public double LocalJulianTimeUTC
      {
         get
         {
            double localTimeZoneOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalHours;   // Taken from Util.GetTimeZoneOffset()
            DateTime testTime = DateTime.Now.AddHours(localTimeZoneOffset).AddSeconds(Constants.UTIL_LOCAL2JULIAN_TIME_CORRECTION);     // Fix for daylight saving 0.2 seconds
            return this._Util.DateLocalToJulian(testTime);
         }
      }

      //private AscomTools()
      //{
      //   _Util = new Util();
      //   _Transform = new Transform();
      //}

      public AscomTools()
      {
         _Util = new Util();
         _Transform = new Transform();
      }

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (disposing) {
            if (_Util != null) {
               _Util.Dispose();
            }
            if (_Transform != null) {
               _Transform.Dispose();
            }
         }
      }
   }
}
