using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Lunatic.Telescope
{
   public class Globals
   {
      // Define all Global Variables


      public double Xshift { get; set; }
      public double Yshift { get; set; }
      public double Xmouse { get; set; }
      public double Ymouse { get; set; }


      public double EQ_MAXSYNC { get; set; }                                // Max Sync Diff
      public double SiderealRate { get; set; }                              // Sidereal rate arcsecs/sec
      public double Mount_Ver { get; set; }                                 // Mount Version
      public int Mount_Features { get; set; }                              // Mount Features

      public double RA_LastRate { get; set; }                               // Last PEC Rate
      public int pl_interval { get; set; }                              // Pulseguide Interval

      public double eqres { get; set; }
      public double Tot_step { get; set; }                                  // Total Common RA-Encoder Steps
      public double Tot_RA { get; set; }                                    // Total RA Encoder Steps
      public double Tot_DEC { get; set; }                                   // Total DEC Encoder Steps
      public double RAWormSteps { get; set; }                               // Steps per RA worm revolution
      public double RAWormPeriod { get; set; }                              // Period of RA worm revolution
      public double DECWormSteps { get; set; }                              // Steps per DEC worm revolution
      public double DECWormPeriod { get; set; }                             // Period of DEC worm revolution

      public double Latitude { get; set; }                                  // Site Latitude
      public double Longitude { get; set; }                                 // Site Longitude
      public double Elevation { get; set; }                                 // Site Elevation
      public int Hemisphere { get; set; }

      public double DECEncoder_Home_pos { get; set; }                       // DEC HomePos - Varies with different mounts

      public double RA_Encoder { get; set; }                                // RA Current Polled RA Encoder value
      public double Dec_Encoder { get; set; }                               // DEC Current Polled Encoder value
      public double RA_Hours { get; set; }                                  // RA Encoder to Hour position
      public double Dec_Degrees { get; set; }                               // DEC Encoder to Degree position Ranged to -90 to 90
      public double Dec_DegNoAdjust { get; set; }                           // DEC Encoder to actual degree position
      public double RAStatus { get; set; }                                  // RA Polled Motor Status
      public bool RAStatus_slew { get; set; }                              // RA motor tracking poll status
      public double DECStatus { get; set; }                                 // DEC Polloed motor status


      public double RA_Limit_East { get; set; }                             // RA Limit at East Side
      public double RA_Limit_West { get; set; }                             // RA Limit at West Side


      public double RA1Star { get; set; }                                   // Initial RA Alignment adjustment
      public double DEC1Star { get; set; }                                  // Initial DEC Alignment adjustment


      public double RASync01 { get; set; }                                  // Initial RA sync adjustment
      public double DECSync01 { get; set; }                                 // Initial DEC sync adjustment

      public double RA { get; set; }
      public double Dec { get; set; }
      public double Alt { get; set; }
      public double Az { get; set; }
      public double ha { get; set; }
      public double SOP { get; set; }


      public int TrackingStatus { get; set; }
      public bool SlewStatus { get; set; }

      public double RAMoveAxis_Rate { get; set; }
      public double DECMoveAxis_Rate { get; set; }


      // Added for emulated Stepper Counters
      public double EmulRA { get; set; }
      public double EmulDEC { get; set; }
      public bool EmulOneShot { get; set; }
      public bool EmulNudge { get; set; }

      public double Current_time { get; set; }
      public double Last_time { get; set; }
      public double EmulRA_Init { get; set; }

      public PierSide SideofPier { get; set; }


      public int RAEncoderPolarHomeGoto { get; set; }
      public int DECEncoderPolarHomeGoto { get; set; }
      public int RAEncoderUNPark { get; set; }
      public int DECEncoderUNPark { get; set; }
      public int RAEncoderPark { get; set; }
      public int DECEncoderPark { get; set; }
      public int RAEncoderlastpos { get; set; }
      public int DECEncoderlastpos { get; set; }
      public int EQparkstatus { get; set; }

      public int EQRAPulseDuration { get; set; }
      public int EQDECPulseDuration { get; set; }
      public int EQRAPulseEnd { get; set; }
      public int EQDECPulseEnd { get; set; }
      public int EQDECPulseStart { get; set; }
      public int EQRAPulseStart { get; set; }
      public bool EQPulsetimerflag { get; set; }

      public double EQTimeDelta { get; set; }


      // Public variables for Custom Tracking rates

      public double DeclinationRate { get; set; }
      public double RightAscensionRate { get; set; }


      // Public Variables for Spiral Slew

      public int SPIRAL_JUMP { get; set; }
      public double Declination_Start { get; set; }
      public double RightAscension_Start { get; set; }
      public double Declination_Dir { get; set; }
      public double RightAscension_Dir { get; set; }
      public int Declination_Len { get; set; }
      public int RightAscension_Len { get; set; }

      public double Spiral_AxisFlag { get; set; }



      // Public variables for debugging

      public double Affine1 { get; set; }
      public double Affine2 { get; set; }
      public double Affine3 { get; set; }

      public double Taki1 { get; set; }
      public double Taki2 { get; set; }
      public double Taki3 { get; set; }


      //Pulseguide Indicators

      public int MAX_plotpoints { get; set; }

      public int MAX_RAlevel { get; set; }
      public int MAX_DEClevel { get; set; }
      public int Plot_ra_pos { get; set; }
      public int Plot_dec_pos { get; set; }
      public double plot_ra_cur { get; set; }
      public double Plot_dec_cur { get; set; }
      public double RAHeight { get; set; }
      public double DecHeight { get; set; }

      // Polar Alignment Variables

      public double PolarAlign_RA { get; set; }
      public double PolarAlign_DEC { get; set; }

      public Globals()
      {
         MAX_plotpoints = 100;
      }
   }
}
