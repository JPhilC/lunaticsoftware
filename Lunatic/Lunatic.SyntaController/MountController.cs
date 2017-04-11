using Lunatic.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TA.Ascom.ReactiveCommunications;
using TA.Ascom.ReactiveCommunications.Transactions;
using Lunatic.SyntaController.Transactions;
using ASCOM.DeviceInterface;

namespace Lunatic.SyntaController
{
   public sealed class MountController
   {
      #region Singleton code ...
      private static MountController _Instance = null;

      public static MountController Instance
      {
         get
         {
            if (_Instance == null) {
               _Instance = new MountController();
            }
            return _Instance;
         }
      }

      #endregion


      #region Settings related stuff ...
      private ISettingsProvider<ControllerSettings> _SettingsManager = null;

      public ISettingsProvider<ControllerSettings> SettingsManager
      {
         get
         {
            if (_SettingsManager == null) {
               _SettingsManager = new SettingsProvider();
            }
            return _SettingsManager;
         }
      }

      private ControllerSettings Settings
      {
         get
         {
            return SettingsManager.Settings;
         }
      }
      #endregion

      #region Properties ...
      // special charactor for communication.
      const char cStartChar_Out = ':';    // Leading charactor of a command 
      const char cStartChar_In = '=';        // Leading charactor of a NORMAL response.
      const char cErrChar = '!';              // Leading charactor of an ABNORMAL response.
      const char cEndChar = (char)13;         // Tailing charactor of command and response.
      const double MAX_SPEED = (800 * Constants.SIDEREAL_RATE_RADIANS);           //?
      const double LOW_SPEED_MARGIN = (128.0 * Constants.SIDEREAL_RATE_RADIANS);

      private char dir = '0'; // direction
                              // Mount code: 0x00=EQ6, 0x01=HEQ5, 0x02=EQ5, 0x03=EQ3
                              //             0x80=GT,  0x81=MF,   0x82=114GT
                              //             0x90=DOB
      private long MountCode;
      private long[] StepTimerFreq = new long[2];        // Frequency of stepping timer (read from mount)
      private long[] PESteps = new long[2];              // PEC Period (read from mount)
      private long[] HighSpeedRatio = new long[2];       // High Speed Ratio (read from mount)
      //private long[] StepPosition = new long[2];       // Never Used
      private long[] BreakSteps = new long[2];           // Break steps from slewing to stop. (currently hard coded)
      private long[] LowSpeedGotoMargin = new long[2];   // If slewing steps exceeds this LowSpeedGotoMargin, 
                                                         // GOTO is in high speed slewing.

      private bool IsDCMotor;                // Ture: The motor controller is a DC motor controller. It uses TX/RX line is bus topology.
                                             // False: The motor controller is a stepper motor controller. TX/RX lines are seperated.
      private bool InstantStop;              // Use InstantStop command for MCAxisStop

      private object lockObject = new object();

      private long MCVersion = 0;   // Motor controller version number


      /// <summary>
      /// Connection string that is currently being used'
      /// </summary>
      private string ConnectionString;

      /// <summary>
      /// End point for connection to mount.
      /// </summary>
      private DeviceEndpoint EndPoint;

      /// <summary>
      /// Timeout in seconds.
      /// </summary>
      private double TimeOut = 2;

      private int Retry = 1;


      public bool IsConnected
      {
         get
         {
            return (EndPoint != null);
         }
      }

      /// <summary>
      /// The number of open connections.
      /// </summary>
      private int openConnections;

      #endregion
      /// <summary>
      /// Private constructor so that instances cannot be created outside this definition.
      /// </summary>
      private MountController()
      {
         ConnectionString = string.Empty;
         EndPoint = null;

         MCVersion = 0;
         Positions[0] = 0;
         Positions[1] = 0;
         TargetPositions[0] = 0;
         TargetPositions[1] = 0;
         SlewingSpeed[0] = 0;
         SlewingSpeed[1] = 0;
         AxesStatus[0] = new AxisStatus { FullStop = false, NotInitialized = true, HighSpeed = false, Slewing = false, SlewingForward = false, SlewingTo = false };
         AxesStatus[1] = new AxisStatus { FullStop = false, NotInitialized = true, HighSpeed = false, Slewing = false, SlewingForward = false, SlewingTo = false };
      }

      #region Old EQ_Contrl.dll methods ....
      //---------------------------------------------------------------------
      // Copyright © 2006 Raymund Sarmiento
      //
      // Permission is hereby granted to use this Software for any purpose
      // including combining with commercial products, creating derivative
      // works, and redistribution of source or binary code, without
      // limitation or consideration. Any redistributed copies of this
      // Software must include the above Copyright Notice.
      //
      // THIS SOFTWARE IS PROVIDED "AS IS". THE AUTHOR OF THIS CODE MAKES NO
      // WARRANTIES REGARDING THIS SOFTWARE, EXPRESS OR IMPLIED, AS TO ITS
      // SUITABILITY OR FITNESS FOR A PARTICULAR PURPOSE.
      //---------------------------------------------------------------------
      //
      //
      // Written:  07-Oct-06   Raymund Sarmiento
      //
      // Edits:
      //
      // When      Who     What
      // --------- ---     --------------------------------------------------
      // 24-Oct-03 rcs     Initial edit for EQ Mount Driver Function Prototype
      // 29-Jan-07 rcs     Added functions for ALT/AZ tracking
      //---------------------------------------------------------------------
      //
      //
      //  SYNOPSIS:
      //
      //  This is a demonstration of a EQ6/ATLAS/EQG direct stepper motor control access
      //  using the EQCONTRL.DLL driver code.
      //
      //  File EQCONTROL.bas contains all the function prototypes of all subroutines
      //  encoded in the EQCONTRL.dll
      //
      //  The EQ6CONTRL.DLL simplifies execution of the Mount controller board stepper
      //  commands.
      //
      //  The mount circuitry needs to be modified for this test program to work.
      //  Circuit details can be found at http://www.freewebs.com/eq6mod/
      //

      //  DISCLAIMER:

      //  You can use the information on this site COMPLETELY AT YOUR OWN RISK.
      //  The modification steps and other information on this site is provided
      //  to you "AS IS" and WITHOUT WARRANTY OF ANY KIND, express, statutory,
      //  implied or otherwise, including without limitation any warranty of
      //  merchantability or fitness for any particular or intended purpose.
      //  In no event the author will  be liable for any direct, indirect,
      //  punitive, special, incidental or consequential damages or loss of any
      //  kind whether or not the author  has been advised of the possibility
      //  of such loss.

