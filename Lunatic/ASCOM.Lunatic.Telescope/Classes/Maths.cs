using Lunatic.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* VB6 type declation characters
  % Integer
  & Long
  ! Single
  # Double
  $ String
  @ Currency
*/
namespace ASCOM.Lunatic.Telescope
{
   public class Maths
   {
      public static double Get_EncoderHours(double encOffset0, double encoderval, double Tot_enc, int hmspr)
      {

         double i;
         double result = 0.0;
         // Compute in Hours the encoder value based on 0 position value (RAOffset0)
         // and Total 360 degree rotation microstep count (Tot_Enc

         if (encoderval > encOffset0) {
            i = ((encoderval - encOffset0) / Tot_enc) * 24;
            i = 24 - i;
         }
         else {
            i = ((encOffset0 - encoderval) / Tot_enc) * 24;
         }

         if (hmspr == 0) {
            result = Range24(i + 6d);       // Set to true Hours which is perpendicula to RA Axis
         }
         else {
            result = Range24((24 - i) + 6d);
         }
         return result;
      }

      //      public static double Get_EncoderfromHours(encOffset0 As Double, hourval As Double, Tot_enc As Double, hmspr As Long) As Long


      //    hourval = Range24(hourval - 6#)         // Re-normalize from a perpendicular position
      //    If hmspr = 0 {
      //        If(hourval< 12) {
      //           Get_EncoderfromHours = encOffset0 - ((hourval // 24) * Tot_enc)
      //        } else {
      //            Get_EncoderfromHours = (((24 - hourval) // 24) * Tot_enc) + encOffset0
      //        }
      //    } else {
      //        If(hourval< 12) {
      //          Get_EncoderfromHours = ((hourval // 24) * Tot_enc) + encOffset0
      //        } else {
      //            Get_EncoderfromHours = encOffset0 - (((24 - hourval) // 24) * Tot_enc)
      //        }
      //    }

      //}

      //   public static double Get_EncoderfromDegrees(encOffset0 As Double, degval As Double, Tot_enc As Double, Pier As Double, hmspr As Long) As Long

      //    If hmspr = 1 { degval = 360 - degval
      //    If(degval > 180) And(Pier = 0) {
      //        Get_EncoderfromDegrees = encOffset0 - (((360 - degval) // 360) * Tot_enc)
      //    } else {
      //        Get_EncoderfromDegrees = ((degval // 360) * Tot_enc) + encOffset0
      //    }

      //}


      //public static double Get_EncoderDegrees(encOffset0 As Double, encoderval As Double, Tot_enc As Double, hmspr As Long)
      //{

      //   Dim i As Double

      //    // Compute in Hours the encoder value based on 0 position value (EncOffset0)
      //    // and Total 360 degree rotation microstep count (Tot_Enc

      //   If encoderval > encOffset0 {
      //        i = ((encoderval - encOffset0) // Tot_enc) * 360
      //         } else {
      //        i = ((encOffset0 - encoderval) // Tot_enc) * 360
      //        i = 360 - i
      //    }

      //    If hmspr = 0 {
      //        Get_EncoderDegrees = Range360(i)
      //    } else {
      //        Get_EncoderDegrees = Range360(360 - i)
      //    }
      //}

      //// Function that will ensure that the DEC value will be between -90 to 90
      //// Even if it is set at the other side of the pier

      //public static double Range_DEC(decdegrees As Double)
      //{

      //   If(decdegrees >= 270) And(decdegrees <= 360) {
      //       Range_DEC = decdegrees - 360
      //        Exit Function
      //    }


      //    If(decdegrees >= 180) And(decdegrees < 270) {
      //        Range_DEC = 180 - decdegrees
      //        Exit Function
      //    }


      //    If(decdegrees >= 90) And(decdegrees < 180) {
      //        Range_DEC = 180 - decdegrees
      //        Exit Function
      //    }


      //    Range_DEC = decdegrees

      //}



      //public static double Get_RAEncoderfromRA(ra_in_hours As Double, dec_in_degrees As Double, pLongitude As Double, encOffset0 As Double, Tot_enc As Double, hmspr As Long) As Long

