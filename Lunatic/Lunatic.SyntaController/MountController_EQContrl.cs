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

using Lunatic.Core;
using Lunatic.SyntaController.Transactions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TA.Ascom.ReactiveCommunications;

namespace Lunatic.SyntaController
{
   partial class MountController
   {
      #region Old EQ_Contrl.dll methods ....

      #region Constants ...
      private const int RAMotor = 0;
      private const int DECMotor = 1;

      private const double SID_RATE = 15.041067;
      private const double MAX_RATE = (800 * SID_RATE);

      private const int SecondsPerSiderealDay = 86164;
      private const int EQDRIVERVERSION = 0x206;

      private const int EQMOUNT = 1;         // EQG Protocol 
      private const int AUTO_DETECT = 0;     // Detected Current Mount

      private const int RA_AUX_ENCODER = 3;
      private const int DEC_AUX_ENCODER = 4;

      private const int POSITIVE = 0;
      private const int NEGATIVE = 1;

      private const int NO_PARAMS = 0;





      //private const char CR = (char)0x0D;    // Command terminator
      //private const char LF = (char)0x0A;    // Command terminator

      public const int MOUNT_SUCCESS = 0;            // Success
      public const int MOUNT_NOCOMPORT = 1;         // Comport Not available
      public const int MOUNT_COMCONNECTED = 2;      // Mount already connected
      public const int MOUNT_COMERROR = 3;          // COM Timeout Error
      public const int MOUNT_MOTORBUSY = 4;         // Motor still busy
      public const int MOUNT_NONSTANDARD = 5;       // Mount Initialized on non-standard parameters
      public const int MOUNT_RARUNNING = 6;         // RA Motor still running
      public const int MOUNT_DECRUNNING = 7;        // DEC Motor still running 
      public const int MOUNT_RAERROR = 8;           // Error Initializing RA Motor
      public const int MOUNT_DECERROR = 9;          // Error Initilizing DEC Motor
      public const int MOUNT_MOUNTBUSY = 10;        // Cannot execute command at the current state
      public const int MOUNT_MOTORERROR = 11;       // Motor not initialized
      public const int MOUNT_GENERALERROR = 12;     //
      public const int MOUNT_MOTORINACTIVE = 200;   // Motor not initialized
      public const int MOUNT_EQMOUNT = 301;         // EQG series mount
      public const int MOUNT_NXMOUNT = 302;         // Nexstar series mount
      public const int MOUNT_LXMOUNT = 303;         // LX200 series mount
      public const int MOUNT_BADMOUNT = 998;        // Cant detect mount type
      public const int MOUNT_BADPARAM = 999;        // Invalid parameter

      public const int MOUNT_CONNECTED = 1;         // Connected to EQMOD
      public const int MOUNT_NOTCONNECTED = 0;      // Not connected to EQMOD


      public const int EQ_OK = 0x2000000;         // Success with no return values
      public const int EQ_OKRETURN = 0x0000000;   // 0x0999999 - Success with Mount Return Values
      public const int EQ_BADSTATE = 0x10000ff;   // Unexpected return value from mount
      public const int EQ_ERROR = 0x1000000;      // Bad command to send to mount
      public const int EQ_BADPACKET = 0x1000001;  // Missing or too many parameters
      public const int EQ_MOUNTBUSY = 0x1000002;  // Cannot execute command in current state
      public const int EQ_BADVALUE = 0x1000003;   // Bad Parameter Value
      public const int EQ_NOMOUNT = 0x1000004;    // Mount not enabled
      public const int EQ_COMTIMEOUT = 0x1000005; // Mount communications timeout
      public const int EQ_CRCERROR = 0x1000006;   // Data Packet CRC error
      public const int EQ_PPECERROR = 0x1000008;  // Data Packet CRC error
      public const int EQ_INVALID = 0x3000000;    // Invalid Parameter


      #endregion


      #region Members ...
      //Detected mount type
      private MountType MountType;      // EQG, NexStar, ..
      private AxisId MountMotor;    // RA, DEC
      private double MountRate;     // in EQG values
      private MountSpeed MountSpeed;     // High/Low
      private MountTracking MountTracking;  // Sidereal, Solar, Lunar
      private MountMode MountMode;      // Step/Slew
      private AxisDirection MountDirection; // Forward/Reverse
      private HemisphereOption MountHemiSphere;   // North/South
      private char MountCommand;   // Current command
      private int MountParameter;
      private int MountCount;
      private int TargetRA;
      private int TargetDEC;
      private int FastRA;
      private int FastDEC;
      private int MountRA;
      private int MountDEC;
      private byte MountGOTO;


      // EQ Mount Active State

      private bool MountActive;

      //float eq_s1;
      //float eq_s2;
      //float eq_s3;
      //float eq_s4;
      //int eq_of1;
      //int eq_of2;

      #endregion


      /// <summary>
      /// Send the command to the correct mount
      /// </summary>
      /// <param name="axisId">The Axis Id enum value</param>
      /// <param name="command">command (ASCII command to send to mount)</param>
      /// <param name="parameters">parameter (Binary parameter or 0)</param>
      /// <param name="count">count (# parameter bytes)</param>
      /// <returns>Driver Return Value
      ///   -	EQ_OK			0x2000000 - Success with no return values
      ///   -	EQ_COMTIMEOUT	0x1000005 - COM TIMEOUT
      ///   -	EQ_INVALID		0x3000000 - Invalid Parameter</returns>
      /// <remarks></remarks>
      public int EQ_SendCommand(AxisId axisId, char command, int parameters, short count)
      {
         return EQ_SendCommand((int)axisId, command, parameters, count);
      }