      //  WARNING:

      //  Circuit modifications implemented on your setup could invalidate
      //  any warranty that you may have with your product. Use this
      //  information at your own risk. The modifications involve direct
      //  access to the stepper motor controls of your mount. Any "mis-control"
      //  or "mis-command"  / "invalid parameter" or "garbage" data sent to the
      //  mount could accidentally activate the stepper motors and allow it to
      //  rotate "freely" damaging any equipment connected to your mount.
      //  It is also possible that any garbage or invalid data sent to the mount
      //  could cause its firmware to generate mis-steps pulse sequences to the
      //  motors causing it to overheat. Make sure that you perform the
      //  modifications and testing while there is no physical "load" or
      //  dangling wires on your mount. Be sure to disconnect the power once
      //  this event happens or if you notice any unusual sound coming from
      //  the motor assembly.
      //
      //  CREDITS:
      //
      //  Portions of the information on this code should be attributed
      //  to Mr. John Archbold from his initial observations and analysis
      //  of the interface circuits and of the ASCII data stream between
      //  the Hand Controller (HC) and the Go To Controller.
      //



      /////// Conection-Initalization Functions /////

      //
      // Function name    : Connection (Was EQ_Init() in EQMOD)
      // Description      : Connect to the Controller via Serial and initialize the stepper board
      // Return type      : DOUBLE
      //                      000 - Success (was NOT already initialised).
      //                      001 - Success (WAS already initialised).
      //                      002 - COM Port already Open
      //                      003 - COM Timeout Error
      //                      005 - Mount Initialized on using non-standard parameters
      //                      010 - Cannot execute command at the current stepper controller state
      //                      999 - Invalid parameter
      // Argument         : STRING COMPORT Name
      // Argument         : DOUBLE baud - Baud Rate
      // Argument         : DOUBLE timeout - COMPORT Timeout (1 - 50000) seconds
      // Argument         : DOUBLE retry - COMPORT Retry (0 - 100)
      //
      // Public Declare Function EQ_Init Lib "EQCONTRL" (ByVal COMPORT As String, ByVal baud As Long, ByVal timeout As Long, ByVal retry As Long) As Long
      public int Connect(string ComPort, int baud, int timeout, int retry)
      {
         lock (lockObject) {
            int result = 0;
            if (EndPoint == null) {
               #region Capture connection parameters
               // ConnectionString = string.Format("{0}:{1},None,8,One,DTR,RTS", ComPort, baud);
               ConnectionString = string.Format("{0}:{1},None,8,One,NoDTR,NoRTS", ComPort, baud);
               EndPoint = DeviceEndpoint.FromConnectionString(ConnectionString);
               TimeOut = timeout * 0.001;  // Convert from milliseconds to seconds.
               Retry = retry;
               // Initialise the settings.
               MCInit();

            }
            else {
               result = 1;
            }
            #endregion

            Interlocked.Increment(ref openConnections);

            return result;
         }
      }


      //
      // Function name    : EQ_End()
      // Description      : Close the COM Port and end EQ Connection
      // Return type      : DOUBLE
      //          00 - Success
      //          01 - COM Port Not Openavailable
      //
      // Public Declare Function EQ_End Lib "EQContrl.dll" () As Long
      public int Disconnect()
      {
         lock (lockObject) {
            int result = 0;
            Interlocked.Decrement(ref openConnections);
            if (openConnections <= 0) {
               EndPoint = null;
               ConnectionString = string.Empty;
            }
            return result;
         }
      }

      //
      // Function name    : EQ_InitMotors()
      // Description      : Initialize RA/DEC Motors and activate Motor Driver Coils
      // Return type      : DOUBLE
      //                     000 - Success
      //                     001 - COM PORT Not available
      //                     003 - COM Timeout Error
      //                     006 - RA Motor still running
      //                     007 - DEC Motor still running
      //                     008 - Error Initializing RA Motor
      //                     009 - Error Initilizing DEC Motor
      //                     010 - Cannot execute command at the current stepper controller state
      // Argument         : DOUBLE RA_val       Initial ra microstep counter value
      // Argument         : DOUBLE DEC_val     Initial dec microstep counter value
      //
      // Public Declare Function EQ_InitMotors Lib "EQCONTRL" (ByVal RA As Long, ByVal DEC As Long) As Long
      public int EQ_InitMotors(int RA, int DEC)
      {
         throw new NotImplementedException();
      }

      /////// Motor Status Functions /////


      //
      // Function name    : EQ_GetMotorValues()
      // Description      : Get RA/DEC Motor microstep counts
      // Return type      : Double - Stepper Counter Values
      //                     0 - 16777215  Valid Count Values
      //                     0x1000000 - Mount Not available
      //                     0x1000005 - COM TIMEOUT
      //                     0x10000FF - Illegal Mount reply
      //                     0x3000000 - Invalid Parameter
      // Argument         : DOUBLE motor_id
      //                     00 - RA Motor
      //                     01 - DEC Motor
      //
      // Public Declare Function EQ_GetMotorValues Lib "EQCONTRL" (ByVal motor_id As Long) As Long
      public int EQ_MotorValues(int motorId)
      {
         throw new NotImplementedException();
      }

      //
      // Function name    : EQ_GetMotorStatus()
      // Description      : Get RA/DEC Stepper Motor Status
      // Return type      : DOUBLE
      //                     128 - Motor not rotating, Teeth at front contact
      //                     144 - Motor rotating, Teeth at front contact
      //                     160 - Motor not rotating, Teeth at rear contact
      //                     176 - Motor rotating, Teeth at rear contact
      //                     200 - Motor not initialized
      //                     001 - COM Port Not available
      //                     003 - COM Timeout Error
      //                     999 - Invalid Parameter
      // Argument         : DOUBLE motor_id
      //                     00 - RA Motor
      //                     01 - DEC Motor
      //
      // Public Declare Function EQ_GetMotorStatus Lib "EQCONTRL" (ByVal motor_id As Long) As Long
      public int EQ_GetMotorStatus(int motorId)
      {
         throw new NotImplementedException();
      }



      //
      // Function name    : EQ_SeTMotorValues()
      // Description      : Sets RA/DEC Motor microstep counters (pseudo encoder position)
      // Return type      : DOUBLE - Stepper Counter Values
      //                     000 - Success
      //                     001 - Comport Not available
      //                     003 - COM Timeout Error
      //                     010 - Cannot execute command at the current stepper controller state
      //                     011 - Motor not initialized
      //                     999 - Invalid Parameter
      // Argument         : DOUBLE motor_id
      //                     00 - RA Motor
      //                     01 - DEC Motor
      // Argument         : DOUBLE motor_val
      //                     0 - 16777215  Valid Count Values
      //
      // Public Declare Function EQ_SetMotorValues Lib "EQCONTRL" (ByVal motor_id As Long, ByVal motor_val As Long) As Long
      public int EQ_SetMotorValues(int motorId, int motorValue)
      {
         throw new NotImplementedException();
      }



