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

      private const int DRIVER_VERSION = 0x206;

      private const int EQMOUNT = 1;         // EQG Protocol 
      private const int AUTO_DETECT = 0;     // Detected Current Mount

      private const int RA_AUX_ENCODER = 3;
      private const int DEC_AUX_ENCODER = 4;

      private const int POSITIVE = 0;
      private const int NEGATIVE = 1;

      private const int NO_PARAMS = 0;





      //private const char CR = (char)0x0D;    // Command terminator
      //private const char LF = (char)0x0A;    // Command terminator





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
      #endregion



      /// <summary>
      /// Translates the internal comms error to the dll error
      /// </summary>
      /// <param name="commandError">Error returned from EQ_SendCommand</param>
      /// <returns>Mount error</returns>
      private int EQ_GetMountError(int commandError)
      {
         //todo case statement
         //todo The order is important here because the individual errors
         //todo  change the return value of the overall error - Constants.MOUNT_COMERROR

         // Convert EQ Mount errors to dll error return value
         if (commandError == Constants.EQ_COMTIMEOUT) {
            return Constants.MOUNT_COMERROR;
         }
         if ((commandError & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return Constants.MOUNT_COMERROR;
         }
         if (commandError == Constants.EQ_MOUNTBUSY) {
            return Constants.MOUNT_MOUNTBUSY;
         }
         if (commandError == Constants.EQ_NOMOUNT) {
            return Constants.MOUNT_MOTORERROR;
         }
         if (commandError == Constants.EQ_PPECERROR) {
            return Constants.MOUNT_GENERALERROR;
         }
         if (commandError != Constants.EQ_OK) {
            return Constants.MOUNT_COMERROR;
         }
         return Constants.EQ_OK;
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
         System.Diagnostics.Debug.WriteLine(string.Format("EQ_SendGCode({0}, {1}, {2}, {3}, {4})", axisId, hemisphere, mode, direction, speed));
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
         if (EQ_SendCommand(axisId, 'G', ch, 2) != Constants.EQ_OK) {
            return (Constants.EQ_COMTIMEOUT);
         }

         return Constants.EQ_OK;

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
         GuideRateOffset[0] = 0;            // RA Offset
         GuideRateOffset[1] = 0;            // DEC Offset

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
      ///	- Constants.MOUNT_SUCCESS		     000		 Success (new connection)
      ///	- Constants.MOUNT_NOCOMPORT		     001		 COM port not available
      ///	- Constants.MOUNT_COMCONNECTED	     002		 Mount already connected (success)
      ///	- Constants.MOUNT_COMERROR		     003		 COM Timeout Error
      ///	- Constants.MOUNT_MOTORBUSY		     004		 Motor still busy
      ///	- Constants.MOUNT_NONSTANDARD	     005		 Mount Initialized on non-standard parameters
      ///	- Constants.MOUNT_MOUNTBUSY		     010		 Cannot execute command at the current state
      ///	- Constants.MOUNT_MOTORERROR	     011		 Motor not initialized
      ///	- Constants.MOUNT_MOTORINACTIVE	  200		 Motor coils not active
      ///	-  Constants.MOUNT_BADPARAM		     999		 Invalid parameter
      /// </returns>
      public int EQ_Init(string comportname, int baud, int timeout, int retry)
      {
         int result;
         if (MountActive) {
            return Constants.MOUNT_COMCONNECTED;
         }

         if ((timeout == 0) || (timeout > 50000)) {
            return  Constants.MOUNT_BADPARAM;
         }

         if (retry > 100) {
            return  Constants.MOUNT_BADPARAM;
         }

         lock (lockObject) {
            try {
               result = Constants.MOUNT_SUCCESS; ;
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

                  LowSpeedSlewRate[RAMotor] = ((double)StepTimerFreq[RAMotor] / ((double)GridPerRevolution[RAMotor] / Constants.SECONDS_PER_SIDERIAL_DAY));
                  LowSpeedSlewRate[DECMotor] = ((double)StepTimerFreq[DECMotor] / ((double)GridPerRevolution[DECMotor] / Constants.SECONDS_PER_SIDERIAL_DAY));
                  HighSpeedSlewRate[RAMotor] = ((double)HighSpeedRatio[RAMotor] * ((double)StepTimerFreq[RAMotor] / ((double)GridPerRevolution[RAMotor] / Constants.SECONDS_PER_SIDERIAL_DAY)));
                  HighSpeedSlewRate[DECMotor] = ((double)HighSpeedRatio[DECMotor] * ((double)StepTimerFreq[DECMotor] / ((double)GridPerRevolution[DECMotor] / Constants.SECONDS_PER_SIDERIAL_DAY)));

                  MountRate = LowSpeedSlewRate[0];    // Default to SIDEREAL

                  result = Constants.MOUNT_SUCCESS;

                  #endregion
               }
               else {
                  result = Constants.MOUNT_COMCONNECTED;
               }

               Interlocked.Increment(ref openConnections);
            }
            catch {
               result = Constants.MOUNT_COMERROR;
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
            return Constants.MOUNT_NOCOMPORT;
         }

         MountActive = false;    //Set Mount to inactive state
         return Constants.MOUNT_SUCCESS;
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
            return Constants.MOUNT_NOCOMPORT;
         }

         switch (axisId) {
            case AxisId.Axis1_RA:
            case AxisId.Axis2_DEC:
               // Stop Motor	
               i = EQ_SendCommand(axisId, 'K', 0, NO_PARAMS);
               if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                  return EQ_GetMountError(i);
               }

               // now wait for motor to stop
               do {
                  // Send Command
                  i = EQ_SendCommand(axisId, 'f', 0, NO_PARAMS);
                  if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                     return EQ_GetMountError(i);
                  }

                  // Return extended status
                  if ((i & 0x100) != 0x100) {
                     return Constants.MOUNT_MOTORINACTIVE;                 // Motor not initialized
                  }
               }
               while ((i & 0x01) == 0x01);
               break;
            case AxisId.Both_Axes:
               // stop RA motor
               i = EQ_SendCommand(AxisId.Axis1_RA, 'K', 0, NO_PARAMS);
               if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                  return EQ_GetMountError(i);               // Check errors, return "dll_error" value
               }

               // stop DEC motor
               i = EQ_SendCommand(AxisId.Axis2_DEC, 'K', 0, NO_PARAMS);
               if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                  return EQ_GetMountError(i);               // Check errors, return "dll_error" value
               }

               // now wait for motor to stop
               do {
                  // Send Command
                  i = EQ_SendCommand(AxisId.Axis1_RA, 'f', 0, NO_PARAMS);
                  if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                     return EQ_GetMountError(i);                  // Check errors, return "dll_error" value
                  }

                  // Return extended status
                  if ((i & 0x100) != 0x100) {
                     return Constants.MOUNT_MOTORINACTIVE;                 // Motor not initialized
                  }
               }
               while ((i & 0x01) == 0x01);

               do {
                  // Send Command
                  i = EQ_SendCommand(AxisId.Axis2_DEC, 'f', 0, NO_PARAMS);
                  if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                     return EQ_GetMountError(i);                  // Check errors, return "dll_error" value
                  }

                  // Return extended status
                  if ((i & 0x100) != 0x100) {
                     return Constants.MOUNT_MOTORINACTIVE;                 // Motor not initialized
                  }
               }
               while ((i & 0x01) == 0x01);
               break;
            default:
               return  Constants.MOUNT_BADPARAM;
         }
         return Constants.MOUNT_SUCCESS;

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
            return Constants.MOUNT_NOCOMPORT;
         }

         // Check Parameters
         if (!((axisId == AxisId.Axis1_RA) || (axisId == AxisId.Axis2_DEC))) {
            return  Constants.MOUNT_BADPARAM;
         }


         // Send Command
         response = EQ_SendCommand(axisId, 'f', 0, NO_PARAMS);
         if ((response & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
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
            return Constants.MOUNT_MOTORINACTIVE;  // Motor not initialized
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
            return Constants.MOUNT_NOCOMPORT;
         }

         // Check Parameters
         if (!((axisId == AxisId.Axis1_RA) || (axisId == AxisId.Axis2_DEC))) {
            return  Constants.MOUNT_BADPARAM;
         }

         // Check Motor Status first
         i = EQ_GetMotorStatus(axisId);
         if ((i >= Constants.MOUNT_MOTORINACTIVE) || (i < 0x80)) {
            //we have an error code 
            return i;
         }
         else {
            if ((i & 0x90) != 0x80) {
               // motor is moving already - can't do a goto if one is in progress.
               return Constants.MOUNT_MOTORBUSY;
            }
         }


         // Make sure motor is stopped
         i = EQ_MotorStop(axisId);
         if (i != Constants.MOUNT_SUCCESS) {
            return i;
         }

         // Set the motor hemisphere, mode, direction and speed
         i = EQ_SendGCode(axisId, hemisphere, MountMode.Goto, direction, MountSpeed.HighSpeed);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);               // Check errors, return "dll_error" value
         }

         // Set the mount relative target
         i = EQ_SendCommand(axisId, 'H', steps, 6);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);               // Check errors, return "dll_error" value
         }

         // Set the mount deceleration point
         // ### AJ  motor card doesnt use this????   
         j = stepSlowDown;                      // Stepper Motor Deceleration point
         i = EQ_SendCommand(axisId, 'M', j, 6);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);               // Check errors, return "dll_error" value
         }

         // Start the motor
         i = EQ_SendCommand(axisId, 'J', 0, NO_PARAMS);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);               // Check errors, return "dll_error" value
         }

         return Constants.MOUNT_SUCCESS;
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
            return Constants.MOUNT_NOCOMPORT;
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
               return Constants.MOUNT_RARUNNING;
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
               return Constants.MOUNT_DECRUNNING;
            }
         }

         // Set RA
         response = EQ_SendCommand(AxisId.Axis1_RA, 'E', RA_val, 6);
         if ((response & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         // Set DEC
         response = EQ_SendCommand(AxisId.Axis2_DEC, 'E', DEC_val, 6);
         if ((response & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         // Confirm RA
         response = EQ_SendCommand(AxisId.Axis1_RA, 'j', 0, NO_PARAMS);
         if ((response & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         // Confirm DEC
         response = EQ_SendCommand(AxisId.Axis2_DEC, 'j', 0, NO_PARAMS);
         if ((response & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         // Activate RA  Motor
         response = EQ_SendCommand(AxisId.Axis1_RA, 'F', 0, NO_PARAMS);
         if ((response & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         // Activate DEC Motor
         response = EQ_SendCommand(AxisId.Axis2_DEC, 'F', 0, NO_PARAMS);
         if ((response & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(response);
         }

         return Constants.MOUNT_SUCCESS;
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
            return Constants.EQ_ERROR;  // can't return Constants.MOUNT_NOCOMPORT as 001 is a potentially valide motor value
         }

         switch (axisId) {
            case AxisId.Axis1_RA:
            case AxisId.Axis2_DEC:
               // Get mount position
               // Mount position returns EQ_COMTIMEOUT because MotorValues range to 0xFFFFFF (16,777,215)
               i = EQ_SendCommand(axisId, 'j', 0, NO_PARAMS);
               if (i == Constants.EQ_COMTIMEOUT) {
                  return Constants.EQ_COMTIMEOUT;
               }
               return i;
            case AxisId.Aux_RA_Encoder:
               if (HasEncoder[0] == false) {
                  // mount doesn't have aux encoders
                  return Constants.EQ_INVALID;
               }
               // Get mount position
               // Mount position returns EQ_COMTIMEOUT because MotorValues range to 0xFFFFFF (16,777,215)
               i = EQ_SendCommand(AxisId.Axis1_RA, 'd', 0, NO_PARAMS);
               if (i == Constants.EQ_COMTIMEOUT) {
                  return Constants.EQ_COMTIMEOUT;
               }
               return i;
            case AxisId.Aux_DEC_Encoder:
               if (HasEncoder[1] == false) {
                  // mount doesn't have aux encoders
                  return Constants.EQ_INVALID;
               }
               // Get mount position
               // Mount position returns EQ_COMTIMEOUT because MotorValues range to 0xFFFFFF (16,777,215)
               i = EQ_SendCommand(AxisId.Axis2_DEC, 'd', 0, NO_PARAMS);
               if (i == Constants.EQ_COMTIMEOUT) {
                  return Constants.EQ_COMTIMEOUT;
               }
               return i;
         }
         return Constants.EQ_INVALID; // can't return  Constants.MOUNT_BADPARAM as 999 is a potentially valide motor value
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
            return Constants.MOUNT_NOCOMPORT;
         }

         // Check parameters
         if (!((axisId == AxisId.Axis1_RA) || (axisId == AxisId.Axis2_DEC))) {
            return  Constants.MOUNT_BADPARAM;
         }


         // Send Set Reference Mount position 
         i = EQ_SendCommand(axisId, 'E', motorValue, 6);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);         // Check errors, return "dll_error" value
         }

         return Constants.MOUNT_SUCCESS;
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
            return Constants.MOUNT_NOCOMPORT;
         }


         // Check parameters
         if (axisId == AxisId.Both_Axes) {
            return  Constants.MOUNT_BADPARAM;
         }
         if ((rate < 1) || (rate > 800)) {
            return  Constants.MOUNT_BADPARAM;
         }


         // Stop motor
         i = EQ_MotorStop(axisId);
         if (i != Core.Constants.MOUNT_SUCCESS) {
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
            if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
               return EQ_GetMountError(i);         // Check errors, return "dll_error" value
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
            if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
               return EQ_GetMountError(i);         // Check errors, return "dll_error" value
            }

            if (axisId == AxisId.Axis1_RA) {
               k = HighSpeedSlewRate[0] / (double)rate;
               j = (int)k;                 // Round to nearest integer - Ignore compile warning
               if (rate < 20) {
                  j = ((j + GuideRateOffset[0]) & 0xffffff);
               }
               else {
                  j = j & 0xffffff;
               }
            }
            else {
               k = HighSpeedSlewRate[1] / (double)rate;
               j = (int)k;           // Round to nearest integer - Ignore compile warning
               if (rate < 20) {
                  j = ((j + GuideRateOffset[1]) & 0xffffff);
               }
               else {
                  j = j & 0xffffff;
               }
            }
         }


         // Send Speed Command
         i = EQ_SendCommand(axisId, 'I', j, 6);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);         // Check errors, return "dll_error" value
         }

         // Send Go Command
         i = EQ_SendCommand(axisId, 'J', 0, NO_PARAMS);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);         // Check errors, return "dll_error" value
         }

         return Constants.MOUNT_SUCCESS;
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
            return Constants.MOUNT_NOCOMPORT;
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
               return  Constants.MOUNT_BADPARAM;

         }

         // Adjust for offset
         j = (j + GuideRateOffset[0]) & 0xffffff;

         i = EQ_MotorStop(AxisId.Axis1_RA);
         if (i != Core.Constants.MOUNT_SUCCESS) {
            return i;
         }

         // Set the motor hemisphere, mode, direction and speed
         i = EQ_SendGCode(RAMotor, hemisphere, MountMode.Slew, direction, MountSpeed.LowSpeed);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);               // Check errors, return "dll_error" value
         }

         // Send I Command
         i = EQ_SendCommand(RAMotor, 'I', j, 6);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);               // Check errors, return "dll_error" value
         }

         // Start RA Motor
         i = EQ_SendCommand(RAMotor, 'J', 0, NO_PARAMS);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);               // Check errors, return "dll_error" value
         }

         return Constants.MOUNT_SUCCESS;
      }


      /// <summary>
      /// Adjust the RA/DEC rotation trackrate based on a given speed adjustment rate
      /// </summary>
      /// <param name="axisId"></param>
      /// <param name="trackRate"></param>
      /// <param name="guideRate">Guide rate
      ///                     00 - No Change
      ///                     01 - 10%
      ///                     02 - 20%
      ///                     03 - 30%
      ///                     04 - 40%
      ///                     05 - 50%
      ///                     06 - 60%
      ///                     07 - 70%
      ///                     08 - 80%
      ///                     09 - 90%</param>
      /// <param name="guideDirection">Guide dirextion</param>
      /// <param name="hemisphere">Hemisphere (used for DEC Motor control)</param>
      /// <param name="direction">Direction (used for DEC Motor control)</param>
      /// <returns>000 - Success
      ///                     001 - Comport Not available
      ///                     003 - COM Timeout Error
      ///                     004 - Motor still busy
      ///                     010 - Cannot execute command at the current stepper controller state
      ///                     011 - Motor not initialized
      ///                     999 - Invalid Parameter</returns>
      public int EQ_SendGuideRate(AxisId axisId, MountTracking trackRate, int guideRate, AxisDirection guideDirection, HemisphereOption hemisphere, AxisDirection direction)
      {
         int i, newrate;
         double k, j;

         // Check mount	
         if (!MountActive) {
            return Constants.MOUNT_NOCOMPORT;
         }

         // Check parameters
         if (!((axisId == AxisId.Axis1_RA) || (axisId == AxisId.Axis2_DEC))) {
            return  Constants.MOUNT_BADPARAM;
         }
         if ((guideRate < 0) || (guideRate > 9)) {
            return  Constants.MOUNT_BADPARAM;
         }

         // Update Tracking Rate
         switch (trackRate) {
            case MountTracking.Solar:  // Solar
               MountTracking = trackRate;
               k = (double)(LowSpeedSlewRate[0] * 1.0016129032258064516129032258065);
               break;

            case MountTracking.Lunar:  // Lunar
               MountTracking = trackRate;
               k = (double)(LowSpeedSlewRate[0] * 1.0370967741935483870967741935484);
               break;

            case MountTracking.Sidereal: // Sidereal
               MountTracking = trackRate;
               k = LowSpeedSlewRate[0];
               break;

            default:
               return  Constants.MOUNT_BADPARAM;
         }

         // Update GUIDING Rate
         if (guideRate > 0) {
            MountTracking = MountTracking.Custom;   // For the other mounts

            j = (double)(0.1 * guideRate);
            if (axisId == AxisId.Axis1_RA) {
               if (guideDirection == AxisDirection.Forward) {
                  newrate = (int)(k / (1 + j));
               }
               else {
                  newrate = (int)(k / (1 - j));
               }
            }
            else {
               newrate = (int)(k / j);
            }
         }
         else {
            newrate = (int)(k);
         }

         if (axisId == RAMotor) {
            newrate = ((newrate + GuideRateOffset[0]) & 0xffffff);
         }
         else {
            newrate = ((newrate + GuideRateOffset[1]) & 0xffffff);
         }

         if (newrate != 0) {
            MountRate = newrate;      // As long as it is not zero!
         }

         // Send Command
         if (axisId == AxisId.Axis1_RA) {
            // RA  Motor
            i = EQ_SendCommand(axisId, 'I', newrate, 6);
            if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
               return EQ_GetMountError(i);   // Check errors, return "dll_error" value
            }
         }
         else {
            // DEC Motor
            // Stop DEC motor
            i = EQ_MotorStop(AxisId.Axis2_DEC);
            if (i != Core.Constants.MOUNT_SUCCESS) {
               return i;
            }

            // Set the direction
            AxisDirection decDirection = direction;          // surely this is where this belongs!
            if (guideDirection == AxisDirection.Reverse) {
               // i = direction;		// and surely this was wrong!
               if (decDirection == AxisDirection.Reverse) {
                  decDirection = AxisDirection.Forward;
               }
               else {
                  decDirection = AxisDirection.Reverse;
               }
            }

            // Set the motor hemisphere, mode, direction and speed
            i = EQ_SendGCode(AxisId.Axis2_DEC, hemisphere, MountMode.Slew, decDirection, MountSpeed.LowSpeed);
            if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
               return EQ_GetMountError(i);               // Check errors, return "dll_error" value
            }

            // Set the DEC motor speed
            i = EQ_SendCommand(AxisId.Axis2_DEC, 'I', newrate, 6);
            if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
               return EQ_GetMountError(i);   // Check errors, return "dll_error" value
            }

            // Start the DEC motor
            i = EQ_SendCommand(AxisId.Axis2_DEC, 'J', 0, NO_PARAMS);
            if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
               return EQ_GetMountError(i);   // Check errors, return "dll_error" value
            }
         }
         return Constants.MOUNT_SUCCESS;
      }


      /// <summary>
      /// Adjust the RA/DEC rotation trackrate based on a given speed adjustment offset
      /// </summary>
      /// <param name="axisId"></param>
      /// <param name="trackRate">Siderial, Lunar or Solar</param>
      /// <param name="trackOffset">0 - 400</param>
      /// <param name="trackDirection"></param>
      /// <param name="hemisphere">Hemisphere (used for DEC Motor)</param>
      /// <param name="direction">Direction (used for DEC Motor)</param>
      /// <returns>
      ///                     000 - Success
      ///                     001 - Comport Not available
      ///                     003 - COM Timeout Error
      ///                     004 - Motor still busy
      ///                     010 - Cannot Execute command at the current state
      ///                     011 - Motor not initialized
      ///                     999 - Invalid Parameter
      /// </returns>
      public int EQ_SendCustomTrackRate(AxisId axisId, MountTracking trackRate, int trackOffset, AxisDirection trackDirection, HemisphereOption hemisphere, AxisDirection direction)
      {
         int i, j, newrate;

         // Check Mount	
         if (!MountActive) {
            return Constants.MOUNT_NOCOMPORT;
         }

         // Check Parameters	
         if (!((axisId == AxisId.Axis1_RA) || (axisId == AxisId.Axis2_DEC))) {
            return  Constants.MOUNT_BADPARAM;
         }
         if ((trackOffset < 0) || (trackOffset > 400)) {
            return  Constants.MOUNT_BADPARAM;
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
               j = (int)(LowSpeedSlewRate[0]);
               break;
            default:
               return  Constants.MOUNT_BADPARAM;
         }

         if (trackOffset != 0) {
            MountTracking = MountTracking.Custom;
            if (trackDirection == AxisDirection.Forward) {
               newrate = j - trackOffset;
            }
            else {
               newrate = j + trackOffset;
            }
         }
         else {
            newrate = j;
         }
         if (axisId == RAMotor) {
            newrate = ((newrate + GuideRateOffset[0]) & 0xffffff);
         }
         else {
            newrate = ((newrate + GuideRateOffset[1]) & 0xffffff);
         }
         if (newrate != 0) {
            MountRate = newrate;      // As long as it is not zero!
         }

         // Set the direction
         AxisDirection axisDirection = direction;
         if (trackDirection == AxisDirection.Reverse) {
            if (direction == AxisDirection.Reverse) {
               axisDirection = AxisDirection.Forward;
            }
            else {
               axisDirection = AxisDirection.Reverse;
            }
         }

         // Stop the motors if new custom rate
         if (MountTracking == MountTracking.Custom) {
            i = EQ_MotorStop(axisId);
            if (i != Core.Constants.MOUNT_SUCCESS) {
               return i;
            }
         }

         // Set the motor hemisphere, mode, direction and speed
         i = EQ_SendGCode(axisId, hemisphere, MountMode.Slew, axisDirection, MountSpeed.LowSpeed);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);               // Check errors, return "dll_error" value
         }

         // Set the motor speed
         i = EQ_SendCommand(axisId, 'I', newrate, 6);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);               // Check errors, return "dll_error" value
         }

         // Start the motor
         i = EQ_SendCommand(axisId, 'J', 0, NO_PARAMS);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);               // Check errors, return "dll_error" value// Start the motor
         }

         return Constants.MOUNT_SUCCESS;
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
      public int EQ_SetCustomTrackRate(AxisId axisId, TrackMode trackMode, int trackOffset, MountSpeed trackBase, HemisphereOption hemisphere, AxisDirection direction)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("EQ_SetCustomTrackRate({0}, {1}, {2}, {3}, {4}, {5})", axisId, trackMode, trackOffset, trackBase, hemisphere, direction));
         int i, newrate;

         // Check Mount
         // Check Mount	
         if (!MountActive) {
            return Constants.MOUNT_NOCOMPORT;
         }

         // Check Parameters	
         if (!((axisId == AxisId.Axis1_RA) || (axisId == AxisId.Axis2_DEC))) {
            return Constants.EQ_INVALID;
         }

         if (trackOffset < 30000) {
            return  Constants.MOUNT_BADPARAM;
         }

         newrate = trackOffset - 30000;
         if (newrate != 0) {
            MountRate = newrate;      // As long as it is not zero!
         }
         else {
            MountRate = 1;
         }

         if (trackBase == MountSpeed.LowSpeed) {
            if (axisId == AxisId.Axis1_RA) {
               newrate = newrate + GuideRateOffset[0];
            }
            else {
               newrate = newrate + GuideRateOffset[1];
            }
         }

         newrate = newrate & 0xffffff;

         MountTracking = MountTracking.Custom;

         if (trackMode == TrackMode.Initial) {

            // Stop the motor
            i = EQ_MotorStop(axisId);
            if (i != Core.Constants.MOUNT_SUCCESS) {
               return i;
            }

            // Set the motor hemisphere, mode, direction and speed	
            i = EQ_SendGCode(axisId, hemisphere, MountMode.Slew, direction, trackBase);
            if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
               return EQ_GetMountError(i);   // Check errors, return "MOUNT_error" value
            }
         }

         // Set the motor speed
         i = EQ_SendCommand(axisId, 'I', newrate, 6);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);   // Check errors, return "MOUNT_error" value
         }

         if (trackMode == TrackMode.Initial) {

            // Start the motor
            i = EQ_SendCommand(axisId, 'J', 0, NO_PARAMS);
            if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
               return EQ_GetMountError(i);   // Check errors, return "MOUNT_error" value
            }
         }
         return Constants.MOUNT_SUCCESS;
      }


      /// <summary>
      /// Get microstep count to complete a 360 degree revolution
      /// </summary>
      /// <param name="axisId"></param>
      /// <returns>
      ///	- EQ_ERROR				0x1000000	Mount Not Available
      ///	- EQ_INVALID			0x3000000	Invalid Motor ID
      ///	- 0 -   16777215		Valid Count Values
      ///	- 0 - 0x00FFFFFF		Valid Count Values
      /// </returns>
      public int EQ_GetTotal360microstep(AxisId axisId)
      {
         if (!MountActive) {
            return Constants.EQ_ERROR;
         }

         switch (axisId) {
            case AxisId.Axis1_RA:
            case AxisId.Axis2_DEC:
               return GridPerRevolution[(int)axisId];
            default:
               return Constants.EQ_INVALID;
         }
      }

      /// <summary>
      /// Attempts to detect a mount by sending version commands
      /// </summary>
      /// <returns>
      ///		301 - EQG Series mount
      ///		302 - Nexstar Series mount
      ///		998 - No valid mount detected
      /// </returns>
      public int EQ_GetMountType()
      {
         int i;

         if (!MountActive) {
            return Constants.MOUNT_NOCOMPORT;
         }


         // Set default value
         MountType = MountType.EqMount;

         // Check EQMOUNT
         i = EQ_SendCommand(AxisId.Axis1_RA, 'e', 0, NO_PARAMS);
         if ((i & Constants.EQ_ERROR) != Constants.EQ_ERROR)       // No errors so mount returned firmware version OK
         {
            MountType = MountType.EqMount;     // So its an EQG mount
            return Constants.MOUNT_EQMOUNT;
         }

         // Else bad mount
         return Constants.MOUNT_BADMOUNT;
      }



      /// <summary>
      /// Get Mount//s Firmware version
      /// </summary>
      /// <returns>
      ///   - Mount//s Firmware Version
      ///   - 0x1000000 - Mount Not available
      /// </returns>
      public int EQ_GetMountVersion()
      {
         if (!MountActive) {
            return Constants.EQ_ERROR;
         }
         return MCVersion;
      }


      /// <summary>
      /// Get Mount's Status
      /// </summary>
      /// <returns>
      ///      000 - Not Connected
      ///      001 - Connected
      /// </returns>
      public int EQ_GetMountStatus()
      {
         if (MountActive) {
            return Constants.MOUNT_CONNECTED;
         }
         else {
            return Constants.MOUNT_NOTCONNECTED;
         }
      }


      /// <summary>
      /// Get Driver Version
      /// </summary>
      /// <returns>Driver Version</returns>
      public int EQ_DriverVersion()
      {
         return DRIVER_VERSION;
      }


      /// <summary>
      /// Set the mount's autoguiderport rate
      /// </summary>
      /// <param name="axisId">RA or DEC</param>
      /// <param name="guideportRate">Guide port rate</param>
      /// <returns>
      ///     000 - Success
      ///     001 - Comport Not available
      ///     003 - COM Timeout Error
      ///     999 - Invalid Parameter
      /// </returns>
      public int EQ_SetAutoguiderPortRate(AxisId axisId, AutoguiderPortRate guideportRate)
      {
         int i;
         int r;

         // Check Mount
         if (!MountActive) {
            return Constants.MOUNT_NOCOMPORT;
         }

         // Check Parameters	
         if (!((axisId == AxisId.Axis1_RA) || (axisId == AxisId.Axis2_DEC))) {
            return  Constants.MOUNT_BADPARAM;
         }


         switch (guideportRate) {
            case AutoguiderPortRate.OneTimesX:                 // 1x
            case AutoguiderPortRate.Point75Times:              //0.75x
            case AutoguiderPortRate.Point50Times:              //0.5x
            case AutoguiderPortRate.Point25Times:              //0.25x
            case AutoguiderPortRate.Point125Times:             //0.125x
               r = (int)guideportRate;
               break;
            default:
               return  Constants.MOUNT_BADPARAM;
         }
         i = EQ_SendCommand(axisId, 'P', r, 1);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);         // Check errors, return "dll_error" value
         }

         return Constants.MOUNT_SUCCESS;
      }


      /// <summary>
      /// Set the mount's offset
      /// </summary>
      /// <param name="axisId">RA/DEC</param>
      /// <param name="offset">Guiderate offset</param>
      /// <returns>
      ///	- Constants.MOUNT_SUCCESS		000		 Success
      ///	- Constants.MOUNT_NOCOMPORT   001		 Comport Not available
      ///	-  Constants.MOUNT_BADPARAM		999		 Invalid parameter
      /// </returns>
      public int EQ_SetOffset(AxisId axisId, AutoguiderPortRate offset)
      {
         if (!MountActive) {
            return Constants.MOUNT_NOCOMPORT;
         }
         switch (axisId) {
            case AxisId.Axis1_RA:
            case AxisId.Axis2_DEC:
               GuideRateOffset[(int)axisId] = (int)offset;
               break;
            default:
               return  Constants.MOUNT_BADPARAM;
         }
         return Constants.MOUNT_SUCCESS;
      }


      // Function name    : EQ_GP()
      // Description      : Get Mount Parameters
      // Return type      : Double - parameter value
      // Public Declare Function EQ_GP Lib "EQCONTRL" (ByVal motor_id As Long, ByVal p_id As Long) As Long

      /// <summary>
      /// Get Mount Parameters
      /// </summary>
      /// <param name="axisId">Axis Id (RA/DEC or using Aux_RA_Encoder for MountActive</param>
      /// <param name="parameterId">Parameter index	(base 10000)
      ///		0 - Firmware Version
      ///		1 - Motor Steps per full revolution
      ///		2 - Sidereal rate factor
      ///		3 - Motor High Speed slew scale factor
      ///		4 - Low speed slew rate
      ///		5 - High speed slew rate
      ///		6 - Steps per worm turn
      ///		7 - Track rate offset
      /// </param>
      /// <returns>Values stored in parameter or  Constants.MOUNT_BADPARAM</returns>
      public int EQ_GetMountParameter(AxisId axisId, int parameterId)
      {
         int i, tmp;
         int axis;

         // Check Parameters	
         if (parameterId < 10000) {
            return  Constants.MOUNT_BADPARAM;
         }
         axis = (int)axisId;

         i = parameterId - 10000;

         if (axisId == AxisId.Axis1_RA || axisId == AxisId.Axis2_DEC) {
            switch (i) {
               case 1:
                  return GridPerRevolution[axis];
               case 2:
                  return StepTimerFreq[axis];
               case 3:
                  return HighSpeedRatio[axis];
               case 4:
                  return (int)(LowSpeedSlewRate[axis]);
               case 5:
                  return (int)(HighSpeedSlewRate[axis]);
               case 6:
                  return PESteps[axis];
               case 7:
                  return GuideRateOffset[axis];
               case 8:
                  return MountParameters[axis];
               case 9:
                  if (axisId == AxisId.Axis1_RA) {
                     tmp = 0;
                     if (HasSnap[0]) { tmp |= 0x01; }
                     if (HasSnap[1]) { tmp |= 0x02; }
                     if (HasPPEC[0]) { tmp |= 0x04; }
                     if (HasPPEC[1]) { tmp |= 0x08; }
                     if (HasEncoder[0]) { tmp |= 0x10; }
                     if (HasEncoder[1]) { tmp |= 0x20; }
                     if (HasHalfCurrent[0]) { tmp |= 0x40; }
                     if (HasHalfCurrent[1]) { tmp |= 0x80; }
                     if (HasPolarscopeLED) { tmp |= 0x010000; }
                     if (HasHomeSensor) { tmp |= 0x020000; }
                     return tmp;
                  }
                  else {
                     return  Constants.MOUNT_BADPARAM;
                  }
               case 10:
                  // get home position index data
                  tmp = EQ_SendCommand(axisId, 'q', 0, 6);
                  if ((tmp & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                     return EQ_GetMountError(tmp);
                  }
                  return tmp;

               case 11:
                  tmp = EQ_SendCommand(axisId, 'q', 1, 6);
                  if ((tmp & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                     return EQ_GetMountError(tmp);
                  }
                  // assume pec is off and not training
                  i = 0x00000000;
                  if ((tmp & 0x00000020) == 0x00000020) {
                     //	PEC on						= 0x00000020
                     i = 0x00000002;
                  }
                  else {
                     // PPEC is off
                     if ((tmp & 0x00000010) == 0x00000010) {
                        //	PEC training in progress	= 0x00000010
                        i = 0x00000001;
                     }
                  }
                  return i;


               default:
                  return MCVersion;
            }
         }
         else if (axisId == AxisId.Aux_RA_Encoder) {
            return (MountActive ? 1 : 0);
         }
         else {
            return  Constants.MOUNT_BADPARAM;
         }
      }

      /// <summary>
      /// Set a mount parameter
      /// </summary>
      /// <param name="motorId">Axis ID (RA/DEC or both)</param>
      /// <param name="parameterId">Parameter index (base 10000)
      /// 			1 - SNAP Port
      ///  			2 - PPEC Train
      ///  			3 - PPEC
      ///  			4 - Auxillary Encoder
      ///  			5 - Reset Encoder Dataum
      ///  			6 - Polar Scope LED Brightness
      ///	   	7 - Slew Rate threshold
      /// </param>
      /// <param name="value"></param>
      /// <returns></returns>
      public int EQ_SetMountParameter(AxisId axisId, int parameterId, int value)
      {
         int result;

         // Check Parameter id
         if (parameterId < 10000) {
            return  Constants.MOUNT_BADPARAM;
         }

         parameterId -= 10000;

         switch (parameterId) {

            case 1:
               // snap port
               switch (axisId) {
                  case AxisId.Axis1_RA:
                     if (!HasSnap[0]) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  case AxisId.Axis2_DEC:
                     if (!HasSnap[1]) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  case AxisId.Both_Axes:
                     if ((!HasSnap[0]) || (!HasSnap[1])) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  default:
                     return  Constants.MOUNT_BADPARAM;
               }

               if ((value < 0) || (value > 1)) {
                  return  Constants.MOUNT_BADPARAM;
               }

               switch (axisId) {
                  case AxisId.Axis1_RA:
                  case AxisId.Axis2_DEC:
                     result = EQ_SendCommand(axisId, 'O', value, 1);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     break;
                  case AxisId.Both_Axes:
                     result = EQ_SendCommand(AxisId.Axis1_RA, 'O', value, 1);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     result = EQ_SendCommand(AxisId.Axis2_DEC, 'O', value, 1);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     break;
               }
               break;

            case 2:
               // Start/Stop PPEC Train
               switch (axisId) {
                  case AxisId.Axis1_RA:
                     if (!HasPPEC[0]) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  case AxisId.Axis2_DEC:
                     if (!HasPPEC[1]) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  case AxisId.Both_Axes:
                     if ((!HasPPEC[0]) || (!HasPPEC[1])) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  default:
                     return  Constants.MOUNT_BADPARAM;
               }
               switch (value) {
                  case 0:
                     value = 1;
                     break;
                  case 1:
                     value = 0;
                     break;
                  default:
                     return  Constants.MOUNT_BADPARAM;
               }
               switch (axisId) {
                  case AxisId.Axis1_RA:
                  case AxisId.Axis2_DEC:
                     // Start/stop PPEC train
                     result = EQ_SendCommand(axisId, 'W', value, 6);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     break;
                  case AxisId.Both_Axes:
                     result = EQ_SendCommand(RAMotor, 'W', value, 6);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     result = EQ_SendCommand(AxisId.Axis2_DEC, 'W', value, 6);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     break;
               }
               break;

            case 3:
               // Start/Stop PPEC
               switch (axisId) {
                  case AxisId.Axis1_RA:
                     if (!HasPPEC[0]) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  case AxisId.Axis2_DEC:
                     if (!HasPPEC[1]) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  case AxisId.Both_Axes:
                     if ((!HasPPEC[0]) || (!HasPPEC[1])) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  default:
                     return  Constants.MOUNT_BADPARAM;
                     break;
               }
               switch (value) {
                  case 0:
                     value = 3;
                     break;
                  case 1:
                     value = 2;
                     break;
                  default:
                     return  Constants.MOUNT_BADPARAM;
               }

               switch (axisId) {
                  case AxisId.Axis1_RA:
                  case AxisId.Axis2_DEC:
                     result = EQ_SendCommand(axisId, 'W', value, 6);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     break;
                  case AxisId.Both_Axes:
                     result = EQ_SendCommand(RAMotor, 'W', value, 6);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     result = EQ_SendCommand(AxisId.Axis2_DEC, 'W', value, 6);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     break;
               }
               break;

            case 4:
               // Enable/Disable Encoder
               switch (axisId) {
                  case AxisId.Axis1_RA:
                     if (!HasEncoder[0]) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  case AxisId.Axis2_DEC:
                     if (!HasEncoder[1]) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  case AxisId.Both_Axes:
                     if ((!HasEncoder[0]) || (!HasEncoder[1])) {
                        return Constants.MOUNT_GENERALERROR;
                     }
                     break;
                  default:
                     return  Constants.MOUNT_BADPARAM;
               }

               if (value < 0 || value > 1) {
                  return  Constants.MOUNT_BADPARAM;
               }
               if (value == 0) {
                  value = 4;
               }
               else {
                  value = 5;
               }
               switch (axisId) {
                  case AxisId.Axis1_RA:
                  case AxisId.Axis2_DEC:
                     result = EQ_SendCommand(axisId, 'W', value, 6);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     break;

                  case AxisId.Both_Axes:
                     result = EQ_SendCommand(RAMotor, 'W', value, 6);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     result = EQ_SendCommand(AxisId.Axis2_DEC, 'W', value, 6);
                     if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                        return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
                     }
                     break;
               }
               break;

            case 10:
               // Encoder reset datum
               if (!HasHomeSensor) {
                  return Constants.MOUNT_GENERALERROR;
               }

               result = EQ_SendCommand(axisId, 'W', 8, 6);
               if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                  return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
               }
               break;

            case 6:
               // Polar Scope LED brightness
               if (!HasPolarscopeLED) {
                  return Constants.MOUNT_GENERALERROR;
               }

               if ((value < 0) || (value > 255)) {
                  return  Constants.MOUNT_BADPARAM;
               }
               result = EQ_SendCommand(AxisId.Axis2_DEC, 'V', value, 2);
               if ((result & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
                  return (EQ_GetMountError(result)); // Check errors, return "dll_error" value
               }
               break;

            case 7:
               // Slew rate - Highspeed/Lowspeed threshold
               if ((value < 20) || (value > 800)) {
                  return  Constants.MOUNT_BADPARAM;
               }
               switch (axisId) {
                  case AxisId.Axis1_RA:
                     LowSpeedGotoMargin[0] = value;
                     break;
                  case AxisId.Axis2_DEC:
                     LowSpeedGotoMargin[1] = value;
                     break;
                  case AxisId.Both_Axes:
                     LowSpeedGotoMargin[0] = value;
                     LowSpeedGotoMargin[1] = value;
                     break;
                  default:
                     return  Constants.MOUNT_BADPARAM;
               }
               break;

            default:
               return  Constants.MOUNT_BADPARAM;

         }
         return Constants.MOUNT_SUCCESS;
      }



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
            return  Constants.MOUNT_BADPARAM;
         }
         System.Diagnostics.Debug.WriteLine(String.Format("EQ_SendCommand({0}, {1}, {2}, {3})", motorId, command, parameters, count));
         int response = Constants.EQ_OK;
         char[] hex_str = "0123456789ABCDEF     ".ToCharArray();   // Hexadecimal translation
         const int BufferSize = 20;
         StringBuilder sb = new StringBuilder(BufferSize);
         sb.Append(cStartChar_Out);
         sb.Append(command);
         sb.Append((motorId + 1).ToString());
         switch (count) {
            case 0:
               // Do nothing
               break;
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
               return Constants.EQ_INVALID;
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

         System.Diagnostics.Debug.WriteLine(string.Format("    -> Response: {0} (0x{0:X})", response));
         return response;
      }

      /// <summary>
      /// Guiderate activate
      /// </summary>
      /// <param name="axisId">RA/DEC</param>
      /// <param name="rate">rate arcsec/sec (0 to 12032.8536 arcsec/sec)</param>
      /// <param name="hemisphere"></param>
      /// <param name="direction"></param>
      /// <returns>
      ///	- DLL_SUCCESS		   000		 Success
      ///	- DLL_NOCOMPORT		001		 Comport Not available
      ///	- DLL_COMERROR		   003		 COM Timeout Error
      ///	- DLL_MOTORBUSY		004		 Motor still busy
      ///	- DLL_NONSTANDARD	   005		 Mount Initialized on non-standard parameters
      ///	- DLL_MOUNTBUSY		010		 Cannot execute command at the current state
      ///	- DLL_MOTORERROR	   011		 Motor not initialized
      ///	- DLL_MOTORINACTIVE	200		 Motor coils not active
      ///	- DLL_BADPARAM		   999		 Invalid parameter
      /// </returns>
      public int EQ_SetAxisRate(AxisId axisId, double rate, HemisphereOption hemisphere, AxisDirection direction)
      {
         int i;
         bool highspeed = false;
         bool trackmode = false;
         int SpeedInt;

         // Check Mount
         if (!MountActive) {
            return Constants.MOUNT_NOCOMPORT;
         }

         // Check Parameters	
         if (axisId > AxisId.Axis2_DEC) {
            return  Constants.MOUNT_BADPARAM;
         }


         if ((rate > Constants.MAX_RATE) || (rate < 0)) {
            return  Constants.MOUNT_BADPARAM - 3;
         }


         if (rate > -0.015 && rate < 0.015) {
            // stop motor
            i = EQ_MotorStop(axisId);
            return (i);
         }


         switch (axisId) {
            case AxisId.Axis1_RA:
               if (rate > (double)LowSpeedGotoMargin[0] * 15.041067) {
                  highspeed = true;
                  rate /= (double)HighSpeedRatio[0];
               }
               break;
            case AxisId.Axis2_DEC:
               if (rate > (double)LowSpeedGotoMargin[1] * 15.041067) {
                  highspeed = true;
                  rate /= (double)HighSpeedRatio[1];
               }
               break;
         }

         MountTracking = MountTracking.Custom;


         if (axisId == AxisId.Axis1_RA) {
            SpeedInt = (int)(15.041067 * (double)LowSpeedSlewRate[0] / rate);
            SpeedInt += GuideRateOffset[0];
         }
         else {
            SpeedInt = (int)(15.041067 * (double)LowSpeedSlewRate[1] / rate);
            SpeedInt += GuideRateOffset[1];
         }
         if (highspeed) {
            trackmode = true;
         }
         else {
            // if dirction has change or last speed was high
            trackmode = true;


            // Check Motor Status
            // Send Command
            i = EQ_SendCommand(axisId, 'f', 0, NO_PARAMS);
            if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
               return EQ_GetMountError(i);                  // Check errors, return "dll_error" value
            }

            // :fx=ABC[0D]  is actually returned from GetReply as an integer as #x0CAB
            //              "is energised" is given by C  ie  ( i & x100 )
            //              "is moving"    is given by B  ie  ( i & x001 )
            //              "direction"    is given by A  ie  ( i & x020 )
            //              "Goto/Slew"                       ( i & x010 )
            //              "Curr Rate"                       ( i & x040 )

            if ((i & 0x0100) == 0) {
               //motor not initialised
               return Constants.MOUNT_MOTORINACTIVE;
            }
            else {
               if ((i & 0x01) == 0x01) {
                  // motor is moving
                  if (direction == AxisDirection.Forward) {
                     if ((i & 0x20) == 0x20) {
                        trackmode = true;
                     }
                  }
                  else {
                     if ((i & 0x20) == 0x20) {
                        trackmode = true;
                     }
                  }
                  if ((i & 0x40) == 0x40) {
                     // currently moving at high speed
                     trackmode = true;
                  }
               }
               else {
                  // motor is stopped
                  trackmode = true;
               }
            }
         }


         if (trackmode) {

            // Stop the motor
            i = EQ_MotorStop(axisId);
            if (i != Core.Constants.MOUNT_SUCCESS) {
               return i;
            }

            MountSpeed trackbase = MountSpeed.LowSpeed;
            if (highspeed) {
               trackbase = MountSpeed.HighSpeed;
            }

            // Set the motor hemisphere, mode, direction and speed	
            i = EQ_SendGCode(axisId, hemisphere, MountMode.Slew, direction, trackbase);
            if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
               return EQ_GetMountError(i);   // Check errors, return "dll_error" value
            }
         }


         // Set the motor speed
         i = EQ_SendCommand(axisId, 'I', SpeedInt, 6);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return EQ_GetMountError(i);   // Check errors, return "dll_error" value
         }

         if (trackmode) {

            // Start the motor
            i = EQ_SendCommand(axisId, 'J', 0, NO_PARAMS);
            if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
               return EQ_GetMountError(i);   // Check errors, return "dll_error" value
            }
         }
         return Constants.MOUNT_SUCCESS;
      }



      /// <summary>
      /// Slew RA/DEC Motor
      /// </summary>
      /// <param name="axisId"></param>
      /// <param name="hemisphere"></param>
      /// <param name="direction"></param>
      /// <param name="angle"></param>
      /// <returns>
      ///	- DLL_SUCCESS		000		 Success
      ///	- DLL_NOCOMPORT		001		 Comport Not available
      ///	- DLL_COMERROR		003		 COM Timeout Error
      ///	- DLL_MOTORBUSY		004		 Motor still busy
      ///	- DLL_NONSTANDARD	005		 Mount Initialized on non-standard parameters
      ///	- DLL_MOUNTBUSY		010		 Cannot execute command at the current state
      ///	- DLL_MOTORERROR	011		 Motor not initialized
      ///	- DLL_MOTORINACTIVE	200		 Motor coils not active
      ///	- DLL_BADPARAM		999		 Invalid parameter
      /// </returns>
      public int EQ_MoveMotorAngle(AxisId axisId, HemisphereOption hemisphere, AxisDirection direction, double angle)
      {
         int i, steps;

         // Check Mount
         if (!MountActive) {
            return Constants.MOUNT_NOCOMPORT;
         }

         // Check Parameters
         if (axisId > AxisId.Axis2_DEC) {
            return  Constants.MOUNT_BADPARAM;
         }
         if ((angle < 0) || (angle > 648000)) {
            return  Constants.MOUNT_BADPARAM;
         }


         // Check Motor Status first
         i = EQ_GetMotorStatus(axisId);
         if ((i >= Constants.MOUNT_MOTORINACTIVE) || (i < 0x80)) {
            //we have an error code 
            return i;
         }
         else {
            if ((i & 0x90) != 0x80) {
               // motor is moving already - can't do a goto if one is in progress.
               return Constants.MOUNT_MOTORBUSY;
            }
         }

         steps = 0;
         switch (axisId) {
            case AxisId.Axis1_RA:
               steps = (int)(LowSpeedSlewRate[0] * angle / (360 * 60 * 60));
               break;
            case AxisId.Axis2_DEC:
               steps = (int)(LowSpeedSlewRate[1] * angle / (360 * 60 * 60));
               break;
         }
         if (steps <= 0.0) {
            // nothing to do;
            return Constants.MOUNT_SUCCESS;
         }


         // Set the motor hemisphere, mode, direction and speed
         i = EQ_SendGCode(axisId, hemisphere, MountMode.Goto, direction, MountSpeed.LowSpeed);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
         }


         // Set the mount relative target
         i = EQ_SendCommand(axisId, 'H', steps, 6);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
         }

         // Start the motor
         i = EQ_SendCommand(axisId, 'J', 0, NO_PARAMS);
         if ((i & Constants.EQ_ERROR) == Constants.EQ_ERROR) {
            return (EQ_GetMountError(i));               // Check errors, return "dll_error" value
         }

         return Constants.MOUNT_SUCCESS;
      }


      #endregion

   }
}