      /// <summary>
      /// Send the command to the correct mount
      /// </summary>
      /// <param name="motorId">motor_id (0 RA, 1 DEC)</param>
      /// <param name="command">command (ASCII command to send to mount)</param>
      /// <param name="parameters">parameter (Binary parameter or 0)</param>
      /// <param name="count">count (# parameter bytes)</param>
      /// <returns>Driver Return Value
      ///   -	EQ_OK			0x2000000 - Success with no return values
      ///   -	EQ_COMTIMEOUT	0x1000005 - COM TIMEOUT
      ///   -	EQ_INVALID		0x3000000 - Invalid Parameter</returns>
      /// <remarks></remarks>
      public int EQ_SendCommand(int motorId, char command, int parameters, short count)
      {
         if (motorId == (int)AxisId.Both_Axes) {
            return MOUNT_BADPARAM;
         }
         int response = EQ_OK;
         char[] hex_str = "0123456789ABCDEF     ".ToCharArray();   // Hexadecimal translation
         const int BufferSize = 20;
         StringBuilder sb = new StringBuilder(BufferSize);
         sb.Append(cStartChar_Out);
         sb.Append(command);
         sb.Append((motorId + 1).ToString());
         switch (count) {
            case 1:
               // nibble 1
               sb.Append(hex_str[(parameters & 0x00000f)]);
               break;
            case 2:
               // Byte 1
               sb.Append(hex_str[(parameters & 0x0000f0) >> 4]);
               sb.Append(hex_str[(parameters & 0x00000f)]);
               break;
            case 3:
               // Byte 1
               sb.Append(hex_str[(parameters & 0x0000f0) >> 4]);
               sb.Append(hex_str[(parameters & 0x00000f)]);
               // Nibble 3
               sb.Append(hex_str[(parameters & 0x000f00) >> 8]);
               break;
            case 4:
               // Byte 1
               sb.Append(hex_str[(parameters & 0x0000f0) >> 4]);
               sb.Append(hex_str[(parameters & 0x00000f)]);
               // Byte 2
               sb.Append(hex_str[(parameters & 0x00f000) >> 12]);
               sb.Append(hex_str[(parameters & 0x000f00) >> 8]);
               break;
            case 5:
               // Byte 1
               sb.Append(hex_str[(parameters & 0x0000f0) >> 4]);
               sb.Append(hex_str[(parameters & 0x00000f)]);
               // Byte 2
               sb.Append(hex_str[(parameters & 0x00f000) >> 12]);
               sb.Append(hex_str[(parameters & 0x000f00) >> 8]);
               // nibble
               sb.Append(hex_str[(parameters & 0x0f0000) >> 16]);
               break;
            case 6:
               // Byte 1
               sb.Append(hex_str[(parameters & 0x0000f0) >> 4]);
               sb.Append(hex_str[(parameters & 0x00000f)]);
               // Byte 2
               sb.Append(hex_str[(parameters & 0x00f000) >> 12]);
               sb.Append(hex_str[(parameters & 0x000f00) >> 8]);
               // Byte 3
               sb.Append(hex_str[(parameters & 0xf00000) >> 20]);
               sb.Append(hex_str[(parameters & 0x0f0000) >> 16]);
               break;
            default:
               return EQ_INVALID;
         }
         sb.Append(cEndChar);
         string cmdString = sb.ToString();
         var cmdTransaction = new EQContrlTransaction(cmdString) { Timeout = TimeSpan.FromSeconds(TimeOut) };


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

         System.Diagnostics.Debug.WriteLine(" -> Response: " + response);
         return response;
      }

      /// <summary>
      /// Translates the internal comms error to the dll error
      /// </summary>
      /// <param name="commandError">Error returned from EQ_SendCommand</param>
      /// <returns>Mount error</returns>
      private int EQ_GetMountError(int commandError)
      {
         //todo case statement
         //todo The order is important here because the individual errors
         //todo  change the return value of the overall error - MOUNT_COMERROR

         // Convert EQ Mount errors to dll error return value
         if (commandError == EQ_COMTIMEOUT) {
            return MOUNT_COMERROR;
         }
         if ((commandError & EQ_ERROR) == EQ_ERROR) {
            return MOUNT_COMERROR;
         }
         if (commandError == EQ_MOUNTBUSY) {
            return MOUNT_MOUNTBUSY;
         }
         if (commandError == EQ_NOMOUNT) {
            return MOUNT_MOTORERROR;
         }
         if (commandError == EQ_PPECERROR) {
            return MOUNT_GENERALERROR;
         }
         if (commandError != EQ_OK) {
            return MOUNT_COMERROR;
         }
         return EQ_OK;
      }


