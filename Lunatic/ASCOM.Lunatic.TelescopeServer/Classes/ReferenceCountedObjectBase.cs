using GalaSoft.MvvmLight;
using System;
using System.Runtime.InteropServices;

namespace ASCOM.Lunatic
{
   [ComVisible(false)]
   public class ReferenceCountedObjectBase: ObservableObject
   {
      public ReferenceCountedObjectBase()
      {
         // We increment the global count of objects.
         TelescopeServer.CountObject();
      }

      ~ReferenceCountedObjectBase()
      {
         // We decrement the global count of objects.
         TelescopeServer.UncountObject();
         // We then immediately test to see if we the conditions
         // are right to attempt to terminate this server application.
         TelescopeServer.ExitIf();
      }
   }
}