      /////// Motor Movement Functions /////

      //
      // Function name    : EQ_StartMoveMotor
      // Description      : Slew RA/DEC Motor based on provided microstep counts
      // Return type      : DOUBLE
      //                     000 - Success
      //                     001 - COM PORT Not available
      //                     003 - COM Timeout Error
      //                     004 - Motor still busy, aborted
      //                     010 - Cannot execute command at the current stepper controller state
      //                     011 - Motor not initialized
      //                     999 - Invalid Parameter
      // Argument         : DOUBLE motor_id
      //                     00 - RA Motor
      //                     01 - DEC Motor
      // Argument         : DOUBLE hemisphere
      //                     00 - North
      //                     01 - South
      // Argument         : DOUBLE direction
      //                     00 - Forward(+)
      //                     01 - Reverse(-)
      // Argument         : DOUBLE steps count
      // Argument         : DOUBLE motor de-acceleration  point (set between 50% t0 90% of total steps)
      //
      /// Public Declare Function EQ_StartMoveMotor Lib "EQCONTRL" (ByVal motor_id As Long, ByVal hemisphere As Long, ByVal direction As Long, ByVal Steps As Long, ByVal stepslowdown As Long) As Long
      public int EQ_StartMoveMotor(int motorId, int hemisphere, int direction, int steps, int stepSlowDown)
      {
         throw new NotImplementedException();
      }


      //
      // Function name    : EQ_Slew()
      // Description      : Slew RA/DEC Motor based on given rate
      // Return type      : DOUBLE
      //                     000 - Success
      //                     001 - Comport Not available
      //                     003 - COM Timeout Error
      //                     004 - Motor still busy
      //                     010 - Cannot execute command at the current stepper controller state
      //                     011 - Motor not initialized
      //                     999 - Invalid Parameter
      // Argument         : DOUBLE motor_id
      //                     00 - RA Motor
      //                     01 - DEC Motor
      // Argument         : INTEGER direction
      //                    00 - Forward(+)
      //                    01 - Reverse(-)
      // Argument         : INTEGER rate
      //                         1-800 of Sidreal Rate
      //
      // Public Declare Function EQ_Slew Lib "EQCONTRL" (ByVal motor_id As Long, ByVal hemisphere As Long, ByVal direction As Long, ByVal rate As Long) As Long
      public int EQ_Slew(int motorId, int hemisphere, int direction, int rate)
      {
         throw new NotImplementedException();
      }


      //
      // Function name    : EQ_StartRATrack()
      // Description      : Track or rotate RA/DEC Stepper Motors at the specified rate
      // Return type      : DOUBLE
      //                     000 - Success
      //                     001 - Comport Not available
      //                     003 - COM Timeout Error
      //                     010 - Cannot execute command at the current stepper controller state
      //                     011 - Motor not initialized
      //                     999 - Invalid Parameter
      // Argument         : DOUBLE trackrate
      //                     00 - Sidreal
      //                     01 - Lunar
      //                     02 - Solar
      // Argument         : DOUBLE hemisphere
      //                     00 - North
      //                     01 - South
      // Argument         : DOUBLE direction
      //                     00 - Forward(+)
      //                     01 - Reverse(-)
      //
      // Public Declare Function EQ_StartRATrack Lib "EQCONTRL" (ByVal trackrate As Long, ByVal hemisphere As Long, ByVal direction As Long) As Long
      public int EQ_StartRATrack(int trackRate, int hemisphere, int direction)
      {
         throw new NotImplementedException();
      }

      //
      // Function name    : EQ_SendGuideRate()
      // Description      : Adjust the RA/DEC rotation trackrate based on a given speed adjustment rate
      // Return type      : int
      //                     000 - Success
      //                     001 - Comport Not available
      //                     003 - COM Timeout Error
      //                     004 - Motor still busy
      //                     010 - Cannot execute command at the current stepper controller state
      //                     011 - Motor not initialized
      //                     999 - Invalid Parameter
      //
      // Argument         : DOUBLE motor_id
      //                     00 - RA Motor
      //                     01 - DEC Motor
      // Argument         : DOUBLE trackrate
      //                     00 - Sidreal
      //                     01 - Lunar
      //                     02 - Solar
      // Argument         : DOUBLE guiderate
      //                     00 - No Change
      //                     01 - 10%
      //                     02 - 20%
      //                     03 - 30%
      //                     04 - 40%
      //                     05 - 50%
      //                     06 - 60%
      //                     07 - 70%
      //                     08 - 80%
      //                     09 - 90%
      // Argument         : DOUBLE guidedir
      //                     00 - Positive
      //                     01 - Negative
      // Argument         : DOUBLE hemisphere (used for DEC Motor control)
      //                     00 - North
      //                     01 - South
      // Argument         : DOUBLE direction (used for DEC Motor control)
      //                     00 - Forward(+)
      //                     01 - Reverse(-)
      //
      // Public Declare Function EQ_SendGuideRate Lib "EQCONTRL" (ByVal motor_id As Long, ByVal trackrate As Long, ByVal guiderate As Long, ByVal guidedir As Long, ByVal hemisphere As Long, ByVal direction As Long) As Long
      public int EQ_SendGuideRate(int motorId, int trackRate, int guideRate, int guideDirection, int hemisphere, int direction)
      {
         throw new NotImplementedException();
      }


      //
      // Function name    : EQ_SendCustomTrackRate()
      // Description      : Adjust the RA/DEC rotation trackrate based on a given speed adjustment offset
      // Return type      : DOUBLE
      //                     000 - Success
      //                     001 - Comport Not available
      //                     003 - COM Timeout Error
      //                     004 - Motor still busy
      //                     010 - Cannot Execute command at the current state
      //                     011 - Motor not initialized
      //                     999 - Invalid Parameter
      //
      // Argument         : DOUBLE motor_id
      //                     00 - RA Motor
      //                     01 - DEC Motor
      // Argument         : DOUBLE trackrate
      //                     00 - Sidreal
      //                     01 - Lunar
      //                     02 - Solar
      // Argument         : DOUBLE trackoffset
      //                     0 - 300
      // Argument         : DOUBLE trackdir
      //                     00 - Positive
      //                     01 - Negative
      // Argument         : DOUBLE hemisphere (used for DEC Motor)
      //                     00 - North
      //                     01 - South
      // Argument         : DOUBLE direction (used for DEC Motor)
      //                     00 - Forward(+)
      //                     01 - Reverse(-)
      //
      // Public Declare Function EQ_SendCustomTrackRate Lib "EQCONTRL" (ByVal motor_id As Long, ByVal trackrate As Long, ByVal trackoffset As Long, ByVal trackdir As Long, ByVal hemisphere As Long, ByVal direction As Long) As Long
      public int EQ_SendCustomTrackRate(int motorId, int trackRate, int trackOffset, int trackDirection, int hemisphere, int direction)
      {
         throw new NotImplementedException();
      }