      //Dim i As Double
      //Dim j As Double

      //    i = ra_in_hours - EQnow_lst(pLongitude* DEG_RAD)


      //    If hmspr = 0 {
      //        If(dec_in_degrees > 90) And(dec_in_degrees <= 270) { i = i - 12#
      //    } else {
      //        If(dec_in_degrees > 90) And(dec_in_degrees <= 270) { i = i + 12#
      //    }

      //    i = Range24(i)


      //    Get_RAEncoderfromRA = Get_EncoderfromHours(encOffset0, i, Tot_enc, hmspr)


      //}

      //public static double Get_RAEncoderfromAltAz(Alt_in_deg As Double, Az_in_deg As Double, pLongitude As Double, pLatitude As Double, encOffset0 As Double, Tot_enc As Double, hmspr As Long) As Long

      //Dim i As Double
      //Dim ttha As Double
      //Dim ttdec As Double

      //    aa_hadec(pLatitude* DEG_RAD), (Alt_in_deg* DEG_RAD), ((360# - Az_in_deg) * DEG_RAD), ttha, ttdec
      //    i = (ttha* RAD_HRS)
      //    i = Range24(i)
      //    Get_RAEncoderfromAltAz = Get_EncoderfromHours(encOffset0, i, Tot_enc, hmspr)


      //}

      //public static double Get_DECEncoderfromAltAz(Alt_in_deg As Double, Az_in_deg As Double, pLongitude As Double, pLatitude As Double, encOffset0 As Double, Tot_enc As Double, Pier As Double, hmspr As Long) As Long

      //Dim i As Double
      //Dim ttha As Double
      //Dim ttdec As Double

      //    aa_hadec(pLatitude* DEG_RAD), (Alt_in_deg* DEG_RAD), ((360# - Az_in_deg) * DEG_RAD), ttha, ttdec
      //    i = ttdec* RAD_DEG // tDec was in Radians
      //    If Pier = 1 { i = 180 - i
      //    Get_DECEncoderfromAltAz = Get_EncoderfromDegrees(encOffset0, i, Tot_enc, Pier, hmspr)


      //}

      //public static double Get_DECEncoderfromDEC(dec_in_degrees As Double, Pier As Double, encOffset0 As Double, Tot_enc As Double, hmspr As Long) As Long

      //Dim i As Double

      //    i = dec_in_degrees
      //    If Pier = 1 { i = 180 - i
      //    Get_DECEncoderfromDEC = Get_EncoderfromDegrees(encOffset0, i, Tot_enc, Pier, hmspr)


      //}

      //public static double printhex(inpval As Double) As String

      //    printhex = " " & Hex$((inpval And & HF00000) // 1048576 And & HF) + Hex$((inpval And & HF0000) // 65536 And 0xF) + Hex$((inpval And 0xF000) // 4096 And 0xF) + Hex$((inpval And 0xF00) // 256 And 0xF) + Hex$((inpval And 0xF0) // 16 And 0xF) + Hex$(inpval And 0xF)

      //}

      //public static double FmtSexa(ByVal N As Double, ShowPlus As Boolean) As String
      //    Dim sg As String
      //    Dim us As String
      //    Dim ms As String
      //    Dim ss As String
      //    Dim u As Long
      //    Dim m As Long
      //    Dim fmt


      //    sg = "+"                                // Assume positive
      //    If N< 0 {                           // Check neg.
      //        N = -N                              // Make pos.
      //        sg = "-"                            // Remember sign
      //    }

      //    m = Fix(N)                              // Units (deg or hr)
      //    us = Format$(m, "00")

      //    N = (N - m) * 60#
      //    m = Fix(N)                              // Minutes
      //    ms = Format$(m, "00")

      //    N = (N - m) * 60#
      //    m = Fix(N)                              // Minutes
      //    ss = Format$(m, "00")

      //    FmtSexa = us & ":" & ms & ":" & ss
      //    If ShowPlus Or(sg = "-") { FmtSexa = sg & FmtSexa


      //}
      //public static double EQnow_lst(plong As Double)
      //{

