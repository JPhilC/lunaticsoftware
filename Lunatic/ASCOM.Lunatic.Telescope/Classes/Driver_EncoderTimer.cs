using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lunatic.Core;
using Lunatic.Core.Geometry;
using System.Timers;

namespace ASCOM.Lunatic.Telescope
{
   partial class Telescope
   {
      #region Timers ...
      
      private Timer _EncoderTimer;
      private bool ProcessingEncoderTimerTick = false;
      private double LastTickTime;

      private void _EncoderTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
      {
         System.Diagnostics.Debug.WriteLine("Processing driver Encoder Timer tick");
         AxisPosition adjustedAxisPosition;
         double localJulianTimeUTC;
         // Don't bother if the Site Lat and Long have not been set.
         if (_SiteLongitude == double.MinValue || _SiteLatitude == double.MinValue) {
            return;
         }

         if (!ProcessingEncoderTimerTick) {
            ProcessingEncoderTimerTick = true;
            if (EmulatorOneShot || Slewing || Settings.CheckRASync) {
               // Read true motor positions
               CurrentAxisPosition[RA_AXIS] = _Mount.MCGetAxisPosition(AxisId.Axis1_RA);
               CurrentAxisPosition[DEC_AXIS] = _Mount.MCGetAxisPosition(AxisId.Axis2_DEC);
               EmulatorOneShot = false;
            }
            localJulianTimeUTC = AscomTools.LocalJulianTimeUTC;      // Grab the time at the time the motors were read
            double elapsedTime = (localJulianTimeUTC - Settings.CurrentMountPosition.LocalJulianTimeUTC) * 24.0;     // Elapsed time since last position update in hours.
            double last = SiderealTime;


            if (!Settings.ThreeStarEnable) {
               adjustedAxisPosition = DeltaMap(CurrentAxisPosition);
               // Calculate the suggested Equatorial coordinate (and hence AltAz)
               double tempHours = AstroConvert.AxisHours(AxisZeroPosition[RA_AXIS], adjustedAxisPosition[RA_AXIS], Hemisphere);
               double decDegreesNoAdjustment = AstroConvert.AxisDegrees(AxisZeroPosition[DEC_AXIS], adjustedAxisPosition[DEC_AXIS], Hemisphere);
               double decDegrees = AstroConvert.RangeDEC(decDegreesNoAdjustment);
               double tRa = last + tempHours + elapsedTime;
               if (Hemisphere == HemisphereOption.Northern) {
                  tRa += 12.0;
               }
               else {
                  tRa -= 12.0;
               }
               // Update the current mount position
               Settings.CurrentMountPosition.Refresh(new EquatorialCoordinate(AstroConvert.Range24(tRa), decDegrees),
                  adjustedAxisPosition, AscomTools.Transform, localJulianTimeUTC);
               // Create a suggested mount coordinate.
            }
            else {
               //switch (SyncAlignmentMode) {


               //   case SyncAlignmentModeOptions.NearestStar:
               //      tmpCoord = DeltaSyncMatrixMap(suggestedPosition.ObservedAltAzimuth);
               //      CurrentMountPosition = new MountCoordinate(tmpCoord, ascomTools.Transform, localJulianTimeUTC); 
               //      CurrentAxisPosition[RA_AXIS] = tmpCoord.X;
               //      CurrentAxisPosition[DEC_AXIS] = tmpCoord.Y;
               //      break;


               //   //case 1:
               //   //   tmpcoord = Delta_Matrix_Reverse_Map(gEmulRA, gEmulDEC)
               //   //    gRA_Encoder = tmpcoord.X
               //   //    gDec_Encoder = tmpcoord.Y
               //   //      break;


               //   case SyncAlignmentModeOptions.ThreePoint:
               //      tmpCoord = DeltaMatrixReverseMap(suggestedPosition.ObservedAltAzimuth);
               //      CurrentAxisPosition[RA_AXIS] = tmpCoord.X;
               //      CurrentAxisPosition[DEC_AXIS] = tmpCoord.Y;
               //      if (!tmpCoord.Flag) {
               //         tmpCoord = DeltaSyncMatrixMap(suggestedPosition.ObservedAltAzimuth);
               //         CurrentAxisPosition[RA_AXIS] = tmpCoord.X;
               //         CurrentAxisPosition[DEC_AXIS] = tmpCoord.Y;
               //      }
               //      break;

               //}
            }


            //// Convert RA_Encoder to Hours
            //RAHours = AstroConvert.AxisHours(AxisZeroPosition[RA_AXIS], CurrentAxisPosition[RA_AXIS], Hemisphere);

            //// Convert DEC_Encoder to DEC Degrees
            //DecDegreesNoAdjustment = AstroConvert.AxisDegrees(AxisZeroPosition[DEC_AXIS], CurrentAxisPosition[DEC_AXIS], Hemisphere);
            //DecDegrees = AstroConvert.RangeDEC(DecDegreesNoAdjustment);

            //tRa = last + RAHours;
            //if (Hemisphere == HemisphereOption.Northern) {
            //   if (DecDegreesNoAdjustment > 90 && DecDegreesNoAdjustment <= 270) {
            //      tRa = tRa - 12.0;
            //   }
            //}
            //else {
            //   if (DecDegreesNoAdjustment <= 90 || DecDegreesNoAdjustment > 270) {
            //      tRa = tRa + 12.0;
            //   }
            //}

            // assign global RA/Dec
            //gRA = tRa;
            //gDec = DecDegrees;
            //CurrentMountPosition = new MountCoordinate(new EquatorialCoordinate(AstroConvert.Range24(tRa), DecDegrees),
            //   ascomTools.Transform, localJulianTimeUTC);

            //       // calc alt/az poition
            //       hadec_aa(gLatitude * DEG_RAD), (gha * HRS_RAD), (gDec_Degrees * DEG_RAD), tAlt, tAz
            //// asign global Alt/Az
            //gAlt = tAlt * RAD_DEG           // convert to degrees from Radians
            //gAz = 360# - (tAz * RAD_DEG)    //  convert to degrees from Radians

            // Poll the Motor Status while slew is active
            if (Slewing) {
               MotorStatus[RA_AXIS] = _Mount.EQ_GetMotorStatus(AxisId.Axis1_RA);
               MotorStatus[DEC_AXIS] = _Mount.EQ_GetMotorStatus(AxisId.Axis2_DEC);
               if (ParkStatus == ParkStatus.Unparked) {
                  //TODO: ManageGoto();
               }
            }


            // AlignmentCountLbl.Caption = CStr(gAlignmentStars_count)


            // do limit management
            // TODO: LimitsExecute();
            ProcessingEncoderTimerTick = false;
         }
      }
      #endregion

      #region helper functions ...
      #endregion

   }
}