      //
      // Function name    : EQ_SetCustomTrackRate()
      // Description      : Adjust the RA/DEC rotation trackrate based on a given speed adjustment offset
      // Return type      : DOUBLE
      //                     000 - Success
      //                     001 - Comport Not available
      //                     003 - COM Timeout Error
      //                     004 - Motor still busy
      //                     010 - Cannot Execute command at the current state
      //                     011 - Motor not initialized
      //                     999 - Invalid Parameter
      //
      // Argument         : DOUBLE motor_id
      //                     00 - RA Motor
      //                     01 - DEC Motor
      // Argument         : DOUBLE trackmode
      //                     01 - Initial
      //                     00 - Update
      // Argument         : DOUBLE trackoffset
      // Argument         : DOUBLE trackbase
      //                     00 - LowSpeed
      // Argument         : DOUBLE hemisphere
      //                     00 - North
      //                     01 - South
      // Argument         : DOUBLE direction
      //                     00 - Forward(+)
      //                     01 - Reverse(-)
      //
      // Public Declare Function EQ_SetCustomTrackRate Lib "EQCONTRL" (ByVal motor_id As Long, ByVal trackmode As Long, ByVal trackoffset As Long, ByVal trackbase As Long, ByVal hemisphere As Long, ByVal direction As Long) As Long
      public int EQ_SetCustomTrackRate(int motorId, int trackMode, int trackOffset, int trackBase, int hemisphere, int direction)
      {
         throw new NotImplementedException();
      }

      //
      // Function name    : EQ_MotorStop()
      // Description      : Stop RA/DEC Motor
      // Return type      : DOUBLE
      //                     000 - Success
      //                     001 - Comport Not available
      //                     003 - COM Timeout Error
      //                     010 - Cannot execute command at the current stepper controller state
      //                     011 - Motor not initialized
      //                     999 - Invalid Parameter
      // Argument         : DOUBLE motor_id
      //                     00 - RA Motor
      //                     01 - DEC Motor
      //                     02 - RA & DEC
      //
      // Public Declare Function EQ_MotorStop Lib "EQCONTRL" (ByVal value As Long) As Long
      public int EQ_MotorStop(int motorId)
      {
         throw new NotImplementedException();
      }


      //
      // Function name    : EQ_SetAutoguiderPortRate()
      // Description      : Sets RA/DEC Autoguideport rate
      // Return type      : DOUBLE - Stepper Counter Values
      //                     000 - Success
      //                     001 - Comport Not available
      //                     003 - COM Timeout Error
      //                     999 - Invalid Parameter
      // Argument         : motor_id
      //                       00 - RA Motor
      //                       01 - DEC Motor
      // Argument         : DOUBLE guideportrate
      //                       00 - 0.25x
      //                       01 - 0.50x
      //                       02 - 0.75x
      //                       03 - 1.00x
      //
      // Public Declare Function EQ_SetAutoguiderPortRate Lib "EQCONTRL" (ByVal motor_id As Long, ByVal guideportrate As Long) As Long
      public int EQ_SetAutoguiderPortRate(int motorId, int guideportRate)
      {
         throw new NotImplementedException();
      }



      // Function name    : EQ_GetTotal360microstep()
      // Description      : Get RA/DEC Motor Total 360 degree microstep counts
      // Return type      : Double - Stepper Counter Values
      //                     0 - 16777215  Valid Count Values
      //                     0x1000000 - Mount Not available
      //                     0x3000000 - Invalid Parameter
      // Argument         : DOUBLE motor_id
      //                     00 - RA Motor
      //                     01 - DEC Motor
      //
      // Public Declare Function EQ_GetTotal360microstep Lib "EQCONTRL" (ByVal value As Long) As Long
      public int EQ_GetTotal360microstep(int motorId)
      {
         throw new NotImplementedException();
      }

      // Function name    : EQ_GetMountVersion()
      // Description      : Get Mount//s Firmware version
      // Return type      : Double - Mount//s Firmware Version
      //
      //                     0x1000000 - Mount Not available
      //
      // Public Declare Function EQ_GetMountVersion Lib "EQCONTRL" () As Long
      public int EQ_GetMountVersion()
      {
         throw new NotImplementedException();
      }

      // Function name    : EQ_GetMountStatus()
      // Description      : Get Mount//s Firmware version
      // Return type      : Double - Mount Status
      //
      //                     000 - Not Connected
      //                     001 - Connected
      //
      // Public Declare Function EQ_GetMountStatus Lib "EQCONTRL" () As Long
      public int EQ_GetMountStatus()
      {
         throw new NotImplementedException();
      }

      // Function name    : EQ_DriverVersion()
      // Description      : Get Drivr Version
      // Return type      : Double - Driver Version
      //
      // Public Declare Function EQ_DriverVersion Lib "EQCONTRL" () As Long
      public int EQ_DriverVersion()
      {
         throw new NotImplementedException();
      }


      // Function name    : EQ_GP()
      // Description      : Get Mount Parameters
      // Return type      : Double - parameter value
      // Public Declare Function EQ_GP Lib "EQCONTRL" (ByVal motor_id As Long, ByVal p_id As Long) As Long
      public int EQ_GP(int motorId, int parameterId)
      {
         throw new NotImplementedException();
      }

      // Function name    : EQ_WP()
      // Description      : write parameter
      // Parameter        : value
      // Return type      : error code
      // Public Declare Function EQ_WP Lib "EQContrl.dll" (ByVal motor_id As Long, ByVal p_id As Long, ByVal value As Long) As Long
      public int EQ_WP(int motorId, int parameterId, int value)
      {
         throw new NotImplementedException();
      }


      // Public Declare Function EQ_SetOffset Lib "EQCONTRL" (ByVal motor_id As Long, ByVal doffset As Long) As Long
      public int EQ_SetOffset(int motorId, int offset)
      {
         throw new NotImplementedException();
      }

