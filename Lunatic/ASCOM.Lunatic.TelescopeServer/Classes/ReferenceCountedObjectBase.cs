using GalaSoft.MvvmLight;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ASCOM.Lunatic
{
   [ComVisible(false)]
   public class ReferenceCountedObjectBase: ObservableObject
   {
      protected object _Lock = new object();


      public ReferenceCountedObjectBase()
      {
         // NOTE: After this, you can use your typeconverter.
         AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

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

      // Needed to allow the current assembly to resolve some assemblies correctly
      private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
      {
         AppDomain domain = (AppDomain)sender;
         foreach (Assembly asm in domain.GetAssemblies()) {
            if (asm.FullName == args.Name) {
               return asm;
            }
         }
         return null;
      }

   }
}