      /// <summary>
      /// Set the motor hemisphere, mode, direction and speed
      /// </summary>
      /// <param name="axisId">AxisID enumeration value</param>
      /// <param name="hemisphere"></param>
      /// <param name="mode"></param>
      /// <param name="direction"></param>
      /// <param name="speed"></param>
      /// <returns>Driver Return Value
      ///				-	EQ_OK			0x2000000 - Success with no return values
      ///				-	EQ_OKRETURN		0x0000000 - 0x0999999 - Success with Mount Return Values
      ///				-	EQ_BADSTATE		0x10000ff - Bad Command
      ///				-	EQ_ERROR		0x1000000 - Bad Command
      ///				-	EQ_BADPACKET	0x1000001 - Missing or too many parameters
      ///				-	EQ_MOUNTBUSY	0x1000002 - Unknown
      ///				-	EQ_BADVALUE		0x1000003 - Bad Parameter Value
      ///				-	EQ_NOMOUNT		0x1000004 - Mount not enabled
      ///				-	EQ_COMTIMEOUT	0x1000005 - COM TIMEOUT
      ///				-	EQ_INVALID		0x3000000 - Invalid Parameter
      /// </returns>
      private int EQ_SendGCode(AxisId axisId, HemisphereOption hemisphere, MountMode mode, AxisDirection direction, MountSpeed speed)
      {
         byte ch;
         ch = 0;

         MountDirection = direction;
         MountHemiSphere = hemisphere;
         MountSpeed = speed;
         MountMode = mode;

         // Set Direction bit	(Bit 0)
         if (direction == AxisDirection.Reverse) {
            ch |= 0x01;
         }

         // Set Hemisphere bit	(Bit 1)
         if (hemisphere == HemisphereOption.Southern) {
            ch |= 0x02;
         }

         // 0 = high speed GOTO mode
         // 1 = low speed SLEW mode
         // 2 = low speed GOTO mode
         // 3 = high speed SLEW mode 

         // Set Mode and speed bits
         if (mode == MountMode.Goto) {
            //goto
            if (speed == MountSpeed.LowSpeed) {
               // Low speed goto = 2
               ch |= 0x20;
            }
            else {
               //high speed goto = 0

            }
         }
         else {
            // slew
            if (speed == MountSpeed.HighSpeed) {
               // High speed slew= 3
               ch |= 0x30;
            }
            else {
               // low speed slew= 1
               ch |= 0x10;
            }
         }


         // Send 'G' Command, with parameter
         if (EQ_SendCommand(axisId, 'G', ch, 2) != EQ_OK) {
            return (EQ_COMTIMEOUT);
         }

         return EQ_OK;

         //               EQ6Pro ignores speed setting when in "Goto" mode, but AZEQ5 doesnt
         // A = '0' high speed GOTO slewing,      doesnt make "bitmapped" sense, but it is as coded by SkyWatcher????? ?????
         //     '1' low  speed slewing mode,      all other bytes use bitmapping ( incl :f ), this doesnt
         //     '2' low  speed GOTO mode,
         //     '3' high speed slewing mode
         // xxxx xxx0   0 means Goto, 1 means slew
         // xxxx xx0x   usage changes based on lsb  ( i think they screwed up ) <<<<<<<<<<<<<<

         // B = '0'  CW  and Nth Hemi
         //     '1'  CCW and Nth Hemi
         //     '2'  CW  and Sth Hemi
         //     '3'  CCW and Sth Hemi
         //  A    B
         // xxxx xxx0   0 means +ve, 1 = -ve  "motor" direction, ie code takes care of whats N/S/E/W etc
         //             +ve speed in RA  is Axle moves CW when viewed from pole
         //             +ve speed in DEC is Axle moves CCW when viewed from above
         // xxxx xx0x   0 means Nth Hemi else Sth Hemi ( ST4 guiding related ) ?????
         // xxx0 xxxx   0 means Goto/Step else Slew
         // xx0x xxxx   0 means HiRate if Goto else LoRate if Slew
         // Note! when using :S type gotos, the direction bit ( lsb in 'B' ) here gets ignored


      }


      /// <summary>
      /// Set all default values based on EQ6 V1.05
      /// </summary>
      /// <param name=""></param>
      private void EQ_InitAll()
      {
         MountActive = false;
         GridPerRevolution[0] = 9024000;       // Total RA  microstep for a complete 360 revolution
         GridPerRevolution[1] = 9024000;       // Total DEC microstep for a complete 360 revolution
         StepTimerFreq[0] = 64935;         // RA  Sidereal rate factor
         StepTimerFreq[1] = 64935;         // DEC Sidereal rate factor
         HighSpeedRatio[0] = 32;            // RA  Motor High Speed slew scale factor
         HighSpeedRatio[1] = 32;            // DEC Motor High Speed slew scale factor
         MCVersion = 0;       // Mount firmware Version 
         MountCode = 0;        // Mount Id
         PESteps[0] = 50133;        // RA  Worm Period
         PESteps[1] = 50133;        // DEC Worm Period
         Offset[0] = 0;            // RA Offset
         Offset[1] = 0;            // DEC Offset

         MountType = MountType.EqMount;         // EQG ..
         // qPort.eqnMountMotor = RAMotor;        // RA, DEC
         MountRate = 1;            // in EQG values
         MountSpeed = MountSpeed.LowSpeed;    // High/Low
         MountTracking = MountTracking.Sidereal;        // Sidereal, Solar, Lunar
         MountMode = MountMode.Slew;            // Step/Slew
         MountDirection = AxisDirection.Forward;    // Forward/Reverse
         MountHemiSphere = HemisphereOption.Northern;  // North/South



         //eqPort.eqnMountCommand = 0;
         //eqPort.eqnMountParameter = 0;
         //eqPort.eqnMountCount = 0;

         FastTarget[0] = 0;           // Target for high-speed slew
         FastTarget[1] = 0;           // Target for high-speed slew
         FinalTarget[0] = 0;          // Final target (perhaps low speed)
         FinalTarget[1] = 0;
         CurrentPosition[0] = 0;      // Current Mount position (in EQG values)
         CurrentPosition[1] = 0;
         // eqPort.eqnMountGOTO = 0;

         MountParameters[0] = 0;
         MountParameters[1] = 0;
         HasPPEC[0] = false;
         HasPPEC[1] = false;
         HasEncoder[0] = false;
         HasEncoder[1] = false;
         HasHalfCurrent[0] = false;
         HasHalfCurrent[1] = false;
         HasSnap[0] = false;
         HasSnap[1] = false;
         HasPolarscopeLED = false;
         HasHomeSensor = false;
         LowSpeedGotoMargin[0] = 200;
         LowSpeedGotoMargin[1] = 200;

      }