      //   Dim typTime As SYSTEMTIME
      //    Dim eps As Double
      //    Dim lst As Double
      //    Dim deps As Double
      //    Dim dpsi As Double
      //    Dim mjd As Double

      ////    mjd = vb_mjd(CDbl(Now) + gGPSTimeDelta)

      //   GetSystemTime typTime
      //    mjd = vb_mjd(CDbl(gEQTimeDelta + Now + (typTime.wMilliseconds // 86400000)))
      //    Call utc_gst(mjd_day(mjd), mjd_hr(mjd), lst)
      //    lst = lst + radhr(plong)
      //    Call obliq(mjd, eps)
      //    Call nut(mjd, deps, dpsi)
      //    lst = lst + radhr(dpsi * Cos(eps + deps))
      //    Call range(lst, 24#)

      //    EQnow_lst = lst
      ////    EQnow_lst = now_lst(plong)

      //}


      //public static double EQnow_lst_norange()
      //{

      //   Dim typTime As SYSTEMTIME
      //    Dim mjd As Double
      //    Dim MTMP As Double

      //    GetSystemTime typTime
      //    mjd = (typTime.wMinute * 60) + (typTime.wSecond) + (typTime.wMilliseconds // 1000)
      //    MTMP = (typTime.wHour)
      //    MTMP = MTMP * 3600
      //    mjd = mjd + MTMP + (typTime.wDay * 86400)


      //    EQnow_lst_norange = mjd

      //}


      //public static double EQnow_lst_time(plong As Double, ptime As Double)
      //{

      //   Dim eps As Double
      //    Dim lst As Double
      //    Dim deps As Double
      //    Dim dpsi As Double
      //    Dim mjd As Double


      //    mjd = vb_mjd(ptime)
      //    Call utc_gst(mjd_day(mjd), mjd_hr(mjd), lst)
      //    lst = lst + radhr(plong)
      //    Call obliq(mjd, eps)
      //    Call nut(mjd, deps, dpsi)
      //    lst = lst + radhr(dpsi * Cos(eps + deps))
      //    Call range(lst, 24#)

      //    EQnow_lst_time = lst

      //}


      //public static double SOP_DEC(ByVal DEC As Double) As PierSide2


      //    DEC = Abs(DEC - 180)


      //    If DEC <= 90 {
      //        SOP_DEC = PierEast2
      //    } else {
      //        SOP_DEC = PierWest2
      //    }


      //}

      //public static double SOP_Physical(vha As Double) As PierSide2
      //Dim ha As Double


      //    ha = RangeHA(vha - 6#)

      //    If gAscomCompatibility.SwapPhysicalSideOfPier {
      //        SOP_Physical = IIf(ha >= 0, PierWest2, PierEast2)
      //    } else {
      //        SOP_Physical = IIf(ha >= 0, PierEast2, PierWest2)
      //    }




      //}

      //public static double SOP_Pointing(ByVal DEC As Double) As PierSide2


      //    If DEC <= 90 Or DEC >= 270 {
      //        If gAscomCompatibility.SwapPointingSideOfPier {
      //            SOP_Pointing = PierEast2
      //        } else {
      //            SOP_Pointing = PierWest2
      //        }
      //    } else {
      //        If gAscomCompatibility.SwapPointingSideOfPier {
      //            SOP_Pointing = PierWest2
      //        } else {
      //            SOP_Pointing = PierEast2
      //        }
      //    }

      //    // in the south east is west and west is east!
      //    If gHemisphere = 1 {
      //        If SOP_Pointing = PierWest2 {
      //            SOP_Pointing = PierEast2
      //        } else {
      //            SOP_Pointing = PierWest2
      //        }
      //    }


      //}
      //public static double SOP_RA(vRA As Double, pLongitude As Double) As PierSide2
      //Dim i As Double

      //    i = vRA - EQnow_lst(pLongitude* DEG_RAD)
      //    i = RangeHA(i - 6#)
      //    SOP_RA = IIf(i < 0, PierEast2, PierWest2)

      //}

