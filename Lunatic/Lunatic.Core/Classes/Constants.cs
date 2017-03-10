﻿/*---------------------------------------------------------------------
   Copyright © 2017 Phil Crompton
   Permission is hereby granted to use this Software for any purpose
   including combining with commercial products, creating derivative
   works, and redistribution of source or binary code, without
   limitation or consideration. Any redistributed copies of this
   Software must include the above Copyright Notice.

   THIS SOFTWARE IS PROVIDED "AS IS". THE AUTHOR OF THIS CODE MAKES NO
   WARRANTIES REGARDING THIS SOFTWARE, EXPRESS OR IMPLIED, AS TO ITS
   SUITABILITY OR FITNESS FOR A PARTICULAR PURPOSE.
---------------------------------------------------------------------

CREDITS:
   Thanks must go to Raymund Sarmiento and Mr John Archbold for the 
   original EQMOD_ASCOM code on which protions of this code are based.

 ---------------------------------------------------------------------*/

using System;

namespace Lunatic.Core
{
   public static class Constants
   {
      public const string OrganisationName = "LunaticSoftware";
      public const double SIDEREAL_RATE_RADIANS = 2 * Math.PI / 86164.09065;
      public const double SIDEREAL_RATE_ARCSECS = 15.041067;  // arcsecs/sec  (60*60*360) / ((23*60*60)+(56*60)+4)
      public const double SOLAR_RATE = 15;
      public const double LUNAR_RATE = 14.511415;

      public const double DEG_RAD = 0.0174532925;
      public const double RAD_DEG = 57.2957795;
      public const double HRS_RAD = 0.2617993881;
      public const double RAD_HRS = 3.81971863;

      public const double EMUL_RATE = 20.98;             // 0.2 * 9024000/( (23*60*60)+(56*60)+4)
                                                         // 0.2 = 200ms

      public const double EMUL_RATE2 = 104.730403903004;         // (9024000/86164.0905)

      // 104.73040390300411747513310083625

      public const double ARCSECSTEP = 0.144;                  // .144 arcesconds / step

      // Iterative GOTO Constants
      //Public Const NUM_SLEW_RETRIES As Long = 5              // Iterative MAX retries
      public const double RA_Allowed_diff = 10;                // Iterative Slew minimum difference


      // Home Position of the mount (pointing at NCP/SCP)

         /// <summary>
         /// RA home position (radians)
         /// </summary>
      public const double RAEncoder_Home_pos = 0;
      /// <summary>
      /// DEC home position (radians) start at 90 deg
      /// </summary>
      public const double DECEncoder_Home_pos = 90 * DEG_RAD;      // Start at 90 Degree position

      /// <summary>
      /// ENCODER 0 Hour initial position (radians)
      /// </summary>
      public const double RAEncoder_Zero_pos = 0;       // ENCODER 0 Hour initial position
      /// <summary>
      /// ENCODER 0 Degree Initial position (radians)
      /// </summary>
      public const double DECEncoder_Zero_pos = 0;      // 

      // public const double Default_step = 9024000;              // Total Encoder count (EQ5/6)



      //Public Const EQ_MAXSYNC = &H111700

      // Public Const EQ_MAXSYNC_Const = &H88B80                 // Allow a 45 degree discrepancy

      /// <summary>
      /// Allow a 45 degree discrepancy (radians)
      /// </summary>
      public const double MaximumSyncDifference = (2 * Math.PI) / 8.0;    // Allow a 45.0 (360/8) but in degrees discrepancy in Radians.

   }
}