      /// <summary>
      /// Initialize EQ Mount
      /// </summary>
      /// <param name="comportname">COMPORT Name</param>
      /// <param name="baud">Baud Rate</param>
      /// <param name="timeout">Timeout (1 - 50000)</param>
      /// <param name="retry">Retry (0 - 100)</param>
      /// <returns> Mount Error Code:
      ///	- MOUNT_SUCCESS		     000		 Success (new connection)
      ///	- MOUNT_NOCOMPORT		     001		 COM port not available
      ///	- MOUNT_COMCONNECTED	     002		 Mount already connected (success)
      ///	- MOUNT_COMERROR		     003		 COM Timeout Error
      ///	- MOUNT_MOTORBUSY		     004		 Motor still busy
      ///	- MOUNT_NONSTANDARD	     005		 Mount Initialized on non-standard parameters
      ///	- MOUNT_MOUNTBUSY		     010		 Cannot execute command at the current state
      ///	- MOUNT_MOTORERROR	     011		 Motor not initialized
      ///	- MOUNT_MOTORINACTIVE	  200		 Motor coils not active
      ///	- MOUNT_BADPARAM		     999		 Invalid parameter
      /// </returns>
      public int EQ_Init(string comportname, int baud, int timeout, int retry)
      {
         int result;
         if (MountActive) {
            return MOUNT_COMCONNECTED;
         }

         if ((timeout == 0) || (timeout > 50000)) {
            return MOUNT_BADPARAM;
         }

         if (retry > 100) {
            return MOUNT_BADPARAM;
         }

         lock (lockObject) {
            try {
               result = MOUNT_SUCCESS; ;
               if (EndPoint == null) {
                  #region Capture connection parameters ...
                  // ConnectionString = string.Format("{0}:{1},None,8,One,DTR,RTS", ComPort, baud);
                  ConnectionString = string.Format("{0}:{1},None,8,One,NoDTR,NoRTS", comportname, baud);
                  EndPoint = DeviceEndpoint.FromConnectionString(ConnectionString);
                  TimeOut = timeout * 0.001;  // Convert from milliseconds to seconds.
                  Retry = retry;
                  #endregion

                  #region Initialise values ...
                  // Set Mount to Inactive State
                  MountActive = false;
                  // Set default values for EQ6 mount
                  EQ_InitAll();

                  // Get Mount Firmware Version & Mount ID
                  // ======================================
                  // Get Mount Firmware Version & Mount ID (e)
                  try {

                     InquireMotorBoardVersion(AxisId.Axis1_RA);
                  }
                  catch {
                     // try again
                     System.Threading.Thread.Sleep(200);
                     InquireMotorBoardVersion(AxisId.Axis1_RA);
                  }

                  MountCode = MCVersion & 0xFF;

                  // Get Mount Steps per 360 (a)
                  // ===========================
                  InquireGridPerRevolution(AxisId.Axis1_RA);
                  InquireGridPerRevolution(AxisId.Axis2_DEC);


                  // Get Mount Tracking Scale (b)
                  // ============================
                  InquireTimerInterruptFreq(AxisId.Axis1_RA);
                  InquireTimerInterruptFreq(AxisId.Axis2_DEC);


                  // Get Mount Speed Divisor (g)
                  // ===========================	
                  InquireHighSpeedRatio(AxisId.Axis1_RA);
                  InquireHighSpeedRatio(AxisId.Axis2_DEC);


                  // Get Mount Steps per Worm Turn (PEC Period) (s)
                  // ==============================================
                  InquirePECPeriod(AxisId.Axis1_RA);
                  InquirePECPeriod(AxisId.Axis2_DEC);


                  // Get Extended Capabililtes/status
                  // =============================
                  // :qa010000[0D]  returns =ABCDEF[0D] which is bitmapped data for current status/capability
                  // A    xxx1 means PPEC training in progress,
                  //      xx1x means PPEC ON
                  // B    xxx1 Has Dual Encoder
                  //      xx1x Has PPEC
                  //      x1xx Has Has Home Sensors
                  //      1xxx Has EQ/AZ
                  // C    xxx1 Has Polar Scope LED
                  //      xx1x Has Two axes must start independently
                  //      x1xx Has half current tracking
                  //      1xxx Has wireless module
                  // D
                  // E
                  // F
                  // interpeted as 0x00EFCDAB so 
                  //		PEC training in progress	= 0x00000010
                  //		PEC on						= 0x00000020
                  //		Dual Encoder				= 0x00000001
                  //		PPEC						= 0x00000002
                  //		Home Sensor					= 0x00000004
                  //		EQ/AZ						= 0x00000008
                  //		Polar Scope LED				= 0x00001000
                  //		Independent Axes			= 0x00002000
                  //		Half Current Tracking		= 0x00004000
                  //		Wireless Module				= 0x00008000

                  // Inquire Polarscope LED (V)
                  // ==========================
                  InquirePolarScopeLED();

                  // Inquire mount parameters (q)
                  // ============================
                  InquireMountParameters();


                  // InquireSnapPorts (O)
                  // ====================
                  InquireSnapPorts();


                  // Say mount is now active
                  // =======================
                  MountActive = true;  // Set Mount to active State

                  // Compute for all GLOBAL Guiderates/Trackrates

                  LowSpeedSlewRate[RAMotor] = ((double)StepTimerFreq[RAMotor] / ((double)GridPerRevolution[RAMotor] / SecondsPerSiderealDay));
                  LowSpeedSlewRate[DECMotor] = ((double)StepTimerFreq[DECMotor] / ((double)GridPerRevolution[DECMotor] / SecondsPerSiderealDay));
                  HighSpeedSlewRate[RAMotor] = ((double)HighSpeedRatio[RAMotor] * ((float)StepTimerFreq[RAMotor] / ((double)GridPerRevolution[RAMotor] / SecondsPerSiderealDay)));
                  HighSpeedSlewRate[DECMotor] = ((double)HighSpeedRatio[DECMotor] * ((float)StepTimerFreq[DECMotor] / ((double)GridPerRevolution[DECMotor] / SecondsPerSiderealDay)));

                  MountRate = LowSpeedSlewRate[0];    // Default to SIDEREAL

                  result = MOUNT_SUCCESS;

                  #endregion
               }
               else {
                  result = MOUNT_COMCONNECTED;
               }

               Interlocked.Increment(ref openConnections);
            }
            catch {
               result = MOUNT_COMERROR;
            }
         }
         return result;

      }

