using Lunatic.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.TelescopeControl
{
   public class TelescopeControlSettings: DataObjectBase
   {


      public string DriverId { get; set; }

      public DisplayMode DisplayMode { get; set; }

      public TelescopeControlSettings()
      {
         this.DriverId = string.Empty;
         this.DisplayMode = DisplayMode.MountPosition;
      }
   }
}
