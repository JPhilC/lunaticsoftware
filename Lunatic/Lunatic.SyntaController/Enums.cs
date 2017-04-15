using Lunatic.Core;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Lunatic.SyntaController
{


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

   //POLARHOME_RETICULE_START=1
   //POLARHOME_GOTO_DEC=9469288
   //POLARHOME_GOTO_RA=8026995
   //POLAR_HOME_DISABLE=0
   //POLAR_RETICULE_D2=0.355
   //POLAR_RETICULE_D1=2.67
   //POLAR_RETICULE_EPOCH=2000
   //POLAR_RETICULE_TYPE=1
   //POLAR_RETICULE_START=1


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

   public enum MountType
   {
      Unknown,
      EqMount
   }

   public enum MountMode
   {
      Slew,
      Goto
   }

   public enum MountSpeed
   {
      LowSpeed,
      HighSpeed
   }

   public enum MountTracking
   {
      Sidereal,
      Solar,
      Lunar,
      Custom
   }

   public enum TrackMode
   {
      Update,
      Initial
   }

   public enum AutoguiderPortRate
   {

      [Description("1.00x")]
      OneTimesX,
      [Description("0.75x")]
      Point75Times,
      [Description("0.50x")]
      Point50Times,
      [Description("0.25x")]
      Point25Times,
      [Description("0.125x")]
      Point125Times
   }
}