      /// <summary>
      /// Drop EQ Connection
      /// </summary>
      /// <returns>
      ///   000 - Success
      ///	001 - Comport Not available</returns>
      public int EQ_End()
      {

         if (!MountActive) {
            return MOUNT_NOCOMPORT;
         }

         MountActive = false;    //Set Mount to inactive state
         return MOUNT_SUCCESS;
      }


      /// <summary>
      /// Stop RA/DEC Motor
      /// </summary>
      /// <param name="axisId">AxisId enumeration value</param>
      /// <returns></returns>
      /// <remarks>
      /// Return type      : DOUBLE
      ///                     000 - Success
      ///                     001 - Comport Not available
      ///                     003 - COM Timeout Error
      ///                     010 - Cannot execute command at the current stepper controller state
      ///                     011 - Motor not initialized
      ///                     999 - Invalid Parameter
      /// Argument         : DOUBLE motor_id
      ///                     00 - RA Motor
      ///                     01 - DEC Motor
      ///                     02 - RA & DEC
      ///
      /// </remarks>
      public int EQ_MotorStop(AxisId axisId)
      {
         int i;

         // Check Mount
         if (!MountActive) {
            return MOUNT_NOCOMPORT;
         }

         switch (axisId) {
            case AxisId.Axis1_RA:
            case AxisId.Axis2_DEC:
               // Stop Motor	
               i = EQ_SendCommand(axisId, 'K', 0, NO_PARAMS);
               if ((i * EQ_ERROR) == EQ_ERROR) {
                  return EQ_GetMountError(i);
               }

               // now wait for motor to stop
               do {
                  // Send Command
                  i = EQ_SendCommand(axisId, 'f', 0, NO_PARAMS);
                  if ((i & EQ_ERROR) == EQ_ERROR) {
                     return EQ_GetMountError(i);
                  }

                  // Return extended status
                  if ((i & 0x100) != 0x100) {
                     return MOUNT_MOTORINACTIVE;                 // Motor not initialized
                  }
               }
               while ((i & 0x01) == 0x01);
               break;
            case AxisId.Both_Axes:
               // stop RA motor
               i = EQ_SendCommand(AxisId.Axis1_RA, 'K', 0, NO_PARAMS);
               if ((i & EQ_ERROR) == EQ_ERROR) {
                  return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
               }

               // stop DEC motor
               i = EQ_SendCommand(AxisId.Axis2_DEC, 'K', 0, NO_PARAMS);
               if ((i & EQ_ERROR) == EQ_ERROR) {
                  return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
               }

               // now wait for motor to stop
               do {
                  // Send Command
                  i = EQ_SendCommand(AxisId.Axis1_RA, 'f', 0, NO_PARAMS);
                  if ((i & EQ_ERROR) == EQ_ERROR) {
                     return (EQ_GetMountError(i));                  // Check errors, return "dll_error" value
                  }

                  // Return extended status
                  if ((i & 0x100) != 0x100) {
                     return MOUNT_MOTORINACTIVE;                 // Motor not initialized
                  }
               }
               while ((i & 0x01) == 0x01);

               do {
                  // Send Command
                  i = EQ_SendCommand(AxisId.Axis2_DEC, 'f', 0, NO_PARAMS);
                  if ((i & EQ_ERROR) == EQ_ERROR) {
                     return (EQ_GetMountError(i));                  // Check errors, return "dll_error" value
                  }

                  // Return extended status
                  if ((i & 0x100) != 0x100) {
                     return MOUNT_MOTORINACTIVE;                 // Motor not initialized
                  }
               }
               while ((i & 0x01) == 0x01);
               break;
            default:
               return MOUNT_BADPARAM;
         }
         return MOUNT_SUCCESS;

      }

