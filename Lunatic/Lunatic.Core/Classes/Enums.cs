using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   [TypeConverter(typeof(EnumTypeConverter))]
   public enum BaudRate
   {
      [Description("4800")]
      Baud4800 = 4800,
      [Description("9600")]
      Baud9600 = 9600,
      [Description("11520")]
      Baud115200 = 115200,
      [Description("128000")]
      Baud128000 = 128000,
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum TimeOutOption
   {
      [Description("1000")]
      TO1000 = 1000,
      [Description("2000")]
      TO2000 = 2000
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum RetryOption
   {
      [Description("Once")]
      Once = 1,
      [Description("Twice")]
      Twice = 2
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum PulseGuidingOption
   {
      [Description("ASCOM Pulse Guiding")]
      ASCOM,
      [Description("ST-4 Pulse Guiding")]
      ST4
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum ParkStatus
   {
      [Description("Unparked")]
      Unparked,
      [Description("Parked")]
      Parked,
      [Description("Parking")]
      Parking,
      [Description("Unparking")]
      Unparking
   }


}
