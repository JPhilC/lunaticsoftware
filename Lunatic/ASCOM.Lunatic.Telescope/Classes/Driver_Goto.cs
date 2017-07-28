using ASCOM.Lunatic.Telescope.Classes;
using Lunatic.Core.Geometry;
using Lunatic.SyntaController;
using ASCOM.Astrometry.Transform;
using Lunatic.Core;
using System;
using CoreConstants = Lunatic.Core.Constants;

/// <summary>
/// The ASCOM ITelescopeV3 implimentation for the driver.
/// </summary>
namespace ASCOM.Lunatic.Telescope
{
   partial class Telescope
   {
      private bool suppressHorizonLimits;

      private GotoParameters _GotoParameters = new GotoParameters();

      public GotoParameters GotoParameters
      {
         get
         {
            lock (_Lock) {
               return _GotoParameters;
            }
         }
      }


      // Routine to Slew the mount to target location
      public void RADecAsyncSlew(double rightAscension, double declination)
      {
         _EncoderTimer.Stop();
         _IsSlewing = false;

         //      With gGotoParams
         //          Call CalcEncoderTargets
         GotoParameters.TargetCoordinate = new EquatorialCoordinate(rightAscension, declination);
         GotoParameters.FRSlewCount = 0;
         GotoParameters.SlewCount = Settings.DevelopmentOptions.MaximumSlewCount;
         CalculateRADecAxisTargets();
         GotoParameters.Rate = Settings.DevelopmentOptions.GotoSlewLimit;

         if (AllowCounterWeightUpSlewing) {
            suppressHorizonLimits = true;
            // a counterweights up slew has been requested
            if (!LimitsActive) {
               // Limits are off so play safe and slew RA and DEC independently
               if (Settings.CurrentMountPosition.Equatorial.RightAscension > 12) {
                  //  we're currently in a counterweights up position
                  if (GotoParameters.CurrentAxisPosition[RA_AXIS] > Constants.RAEncoder_Home_pos) {
                     // single axis slew to nearest limit position
                     // followed by dual axis slew to target limit
                     // followed by single axis slew to target ra
                     GotoParameters.SuperSafeMode = 3;
                     StartSlew(MeridianWest, GotoParameters.CurrentAxisPosition[DEC_AXIS],
                        GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                  }
                  else {
                     // single axis slew to nearest limit position
                     // followed by dual axis slew to target limit
                     // followed by single axis slew to target ra
                     GotoParameters.SuperSafeMode = 3;
                     StartSlew(MeridianEast, GotoParameters.CurrentAxisPosition[DEC_AXIS],
                        GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                  }
               }
               else {
                  // we're currently in a counterweights down position
                  if (GotoParameters.TargetAxisPosition[RA_AXIS] > Constants.RAEncoder_Home_pos) {
                     // dual axis slew to limit position followed by ra only slew to target
                     GotoParameters.SuperSafeMode = 1;
                     StartSlew(MeridianWest, GotoParameters.TargetAxisPosition[DEC_AXIS],
                        GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                  }
                  else {
                     // dual axis slew to limit position followed by ra only slew to target
                     GotoParameters.SuperSafeMode = 1;
                     StartSlew(MeridianEast, GotoParameters.TargetAxisPosition[DEC_AXIS],
                        GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                  }
               }
            }
            else {
               // Limits are active so allow simulatenous RA/DEC movement
               GotoParameters.SuperSafeMode = 0;
               StartSlew(GotoParameters.TargetAxisPosition[RA_AXIS], GotoParameters.TargetAxisPosition[DEC_AXIS],
                  GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
            }
         }
         else {
            // we're currently in a counterweights up position
            if (!LimitsActive) {
               //  Limits are off
               if (GotoParameters.CurrentAxisPosition[RA_AXIS] > MeridianWest) {
                  // Slew in RA to limit position - then complete move as dual axis slew
                  GotoParameters.SuperSafeMode = 1;
                  suppressHorizonLimits = true;
                  StartSlew(MeridianWest, GotoParameters.CurrentAxisPosition[DEC_AXIS],
                     GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
               }
               else {
                  if (GotoParameters.CurrentAxisPosition[RA_AXIS] < MeridianEast) {
                     // Slew in RA to limit position - then complete move as dual axis slew
                     GotoParameters.SuperSafeMode = 1;
                     suppressHorizonLimits = true;
                     StartSlew(MeridianEast, GotoParameters.CurrentAxisPosition[DEC_AXIS],
                        GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                  }
                  else {
                     //  standard slew - simulatanous RA and DEc movement
                     GotoParameters.SuperSafeMode = 0;
                     StartSlew(GotoParameters.TargetAxisPosition[RA_AXIS], GotoParameters.TargetAxisPosition[DEC_AXIS],
                        GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                  }
               }
            }
            else {
               // Limits are enabled
               if (GotoParameters.CurrentAxisPosition[RA_AXIS] > LimitWest) {
                  // Slew in RA to limit position - then complete move as dual axis slew
                  GotoParameters.SuperSafeMode = 1;
                  suppressHorizonLimits = true;
                  StartSlew(LimitWest, GotoParameters.CurrentAxisPosition[DEC_AXIS],
                     GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
               }
               else {
                  if (GotoParameters.CurrentAxisPosition[RA_AXIS] < LimitEast) {
                     // Slew in RA to limit position - then complete move as dual axis slew
                     GotoParameters.SuperSafeMode = 1;
                     suppressHorizonLimits = true;
                     StartSlew(LimitEast, GotoParameters.CurrentAxisPosition[DEC_AXIS],
                        GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                  }
                  else {
                     //  standard slew - simulatanous RA and DEc movement
                     GotoParameters.SuperSafeMode = 0;
                     StartSlew(GotoParameters.TargetAxisPosition[RA_AXIS], GotoParameters.TargetAxisPosition[DEC_AXIS],
                        GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                  }
               }
            }
         }
         //End With
         _EncoderTimer.Start();

      }

      private void CalculateRADecAxisTargets()
      {
         double tRa;
         double tHa;
         int tPier;
         double rightAscension = GotoParameters.TargetCoordinate.RightAscension;
         double declination = GotoParameters.TargetCoordinate.Declination;
         // stop the motors
         // TODO:    PEC_StopTracking
         _Mount.MCAxisStop(AxisId.Both_Axes);

         //    // read current
         AxisPosition currentAxisPosition = _Mount.MCGetAxisPosition();
         //    currentRAEncoder = EQGetMotorValues(0)
         //    currentDECEncoder = EQGetMotorValues(1)

         tHa = AstroConvert.RangeHA(rightAscension - AstroConvert.LocalApparentSiderealTime(SiteLongitude));
         if (tHa < 0.0) {
            if (AllowCounterWeightUpSlewing) {
               if (Hemisphere == HemisphereOption.Northern) {
                  tPier = 0;
               }
               else {
                  tPier = 1;
               }
               tRa = rightAscension;
            }
            else {
               if (Hemisphere == HemisphereOption.Northern) {
                  tPier = 1;
               }
               else {
                  tPier = 0;
               }
               tRa = AstroConvert.Range24(rightAscension - 12);
            }
         }
         else {
            if (AllowCounterWeightUpSlewing) {
               if (Hemisphere == HemisphereOption.Northern) {
                  tPier = 1;
               }
               else {
                  tPier = 0;
               }
               tRa = AstroConvert.Range24(rightAscension - 12);
            }
            else {
               if (Hemisphere == HemisphereOption.Northern) {
                  tPier = 0;
               }
               else {
                  tPier = 1;
               }
               tRa = rightAscension;
            }
         }

         // Compute for Target RA/DEC Encoder

         double targetRAEncoder = AstroConvert.RAAxisPositionFromRA(tRa, 0, SiteLongitude, Constants.RAEncoder_Zero_pos, Hemisphere);
         double targetDECEncoder = AstroConvert.DECAxisPositionFromDEC(declination, tPier, Constants.DECEncoder_Zero_pos, Hemisphere);


         if (AllowCounterWeightUpSlewing) {
            // TODO: HC.Add_Message "Goto: CW-UP slew requested"
            // if RA limits are active
            if (CheckLimitsActive && LimitEast != 0.0 && LimitWest != 0.0) {
               // check that the target position is within limits
               if (Hemisphere == HemisphereOption.Northern) {
                  if (targetRAEncoder < LimitEast || targetRAEncoder > LimitWest) {
                     // target position is outside limits
                     AllowCounterWeightUpSlewing = false;
                  }
               }
               else {
                  if (targetRAEncoder > LimitEast || targetRAEncoder < LimitWest) {
                     //  target position is outside limits
                     AllowCounterWeightUpSlewing = false;
                  }
               }


               //  if target position is outside limits
               if (!AllowCounterWeightUpSlewing) {
                  // TODO: HC.Add_Message "Goto: RA Limits prevent CW-UP slew"
                  // then abandon Counter Weights up Slew and recalculate for a standard slew.
                  if (tHa < 0.0) {
                     if (Hemisphere == HemisphereOption.Northern) {
                        tPier = 1;
                     }
                     else {
                        tPier = 0;
                     }
                     tRa = AstroConvert.Range24(rightAscension - 12);
                  }
                  else {
                     if (Hemisphere == HemisphereOption.Northern) {
                        tPier = 0;
                     }
                     else {
                        tPier = 1;
                     }
                     tRa = rightAscension;
                  }
                  targetRAEncoder = AstroConvert.RAAxisPositionFromRA(tRa, 0, SiteLongitude, Constants.RAEncoder_Zero_pos, Hemisphere);
                  targetDECEncoder = AstroConvert.DECAxisPositionFromDEC(declination, tPier, Constants.DECEncoder_Zero_pos, Hemisphere);
               }
            }
         }

         // TODO: Sort out the alignment translation

         //    if (gThreeStarEnable = False {
         //       gSelectStar = 0
         //       currentRAEncoder = Delta_RA_Map(currentRAEncoder)
         //       currentDECEncoder = Delta_DEC_Map(currentDECEncoder)
         //    } else {
         //       ' Transform target using model
         //       Select Case gAlignmentMode
         //           Case 2
         //             ' n-star+nearest
         //             tmpcoord = DeltaSyncReverse_Matrix_Map(targetRAEncoder - gRASync01, targetDECEncoder - gDECSync01)
         //           Case 1
         //               ' n-star
         //               tmpcoord = Delta_Matrix_Map(targetRAEncoder - gRASync01, targetDECEncoder - gDECSync01)
         //           Case } else {
         //               ' nearest
         //               tmpcoord = Delta_Matrix_Map(targetRAEncoder - gRASync01, targetDECEncoder - gDECSync01)


         //               if (tmpcoord.F = 0 {
         //                   tmpcoord = DeltaSyncReverse_Matrix_Map(targetRAEncoder - gRASync01, targetDECEncoder - gDECSync01)
         //               }
         //       End Select
         //       targetRAEncoder = tmpcoord.x
         //       targetDECEncoder = tmpcoord.Y
         //    }



         GotoParameters.TargetAxisPosition = new AxisPosition(targetRAEncoder, targetDECEncoder);
         GotoParameters.CurrentAxisPosition = currentAxisPosition;
         // TODO:    HC.Add_Message "Goto: " & FmtSexa(gTargetRA, False) & " " & FmtSexa(gTargetDec, True)
         // TODO:    HC.Add_Message "Goto: RaEnc=" & CStr(currentRAEncoder) & " Target=" & CStr(targetRAEncoder)
         // TODO:    HC.Add_Message "Goto: DecEnc=" & CStr(currentDECEncoder) & " Target=" & CStr(targetDECEncoder)
      }

      private void StartSlew(double targetRAAxisPosition, double targetDecAxisPosition, double currentRAAxisPosition, double currentDecAxisPosition)
      {

         // calculate relative amount to move
         double deltaRAStep = Math.Abs(targetRAAxisPosition - currentRAAxisPosition);
         double deltaDECStep = Math.Abs(targetDecAxisPosition - currentDecAxisPosition);


         if (deltaRAStep != 0.0) {
            // Compensate for the smallest discrepancy after the final slew
            if (TrackingState != TrackingStatus.Off) {
               if (targetRAAxisPosition > currentRAAxisPosition) {
                  if (Hemisphere == HemisphereOption.Northern) {
                     deltaRAStep = deltaRAStep + Settings.DevelopmentOptions.GotoRACompensation;
                  }
                  else {
                     deltaRAStep = deltaRAStep - Settings.DevelopmentOptions.GotoRACompensation;
                  }
               }
               else {
                  if (Hemisphere == HemisphereOption.Northern) {
                     deltaRAStep = deltaRAStep - Settings.DevelopmentOptions.GotoRACompensation;
                  }
                  else {
                     deltaRAStep = deltaRAStep + Settings.DevelopmentOptions.GotoRACompensation;
                  }
               }
               if (deltaRAStep < 0.0) {
                  deltaRAStep = 0.0;
               }


               if (targetRAAxisPosition > currentRAAxisPosition) {
                  GotoParameters.RADirection = AxisDirection.Forward;
                  if (GotoParameters.Rate == 0) {
                     // let mount decide on slew rate
                     GotoParameters.RASlewActive = false;
                     _Mount.EQ_StartMoveMotor(0, HemisphereOption.Northern, AxisDirection.Forward, deltaRAStep, GetSlowdown(deltaRAStep, RA_AXIS));
                  }
                  else {
                     GotoParameters.RASlewActive = true;
                     _Mount.EQ_Slew(AxisId.Axis1_RA, HemisphereOption.Northern, AxisDirection.Forward, GotoParameters.Rate);
                  }
               }
               else {
                  GotoParameters.RADirection = AxisDirection.Reverse;
                  if (GotoParameters.Rate == 0) {
                     GotoParameters.RASlewActive = false;
                     _Mount.EQ_StartMoveMotor(AxisId.Axis1_RA, HemisphereOption.Northern, AxisDirection.Reverse, deltaRAStep, GetSlowdown(deltaRAStep, RA_AXIS));
                  }
                  else {
                     GotoParameters.RASlewActive = true;
                     _Mount.EQ_Slew(AxisId.Axis1_RA, HemisphereOption.Northern, AxisDirection.Reverse, GotoParameters.Rate);
                  }
               }
            }

         }

         if (deltaDECStep != 0.0) {
            if (targetDecAxisPosition > currentDecAxisPosition) {
               GotoParameters.DecDirection = AxisDirection.Forward;
               if (GotoParameters.Rate == 0) {
                  // let mount decide on slew rate
                  GotoParameters.DecSlewActive = false;
                  _Mount.EQ_StartMoveMotor(AxisId.Axis2_DEC, HemisphereOption.Northern, AxisDirection.Forward, deltaDECStep, GetSlowdown(deltaDECStep, DEC_AXIS));
               }
               else {
                  GotoParameters.DecSlewActive = true;
                  _Mount.EQ_Slew(AxisId.Axis2_DEC, HemisphereOption.Northern, AxisDirection.Forward, GotoParameters.Rate);
               }
            }
            else {
               GotoParameters.DecDirection = AxisDirection.Reverse;
               if (GotoParameters.Rate == 0) {
                  // let mount decide on slew rate
                  GotoParameters.DecSlewActive = false;
                  _Mount.EQ_StartMoveMotor(AxisId.Axis2_DEC, HemisphereOption.Northern, AxisDirection.Reverse, deltaDECStep, GetSlowdown(deltaDECStep, DEC_AXIS));
               }
               else {
                  GotoParameters.DecSlewActive = true;
                  _Mount.EQ_Slew(AxisId.Axis2_DEC, HemisphereOption.Northern, AxisDirection.Reverse, GotoParameters.Rate);
               }
            }
         }

         // Activate Asynchronous Slew Monitoring Routine
         RAAxisSlewing = false;
         _IsSlewing = true;
         MotorStatus[RA_AXIS] = CoreConstants.MOUNT_MOTORBUSY;
         MotorStatus[DEC_AXIS] = CoreConstants.MOUNT_MOTORBUSY;
      }


      private void ManageGoto()
      {
         double raDiff;
         double decDiff;
         // =================================================
         // Fixed rate slew
         // =================================================
         if (GotoParameters.RASlewActive || GotoParameters.DecSlewActive) {

            // Handle as fixed rate slew
            if (GotoParameters.RASlewActive) {
               if (GotoParameters.RADirection == AxisDirection.Forward) {
                  if (CurrentAxisPosition[RA_AXIS] >= GotoParameters.TargetAxisPosition[RA_AXIS]) {
                     _Mount.EQ_MotorStop(AxisId.Axis1_RA);
                     GotoParameters.RASlewActive = false;
                     _Mount.EQ_StartRATrack(MountTracking.Sidereal, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
                  }
               }
               else {
                  if (CurrentAxisPosition[RA_AXIS] <= GotoParameters.TargetAxisPosition[RA_AXIS]) {
                     _Mount.EQ_MotorStop(AxisId.Axis1_RA);
                     GotoParameters.RASlewActive = false;
                     _Mount.EQ_StartRATrack(MountTracking.Sidereal, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
                  }
               }
            }

            if (GotoParameters.DecSlewActive) {
               if (GotoParameters.DecDirection == AxisDirection.Forward) {
                  if (CurrentAxisPosition[DEC_AXIS] >= GotoParameters.TargetAxisPosition[DEC_AXIS]) {
                     _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
                     GotoParameters.DecSlewActive = false;
                  }
               }
               else {
                  if (CurrentAxisPosition[DEC_AXIS] <= GotoParameters.TargetAxisPosition[DEC_AXIS]) {
                     _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
                     GotoParameters.DecSlewActive = false;
                  }
               }
            }

            if (!GotoParameters.RASlewActive && !GotoParameters.DecSlewActive) {

               switch (GotoParameters.SuperSafeMode) {
                  case 0:
                     // rough fixed rate slew complete
                     CalculateRADecAxisTargets();
                     raDiff = Math.Abs(GotoParameters.TargetAxisPosition[RA_AXIS] - CurrentAxisPosition[RA_AXIS]);
                     decDiff = Math.Abs(GotoParameters.TargetAxisPosition[DEC_AXIS] - CurrentAxisPosition[DEC_AXIS]);
                     // TODO: HC.Add_Message "Goto: FRSlew complete ra_diff=" & CStr(ra_diff) & " dec_diff=" & CStr(dec_diff)
                     if ((raDiff < CoreConstants.DEG_RAD) && (decDiff < (0.6666 * CoreConstants.DEG_RAD))) {
                        // initiate a standard itterative goto if within a 3/4 of a degree. (Actually the original code had RA < 1 degree and Dec < 0.6666 degree)
                        GotoParameters.Rate = 0;
                        StartSlew(GotoParameters.TargetAxisPosition[RA_AXIS], GotoParameters.TargetAxisPosition[DEC_AXIS], GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                     }
                     else {
                        // Do another rough slew.
                        // TODO: HC.Add_Message "Goto: FRSlew"
                        GotoParameters.FRSlewCount = GotoParameters.FRSlewCount + 1;
                        if (GotoParameters.FRSlewCount >= 5) {
                           // if we can't get close after 5 attempts then abandon the FR slew
                           // and use the full speed iterative slew
                           GotoParameters.FRSlewCount = 0;
                           GotoParameters.Rate = 0;
                        }
                        StartSlew(GotoParameters.TargetAxisPosition[RA_AXIS], GotoParameters.TargetAxisPosition[DEC_AXIS], GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                     }
                     break;

                  case 1:
                     // move to RA target
                     CalculateRADecAxisTargets();
                     GotoParameters.SuperSafeMode = 0;
                     StartSlew(GotoParameters.TargetAxisPosition[RA_AXIS], GotoParameters.TargetAxisPosition[DEC_AXIS], GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                     break;

                  case 3:
                     // were at a limit position
                     if (GotoParameters.TargetAxisPosition[RA_AXIS] > CoreConstants.RAEncoder_Home_pos) {
                        // dual axis slew to limit position nearest to target
                        GotoParameters.SuperSafeMode = 1;
                        if (!LimitsActive) {
                           StartSlew(MeridianWest, GotoParameters.TargetAxisPosition[DEC_AXIS], EmulatorAxisPosition[RA_AXIS], EmulatorAxisPosition[DEC_AXIS]);
                        }
                        else {
                           StartSlew(LimitWest, GotoParameters.TargetAxisPosition[DEC_AXIS], EmulatorAxisPosition[RA_AXIS], EmulatorAxisPosition[DEC_AXIS]);
                        }
                     }
                     else {
                        // dual axis slew to limit position nearest to target
                        GotoParameters.SuperSafeMode = 1;
                        if (!LimitsActive) {
                           StartSlew(MeridianEast, GotoParameters.TargetAxisPosition[DEC_AXIS], EmulatorAxisPosition[RA_AXIS], EmulatorAxisPosition[DEC_AXIS]);
                        }
                        else {
                           StartSlew(LimitEast, GotoParameters.TargetAxisPosition[DEC_AXIS], EmulatorAxisPosition[RA_AXIS], EmulatorAxisPosition[DEC_AXIS]);
                        }
                     }
                     break;

               }
            }
            return;


         }



         // =================================================
         // Iterative slew - variable rate
         // =================================================
         if ((MotorStatus[RA_AXIS] & CoreConstants.MOUNT_MOTORBUSY) == 0) {
            // At This point RA motor has completed the slew
            RAAxisSlewing = true;
            if ((MotorStatus[DEC_AXIS] & CoreConstants.MOUNT_MOTORBUSY) != 0) {
               // The DEC motor is still moving so start sidereal tracking to hold position in RA
               if (Hemisphere == HemisphereOption.Northern) {
                  _Mount.EQ_StartRATrack(MountTracking.Sidereal, Hemisphere, AxisDirection.Forward);
               }
               else {
                  _Mount.EQ_StartRATrack(MountTracking.Sidereal, Hemisphere, AxisDirection.Reverse);
               }
            }
         }


         if (((MotorStatus[DEC_AXIS] & CoreConstants.MOUNT_MOTORBUSY) == 0) && RAAxisSlewing) {
            // DEC and RA motors have finished slewing at this point
            // We need to check if a new slew is needed to reduce the any difference
            // Caused by the Movement of the earth during the slew process

            switch (GotoParameters.SuperSafeMode) {
               case 0:
                  //  decrement the slew retry count
                  GotoParameters.SlewCount = GotoParameters.SlewCount - 1;

                  //  calculate the difference (arcsec)  between target and current coords
                  raDiff = 3600 * Math.Abs(Settings.CurrentMountPosition.Equatorial.RightAscension - GotoParameters.TargetCoordinate.RightAscension);
                  decDiff = 3600 * Math.Abs(Settings.CurrentMountPosition.Equatorial.Declination - GotoParameters.TargetCoordinate.Declination);


                  if ((GotoParameters.SlewCount > 0) && (TrackingState != TrackingStatus.Off)) {  //  Retry only if tracking is enabled
                                                                                                  //  aim to get within the goto resolution (default = 10 steps)
                     if (Settings.DevelopmentOptions.GotoResolution > 0 && raDiff <= GotoResolution[RA_AXIS] && decDiff <= GotoResolution[DEC_AXIS]) {
                        SlewComplete();
                     }
                     else {
                        // Re Execute a new RA-Only slew here
                        CalculateRADecAxisTargets();
                        GotoParameters.Rate = 0;
                        StartSlew(GotoParameters.TargetAxisPosition[RA_AXIS], GotoParameters.TargetAxisPosition[DEC_AXIS], GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                     }
                  }
                  else {
                     SlewComplete();
                  }
                  break;

               case 1:
                  //  move to target
                  GotoParameters.SuperSafeMode = 0;
                  CalculateRADecAxisTargets();
                  GotoParameters.Rate = 0;
                  // kick of an iterative slew to get us accurately to target RA
                  StartSlew(GotoParameters.TargetAxisPosition[RA_AXIS], GotoParameters.TargetAxisPosition[DEC_AXIS], GotoParameters.CurrentAxisPosition[RA_AXIS], GotoParameters.CurrentAxisPosition[DEC_AXIS]);
                  break;


               case 3:
                  //  we are at a limit position
                  if (GotoParameters.TargetAxisPosition[RA_AXIS] > CoreConstants.RAEncoder_Home_pos) {
                     //  dual axis slew to limit position nearest to target
                     GotoParameters.SuperSafeMode = 1;
                     if (!LimitsActive) {
                        StartSlew(MeridianWest, GotoParameters.TargetAxisPosition[DEC_AXIS], EmulatorAxisPosition[RA_AXIS], EmulatorAxisPosition[DEC_AXIS]);
                     }
                     else {
                        StartSlew(LimitWest, GotoParameters.TargetAxisPosition[DEC_AXIS], EmulatorAxisPosition[RA_AXIS], EmulatorAxisPosition[DEC_AXIS]);
                     }
                  }
                  else {
                     //  dual axis slew to limit position nearest to target
                     GotoParameters.SuperSafeMode = 1;
                     if (!LimitsActive) {
                        StartSlew(MeridianEast, GotoParameters.TargetAxisPosition[DEC_AXIS], EmulatorAxisPosition[RA_AXIS], EmulatorAxisPosition[DEC_AXIS]);
                     }
                     else {
                        StartSlew(LimitWest, GotoParameters.TargetAxisPosition[DEC_AXIS], EmulatorAxisPosition[RA_AXIS], EmulatorAxisPosition[DEC_AXIS]);
                     }
                  }
                  break;



            }
         }

      }


      private void SlewComplete()
      {
         _IsSlewing = false;
         RAAxisSlewing = false;
         GotoParameters.SupressHorizonLimits = false;

         // Slew may have terminated early if parked
         if (Settings.ParkStatus != ParkStatus.Parked) {
            // We've reached the desired target coords -resume tracking.
            //   Select Case gTrackingStatus
            switch (TrackingState) {
               case TrackingStatus.Off:
               case TrackingStatus.Sidereal:
                  StartSiderealTracking(true);
                  break;
               case TrackingStatus.Lunar:
                  StartLunarTracking(true);
                  break;
               case TrackingStatus.Solar:
                  StartSolarTracking(true);
                  break;
               case TrackingStatus.Custom:
                  StartCustomTracking(true);
                  break;
            }


            // TODO:   HC.Add_Message(oLangDll.GetLangString(5018) & " " & FmtSexa(gRA, False) & " " & FmtSexa(gDec, True))
            // TODO:   HC.Add_Message("Goto: SlewItereations=" & CStr(gMaxSlewCount - gSlewCount))
            // TODO:   HC.Add_Message("Goto: " & "RaDiff=" & Format$(str(ra_diff), "000.00") & " DecDiff=" & Format$(str(dec_diff), "000.00"))

            //    ' goto complete
            //    Call EQ_Beep(6)
         }


         //If gDisbleFlipGotoReset = 0 Then
         //  TODO:  HC.ChkForceFlip.value = 0
         //End If
      }


      /// <summary>
      /// Calculate the brake point position
      /// </summary>
      /// <param name="deltaVal">The slew distance in radians</param>
      /// <param name="axis"></param>
      /// <returns></returns>
      private double GetSlowdown(double deltaVal, int axis)
      {
         double initialOffset = (80000 / TotalStepsPer360[axis]) * CoreConstants.TWO_PI;
         double slowdownPosition = deltaVal - initialOffset;
         if (slowdownPosition < 0) {
            slowdownPosition = deltaVal * 0.5;
         }
         return slowdownPosition;
      }
   }
}