      /// <summary>
      /// Get RA/DEC Stepper Motor Status
      /// </summary>
      /// <param name="axisId">AxisId enumeration value</param>
      /// <returns>
      ///                     128 - Motor not rotating, Teeth at front contact
      ///                     144 - Motor rotating, Teeth at front contact
      ///                     160 - Motor not rotating, Teeth at rear contact
      ///                     176 - Motor rotating, Teeth at rear contact
      ///                     200 - Motor not initialized
      ///                     001 - COM Port Not available
      ///                     003 - COM Timeout Error
      ///                     999 - Invalid Parameter
      /// </returns>
      public int EQ_GetMotorStatus(AxisId axisId)
      {
         int response, status;

         // Check Port
         if (!MountActive) {
            return MOUNT_NOCOMPORT;
         }

         // Check Parameters
         if (!((axisId == AxisId.Axis1_RA) || (axisId == AxisId.Axis2_DEC))) {
            return MOUNT_BADPARAM;
         }


         // Send Command
         response = EQ_SendCommand(axisId, 'f', 0, NO_PARAMS);
         if ((response & EQ_ERROR) == EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         // :fx=ABC[0D]  is actually returned from GetReply as an integer as #x0CAB
         //              "is energised" is given by C  ie  ( response & x100 )
         //              "is moving"    is given by B  ie  ( response & x001 )
         //              "direction"    is given by A  ie  ( response & x020 )
         //              "Goto/Slew"                       ( response & x010 )
         //              "Curr Rate"                       ( response & x040 )


         // Return extended status
         if ((response & 0x100) != 0x100) {
            return MOUNT_MOTORINACTIVE;  // Motor not initialized
         }

         // assume Motor not rotating, Teeth at front contact (forward)
         status = 0x0080;

         // adjust Motor status is rotating
         if ((response & 0x01) == 0x01) {
            status += 0x10;  // Rotate State = rotating
         }

         // Adjust contact state if at rear
         if ((response & 0x20) == 0x20) {
            status += 0x20;  // Gear Contact State = rear (reverse)
         }

         return status;
      }


      /////// Motor Movement Functions /////

      /// <summary>
      /// Slew RA/DEC Motor based on provided microstep counts
      /// </summary>
      /// <param name="axisId"></param>
      /// <param name="hemisphere"></param>
      /// <param name="direction"></param>
      /// <param name="steps">Steps count</param>
      /// <param name="stepSlowDown">Motor de-acceleration  point (set between 50% t0 90% of total steps)</param>
      /// <returns></returns>
      public int EQ_StartMoveMotor(AxisId axisId, HemisphereOption hemisphere, AxisDirection direction, int steps, int stepSlowDown)
      {
         int i, j;

         // Check Mount
         if (!MountActive) {
            return MOUNT_NOCOMPORT;
         }

         // Check Parameters
         if (!((axisId == AxisId.Axis1_RA) || (axisId == AxisId.Axis2_DEC))) {
            return MOUNT_BADPARAM;
         }

         // Check Motor Status first
         i = EQ_GetMotorStatus(axisId);
         if ((i >= MOUNT_MOTORINACTIVE) || (i < 0x80)) {
            //we have an error code 
            return i;
         }
         else {
            if ((i & 0x90) != 0x80) {
               // motor is moving already - can't do a goto if one is in progress.
               return MOUNT_MOTORBUSY;
            }
         }


         // Make sure motor is stopped
         i = EQ_MotorStop(axisId);
         if (i != MOUNT_SUCCESS) {
            return i;
         }

         // Set the motor hemisphere, mode, direction and speed
         i = EQ_SendGCode(axisId, hemisphere, MountMode.Goto, direction, MountSpeed.HighSpeed);
         if ((i & EQ_ERROR) == EQ_ERROR) {
            return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
         }

         // Set the mount relative target
         i = EQ_SendCommand(axisId, 'H', steps, 6);
         if ((i & EQ_ERROR) == EQ_ERROR) {
            return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
         }

         // Set the mount deceleration point
         // ### AJ  motor card doesnt use this????   
         j = stepSlowDown;                      // Stepper Motor Deceleration point
         i = EQ_SendCommand(axisId, 'M', j, 6);
         if ((i & EQ_ERROR) == EQ_ERROR) {
            return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
         }

         // Start the motor
         i = EQ_SendCommand(axisId, 'J', 0, NO_PARAMS);
         if ((i & EQ_ERROR) == EQ_ERROR) {
            return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
         }

         return MOUNT_SUCCESS;
      }


      /// <summary>
      /// Initialize RA/DEC Motors and activate Motor Driver Coils
      /// </summary>
      /// <param name="RA_val">Initial ra microstep counter value</param>
      /// <param name="DEC_val">Initial dec microstep counter value</param>
      /// <returns>      
      ///   000 - Success
      ///   001 - COM PORT Not available
      ///   003 - COM Timeout Error
      ///   006 - RA Motor still running
      ///   007 - DEC Motor still running
      ///   008 - Error Initializing RA Motor
      ///   009 - Error Initilizing DEC Motor
      ///   010 - Cannot execute command at the current stepper controller state
      /// </returns>
      public int EQ_InitMotors(int RA_val, int DEC_val)
      {
         int response;

         // Check mount is active
         if (!MountActive) {
            return MOUNT_NOCOMPORT;
         }

         // Check if Both Motors are at rest
         response = EQ_GetMotorStatus(AxisId.Axis1_RA);              // Get RA Motor Status
         if (response < 0x80) {
            // all but dll_motorinactive need to be reported
            return response;
         }
         else {
            if ((response & 0x90) != 0x80) {
               // ra motor is apprently moving -don't reinitialise
               return MOUNT_RARUNNING;
            }
         }

         response = EQ_GetMotorStatus(AxisId.Axis2_DEC);          // Get DEC Motor Status
         if (response < 0x80) {
            // all but dll_motorinactive need to be reported
            return response;
         }
         else {
            if ((response & 0x90) != 0x80) {
               // dec motor is apprently moving - don't reiitialise
               return MOUNT_DECRUNNING;
            }
         }

         // Set RA
         response = EQ_SendCommand(AxisId.Axis1_RA, 'E', RA_val, 6);
         if ((response & EQ_ERROR) == EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         // Set DEC
         response = EQ_SendCommand(AxisId.Axis2_DEC, 'E', DEC_val, 6);
         if ((response & EQ_ERROR) == EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         // Confirm RA
         response = EQ_SendCommand(AxisId.Axis1_RA, 'j', 0, NO_PARAMS);
         if ((response & EQ_ERROR) == EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         // Confirm DEC
         response = EQ_SendCommand(AxisId.Axis2_DEC, 'j', 0, NO_PARAMS);
         if ((response & EQ_ERROR) == EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         // Activate RA  Motor
         response = EQ_SendCommand(AxisId.Axis1_RA, 'F', 0, NO_PARAMS);
         if ((response & EQ_ERROR) == EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         // Activate DEC Motor
         response = EQ_SendCommand(AxisId.Axis2_DEC, 'F', 0, NO_PARAMS);
         if ((response & EQ_ERROR) == EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         return MOUNT_SUCCESS;
      }

      /////// Motor Status Functions /////

      /// <summary>
      /// Get RA/DEC Motor microstep counts
      /// </summary>
      /// <param name="axisId">RA Axis or DEC Axis</param>
      /// <returns>
      ///         0 - 16777215  Valid Count Values
      ///         0x1000000 - Mount Not available
      ///         0x1000005 - COM TIMEOUT
      ///         0x10000FF - Illegal Mount reply
      ///         0x3000000 - Invalid Parameter</returns>
      public int EQ_GetMotorValues(AxisId axisId)
      {
         int i;

         // Check Mount
         if (!MountActive) {
            return EQ_ERROR;  // can't return DLL_NOCOMPORT as 001 is a potentially valide motor value
         }

         switch (axisId) {
            case AxisId.Axis1_RA:
            case AxisId.Axis2_DEC:
               // Get mount position
               // Mount position returns EQ_COMTIMEOUT because MotorValues range to 0xFFFFFF (16,777,215)
               i = EQ_SendCommand(axisId, 'j', 0, NO_PARAMS);
               if (i == EQ_COMTIMEOUT) {
                  return EQ_COMTIMEOUT;
               }
               return i;
            case AxisId.Aux_RA_Encoder:
               if (HasEncoder[0] == false) {
                  // mount doesn't have aux encoders
                  return EQ_INVALID;
               }
               // Get mount position
               // Mount position returns EQ_COMTIMEOUT because MotorValues range to 0xFFFFFF (16,777,215)
               i = EQ_SendCommand(AxisId.Axis1_RA, 'd', 0, NO_PARAMS);
               if (i == EQ_COMTIMEOUT) {
                  return EQ_COMTIMEOUT;
               }
               return i;
            case AxisId.Aux_DEC_Encoder:
               if (HasEncoder[1] == false) {
                  // mount doesn't have aux encoders
                  return EQ_INVALID;
               }
               // Get mount position
               // Mount position returns EQ_COMTIMEOUT because MotorValues range to 0xFFFFFF (16,777,215)
               i = EQ_SendCommand(AxisId.Axis2_DEC, 'd', 0, NO_PARAMS);
               if (i == EQ_COMTIMEOUT) {
                  return EQ_COMTIMEOUT;
               }
               return i;
         }
         return EQ_INVALID; // can't return DLL_BADPARAM as 999 is a potentially valide motor value
      }




      /// <summary>
      /// Sets RA/DEC Motor microstep counters (pseudo encoder position)
      /// </summary>
      /// <param name="axisId"></param>
      /// <param name="motorValue">0 - 16777215  Valid Count Values</param>
      /// <returns>
      ///                     000 - Success
      ///                     001 - Comport Not available
      ///                     003 - COM Timeout Error
      ///                     010 - Cannot execute command at the current stepper controller state
      ///                     011 - Motor not initialized
      ///                     999 - Invalid Parameter
      /// </returns>
      public int EQ_SetMotorValues(AxisId axisId, int motorValue)
      {
         int i;

         // Check mount
         if (!MountActive) {
            return MOUNT_NOCOMPORT;
         }

         // Check parameters
         if (!((axisId== AxisId.Axis1_RA) || (axisId== AxisId.Axis2_DEC))) {
            return MOUNT_BADPARAM;
         }


         // Send Set Reference Mount position 
         i = EQ_SendCommand(axisId, 'E', motorValue, 6);
         if ((i & EQ_ERROR) == EQ_ERROR) {
            return (EQ_GetMountError(i));         // Check errors, return "dll_error" value
         }

         return MOUNT_SUCCESS;
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

      /// <summary>
      /// Slew RA/DEC Motor based on given rate
      /// </summary>
      /// <param name="axisId"></param>
      /// <param name="hemisphere"></param>
      /// <param name="direction"></param>
      /// <param name="rate"> 1-800 of Sidreal Rate</param>
      /// <returns>
      ///     000 - Success
      ///     001 - Comport Not available
      ///     003 - COM Timeout Error
      ///     004 - Motor still busy
      ///     010 - Cannot execute command at the current stepper controller state
      ///     011 - Motor not initialized
      ///     999 - Invalid Parameter
      /// </returns>
      public int EQ_Slew(AxisId axisId, HemisphereOption hemisphere, AxisDirection direction, int rate)
      {
         int i, j, threshold;
         double k;

         // Check mount
         if (!MountActive) {
            return MOUNT_NOCOMPORT;
         }


         // Check parameters
         if (axisId == AxisId.Both_Axes) {
            return MOUNT_BADPARAM;
         }
         if ((rate < 1) || (rate > 800)) {
            return MOUNT_BADPARAM;
         }


         // Stop motor
         i = EQ_MotorStop(axisId);
         if (i != 0) {
            return i;
         }

         // Set the motor hemisphere, mode, direction and speed
         MountRate = rate;

         // determine slew rate highspeed/lowspeed threshold
         threshold = LowSpeedGotoMargin[0];
         if (axisId == AxisId.Axis2_DEC) {
            threshold = LowSpeedGotoMargin[1];
         }


         if (rate < threshold) {
            i = EQ_SendGCode(axisId, hemisphere, MountMode.Slew, direction, MountSpeed.LowSpeed);
            if ((i & EQ_ERROR) == EQ_ERROR) {
               return (EQ_GetMountError(i));         // Check errors, return "dll_error" value
            }

            if (axisId == AxisId.Axis1_RA) {
               k = LowSpeedSlewRate[0] / (double)rate;
               j = (int)k;                 // Round to nearest integer - Ignore compile warning
            }
            else {
               k = LowSpeedSlewRate[1] / (double)rate;
               j = (int)k;                 // Round to nearest integer - Ignore compile warning
            }
         }
         else {
            i = EQ_SendGCode(axisId, hemisphere, MountMode.Slew, direction, MountSpeed.HighSpeed);
            if ((i & EQ_ERROR) == EQ_ERROR) {
               return (EQ_GetMountError(i));         // Check errors, return "dll_error" value
            }

            if (axisId == AxisId.Axis1_RA) {
               k = HighSpeedSlewRate[0] / (float)rate;
               j = (int)k;                 // Round to nearest integer - Ignore compile warning
               if (rate < 20) {
                  j = ((j + Offset[0]) & 0xffffff);
               }
               else {
                  j = j & 0xffffff;
               }
            }
            else {
               k = HighSpeedSlewRate[1] / (float)rate;
               j = (int)k;           // Round to nearest integer - Ignore compile warning
               if (rate < 20) {
                  j = ((j + Offset[1]) & 0xffffff);
               }
               else {
                  j = j & 0xffffff;
               }
            }
         }


         // Send Speed Command
         i = EQ_SendCommand(axisId, 'I', j, 6);
         if ((i & EQ_ERROR) == EQ_ERROR) {
            return (EQ_GetMountError(i));         // Check errors, return "dll_error" value
         }

         // Send Go Command
         i = EQ_SendCommand(axisId, 'J', 0, NO_PARAMS);
         if ((i & EQ_ERROR) == EQ_ERROR) {
            return (EQ_GetMountError(i));         // Check errors, return "dll_error" value
         }

         return MOUNT_SUCCESS;
      }


      /// <summary>
      /// Track or rotate RA/DEC Stepper Motors at the specified rate
      /// </summary>
      /// <param name="trackRate"></param>
      /// <param name="hemisphere"></param>
      /// <param name="direction"></param>
      /// <returns>
      ///                     000 - Success
      ///                     001 - Comport Not available
      ///                     003 - COM Timeout Error
      ///                     010 - Cannot execute command at the current stepper controller state
      ///                     011 - Motor not initialized
      ///                     999 - Invalid Parameter
      /// </returns>
      public int EQ_StartRATrack(MountTracking trackRate, HemisphereOption hemisphere, AxisDirection direction)
      {
         int i, j;

         // Check mount	
         if (!MountActive) {
            return MOUNT_NOCOMPORT;
         }


         switch (trackRate) {
            case MountTracking.Solar:
               MountTracking = trackRate;
               j = (int)(LowSpeedSlewRate[0] * 1.0016129032258064516129032258065);
               break;

            case MountTracking.Lunar:
               MountTracking = trackRate;
               j = (int)(LowSpeedSlewRate[0] * 1.0370967741935483870967741935484);
               break;

            case MountTracking.Sidereal:
               MountTracking = trackRate;
               j = (int)LowSpeedSlewRate[0];
               break;

            default:
               return MOUNT_BADPARAM;

         }

         // Adjust for offset
         j = (j + Offset[0]) & 0xffffff;

         i = EQ_MotorStop(AxisId.Axis1_RA);
         if (i != 0) {
            return i;
         }

         // Set the motor hemisphere, mode, direction and speed
         i = EQ_SendGCode(RAMotor, hemisphere, MountMode.Slew, direction, MountSpeed.LowSpeed);
         if ((i & EQ_ERROR) == EQ_ERROR) {
            return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
         }

         // Send I Command
         i = EQ_SendCommand(RAMotor, 'I', j, 6);
         if ((i & EQ_ERROR) == EQ_ERROR) {
            return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
         }

         // Start RA Motor
         i = EQ_SendCommand(RAMotor, 'J', 0, NO_PARAMS);
         if ((i & EQ_ERROR) == EQ_ERROR) {
            return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
         }

         return MOUNT_SUCCESS;
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
      // param  uint     : Operation (stop=0; start=1; append=2)
      // return uint     : DLL Return Code
      // - MOUNT_SUCCESS       000      Success
      // - MOUNT_GENERALERROR  012      Error
      // - MOUNT_BADPARAM      999      bad parmComport timeout
      // Public Declare Function EQ_DebugLog Lib "EQCONTRL" (ByVal FileName As String, ByVal comment As String, ByVal operation As Long) As Long
      public int EQ_DebugLog(string filename, string comment, int operation)
      {
         throw new NotImplementedException();
      }

      ///////////////////////////////////////////////////////////////////////////////////////
      ///** \brief  Function name       : EQCom::EQ_SetCustomTrackRate()
      //  * \brief  Description         : Guiderate activate
      //  * \param  uint               : motor_id      (0 RA, 1 DEC)
      //  * \param  DOUBLE              : rate arcsec/sec
      //  * \param  uint               : hemisphere    (0 NORTHERN, 1 SOUTHERN)
      //  * \param  uint               : direction     (0 FORWARD,  1 REVERSE)
      //  * \return uint               : DLL Return Code
      //  *
      //  * - MOUNT_SUCCESS       000      Success
      //  * - MOUNT_NOCOMPORT     001      Comport Not available
      //  * - MOUNT_COMERROR      003      COM Timeout Error
      //  * - MOUNT_MOTORBUSY     004      Motor still busy
      //  * - MOUNT_NONSTANDARD   005      Mount Initialized on non-standard parameters
      //  * - MOUNT_MOUNTBUSY     010      Cannot execute command at the current state
      //  * - MOUNT_MOTORERROR    011      Motor not initialized
      //  * - MOUNT_MOTORINACTIVE 200      Motor coils not active
      //  * - MOUNT_BADPARAM      999      Invalid parameter
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

   }
}
