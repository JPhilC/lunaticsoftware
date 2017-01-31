using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   public abstract class DriverBase : ObservableObject
   {

      protected object _Lock = new object();

      public DriverBase()
      {
         // NOTE: After this, you can use your typeconverter.
         AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

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