      public static double Range24(double vha)
      {

         while (vha < 0) {
            vha = vha + 24;
         }
         while (vha >= 24) {
            vha = vha - 24;
         }
         return vha;
      }

      public static double Range360(double vdeg)
      {
         while (vdeg < 0) {
            vdeg = vdeg + 360;
         }
         while (vdeg >= 360) {
            vdeg = vdeg - 360;
         }
         return vdeg;
      }

      public static double Range90(double vdeg)
      {
         while (vdeg < -90) {
            vdeg = vdeg + 360;
         }
         while (vdeg >= 360) {
            vdeg = vdeg - 90;
         }
         return vdeg;
      }

      public static double RangeHA(double ha)
      {
         while (ha < -12) {
            ha = ha + 24;
         }
         while (ha >= 12) {
            ha = ha - 24;
         }
         return ha;
      }

      public static double GetSlowdown(double deltaval)
      {
         double i = deltaval - 80000;
         if (i < 0) {
            i = deltaval * 0.5;
         }
         return i;
      }

      // Originally used globals gRA1Star + gRASync01;
      public static double Delta_RA_Map(double raEncoder, double ra1Star, double raSync)
      {
         return raEncoder + ra1Star + raSync;
      }

      // Originally used the globals gDEC1Star + gDECSync01
      public static double Delta_DEC_Map(double DecEncoder, double dec1Star, double decSync)
      {
         return DecEncoder + dec1Star + decSync;
      }


      public static Coordt Delta_Matrix_Map(double ra, double dec)
      {
         int i;
         Coord obtmp;
         Coord obtmp2;
         Coordt result;

         if ((ra >= 0x1000000) || (dec >= 0x1000000)) {
            result.X = ra;
            result.Y = dec;
            result.Z = 1;
            result.F = 0;
         }
         else {

            obtmp.X = ra;

            obtmp.Y = dec;

            obtmp.Z = 1;

            // re transform based on the nearest 3 stars

            i = EQ_UpdateTaki(ra, dec);



            obtmp2 = EQ_plTaki(obtmp);



            result.X = obtmp2.X;


            result.Y = obtmp2.Y;


            result.Z = 1;


            result.F = i;
               }
         return result;

      }



      //public static double Delta_Matrix_Reverse_Map(ByVal RA As Double, ByVal DEC As Double) As Coordt

      //Dim i As Integer
      //Dim obtmp As Coord
      //Dim obtmp2 As Coord

      //    If(RA >= 0x1000000) Or(DEC >= 0x1000000) {
      //      Delta_Matrix_Reverse_Map.X = RA
      //      Delta_Matrix_Reverse_Map.Y = DEC

      //      Delta_Matrix_Reverse_Map.z = 1

      //      Delta_Matrix_Reverse_Map.F = 0

      //      Exit Function

      //  }


      //  obtmp.X = RA + gRASync01

      //  obtmp.Y = DEC + gDECSync01

      //  obtmp.z = 1

      //    // re transform using the 3 nearest stars

      //  i = EQ_UpdateAffine(obtmp.X, obtmp.Y)

      //  obtmp2 = EQ_plAffine(obtmp)


      //  Delta_Matrix_Reverse_Map.X = obtmp2.X

      //  Delta_Matrix_Reverse_Map.Y = obtmp2.Y

      //  Delta_Matrix_Reverse_Map.z = 1

      //  Delta_Matrix_Reverse_Map.F = i


      //  gSelectStar = 0


      //}



      //public static double DeltaSync_Matrix_Map(ByVal RA As Double, ByVal DEC As Double) As Coordt
      //Dim i As Long

      //    If(RA >= 0x1000000) Or(DEC >= 0x1000000) { GoTo HandleError

      //  i = GetNearest(RA, DEC)
      //    If i<> -1 {
      //        gSelectStar = i
      //        DeltaSync_Matrix_Map.X = RA + (ct_Points(i).X - my_Points(i).X) + gRASync01
      //        DeltaSync_Matrix_Map.Y = DEC + (ct_Points(i).Y - my_Points(i).Y) + gDECSync01
      //        DeltaSync_Matrix_Map.z = 1
      //        DeltaSync_Matrix_Map.F = 0
      //    } else {
      //HandleError:
      //        DeltaSync_Matrix_Map.X = RA
      //        DeltaSync_Matrix_Map.Y = DEC
      //        DeltaSync_Matrix_Map.z = 0
      //        DeltaSync_Matrix_Map.F = 0
      //    }
      //}