      // Function name    : EQ_SetMountType
      // Description      : Sets Mount protocol tpye
      // Return type      : 0
      // Public Declare Function EQ_SetMountType Lib "EQCONTRL" (ByVal motor_type As Long) As Long
      public int EQ_SetMountType(int motorType)
      {
         throw new NotImplementedException();
      }


      // Function name    : EQ_WriteByte
      // Description      : write a byte out of the serial port
      // Return type      : error code
      // Public Declare Function EQ_WriteByte Lib "EQContrl.dll" (ByVal bData As Byte) As Long
      public int EQ_WriteByte(byte data)
      {
         throw new NotImplementedException();
      }

      // Function name    : EQ_SendMountCommand
      // Description      : send a mount command
      // Return type      : error code
      // Public Declare Function EQ_SendMountCommand Lib "EQContrl.dll" (ByVal motor_id As Long, ByVal command As Byte, ByVal params As Long, ByVal Count As Long) As Long
      public int EQ_SendMountCommand(int motorId, byte command, int parameters, int count)
      {
         throw new NotImplementedException();
      }


      // Function name    : EQ_QueryMount
      // Description      : send a string to the mount and get respnse back
      // Return type      : error code
      // Public Declare Function EQ_QueryMount Lib "EQCONTRL" (ByVal ptx As Long, ByVal prx As Long, ByVal sz As Long) As Long
      public int EQ_QueryMount(int ptx, int prx, int sz)
      {
         throw new NotImplementedException();
      }

      // Function name    : EQCom::EQ_DebugLog
      // Description      : Control of debug logging to file
      // param  BYTE*     : pointer to file name
      // param  BYTE*     : pointer to comment
      // param  DWORD     : Operation (stop=0; start=1; append=2)
      // return DWORD     : DLL Return Code
      // - DLL_SUCCESS       000      Success
      // - DLL_GENERALERROR  012      Error
      // - DLL_BADPARAM      999      bad parmComport timeout
      // Public Declare Function EQ_DebugLog Lib "EQCONTRL" (ByVal FileName As String, ByVal comment As String, ByVal operation As Long) As Long
      public int EQ_DebugLog(string filename, string comment, int operation)
      {
         throw new NotImplementedException();
      }

      ///////////////////////////////////////////////////////////////////////////////////////
      ///** \brief  Function name       : EQCom::EQ_SetCustomTrackRate()
      //  * \brief  Description         : Guiderate activate
      //  * \param  DWORD               : motor_id      (0 RA, 1 DEC)
      //  * \param  DOUBLE              : rate arcsec/sec
      //  * \param  DWORD               : hemisphere    (0 NORTHERN, 1 SOUTHERN)
      //  * \param  DWORD               : direction     (0 FORWARD,  1 REVERSE)
      //  * \return DWORD               : DLL Return Code
      //  *
      //  * - DLL_SUCCESS       000      Success
      //  * - DLL_NOCOMPORT     001      Comport Not available
      //  * - DLL_COMERROR      003      COM Timeout Error
      //  * - DLL_MOTORBUSY     004      Motor still busy
      //  * - DLL_NONSTANDARD   005      Mount Initialized on non-standard parameters
      //  * - DLL_MOUNTBUSY     010      Cannot execute command at the current state
      //  * - DLL_MOTORERROR    011      Motor not initialized
      //  * - DLL_MOTORINACTIVE 200      Motor coils not active
      //  * - DLL_BADPARAM      999      Invalid parameter
      //  */
      // Public Declare Function EQ_SetAxisRate Lib "EQCONTRL" (ByVal motor_id As Long, ByVal rate As Double, hemisphere As Long, direction As Long) As Long
      public int EQ_SetAxisRate(int motorId, double rate, int hemisphere, int direction)
      {
         throw new NotImplementedException();
      }


      //Public Function EQ_GetMountFeatures() As Long
      //    Dim res As Long
      //    res = EQ_GP(0, 10009)
      //    If res<> 999 Then
      //        EQ_GetMountFeatures = res
      //    Else
      //        EQ_GetMountFeatures = 0
      //    End If
      //End Function

      public int EQ_GetMountFeatures()
      {
         int res = EQ_GP(0, 10009);
         if (res != 999) {
            return res;
         }
         else {
            return 0;
         }
      }

      #endregion

      #region Skywatcher_Open code ...

      /// ************ Motion control related **********************
      /// They are variables represent the mount's status, but not grantee always updated.        
      /// 1) The Positions are updated with MCGetAxisPosition and MCSetAxisPosition
      /// 2) The TargetPositions are updated with MCAxisSlewTo        
      /// 3) The SlewingSpeed are updated with MCAxisSlew
      /// 4) The AxesStatus are updated updated with MCGetAxisStatus, MCAxisSlewTo, MCAxisSlew
      /// Notes:
      /// 1. Positions may not represent the mount's position while it is slewing, or user manually update by hand
      public double[] Positions = new double[2] { 0, 0 };            // The axis coordinate position of the carriage, in radians
      public double[] TargetPositions = new double[2] { 0, 0 };      // The target position, in radians
      public double[] SlewingSpeed = new double[2] { 0, 0 };         // The speed in radians per second                
      private AxisStatus[] AxesStatus = new AxisStatus[2];           // The two-axis status of the carriage should pass AxesStatus[AXIS1] and AxesStatus[AXIS2] by Reference

      // Converting an arc angle to a step
      private double[] FactorRadToStep = new double[] { 0, 0 };      // Multiply the radian value by the coefficient to get the motor position value (24-bit number can be discarded the highest byte)
      private long AngleToStep(AxisId Axis, double AngleInRad)
      {
         return (long)(AngleInRad * FactorRadToStep[(int)Axis]);
      }

      // Converts Step to Radian
      private double[] FactorStepToRad = new double[] { 0, 0 };                 // The value of the motor board position (need to deal with the problem after the symbol) multiplied by the coefficient can be a radian value
      private double StepToAngle(AxisId Axis, long Steps)
      {
         return Steps * FactorStepToRad[(int)Axis];
      }

      // Converts the speed in radians per second to an integer used to set the speed
      private double[] FactorRadRateToInt = new double[] { 0, 0 };           // Multiply the radians per second by this factor to obtain a 32-bit integer that sets the speed used by the motor board
      private long RadSpeedToInt(AxisId Axis, double RateInRad)
      {
         return (long)(RateInRad * FactorRadRateToInt[(int)Axis]);
      }

