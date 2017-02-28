/*---------------------------------------------------------------------
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;//Public Const NUM_SLEW_RETRIES As Long = 5                   //

namespace ASCOM.Lunatic.Telescope
{
   public static class Contants
   {
      public const double EMUL_RATE = 20.98;                      //  0.2 * 9024000/( (23*60*60)+(56*60)+4)
                                                                  // 0.2 = 200ms
      public const double EMUL_RATE2 = 104.730403903004;          // (9024000/86164.0905)
                                                                  // 104.73040390300411747513310083625

      public const double ARCSECSTEP = 0.144;                     // .144 arcesconds / step

      // Iterative GOTO Constants
      public const double RA_Allowed_diff = 10;                   // Iterative Slew minimum difference


      // Home Position of the mount (pointing at NCP/SCP)

      public const double RAEncoder_Home_pos = 0x800000;          // Start at 0 Hour
      public const double DECEncoder_Home_pos = 0xA26C80;         // Start at 90 Degree position

      public const double RAEncoder_Zero_pos = 0x800000;          // ENCODER 0 Hour initial position
      public const double DECEncoder_Zero_pos = 0x800000;         // ENCODER 0 Degree Initial position

      public const double Default_step = 9024000;                 // Total Encoder count (EQ5/6)
      public const double EQ_MAXSYNC_Const = 0x113640;                 // Allow a 45 degree discrepancy

   }
}
