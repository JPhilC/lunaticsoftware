using Lunatic.Core.Services;
using System;
using System.Windows.Markup;

namespace ASCOM.Lunatic.Telescope
{
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
