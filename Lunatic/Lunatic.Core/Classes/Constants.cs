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

namespace Lunatic.Core
{
   public static class Constants
   {
      public const string OrganisationName = "LunaticSoftware";
      public const double SIDEREAL_RATE = 2 * Math.PI / 86164.09065;
      public const double SOLAR_RATE = 15;
      public const double LUNAR_RATE = 14.511415;

      public const double DEG_RAD = 0.0174532925;
      public const double RAD_DEG = 57.2957795;
      public const double HRS_RAD = 0.2617993881;
      public const double RAD_HRS = 3.81971863;


   }
}
