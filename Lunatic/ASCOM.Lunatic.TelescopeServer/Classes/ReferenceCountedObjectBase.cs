using GalaSoft.MvvmLight;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ASCOM.Lunatic
{
   [ComVisible(false)]
   public class ReferenceCountedObjectBase: ObservableObject, IDisposable
   {
      protected object _Lock = new object();


      public ReferenceCountedObjectBase()
      {
         // NOTE: After this, you can use your typeconverter.
         AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

         // We increment the global count of objects.
         TelescopeServer.CountObject();
      }

      //~ReferenceCountedObjectBase()
      //{
      //   System.Diagnostics.Trace.WriteLine("ASCOM.Lunatic.ReferenceCountedObjectBase destructor is called.");
      //   // We decrement the global count of objects.
      //   TelescopeServer.UncountObject();
      //   // We then immediately test to see if we the conditions
      //   // are right to attempt to terminate this server application.
      //   TelescopeServer.ExitIf();
      //}

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
      private bool disposedValue = false; // To detect redundant calls

      protected virtual void Dispose(bool disposing)
      {
         System.Diagnostics.Trace.WriteLine(string.Format("ASCOM.Lunatic.ReferenceCountedObjectBase.Dispose({0}) is called.", disposing));
         if (!disposedValue) {
            if (disposing) {
               // We decrement the global count of objects.
               TelescopeServer.UncountObject();
               // We then immediately test to see if we the conditions
               // are right to attempt to terminate this server application.
               TelescopeServer.ExitIf();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
         }
      }

      // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
      // ~ReferenceCountedObjectBase() {
      //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      //   Dispose(false);
      // }

      // This code added to correctly implement the disposable pattern.
      public void Dispose()
      {
         // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
         Dispose(true);
         // TODO: uncomment the following line if the finalizer is overridden above.
         // GC.SuppressFinalize(this);
      }
      #endregion

   }
}
