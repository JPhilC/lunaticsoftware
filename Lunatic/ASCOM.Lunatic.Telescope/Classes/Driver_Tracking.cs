using System;
using System.Text;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using Lunatic.Core;
using Core = Lunatic.Core;
using System.Threading;
using ASCOM.Lunatic.Telescope;
using ASCOM.Lunatic.Telescope.Classes;
using Lunatic.Core.Geometry;
using Lunatic.SyntaController;

/// <summary>
/// The ASCOM ITelescopeV3 implimentation for the driver.
/// </summary>
namespace ASCOM.Lunatic.Telescope
{
   partial class Telescope
   {
      #region Moving and slewing ...
      private void InternalMoveAxis(AxisId axis, double rate)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("InternalMoveAxis({0}, {1})", axis, rate));
         double j;
         double currentRate;
         bool moveRAAxisSlewing = false;
         bool moveDecAxisSlewing = false;

         if (rate != 0.0) {
            // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(189)
         }

         j = rate * 3600; // Convert to Arcseconds

         if (axis == AxisId.Axis1_RA) {

            if (rate == 0 && _DeclinationRate == 0) {
               // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(178)
            }



            if (Hemisphere == HemisphereOption.Southern) {
               j = -1 * j;
               currentRate = _RightAscensionRate * -1;
            }
            else {
               currentRate = _RightAscensionRate;
            }


            // check for change of direction
            if ((currentRate * j) <= 0) {
               StartRAByRate(j);
            }
            else {
               ChangeRAByRate(j);
            }


            _RightAscensionRate = j;



            if (rate == 0) {
               moveRAAxisSlewing = false;
            }
            else {
               TrackingState = TrackingStatus.Custom;
               moveRAAxisSlewing = true;
            }
         }


         if (axis == AxisId.Axis2_DEC) {

            if (rate == 0 && _RightAscensionRate == 0) {
               // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(178)
            }


            // check for change of direction
            if ((_DeclinationRate * j) <= 0) {
               StartDECByRate(j);
            }
            else {
               ChangeDECByRate(j);
            }


            _DeclinationRate = j;
            if (rate == 0) {
               moveDecAxisSlewing = false;
            }
            else {
               TrackingState = TrackingStatus.Custom;
               moveDecAxisSlewing = true;
            }
         }