      public void MCInit()
      {
         try {
            InquireMotorBoardVersion(AxisId.Axis1_RA);
         }
         catch {
            // try again
            System.Threading.Thread.Sleep(200);
            InquireMotorBoardVersion(AxisId.Axis1_RA);
         }

         MountCode = MCVersion & 0xFF;

         //// NOTE: Simulator settings, Mount dependent Settings

         // Inquire Gear Rate
         InquireGridPerRevolution(AxisId.Axis1_RA);
         InquireGridPerRevolution(AxisId.Axis2_DEC);

         // Inquire motor timer interrup frequency
         InquireTimerInterruptFreq(AxisId.Axis1_RA);
         InquireTimerInterruptFreq(AxisId.Axis2_DEC);

         // Inquire motor high speed ratio
         InquireHighSpeedRatio(AxisId.Axis1_RA);
         InquireHighSpeedRatio(AxisId.Axis2_DEC);

         // Inquire PEC period
         // DC motor controller does not support PEC
         if (!IsDCMotor) {
            //InquirePECPeriod(AXISID.AXIS1);
            //InquirePECPeriod(AXISID.AXIS2);
         }

         // Inquire Axis Position
         Positions[(int)AxisId.Axis1_RA] = MCGetAxisPosition(AxisId.Axis1_RA);
         Positions[(int)AxisId.Axis2_DEC] = MCGetAxisPosition(AxisId.Axis2_DEC);

         InitializeMC();

         // These two LowSpeedGotoMargin are calculate from slewing for 5 seconds in 128x sidereal rate
         LowSpeedGotoMargin[(int)AxisId.Axis1_RA] = (long)(640 * Constants.SIDEREAL_RATE_ARCSECS * FactorRadToStep[(int)AxisId.Axis1_RA]);
         LowSpeedGotoMargin[(int)AxisId.Axis2_DEC] = (long)(640 * Constants.SIDEREAL_RATE_ARCSECS * FactorRadToStep[(int)AxisId.Axis2_DEC]);

         // Default break steps
         BreakSteps[(int)AxisId.Axis1_RA] = 3500;
         BreakSteps[(int)AxisId.Axis2_DEC] = 3500;
      }

      /// <summary>
      /// Slew about a given axis
      /// </summary>
      /// <param name="Axis"></param>
      /// <param name="Speed">Slew speed in Radians per second.</param>
      public void MCAxisSlew(AxisId Axis, double Speed)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("MCAxisSlew: ({0}, {1})", Axis, Speed));
         // Limit maximum speed
         if (Speed > MAX_SPEED) {                  // 3.4 degrees/sec, 800X sidereal rate, is the highest speed.
            Speed = MAX_SPEED;
         }
         else if (Speed < -MAX_SPEED) {
            Speed = -MAX_SPEED;
         }

         double InternalSpeed = Speed;
         System.Diagnostics.Debug.WriteLine(string.Format("1. Internal speed: {0}", InternalSpeed));
         bool forward = false, highspeed = false;

         // InternalSpeed lower than 1/1000 of sidereal rate?
         if (Math.Abs(InternalSpeed) <= Constants.SIDEREAL_RATE_ARCSECS / 1000.0) {
            MCAxisStop(Axis);
            return;
         }

         // Stop motor and set motion mode if necessary.
         PrepareForSlewing(Axis, InternalSpeed);
         System.Diagnostics.Debug.WriteLine(string.Format("1. Internal speed: {0}", InternalSpeed));

         if (InternalSpeed > 0.0)
            forward = true;
         else {
            InternalSpeed = -InternalSpeed;
            forward = false;
         }

         // TODO: ask the details

         // Calculate and set step period. 
         //if (InternalSpeed > LOW_SPEED_MARGIN) {                  // High speed adjustment
         //   InternalSpeed = InternalSpeed / (double)HighSpeedRatio[(int)Axis];
         //   highspeed = true;
         //}
         InternalSpeed = 1 / InternalSpeed;                    // For using function RadSpeedToInt(), change to unit Senonds/Rad.
         long SpeedInt = RadSpeedToInt(Axis, InternalSpeed);
         if ((MCVersion == 0x010600) || (MCVersion == 0x010601))  // For special MC version.
            SpeedInt -= 3;
         if (SpeedInt < 6) SpeedInt = 6;
         SetStepPeriod(Axis, SpeedInt);

         // Start motion
         // if (AxesStatus[Axis] & AXIS_FULL_STOPPED)				// It must be remove for the latest DC motor board.
         StartMotion(Axis);

