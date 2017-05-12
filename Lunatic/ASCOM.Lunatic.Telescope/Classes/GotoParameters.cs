using Lunatic.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Lunatic.Telescope
{
   public class GotoParameters
   {
      public double RACurrentEncoder { get; set; }
      public AxisDirection RADirection { get; set; }
      public double RATargetEncoder { get; set; }
      public bool RASlewActive { get; set; }


      public double DecCurrentEncoder { get; set; }
      public AxisDirection DecDirection { get; set; }
      public double DecTargetEncoder { get; set; }
      public bool DecSlewActive { get; set; }

      public int Rate { get; set; }
      public bool SuperSafeMode { get; set; }

      public GotoParameters()
      {
      }
   }
}