         _IsMoveAxisSlewing = (moveRAAxisSlewing || moveDecAxisSlewing);
      }

      private void StartRAByRate(double raRate)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("StartRAByRate({0})", raRate));
         double i;
         double j;
         MountSpeed mountSpeed = MountSpeed.LowSpeed;
         int highSpeedRatio = 1;
         i = Math.Abs(raRate);
         int eqResult;

         if (_MountVersion > 0x301) {
            if (i > 1000) {
               mountSpeed = MountSpeed.HighSpeed;
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);
            }
         }
         else {
            if (i > 3000) {
               mountSpeed = MountSpeed.HighSpeed;
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);
            }
         }

         // HC.Add_Message (oLangDll.GetLangString(117) & " " & str(m) & " , " & str(RA_RATE) & " arcsec/sec")

         eqResult = _Mount.EQ_MotorStop(AxisId.Axis1_RA);          // Stop RA Motor
         if (eqResult != Core.Constants.MOUNT_SUCCESS) {
            return;
         }

         if (raRate == 0) {
            _IsSlewing = false;
            RAStatusSlew = false;
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis1_RA);
            MoveAxisRate[0] = MountSpeed.LowSpeed;
            return;
         }

         i = raRate;
         j = Math.Abs(i);              // Get the absolute value for parameter passing
         if (_MountVersion == 0x301) {
            if ((j > 1350) && (j <= 3000)) {
               if (j < 2175) {
                  j = 1350;
               }
               else {
                  j = 3001;
                  mountSpeed = MountSpeed.HighSpeed;
                  highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);
               }
            }
         }


         MoveAxisRate[0] = mountSpeed;    // Save Speed Settings

         // HC.Add_FileMessage ("StartRARate=" & FormatNumber(RA_RATE, 5))
         j = ((highSpeedRatio * LowSpeedSlewRate[0] / j) + 0.5) + 30000; // Compute for the rate

         if (i >= 0) {
            eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis1_RA, TrackMode.Initial, (int)j, mountSpeed, Hemisphere, AxisDirection.Forward);
         }
         else {
            eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis1_RA, TrackMode.Initial, (int)j, mountSpeed, Hemisphere, AxisDirection.Reverse);
         }

      }

      // Change RA motor rate based on an input rate of arcsec per Second

      private void ChangeRAByRate(double raRate)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("ChangeRAByRate({0})", raRate));

         double j;
         MountSpeed mountSpeed = MountSpeed.LowSpeed;
         int highSpeedRatio;
         AxisDirection dir;
         int eqResult;
         TrackMode init = TrackMode.Update;


         if (raRate >= 0) {
            dir = AxisDirection.Forward;
         }
         else {
            dir = AxisDirection.Reverse;
         }


         if (raRate == 0) {
            // rate = 0 so stop motors
            _IsSlewing = false;
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis1_RA);
            RAStatusSlew = false;
            MoveAxisRate[0] = 0;
            return;
         }

         highSpeedRatio = 1;   // Speed multiplier = 1
         j = Math.Abs(raRate);



         if (_MountVersion > 0x301) {
            // if above high speed theshold
            if (j > 1000) {
               mountSpeed = MountSpeed.HighSpeed;               // HIGH SPEED
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);  // GET HIGH SPEED MULTIPLIER
            }
         }
         else {
            // who knows what Mon is up to here - a special for his mount perhaps?
            if (_MountVersion == 0x301) {
               if ((j > 1350) && (j <= 3000)) {
                  if (j < 2175) {
                     j = 1350;
                  }
                  else {
                     j = 3001;
                     mountSpeed = MountSpeed.HighSpeed;
                     highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);
                  }
               }
            }
            // if above high speed theshold
            if (j > 3000) {
               mountSpeed = MountSpeed.HighSpeed;          // HIGH SPEED
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);  // GET HIGH SPEED MULTIPLIER
            }
         }


         // HC.Add_FileMessage ("ChangeRARate=" & FormatNumber(rate, 5))

         // if there// s a switch between high/low speed or if operating at high speed
         // we ned to do additional initialisation
         if (mountSpeed != MountSpeed.LowSpeed || mountSpeed != MoveAxisRate[0]) {
            init = TrackMode.Initial;
         }
         if (init == TrackMode.Initial) {
            // Stop Motor
            // HC.Add_FileMessage ("Direction or High/Low speed change")
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis1_RA);
            if (eqResult != Core.Constants.MOUNT_SUCCESS) {
               return;
            }

         }
         MoveAxisRate[0] = mountSpeed;

         // Compute for the rate
         j = ((highSpeedRatio * LowSpeedSlewRate[0] / j) + 0.5) + 30000;

         eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis1_RA, init, (int)j, mountSpeed, Hemisphere, dir);
         // HC.Add_FileMessage ("EQ_SetCustomTrackRate=0," & CStr(init) & "," & CStr(j) & "," & CStr(k) & "," & CStr(gHemisphere) & "," & CStr(dir))
         // HC.Add_Message (oLangDll.GetLangString(117) & "=" & str(rate) & " arcsec/sec" & "," & CStr(eqres))
      }


      // Start DEC motor based on an input rate of arcsec per Second

      private void StartDECByRate(double decRate)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("StartDECByRate({0})", decRate));
         double i;
         double j;
         MountSpeed mountSpeed = MountSpeed.LowSpeed;
         int highSpeedRatio = 1;
         i = Math.Abs(decRate);
         int eqResult;


         if (_MountVersion > 0x301) {
            if (i > 1000) {
               mountSpeed = MountSpeed.HighSpeed;
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);
            }
         }
         else {
            if (i > 3000) {
               mountSpeed = MountSpeed.HighSpeed;
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);
            }
         }


         // HC.Add_Message (oLangDll.GetLangString(118) & " " & str(m) & " , " & str(DEC_RATE) & " arcsec/sec")

         eqResult = _Mount.EQ_MotorStop(AxisId.Axis2_DEC);          // Stop RA Motor
         if (eqResult != Core.Constants.MOUNT_SUCCESS) {
            return;
         }

         if (decRate == 0) {
            _IsSlewing = false;
            RAStatusSlew = false;
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
            MoveAxisRate[1] = MountSpeed.LowSpeed;
            return;
         }
         i = decRate;
         j = Math.Abs(i);              // Get the absolute value for parameter passing
         if (_MountVersion == 0x301) {
            if ((j > 1350) && (j <= 3000)) {
               if (j < 2175) {
                  j = 1350;
               }
               else {
                  j = 3001;
                  mountSpeed = MountSpeed.HighSpeed;
                  highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);
               }
            }
         }

         MoveAxisRate[1] = mountSpeed;      // Save Speed Settings

         // HC.Add_FileMessage ("StartDecRate=" & FormatNumber(DEC_RATE, 5))
         //    j = Int((m * 9325.46154 / j) + 0.5) + 30000 // Compute for the rate
         j = ((highSpeedRatio * LowSpeedSlewRate[1] / j) + 0.5) + 30000; // Compute for the rate

         if (i >= 0) {
            eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis2_DEC, TrackMode.Initial, (int)j, mountSpeed, Hemisphere, AxisDirection.Forward);
         }
         else {
            eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis2_DEC, TrackMode.Initial, (int)j, mountSpeed, Hemisphere, AxisDirection.Reverse);
         }
      }



      // Change DEC motor rate based on an input rate of arcsec per Second

      private void ChangeDECByRate(double decRate)
      {

         double j;
         MountSpeed mountSpeed = MountSpeed.LowSpeed;
         int highSpeedRatio = 1;
         AxisDirection dir;
         TrackMode init = TrackMode.Update;
         int eqResult;


         if (decRate >= 0) {
            dir = AxisDirection.Forward;
         }
         else {
            dir = AxisDirection.Forward;
         }


         if (decRate == 0) {
            // rate = 0 so stop motors
            _IsSlewing = false;
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
            //        gRAStatus_slew = False
            MoveAxisRate[1] = MountSpeed.LowSpeed;
            return;
         }

         j = Math.Abs(decRate);

         if (_MountVersion > 0x301) {
            // if above high speed theshold
            if (j > 1000) {
               mountSpeed = MountSpeed.HighSpeed;               // HIGH SPEED
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);  // GET HIGH SPEED MULTIPLIER
            }
         }
         else {
            // who knows what Mon is up to here - a special for his mount perhaps?
            if (_MountVersion == 0x301) {
               if ((j > 1350) && (j <= 3000)) {
                  if (j < 2175) {
                     j = 1350;
                  }
                  else {
                     j = 3001;
                     mountSpeed = MountSpeed.HighSpeed;
                     highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);
                  }
               }
            }
            // if above high speed theshold
            if (j > 3000) {
               mountSpeed = MountSpeed.HighSpeed;               // HIGH SPEED
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);  // GET HIGH SPEED MULTIPLIER
            }
         }


         // HC.Add_FileMessage ("ChangeDECRate=" & FormatNumber(rate, 5))

         // if there// s a switch between high/low speed or if operating at high speed
         // we need to do additional initialisation
         if (mountSpeed != MountSpeed.LowSpeed || mountSpeed != MoveAxisRate[1]) {
            init = TrackMode.Initial;
         }


         if (init == TrackMode.Initial) {
            // Stop Motor
            // HC.Add_FileMessage ("Direction or High/Low speed change")
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
            if (eqResult != Core.Constants.MOUNT_SUCCESS) {
               return;
            }
         }
         MoveAxisRate[1] = mountSpeed;

         // Compute for the rate
         //    j = Int((m * 9325.46154 / j) + 0.5) + 30000
         j = ((highSpeedRatio * LowSpeedSlewRate[1] / j) + 0.5) + 30000;

         eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis2_DEC, init, (int)j, mountSpeed, Hemisphere, dir);
         // HC.Add_FileMessage ("EQ_SetCustomTrackRate=1," & CStr(init) & "," & CStr(j) & "," & CStr(k) & "," & CStr(gHemisphere) & "," & CStr(dir))
         // HC.Add_Message (oLangDll.GetLangString(118) & "=" & str(rate) & " arcsec/sec" & "," & CStr(eqres))

      }

      #endregion

      #region Tracking stuff ...

      private void StopTracking() {
         /*
         gSlewStatus = False

      If gEQparkstatus = 2 Then
        ' we were slewing to park position
        ' well its not happening now!
        gEQparkstatus = 0
        HC.ParkTimer.Enabled = False
        HC.Frame15.Caption = oLangDll.GetLangString(146) & " " & oLangDll.GetLangString(179)
        Call SetParkCaption
    End If


    If gPEC_Enabled Then
       PEC_StopTracking
    End If


'    eqres = EQ_MotorStop(0)
'    eqres = EQ_MotorStop(1)
    eqres = EQ_MotorStop(2)


    gRA_LastRate = 0
'    Do
'        eqres = EQ_GetMotorStatus(0)
'        If (eqres = EQ_NOTINITIALIZED) Or (eqres = EQ_COMNOTOPEN) Or (eqres = EQ_COMTIMEOUT) Then GoTo STOPEND1
'    Loop While (eqres And EQ_MOTORBUSY) <> 0
'
'STOPEND1:
'    Do
'        eqres = EQ_GetMotorStatus(1)
'        If (eqres = EQ_NOTINITIALIZED) Or (eqres = EQ_COMNOTOPEN) Or (eqres = EQ_COMTIMEOUT) Then GoTo STOPEND2
'    Loop While (eqres And EQ_MOTORBUSY) <> 0
'
'STOPEND2:
    ' clear an active flips
    HC.ChkForceFlip.Value = 0
    gCWUP = False
    gGotoParams.SuperSafeMode = 0


    gRAStatus_slew = False
    gTrackingStatus = 0
    gDeclinationRate = 0
    gRightAscensionRate = 0
    HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(178)
    HC.Add_Message(oLangDll.GetLangString(5130))


    gEmulNudge = False               ' Enable Emulation
    gEmulOneShot = True              ' Get One shot cap


    EQ_Beep(7)
*/
}

      private void StartSiderealTracking(bool mute)
      {
         LastPECRate = 0;


         if (ParkStatus != ParkStatus.Unparked) {
            //  no tracking if parked!
            // HC.Add_Message(oLangDll.GetLangString(5013))
            return;
         }
         else {
            //  Stop DEC motor
            _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
            _DeclinationRate = 0;


            //  start RA motor at sidereal

            _Mount.EQ_StartRATrack(MountTracking.Sidereal, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
            MoveAxisRate[0] = MountSpeed.LowSpeed;
            TrackingState = TrackingStatus.Sidereal;
            _TrackingRate = DriveRates.driveSidereal;    // ASCOM TrackingRate backing variable
            _RightAscensionRate = Core.Constants.SIDEREAL_RATE_ARCSECS;


            if (Settings.TrackUsingPEC) {
               //  track using PEC
               PECStartTracking();
               if (!mute) {
                  // EQ_Beep(??)
               }
            }
            else {
               //  Set Caption
               // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(122)
               // HC.Add_Message(oLangDll.GetLangString(5014))
               if (!mute) {
                  // EQ_Beep(10)
               }
            }
         }
      }

      private void StartLunarTracking(bool mute)
      {
         LastPECRate = 0;
         if (ParkStatus != ParkStatus.Unparked) {
            // HC.Add_Message(oLangDll.GetLangString(5013))
            return;
         }


         if (PECEnabled) {
            PECStopTracking();
         }


         _Mount.EQ_StartRATrack(MountTracking.Lunar, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
         _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
         TrackingState = TrackingStatus.Lunar;                 // Lunar rate tracking'
         _TrackingRate = DriveRates.driveLunar;                // Backing variable for ASCOM TrackingRate member.
         _DeclinationRate = 0;
         _RightAscensionRate = Core.Constants.LUNAR_RATE;

         //HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(123)
         //HC.Add_Message(oLangDll.GetLangString(5015))
         EmulatorNudge = false;               //  Enable Emulation
         if (!mute) {
            // EQ_Beep(11)
         }

      }


      private void StartSolarTracking(bool mute)
      {
         LastPECRate = 0;
         if (ParkStatus != ParkStatus.Unparked) {
            // HC.Add_Message(oLangDll.GetLangString(5013))
            return;
         }


         if (PECEnabled) {
            PECStopTracking();
         }


         _Mount.EQ_StartRATrack(MountTracking.Solar, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
         _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
         TrackingState = TrackingStatus.Solar;                 // Lunar rate tracking'
         _TrackingRate = DriveRates.driveSolar;                // Backing variable for ASCOM TrackingRate member.
         _DeclinationRate = 0;
         _RightAscensionRate = Core.Constants.SOLAR_RATE;

         //HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(123)
         //HC.Add_Message(oLangDll.GetLangString(5015))
         EmulatorNudge = false;               //  Enable Emulation
         if (!mute) {
            // EQ_Beep(12)
         }

      }

      private void StartCustomTracking(bool mute)
      {
         LastPECRate = 0;
         if (ParkStatus != ParkStatus.Unparked) {
            // HC.Add_Message(oLangDll.GetLangString(5013))
            return;
         }


         if (PECEnabled) {
            PECStopTracking();
         }


         _Mount.EQ_StartRATrack(MountTracking.Solar, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
         _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
         TrackingState = TrackingStatus.Solar;                 // Lunar rate tracking'
         _TrackingRate = DriveRates.driveKing;                // Backing variable for ASCOM TrackingRate member.
         _DeclinationRate = 0;
         _RightAscensionRate = Core.Constants.SOLAR_RATE;

         //HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(123)
         //HC.Add_Message(oLangDll.GetLangString(5015))
         EmulatorNudge = false;               //  Enable Emulation
         if (!mute) {
            // EQ_Beep(??)
         }

      }
      #endregion

   }
}
