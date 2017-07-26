using Lunatic.Core;
using Lunatic.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Lunatic.Telescope
{
   public class GotoParameters
   {
      public EquatorialCoordinate TargetCoordinate { get; set; }
      public AxisPosition CurrentAxisPosition { get; set; }
      public AxisPosition TargetAxisPosition { get; set; }
      public AxisDirection RADirection { get; set; }
      public bool RASlewActive { get; set; }


      public AxisDirection DecDirection { get; set; }
      public bool DecSlewActive { get; set; }

      public int Rate { get; set; }
      public int SuperSafeMode { get; set; }

      public int FRSlewCount { get; set; }

      public int SlewCount { get; set; }

      public bool SupressHorizonLimits { get; set; }

      public GotoParameters()
      {
      }
   }
}
