using Lunatic.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Lunatic.Telescope.Classes
{
   public class Alignment
   {
      // Public Function EQ_NPointAppend(ByVal RightAscension As Double, ByVal Declination As Double, ByVal pLongitude As Double, ByVal pHemisphere As Long) As Boolean
      public static bool EQ_NPointAppend(double rightAscension, double declination, double longitude, HemisphereOption hemisphere)
      {


         //Dim tRa As Double
         //Dim tha As Double
         //Dim tPier As Double
         //Dim vRA As Double
         //Dim vDEC As Double
         double tRA;
         double tHA;
         int tPier = 0;
         double vRA;
         double vDEC;

         //Dim DeltaRa As Double
         //Dim DeltaDec As Double
         double deltaRA;
         double deltaDEC;

         //Dim curalign As Integer
         //Dim i As Integer
         //Dim Count As Integer
         //Dim ERa As Long
         //Dim EDec As Long
         //Dim RA_Hours As Double
         //Dim flipped As Boolean
         int alignmentStarCount;
         int i;
         int count;
         double eRA;
         double eDEC;
         double RAHours;
         bool isFlipped;

         Settings settings = SettingsProvider.Current.Settings;
         //    EQ_NPointAppend = True
         bool result = true;
         //    If gSlewStatus = True Then
         //        HC.Add_Message(oLangDll.GetLangString(5027))
         //        EQ_NPointAppend = False
         //        Exit Function
         //    End If
         if (settings.IsSlewing) {
            //TODO:        HC.Add_Message(oLangDll.GetLangString(5027))
            result = false;
            return result;
         }

         //TODO:    HC.EncoderTimer.Enabled = False

         alignmentStarCount = settings.AlignmentStars.Count() + 1;

         //    ' build alignment record
         eRA = SharedResources.Controller.MCGetAxisPosition(AxisId.Axis1_RA);  // EQGetMotorValues(0)
         eDEC = SharedResources.Controller.MCGetAxisPosition(AxisId.Axis2_DEC);  // EQGetMotorValues(1)
         vRA = rightAscension;
         vDEC = declination;


         //    ' look at current position and detemrine if flipped
         RAHours = LunaticMath.AxisHours(global::Lunatic.Core.Constants.RAEncoder_Zero_pos, eRA, hemisphere);
         isFlipped = (RAHours > 12);


         tHA = LunaticMath.RangeHA(vRA - LunaticMath.LocalSiderealTime(longitude));
         if (tHA < 0) {
            if (isFlipped) {
               if (hemisphere == HemisphereOption.Northern) {
                  tPier = 0;
               }
               else {
                  tPier = 1;
               }
               tRA = vRA;
            }
            else {
               if (hemisphere == HemisphereOption.Northern) {
                  tPier = 1;
               }
               else {
                  tPier = 0;
               }
               tRA = LunaticMath.Range24(vRA - 12);
            }
         }
         else {
            if (isFlipped) {
               if (hemisphere == HemisphereOption.Northern) {
                  tPier = 1;
               }
               else {
                  tPier = 0;
               }
               tRA = LunaticMath.Range24(vRA - 12);
            }
            else {
               if (hemisphere == HemisphereOption.Northern) {
                  tPier = 0;
               }
               else {
                  tPier = 1;
               }
               tRA = vRA;
            }
         }

         //    'Compute for Sync RA/DEC Encoder Values
         //    With AlignmentStars(curalign)
         //        .OrigTargetDEC = Declination
         //        .OrigTargetRA = RightAscension
         //        .TargetRA = Get_RAEncoderfromRA(tRa, 0, pLongitude, gRAEncoder_Zero_pos, gTot_RA, pHemisphere)
         //        .TargetDEC = Get_DECEncoderfromDEC(vDEC, tPier, gDECEncoder_Zero_pos, gTot_DEC, pHemisphere)
         //        .EncoderRA = ERa
         //        .EncoderDEC = EDec
         //        .AlignTime = Now

         //        DeltaRa = .TargetRA - .EncoderRA
         //        DeltaDec = .TargetDEC - .EncoderDEC

         //    End With
         //AlignmentData newStar = new AlignmentData() {
         //   OriginalTargetDEC = declination,
         //   OriginalTargetRA = rightAscension,
         //   TargetRA = LunaticMath.RAAxisPositionFromRA(tRA, 0, longitude, global::Lunatic.Core.Constants.RAEncoder_Zero_pos, hemisphere),
         //   TargetDEC = LunaticMath.DECAxisPositionFromDEC(vDEC, tPier, global::Lunatic.Core.Constants.DECEncoder_Zero_pos, hemisphere),
         //   EncoderRA = eRA,
         //   EncoderDEC = eDEC,
         //   AlignmentTime = DateTime.Now
         //};
         //settings.AlignmentStars.Add(newStar);
         //deltaRA = newStar.TargetRA - newStar.EncoderRA;
         //deltaDEC = newStar.TargetDEC - newStar.EncoderDEC;

         //TODO:    HC.EncoderTimer.Enabled = True
         //double maxSyncDifference = global::Lunatic.Core.Constants.MaximumSyncDifference;
         //if (((Math.Abs(deltaRA) < maxSyncDifference) && (Math.Abs(deltaDEC) < maxSyncDifference)) || settings.DisableSyncLimit) {


         //   // Use this data also for next sync until a three star is achieved
         //   settings.RA1Star = deltaRA;
         //   settings.DEC1Star = deltaDEC;

         //   if (alignmentStarCount < 3) {
         //      // TODO:           HC.Add_Message(str(curalign) & " " & oLangDll.GetLangString(6009))
         //   }
         //   else {
         //      if (alignmentStarCount == 3) {
         //         //                Call SendtoMatrix
         //      }
         //      else {
         //         // add new point
         //         //                Count = 1
         //         //                // copy points to temp array
         //         //                For i = 1 To curalign - 1
         //         List<AlignmentData> newStars = new List<AlignmentData>();
         //         foreach (AlignmentData star in settings.AlignmentStars) {
         //            deltaRA = Math.Abs(star.EncoderRA - eRA);
         //            deltaDEC = Math.Abs(star.EncoderDEC - eDEC);
         //            double proximity = LunaticMath.DegToRad(settings.AlignmentProximity);
         //            if (deltaRA > proximity || deltaDEC > proximity) {
         //               // point is far enough away from the new point - so keep it
         //               newStars.Add(star);
         //            }
         //            else {
         //               // TODO:                        HC.Add_Message ("Old Point too close " & CStr(deltaRA) & " " & CStr(deltadec) & " " & CStr(ProximityDec))
         //            }
         //            //                Next i
         //         }
         //         // Now clear and refresh the saved stars
         //         settings.AlignmentStars.Clear();
         //         foreach (AlignmentData star in newStars) {
         //            settings.AlignmentStars.Add(star);
         //         }
         //         //                AlignmentStars(Count) = AlignmentStars(curalign)
         //         //                curalign = Count
         //         //                gAlignmentStars_count = curalign


         //         //                Call SendtoMatrix

         //         //                StarEditform.RefreshDisplay = true

         //      }
         //   }
         //}
         //else {
         //   //        // sync is too large!
         //   //        result = false
         //   //        HC.Add_Message(oLangDll.GetLangString(6004))
         //   //        HC.Add_Message("Target  RA=" & FmtSexa(gRA, false))
         //   //        HC.Add_Message("Sync    RA=" & FmtSexa(RightAscension, false))
         //   //        HC.Add_Message("Target DEC=" & FmtSexa(gDec, true))
         //   //        HC.Add_Message("Sync   DEC=" & FmtSexa(Declination, true))
         //   //    }


         //   //    if gSaveAPresetOnAppend = 1 {
         //   //        ' don't write emtpy list!
         //   //        if(gAlignmentStars_count > 0) {
         //   //            'idx = GetPresetIdx
         //   //            Call SaveAlignmentStars(GetPresetIdx, "")
         //}


         //if (settings.SaveAlignmentStarsOnAppend) {
         //   SettingsProvider.Current.SaveSettings();
         //}
         //else {
         //   settings.AlignmentStars.Remove(newStar);
         //}
         return result;
      }


//      Public Sub SendtoMatrix()

//Dim i As Integer


//    For i = 1 To gAlignmentStars_count
//       ct_Points(i).x = AlignmentStars(i).TargetRA
//       ct_Points(i).Y = AlignmentStars(i).TargetDEC
//       ct_Points(i).z = 1
//       ct_PointsC(i) = EQ_sp2Cs(ct_Points(i))
//       my_Points(i).x = AlignmentStars(i).EncoderRA
//       my_Points(i).Y = AlignmentStars(i).EncoderDEC
//       my_Points(i).z = 1
//       my_PointsC(i) = EQ_sp2Cs(my_Points(i))
//    Next i

//    'Activate Matrix here
//    Call ActivateMatrix

//End Sub

//Public Sub ActivateMatrix()

//Dim i As Integer

//    ' assume false - will set true later if 3 stars active
//    gThreeStarEnable = False

//    HC.EncoderTimer.Enabled = False
//    If HC.PolarEnable.Value = 1 Then
//        If gAlignmentStars_count >= 3 Then
//            i = EQ_AssembleMatrix_Taki(0, 0, ct_PointsC(1), ct_PointsC(2), ct_PointsC(3), my_PointsC(1), my_PointsC(2), my_PointsC(3))
//            i = EQ_AssembleMatrix_Affine(0, 0, my_PointsC(1), my_PointsC(2), my_PointsC(3), ct_PointsC(1), ct_PointsC(2), ct_PointsC(3))
//            gThreeStarEnable = True
//        End If
//    Else
//        If gAlignmentStars_count >= 3 Then
//            i = EQ_AssembleMatrix_Taki(0, 0, ct_PointsC(1), ct_PointsC(2), ct_PointsC(3), my_PointsC(1), my_PointsC(2), my_PointsC(3))
//            i = EQ_AssembleMatrix_Affine(0, 0, my_PointsC(1), my_PointsC(2), my_PointsC(3), ct_PointsC(1), ct_PointsC(2), ct_PointsC(3))
//            gThreeStarEnable = True
//        End If
//    End If
//    HC.EncoderTimer.Enabled = True

//End Sub


   }
}
