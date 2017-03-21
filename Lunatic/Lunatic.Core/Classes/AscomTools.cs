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
      #region Singleton ...
      private static AscomTools _Instance = null;
      public static AscomTools Instance
      {
         get
         {
            if (_Instance == null) {
               _Instance = new AscomTools();
            }
            return _Instance;
         }
      }

      public static void DisposeInstance()
      {
         if (_Instance != null) {
            _Instance.Dispose();
            _Instance = null;
         }
      }
      #endregion

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

      private AscomTools()
      {
         _Util = new Util();
         _Transform = new Transform();
      }

      public void Dispose()
      {
         _Util.Dispose();
         _Transform.Dispose();
      }
   }
}
