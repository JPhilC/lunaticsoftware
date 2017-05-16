using Lunatic.Core.Services;
using System;
using System.Runtime.InteropServices;
using System.Windows.Markup;

namespace ASCOM.Lunatic.Telescope
{
   [ComVisible(false)]
   public class COMPortBindingSourceExtension : MarkupExtension
   {

      public COMPortBindingSourceExtension() { }


      public override object ProvideValue(IServiceProvider serviceProvider)
      {
         Array comPorts = COMPortService.GetCOMPortsInfo().ToArray();
         return comPorts;
      }
   }
}
