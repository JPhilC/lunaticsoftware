using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   public class AscomCompliance
   {
         public bool SlewWithTrackingOff { get; set; }
         public bool AllowPulseGuide { get; set; }
         public bool AllowExceptions { get; set; }
         public bool AllowPulseGuideExceptions { get; set; }
         public bool BlockPark { get; set; }
         public bool AllowSiteWrites { get; set; }
         public int Epoch { get; set; }
         public int SideOfPier { get; set; }
         public bool SwapPointingSideOfPier { get; set; }
         public bool SwapPhysicalSideOfPier { get; set; }
         public bool Strict { get; set; }

   }
}
