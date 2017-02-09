using Lunatic.Core;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace ASCOM.Lunatic
{
   [Flags]
   public enum AxisState
   {
      Stopped = 0x0001,             // The axis is in a fully stopped state
      Slewing = 0x0002,             // The axis is in constant speed operation
      Slewing_To = 0x0004,          // The axis is in the process of running to the specified target position
      Slewing_Forward = 0x0008,     // The axis runs forward
      Slewing_Highspeed = 0x0010,   // The axis is in high-speed operation
      Not_Initialised = 0x0020      // MC controller has not been initialized, axis is not initialized.
   }

   // Two-axis telescope code
   public enum AxisId { Axis1 = 0, Axis2 = 1 }; // ID unsed in ASTRO.DLL for axis 1 and axis 2 of a mount.

   public enum ErrorCode
   {
      ERR_INVALID_ID = 1,            // 無效的望遠鏡代碼					// Invalid mount ID
      ERR_ALREADY_CONNECTED = 2,     // 已經連接到另外一個ID的望遠鏡		// Already connected to another mount ID
      ERR_NOT_CONNECTED = 3,         // 尚未連接到望遠鏡					// Telescope not connected.
      ERR_INVALID_DATA = 4,          // 無效或超範圍的資料				// Invalid data, over range etc
      ERR_SERIAL_PORT_BUSY = 5,      // 串口忙				            // Serial port is busy.
      ERR_NORESPONSE_AXIS1 = 100,       // 望遠鏡的主軸沒有回應				// No response from axis1
      ERR_NORESPONSE_AXIS2 = 101,     // 望遠鏡的次軸沒有回應          // The secondary axis of the telescope did not respond
      ERR_AXIS_BUSY = 102,           // 暫時無法執行該操作
      ERR_MAX_PITCH = 103,           // 目標位置仰角過高
      ERR_MIN_PITCH = 104,           // 目標位置仰角過低
      ERR_USER_INTERRUPT = 105,          // 用戶強制終止
      ERR_ALIGN_FAILED = 200,        // 校準望遠鏡失敗
      ERR_UNIMPLEMENT = 300,         // 未實現的方法
      ERR_WRONG_ALIGNMENT_DATA = 400,  // The alignment data is incorect.
   };

   public enum MountId
   {
      // Telescope ID, they must be started from 0 and coded continuously.
      ID_CELESTRON_AZ = 0,          // Celestron Alt/Az Mount
      ID_CELESTRON_EQ = 1,          // Celestron EQ Mount
      ID_SKYWATCHER_AZ = 2,         // Skywatcher Alt/Az Mount
      ID_SKYWATCHER_EQ = 3,         // Skywatcher EQ Mount
      ID_ORION_EQG = 4,             // Orion EQ Mount
      ID_ORION_TELETRACK = 5,       // Orion TeleTrack Mount
      ID_EQ_EMULATOR = 6,           // EQ Mount Emulator
      ID_AZ_EMULATOR = 7,           // Alt/Az Mount Emulator
      ID_NEXSTARGT80 = 8,           // NexStarGT-80 mount
      ID_NEXSTARGT114 = 9,          // NexStarGT-114 mount
      ID_STARSEEKER80 = 10,         // NexStarGT-80 mount
      ID_STARSEEKER114 = 11,        // NexStarGT-114 mount
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum MountOptions
   {
      [Description("Auto detect")]
      AutoDetect,
      [Description("Custom")]
      Custom
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
   public enum PulseGuidingOption
   {
      [Description("ASCOM Pulse Guiding")]
      ASCOM,
      [Description("ST-4 Pulse Guiding")]
      ST4
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
   public enum HemisphereOption
   {
      [Description("North")]
      Northern,
      [Description("South")]
      Southern

   }
}
