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
using ASCOM.Astrometry.Transform;

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
            RAAxisSlewing = false;
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
            RAAxisSlewing = false;
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
            RAAxisSlewing = false;
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

      #region Emulated stuff ...
      private double GetEmulatedRAAxisPosition()
      {
         double raIncrement = 0.0;
         double elapsedTime;
         double newRaAxisPosition;
         if (Tracking) {
            double currentTime = AstroConvert.LocalApparentSiderealTime(SiteLongitude);
            if (EmulatorLastReadTime == 0) {
               currentTime = 0.000002;
            }
            if (EmulatorAxisInitialPosition[RA_AXIS] == 0.0) {
               EmulatorAxisInitialPosition[RA_AXIS] = EmulatorAxisPosition[RA_AXIS];
            }
            if (EmulatorLastReadTime > currentTime) {
               elapsedTime = EmulatorLastReadTime - currentTime;
            }
            else {
               // looped past 24H
               elapsedTime = 24.0 - EmulatorLastReadTime + currentTime;
            }
            EmulatorLastReadTime = currentTime;
            EmulatorAxisInitialPosition[RA_AXIS] = EmulatorAxisPosition[RA_AXIS];
            raIncrement = Core.Constants.SIDEREAL_RATE_RADIANS * elapsedTime;
         }
         if (Hemisphere == HemisphereOption.Northern) {
            newRaAxisPosition = AstroConvert.Range24(EmulatorAxisInitialPosition[RA_AXIS] + raIncrement);
         }
         else {
            newRaAxisPosition = AstroConvert.Range24(EmulatorAxisInitialPosition[RA_AXIS] - raIncrement);
         }
         return newRaAxisPosition;
      }

      #endregion

      #region Tracking stuff ...

      private void StopTracking()
      {
         _IsSlewing = false;

         if (Settings.ParkStatus == ParkStatus.Parking) {
            // we were slewing to park position
            // well its not happening now!
            Settings.ParkStatus = ParkStatus.Unparked;
            // HC.ParkTimer.Enabled = False
            // HC.Frame15.Caption = oLangDll.GetLangString(146) & " " & oLangDll.GetLangString(179)
            // Call SetParkCaption
         }


         if (PECEnabled) {
            PECStopTracking();
         }

         _Mount.EQ_MotorStop(AxisId.Both_Axes);


         LastPECRate = 0;
         //    Do
         //        eqres = EQ_GetMotorStatus(0)
         //        If (eqres = EQ_NOTINITIALIZED) Or (eqres = EQ_COMNOTOPEN) Or (eqres = EQ_COMTIMEOUT) Then GoTo STOPEND1
         //    Loop While (eqres And EQ_MOTORBUSY) <> 0
         //
         //STOPEND1:
         //    Do
         //        eqres = EQ_GetMotorStatus(1)
         //        If (eqres = EQ_NOTINITIALIZED) Or (eqres = EQ_COMNOTOPEN) Or (eqres = EQ_COMTIMEOUT) Then GoTo STOPEND2
         //    Loop While (eqres And EQ_MOTORBUSY) <> 0
         //
         //STOPEND2:
         // clear an active flips
         // HC.ChkForceFlip.Value = 0
         AllowCounterWeightUpSlewing = false;
         GotoParameters.SuperSafeMode = 0;


         RAAxisSlewing = false;
         TrackingState = TrackingStatus.Off;
         _DeclinationRate = 0;
         _RightAscensionRate = 0;
         // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(178)
         //HC.Add_Message(oLangDll.GetLangString(5130))


         EmulatorNudge = false;                // Enable Emulation
         EmulatorOneShot = true;              // Get One shot cap


         // EQ_Beep(7)
      }

      private void StartSiderealTracking(bool mute)
      {
         LastPECRate = 0;


         if (Settings.ParkStatus != ParkStatus.Unparked) {
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
         if (Settings.ParkStatus != ParkStatus.Unparked) {
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
         if (Settings.ParkStatus != ParkStatus.Unparked) {
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
         if (Settings.ParkStatus != ParkStatus.Unparked) {
            // HC.Add_Message(oLangDll.GetLangString(5013))
            return;
         }


         if (PECEnabled) {
            PECStopTracking();
         }


         EmulatorNudge = false;               //  Enable Emulation
         if (!mute) {
            // EQ_Beep(13)
         }

         double[] rate = new double[2];


         // On Error GoTo handlerr

         if (CustomTrackDefinition == null) {

            rate[0] = CustomTrackingRate[0];
            rate[1] = CustomTrackingRate[1];
            if (Hemisphere == HemisphereOption.Southern) {
               rate[0] = -1 * rate[0];
            }



            if (Math.Abs(rate[0]) > 12000 || Math.Abs(rate[1]) > 12000) {
               //   HC.Add_Message (oLangDll.GetLangString(5039))
               StopTracking();
               return;
            }



            // HC.Add_Message(oLangDll.GetLangString(5040) & Format$(str(i), "000.00") & " DEC:" & Format$(str(j), "000.00") &" arcsec/sec")

            CustomMoveAxis(AxisId.Axis1_RA, rate[0], true, "Custom");
            CustomMoveAxis(AxisId.Axis2_DEC, rate[1], true, "Custom");
         }
         else {
            // custom track file is assigned
            // TODO: Sort out custom track stuff
            CustomTrackDefinition.TrackIdx = -1; // = GetTrackFileIdx(1, true);
            if (CustomTrackDefinition.TrackIdx != -1) {
               if (CustomTrackDefinition.IsWaypoint) {
                  // Call GetTrackTarget(i, j)
                  CustomTrackDefinition.RAAdjustment = Settings.CurrentMountPosition.Equatorial.RightAscension - rate[0];
                  CustomTrackDefinition.DECAdjustment = Settings.CurrentMountPosition.Equatorial.Declination - rate[1];
               }
               else {
                  CustomTrackDefinition.RAAdjustment = 0;
                  CustomTrackDefinition.DECAdjustment = 0;
               }
               rate[0] = CustomTrackDefinition.TrackSchedule[CustomTrackDefinition.TrackIdx].RaRate;
               rate[1] = CustomTrackDefinition.TrackSchedule[CustomTrackDefinition.TrackIdx].DecRate;
               // HC.decCustom.Text = FormatNumber(j, 5)
               if (Hemisphere == HemisphereOption.Southern) {
                  // HC.raCustom.Text = FormatNumber(-1 * i, 5)
               }
               else {
                  // HC.raCustom.Text = FormatNumber(i, 5)
               }
               CustomMoveAxis(AxisId.Axis1_RA, rate[0], true, Settings.CustomTrackName);
               CustomMoveAxis(AxisId.Axis2_DEC, rate[1], true, Settings.CustomTrackName);
            }

            CustomTrackDefinition.TrackingChangesEnabled = true;
            // HC.CustomTrackTimer.Enabled = True
         }
         return;



      }

      private void CustomMoveAxis(AxisId axis, double rate, bool initialise, string rateName)
      {
      }


      #endregion

      #region Delta Sync stuff ...
      //private double DeltaRAMap(double raAxisPosition)      // Delta_RA_MAP
      //{
      //   return raAxisPosition + Settings.RA1Star + Settings.RASync01;
      //}

      //private double DeltaDECMap(double DecAxisPosition)      // Delta_DEC_MAP
      //{
      //   return DecAxisPosition + Settings.DEC1Star + Settings.DECSync01;
      //}

      private AxisPosition DeltaMap(AxisPosition originalAxisPosition)
      {
         return originalAxisPosition + Settings.InitialAxisAlignmentAdjustment + Settings.InitialAxisSyncAdjustment;
      }

      public static CarteseanCoordinate DeltaMatrixMap(double raAxisPosition, double decAxisPosition) // Delta_Matrix_Map
      {
         throw new NotImplementedException();
      }

      public AltAzCoordinate DeltaMatrixReverseMap(AltAzCoordinate targetAltAz) //Delta_Matrix_Reverse_Map
      {
         //Public Function Delta_Matrix_Reverse_Map(ByVal RA As Double, ByVal DEC As Double) As Coordt

         //Dim i As Integer
         //Dim obtmp As Coord
         //Dim obtmp2 As Coord

         //    If(RA >= &H1000000) Or(DEC >= &H1000000) Then
         //      Delta_Matrix_Reverse_Map.X = RA
         //      Delta_Matrix_Reverse_Map.Y = DEC

         //      Delta_Matrix_Reverse_Map.z = 1

         //      Delta_Matrix_Reverse_Map.F = 0

         //      Exit Function

         //  End If


         //  obtmp.X = RA + gRASync01

         //  obtmp.Y = DEC + gDECSync01

         //  obtmp.z = 1

         //    ' re transform using the 3 nearest stars

         //  i = EQ_UpdateAffine(obtmp.X, obtmp.Y)    // Gets the nearest 3 points then uses these to generate the Affine transformation

         //  obtmp2 = EQ_plAffine(obtmp)


         //  Delta_Matrix_Reverse_Map.X = obtmp2.X

         //  Delta_Matrix_Reverse_Map.Y = obtmp2.Y

         //  Delta_Matrix_Reverse_Map.z = 1

         //  Delta_Matrix_Reverse_Map.F = i


         //  gSelectStar = 0


         //End Function
         throw new NotImplementedException();
      }

      /// <summary>
      /// Return an AltAzCoordinate with adjustment based on the nearest available alignment point.
      /// </summary>
      /// <param name="targetAltAz"></param>
      /// <returns></returns>
      public AltAzCoordinate DeltaSyncMatrixMap(AltAzCoordinate targetAltAz)    // DeltaSync_Matrix_Map
      {
         AltAzCoordinate result = new AltAzCoordinate(targetAltAz.Altitude, targetAltAz.Azimuth);
         SelectedAlignmentPoint = Settings.AlignmentPoints.GetNearestPoint(targetAltAz, Settings.AlignmentPointFilter);
         if (SelectedAlignmentPoint != null) {
            result = targetAltAz + (SelectedAlignmentPoint.TargetAltAz - SelectedAlignmentPoint.AlignedAltAz);
         }
         return result;
      }

      /// <summary>
      /// Return an AltAzCoordinate with adjustment based on the nearest available alignment point.
      /// </summary>
      /// <param name="targetAltAz"></param>
      /// <returns></returns>
      public MountCoordinate DeltaSyncMatrixMap(MountCoordinate original, Transform transform, double localJulianTimeUTC)    // DeltaSync_Matrix_Map
      {
         MountCoordinate result = original;
         SelectedAlignmentPoint = Settings.AlignmentPoints.GetNearestPoint(original.AltAzimuth, Settings.AlignmentPointFilter);
         if (SelectedAlignmentPoint != null) {
            AltAzCoordinate adjustedAltAz = original.AltAzimuth + (SelectedAlignmentPoint.TargetAltAz - SelectedAlignmentPoint.AlignedAltAz);
            result = new MountCoordinate(adjustedAltAz, transform, localJulianTimeUTC);
         }
         return result;
      }

      public static AltAzCoordinate DeltaSyncReverseMatrixMap(double raAxisPosition, double decAxisPosition)      // DeltaSyncReverse_Matrix_Map
      {
         throw new NotImplementedException();
      }

      //      Public Function Delta_Matrix_Map(ByVal RA As Double, ByVal DEC As Double) As Coordt
      //Dim i As Integer
      //Dim obtmp As Coord
      //Dim obtmp2 As Coord

      //    If(RA >= &H1000000) Or(DEC >= &H1000000) Then
      //      Delta_Matrix_Map.X = RA
      //      Delta_Matrix_Map.Y = DEC

      //      Delta_Matrix_Map.z = 1

      //      Delta_Matrix_Map.F = 0

      //      Exit Function

      //  End If


      //  obtmp.X = RA

      //  obtmp.Y = DEC

      //  obtmp.z = 1

      //    ' re transform based on the nearest 3 stars

      //  i = EQ_UpdateTaki(RA, DEC)      // Find the nearest 3 points and they update the Taki transformation matrix


      //  obtmp2 = EQ_plTaki(obtmp)


      //  Delta_Matrix_Map.X = obtmp2.X

      //  Delta_Matrix_Map.Y = obtmp2.Y

      //  Delta_Matrix_Map.z = 1

      //  Delta_Matrix_Map.F = i


      //End Function






      //Public Function DeltaSyncReverse_Matrix_Map(ByVal RA As Double, ByVal DEC As Double) As Coordt
      //Dim i As Long

      //    If(RA >= &H1000000) Or(DEC >= &H1000000) Or gAlignmentStars_count = 0 Then GoTo HandleError

      //  i = GetNearest(RA, DEC)


      //    If i<> -1 Then
      //        gSelectStar = i
      //        DeltaSyncReverse_Matrix_Map.X = RA - (ct_Points(i).X - my_Points(i).X)
      //        DeltaSyncReverse_Matrix_Map.Y = DEC - (ct_Points(i).Y - my_Points(i).Y)
      //        DeltaSyncReverse_Matrix_Map.z = 1
      //        DeltaSyncReverse_Matrix_Map.F = 0
      //    Else
      //HandleError:
      //        DeltaSyncReverse_Matrix_Map.X = RA
      //        DeltaSyncReverse_Matrix_Map.Y = DEC
      //        DeltaSyncReverse_Matrix_Map.z = 1
      //        DeltaSyncReverse_Matrix_Map.F = 0
      //    End If

      //End Function
      #endregion

      #region Side of pier stuff ...
      /// <summary>
      /// Returns the Side of Pier based on the Declination axis position in radians
      /// V1.24g mode - not ASCOM but folks seem to like it!
      /// </summary>
      /// <param name="decAxisPosition"></param>
      /// <returns></returns>
      private PierSide SOP_Dec(double decAxisPosition)
      {
         double dec = Math.Abs(decAxisPosition - Math.PI);
         return ((dec <= Core.Constants.HALF_PI) ? PierSide.pierEast : PierSide.pierWest);

      }


      /// <summary>
      /// Physical Side of Pier
      /// this is what folks expect side of pier to be - but it won't work in ASCOM land.
      /// </summary>
      /// <param name="hourAngle"></param>
      /// <returns></returns>
      private PierSide SOP_Physical(double hourAngle)
      {
         double ha = AstroConvert.RangeHA(hourAngle);
         if (Settings.AscomCompliance.SwapPhysicalSideOfPier) {
            return (ha >= 0 ? PierSide.pierWest : PierSide.pierEast);
         }
         else {
            return ((ha >= 0) ? PierSide.pierEast : PierSide.pierWest);
         }
      }

      /// <summary>
      /// Returns the side of pier defined by the dec axis position in radians.
      /// Not the side of pier at all - but that's what ASCOM in their widsom chose to call it - duh!
      /// </summary>
      /// <param name="decAxisPosition"></param>
      /// <returns></returns>
      private PierSide SOP_Pointing(double decAxisPosition)
      {
         PierSide result;
         if (decAxisPosition <= Core.Constants.HALF_PI || Core.Constants.ONEANDHALF_PI >= 270) {
            if (Settings.AscomCompliance.SwapPointingSideOfPier) {
               result = PierSide.pierEast;
            }
            else {
               result = PierSide.pierWest;
            }
         }
         else {
            if (Settings.AscomCompliance.SwapPointingSideOfPier) {
               result = PierSide.pierWest;
            }
            else {
               result = PierSide.pierEast;
            }
         }


         //  in the south east is west and west is east!
         if (Hemisphere == HemisphereOption.Southern) {
            if (result == PierSide.pierWest) {
               result = PierSide.pierEast;
            }
            else {
               result = PierSide.pierWest;
            }
         }

         return result;
      }

      #endregion
   }
}
