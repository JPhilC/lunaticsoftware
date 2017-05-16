using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ASCOM.Lunatic
{
   [ComVisible(true)]
   public class ReferenceCountedObjectBase : IDisposable
   {
      protected object _Lock = new object();


      public ReferenceCountedObjectBase()
      {
         // NOTE: After this, you can use your typeconverter.
         AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
         // We increment the global count of objects.
         TelescopeServer.CountObject();
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

      #region IDisposable Support
      private bool disposed = false; // To detect redundant calls

      protected virtual void Dispose(bool disposing)
      {
         System.Diagnostics.Trace.WriteLine(string.Format("ASCOM.Lunatic.ReferenceCountedObjectBase.Dispose({0}) is called.", disposing));
         if (!disposed) {
            // We decrement the global count of objects.
            TelescopeServer.UncountObject();
            // We then immediately test to see if we the conditions
            // are right to attempt to terminate this server application.
            TelescopeServer.ExitIf();

            disposed = true;
         }
      }


      // This code added to correctly implement the disposable pattern.
      public void Dispose()
      {
         // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
         Dispose(true);
         // TODO: uncomment the following line if the finalizer is overridden above.
         GC.SuppressFinalize(this);
      }
      #endregion

   }
}