      //public static double DeltaSyncReverse_Matrix_Map(ByVal RA As Double, ByVal DEC As Double) As Coordt
      //Dim i As Long

      //    If(RA >= 0x1000000) Or(DEC >= 0x1000000) Or gAlignmentStars_count = 0 { GoTo HandleError

      //  i = GetNearest(RA, DEC)


      //    If i<> -1 {
      //        gSelectStar = i
      //        DeltaSyncReverse_Matrix_Map.X = RA - (ct_Points(i).X - my_Points(i).X)
      //        DeltaSyncReverse_Matrix_Map.Y = DEC - (ct_Points(i).Y - my_Points(i).Y)
      //        DeltaSyncReverse_Matrix_Map.z = 1
      //        DeltaSyncReverse_Matrix_Map.F = 0
      //    } else {
      //HandleError:
      //        DeltaSyncReverse_Matrix_Map.X = RA
      //        DeltaSyncReverse_Matrix_Map.Y = DEC
      //        DeltaSyncReverse_Matrix_Map.z = 1
      //        DeltaSyncReverse_Matrix_Map.F = 0
      //    }

      //}
      //public static double GetQuadrant(ByRef tmpcoord As Coord) As Integer
      //Dim ret As Integer


      //    If tmpcoord.X >= 0 {
      //        If tmpcoord.Y >= 0 {
      //            ret = 0
      //        } else {
      //            ret = 1
      //        }
      //    } else {
      //        If tmpcoord.Y >= 0 {
      //            ret = 2
      //        } else {
      //            ret = 3
      //        }
      //    }


      //    GetQuadrant = ret

      //}


      //public static double GetNearest(ByVal RA As Double, ByVal DEC As Double) As Integer
      //Dim i As Integer
      //Dim tmpcoord As Coord
      //Dim tmpcoord2 As Coord
      //Dim datholder(1 To MAX_STARS)
      //{
      //   Dim datholder2(1 To MAX_STARS) As Integer
      //Dim Count As Integer


      //    tmpcoord.X = RA
      //    tmpcoord.Y = DEC
      //    tmpcoord = EQ_sp2Cs(tmpcoord)

      //    Count = 0


      //    For i = 1 To gAlignmentStars_count


      //        tmpcoord2 = my_PointsC(i)
      //        Select Case gPointFilter

      //            Case 0
      //                // all points


      //            Case 1
      //                // only consider points on this side of the meridian
      //                If tmpcoord2.Y* tmpcoord.Y < 0 {
      //                     GoTo NextPoint
      //                 }


      //             Case 2
      //                // local quadrant
      //                If GetQuadrant(tmpcoord) <> GetQuadrant(tmpcoord2) {
      //                    GoTo NextPoint
      //                }

      //        End Select

      //        Count = Count + 1
      //        If HC.CheckLocalPier.Value = 1 {
      //            // calculate polar distance
      //            datholder(Count) = (my_Points(i).X - RA) ^ 2 + (my_Points(i).Y - DEC) ^ 2
      //        } else {
      //            // calculate cartesian disatnce
      //            datholder(Count) = (tmpcoord2.X - tmpcoord.X) ^ 2 + (tmpcoord2.Y - tmpcoord.Y) ^ 2
      //        }


      //        datholder2(Count) = i

      //NextPoint:
      //    Next i

      //    If Count = 0 {
      //        GetNearest = -1
      //    } else {
      //    //    i = EQ_FindLowest(datholder(), 1, gAlignmentStars_count)
      //        i = EQ_FindLowest(datholder(), 1, Count)
      //        If i = -1 {
      //            GetNearest = -1
      //        } else {
      //            GetNearest = datholder2(i)
      //        }
      //    }

      //}

   }
}
