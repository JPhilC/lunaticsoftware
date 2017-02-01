using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   [TypeConverter(typeof(EnumTypeConverter))]
   public enum SideOfPierOption
   {
      [Description("Pointing (ASCOM)")]
      Pointing,
      [Description("Physical")]
      Physical,
      [Description("None (ASCOM)")]
      None,
      [Description("V1.24g")]
      V124g
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum EpochOption
   {
      [Description("EPOCH Unknown")]
      Unknown,
      [Description("JNOW")]
      JNow,
      [Description("J2000")]
      J2000,
      [Description("J2050")]
      J2050,
      [Description("B1950")]
      B1950
   }

   public class AscomCompliance
   {
         public bool SlewWithTrackingOff { get; set; }
         public bool AllowExceptions { get; set; }
         public bool AllowPulseGuidingExceptions { get; set; }
         public bool UseSynchronousParking { get; set; }
         public bool AllowSiteWrites { get; set; }
         public EpochOption Epoch { get; set; }
         public SideOfPierOption SideOfPier { get; set; }
         public bool SwapPointingSideOfPier { get; set; }
         public bool SwapPhysicalSideOfPier { get; set; }
         public bool Strict { get; set; }

   }
}