         AxesStatus[(int)Axis].SetSlewing(forward, highspeed);
         SlewingSpeed[(int)Axis] = Speed;
      }

      public void MCAxisSlewTo(AxisId Axis, double TargetPosition)
      {
         // Get current position of the axis.
         var CurPosition = MCGetAxisPosition(Axis);

         // Calculate slewing distance.
         // Note: For EQ mount, Positions[AXIS1] is offset( -PI/2 ) adjusted in UpdateAxisPosition().
         var MovingAngle = TargetPosition - CurPosition;

         // Convert distance in radian into steps.
         var MovingSteps = AngleToStep(Axis, MovingAngle);

         bool forward = false, highspeed = false;

         // If there is no increment, return directly.
         if (MovingSteps == 0) {
            return;
         }

         // Set moving direction
         if (MovingSteps > 0) {
            dir = '0';
            forward = true;
         }
         else {
            dir = '1';
            MovingSteps = -MovingSteps;
            forward = false;
         }

         // Might need to check whether motor has stopped.

         // Check if the distance is long enough to trigger a high speed GOTO.
         if (MovingSteps > LowSpeedGotoMargin[(int)Axis]) {
            SetMotionMode(Axis, '0', dir);      // high speed GOTO slewing 
            highspeed = true;
         }
         else {
            SetMotionMode(Axis, '2', dir);      // low speed GOTO slewing
            highspeed = false;
         }

         SetGotoTargetIncrement(Axis, MovingSteps);
         SetBreakPointIncrement(Axis, BreakSteps[(int)Axis]);
         StartMotion(Axis);

         TargetPositions[(int)Axis] = TargetPosition;
         AxesStatus[(int)Axis].SetSlewingTo(forward, highspeed);
      }

      public void MCAxisStop(AxisId Axis)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("MCAxisStop: ({0})", Axis));
         if (InstantStop)
            TalkWithAxis(Axis, 'L', null);
         else
            TalkWithAxis(Axis, 'K', null);

         AxesStatus[(int)Axis].SetFullStop();
      }

      public void MCSetAxisPosition(AxisId Axis, double NewValue)
      {
         long NewStepIndex = AngleToStep(Axis, NewValue);
         NewStepIndex += 0x800000;

         string szCmd = longTo6BitHEX(NewStepIndex);
         TalkWithAxis(Axis, 'E', szCmd);

         Positions[(int)Axis] = NewValue;
      }

      public double MCGetAxisPosition(AxisId Axis)
      {
         string response = TalkWithAxis(Axis, 'j', null);

         long iPosition = BCDstr2long(response);
         iPosition -= 0x00800000;
         Positions[(int)Axis] = StepToAngle(Axis, iPosition);

         return Positions[(int)Axis];
      }

      public AxisStatus MCGetAxisStatus(AxisId Axis)
      {

         var response = TalkWithAxis(Axis, 'f', null);

         if ((response[2] & 0x01) != 0) {
            // Axis is running
            if ((response[1] & 0x01) != 0)
               AxesStatus[(int)Axis].Slewing = true;     // Axis in slewing(AstroMisc speed) mode.
            else
               AxesStatus[(int)Axis].SlewingTo = true;      // Axis in SlewingTo mode.
         }
         else {
            AxesStatus[(int)Axis].FullStop = true; // FullStop = 1;	// Axis is fully stop.
         }

         if ((response[1] & 0x02) == 0)
            AxesStatus[(int)Axis].SlewingForward = true; // Angle increase = 1;
         else
            AxesStatus[(int)Axis].SlewingForward = false;

         if ((response[1] & 0x04) != 0)
            AxesStatus[(int)Axis].HighSpeed = true; // HighSpeed running mode = 1;
         else
            AxesStatus[(int)Axis].HighSpeed = false;

         if ((response[3] & 1) == 0)
            AxesStatus[(int)Axis].NotInitialized = true; // MC is not initialized.
         else
            AxesStatus[(int)Axis].NotInitialized = false;


         return AxesStatus[(int)Axis];
      }

      public void MCSetSwitch(bool OnOff)
      {
         if (OnOff)
            TalkWithAxis(AxisId.Axis1_RA, 'O', "1");
         else
            TalkWithAxis(AxisId.Axis1_RA, 'O', "0");
      }

      /// <summary>
      /// One communication between mount and client
      /// </summary>
      /// <param name="Axis">The target of command</param>
      /// <param name="Command">The comamnd char set</param>
      /// <param name="cmdDataStr">The data need to send</param>
      /// <returns>The response string from mount</returns>
      private String TalkWithAxis(AxisId axis, char cmd, string cmdDataStr)
      {
         System.Diagnostics.Debug.Write(String.Format("TalkWithAxis({0}, {1}, {2})", axis, cmd, cmdDataStr));
         string response = string.Empty;

         const int BufferSize = 20;
         StringBuilder sb = new StringBuilder(BufferSize);
         sb.Append(cStartChar_Out);                  // 0: Leading char
         sb.Append(cmd);                         // 1: Length of command( Source, distination, command char, data )

         // Target Device
         sb.Append(((int)axis+1).ToString());    // 2: Target Axis
                                               // Copy command data to buffer
         sb.Append(cmdDataStr);

         sb.Append(cEndChar);    // CR Character            

         string cmdString = sb.ToString();
            //string.Format("{0}{1}{2}{3}{4}",
            //cStartChar_Out,
            //command,
            //(int)axis,
            //(cmdDataStr ?? "."),
            //cEndChar);

         var cmdTransaction = new EQTransaction(cmdString) { Timeout = TimeSpan.FromSeconds(TimeOut) };


         using (ICommunicationChannel channel = new SerialCommunicationChannel(EndPoint)) {
            var transactionObserver = new TransactionObserver(channel);
            var processor = new ReactiveTransactionProcessor();
            processor.SubscribeTransactionObserver(transactionObserver);
            try {
               channel.Open();

               // prepare to communicate
               for (int i = 0; i < Retry; i++) {

                  Task.Run(() => processor.CommitTransaction(cmdTransaction));
                  cmdTransaction.WaitForCompletionOrTimeout();
                  if (!cmdTransaction.Failed) {
                     response = cmdTransaction.Value;
                     break;
                  }
                  else {
                     Trace.TraceError(cmdTransaction.ErrorMessage.Single());
                  }
               }
            }
            catch (Exception ex) {
               Trace.TraceError("Connnection Lost");
               throw new MountControllerException(ErrorCode.ERR_NOT_CONNECTED, ex.Message);
            }
            finally {
               // To clean up, we just need to dispose the TransactionObserver and the channel is closed automatically.
               // Not strictly necessary, but good practice.
               transactionObserver.OnCompleted(); // There will be no more transactions.
               transactionObserver = null; // not necessary, but good practice.
            }

         }
         //if (string.IsNullOrWhiteSpace(response)) {
         //   if (axis == AxisId.Axis1)
         //      throw new MountControllerException(ErrorCode.ERR_NORESPONSE_AXIS1);
         //   else
         //      throw new MountControllerException(ErrorCode.ERR_NORESPONSE_AXIS2);
         //}
         System.Diagnostics.Debug.WriteLine(" -> Response: " + response);
         return response;
      }


      #region Motor command set ...
      /************************ MOTOR COMMAND SET ***************************/
      // Inquire Motor Board Version ":e(*1)", where *1: '1'= CH1, '2'= CH2, '3'= Both.
      private void InquireMotorBoardVersion(AxisId Axis)
      {
         string response = TalkWithAxis(Axis, 'e', null);

         long tmpMCVersion = BCDstr2long(response);

         MCVersion = ((tmpMCVersion & 0xFF) << 16) | ((tmpMCVersion & 0xFF00)) | ((tmpMCVersion & 0xFF0000) >> 16);

      }
      // Inquire Grid Per Revolution ":a(*2)", where *2: '1'= CH1, '2' = CH2.
      private void InquireGridPerRevolution(AxisId Axis)
      {
         string response = TalkWithAxis(Axis, 'a', null);

         long GearRatio = BCDstr2long(response);

         // There is a bug in the earlier version firmware(Before 2.00) of motor controller MC001.
         // Overwrite the GearRatio reported by the MC for 80GT mount and 114GT mount.
         if ((MCVersion & 0x0000FF) == 0x80) {
            GearRatio = 0x162B97;      // for 80GT mount
         }
         if ((MCVersion & 0x0000FF) == 0x82) {
            GearRatio = 0x205318;      // for 114GT mount
         }

         FactorRadToStep[(int)Axis] = GearRatio / (2 * Math.PI);
         FactorStepToRad[(int)Axis] = 2 * Math.PI / GearRatio;
      }

      // Inquire Timer Interrupt Freq ":b1".
      private void InquireTimerInterruptFreq(AxisId Axis)
      {
         string response = TalkWithAxis(Axis, 'b', null);

         long TimeFreq = BCDstr2long(response);
         StepTimerFreq[(int)Axis] = TimeFreq;

         FactorRadRateToInt[(int)Axis] = (double)(StepTimerFreq[(int)Axis]) / FactorRadToStep[(int)Axis];
      }

      // Inquire high speed ratio ":g(*2)", where *2: '1'= CH1, '2' = CH2.
      private void InquireHighSpeedRatio(AxisId Axis)
      {
         string response = TalkWithAxis(Axis, 'g', null);

         long highSpeedRatio = BCDstr2long(response);
         HighSpeedRatio[(int)Axis] = highSpeedRatio;
      }

      // Inquire PEC Period ":s(*1)", where *1: '1'= CH1, '2'= CH2, '3'= Both.
      private void InquirePECPeriod(AxisId Axis)
      {
         string response = TalkWithAxis(Axis, 's', null);

         long PECPeriod = BCDstr2long(response);
         PESteps[(int)Axis] = PECPeriod;
      }
      // Set initialization done ":F3", where '3'= Both CH1 and CH2.
      private void InitializeMC()
      {
         TalkWithAxis(AxisId.Axis1_RA, 'F', null);
         TalkWithAxis(AxisId.Axis2_DEC, 'F', null);
      }
      private void SetMotionMode(AxisId Axis, char func, char direction)
      {
         string szCmd = "" + func + direction;
         TalkWithAxis(Axis, 'G', szCmd);
      }
      private void SetGotoTargetIncrement(AxisId Axis, long StepsCount)
      {
         string cmd = longTo6BitHEX(StepsCount);

         TalkWithAxis(Axis, 'H', cmd);
      }
      private void SetBreakPointIncrement(AxisId Axis, long StepsCount)
      {
         string szCmd = longTo6BitHEX(StepsCount);

         TalkWithAxis(Axis, 'M', szCmd);
      }
      private void SetBreakSteps(AxisId Axis, long NewBrakeSteps)
      {
         string szCmd = longTo6BitHEX(NewBrakeSteps);
         TalkWithAxis(Axis, 'U', szCmd);
      }
      private void SetStepPeriod(AxisId Axis, long StepsCount)
      {
         System.Diagnostics.Debug.WriteLine(String.Format("SetStepPeriod({0}, {1})", Axis, StepsCount));
         string szCmd = longTo6BitHEX(StepsCount);
         TalkWithAxis(Axis, 'I', szCmd);
      }
      private void StartMotion(AxisId Axis)
      {
         TalkWithAxis(Axis, 'J', null);
      }
      #endregion

      #region Skywaterch Helper functions ...
      private bool IsHEXChar(char tmpChar)
      {
         return ((tmpChar >= '0') && (tmpChar <= '9')) || ((tmpChar >= 'A') && (tmpChar <= 'F'));
      }
      private long HEX2Int(char HEX)
      {
         long tmp;
         tmp = HEX - 0x30;
         if (tmp > 9)
            tmp -= 7;
         return tmp;
      }
      private long BCDstr2long(string str)
      {
         // =020782 => 8521474
         try {
            long value = 0;
            for (int i = 1; i + 1 < str.Length; i += 2) {
               value += (long)(int.Parse(str.Substring(i, 2), System.Globalization.NumberStyles.AllowHexSpecifier) * Math.Pow(16, i - 1));
            }

            // if(D)
            // Log.d(TAG,"BCDstr2long " + response + ","+value);
            return value;
         }
         catch (FormatException e) {
            throw new MountControllerException(ErrorCode.ERR_INVALID_DATA,
                            "Parse BCD Failed");
         }
         // return Integer.parseInt(response.substring(0, 2), 16)
         // + Integer.parseInt(response.substring(2, 4), 16) * 256
         // + Integer.parseInt(response.substring(4, 6), 16) * 256 * 256;
      }
      private string longTo6BitHEX(long number)
      {
         // 31 -> 0F0000
         String A = ((int)number & 0xFF).ToString("X").ToUpper();
         String B = (((int)number & 0xFF00) / 256).ToString("X").ToUpper();
         String C = (((int)number & 0xFF0000) / 256 / 256).ToString("X").ToUpper();

         if (A.Length == 1)
            A = "0" + A;
         if (B.Length == 1)
            B = "0" + B;
         if (C.Length == 1)
            C = "0" + C;

         // if (D)
         // Log.d(TAG, "longTo6BitHex " + number + "," + A + "," + B + "," + C);

         return A + B + C;
      }

      private void PrepareForSlewing(AxisId Axis, double speed)
      {
         char cDirection;

         var axesstatus = MCGetAxisStatus(Axis);   
         if (!axesstatus.FullStop) {
            if ((axesstatus.SlewingTo) ||                               // GOTO in action
                 (axesstatus.HighSpeed) ||                              // Currently high speed slewing
                 (Math.Abs(speed) >= LOW_SPEED_MARGIN) ||               // Will be high speed slewing
                 ((axesstatus.SlewingForward) && (speed < 0)) ||        // Different direction
                 (!(axesstatus.SlewingForward) && (speed > 0))          // Different direction
                ) {
               // We need to stop the motor first to change Motion Mode, etc.
               MCAxisStop(Axis);
            }
            else
               // Other situatuion, there is no need to set motion mode.
               return;



            // Wait until the axis stop
            while (true) {
               // Update Mount status, the status of both axes are also updated because _GetMountStatus() includes such operations.
               axesstatus = MCGetAxisStatus(Axis);

               // Return if the axis has stopped.
               if (axesstatus.FullStop)
                  break;

               Thread.Sleep(100);

               // If the axis is asked to stop.
               // if ( (!AxesAskedToRun[Axis] && !(MountStatus & MOUNT_TRACKING_ON)) )		// If AXIS1 or AXIS2 is asked to stop or 
               //	return ERR_USER_INTERRUPT;

            }

         }
         if (speed > 0.0) {
            cDirection = '0';
         }
         else {
            cDirection = '1';
            speed = -speed;                     // Get absolute value of Speed.
         }

         if (speed > LOW_SPEED_MARGIN) {
            SetMotionMode(Axis, '3', cDirection);              // Set HIGH speed slewing mode.
         }
         else
            SetMotionMode(Axis, '1', cDirection);              // Set LOW speed slewing mode.

      }


      #endregion



      #endregion

   }
}
