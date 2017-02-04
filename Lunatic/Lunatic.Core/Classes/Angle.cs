using Lunatic.Core.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Lunatic.Core
{
   public enum AngularFormat
   {
      [Description("(Not specified)")]
      /// <summary>CoordinateFormat not specified</summary>
      NotSpecified = 0,

      [Description("DD.dd\u00B0")]
      /// <summary>Decimal degrees</summary>
      DecimalDegrees = 1,

      [Description("DD\u00B0 MM.mm'")]
      /// <summary>Degrees and decimal minutes</summary>
      DegreesDecimalMinutes = 2,

      [Description("DD\u00B0 MM' SS.ss\"")]
      /// <summary>Degrees, minutes and seconds</summary>
      DegreesMinutesSeconds = 3,

      [Description("DD:MM:SS.ss")]
      /// <summary>Degrees, minutes and seconds (colon-delimited)</summary>
      CadDegreesMinutesSeconds = 4,

      [Description("DDMMSS.ssss")]
      /// <summary>Degrees, minutes and seconds (compact format)</summary>
      CompactDegreesMinutesSeconds = 6,

      [Description("DD.dd\u00B0 W/E")]
      /// <summary>Decimal degrees (West/East)</summary>
      DecimalDegreesWestEast = 5,

      [Description("+/-DD.dd\u00B0")]
      /// <summary>Decimal degrees (plus/minus)</summary>
      DecimalDegreesPlusMinus = 7
   }

   /// <summary>
   /// Represents a compass angle in degrees.
   /// </summary>
   public struct Angle
      : IComparable
   {
      /* Just a helper enum used when parsing strings */
      [Flags]
      internal enum Direction
      {
         None = 0,
         North = 0x01,
         South = 0x02,
         East = 0x04,
         West = 0x08
      }

      /* HACK ALERT  It would seem sensible to define these indices as an enum (since that's what enums
         are for, right?).  But .NET forces them to be explicitly cast back to ints before they can be
         used, which seems to defeat the point of them.  So, since there's only the three of them, define
         them as integer constants instead.
      */
      private const int DmsDegrees = 0;
      private const int DmsMinutes = 1;
      private const int DmsSeconds = 2;

      private const string Int_0 = @"0?0";
      private const string Int_0to59 = @"[0-5]?\d";
      private const string Int_0to89 = @"0?[0-8]?\d";
      private const string Int_90 = @"0?90";
      private const string Int_0to179 = @"(?:0?\d?\d|1[0-7]\d)";
      private const string Int_180 = @"180";
      private const string Dbl_0 = @"0?0(?:\.0+)?";
      private const string Dbl_0to59 = @"[0-5]?\d(?:\.\d+)?";
      private const string Dbl_0to89 = @"0?[0-8]?\d(?:\.\d+)?";
      private const string Dbl_90 = @"0?90(?:\.0+)?";
      private const string Dbl_0to99 = @"0?\d?\d(?:\.\d+)?";
      private const string Dbl_100to179 = @"1[0-7]\d(?:\.\d+)?";

      private const decimal DefaultDegreesDelta = 0.00000000001M;    /* In decimal degrees */
      public const string DegreesSymbol = "\u00B0";
      private const int NumberDecimalDigitsForCompactSeconds = 4;    /* 'Compact' format for DMS uses a standard no of decimal places */

      public const string DegreesPrefixedEw = @"(?<EW>[EW])\s*(?<LongDeg>0?\d?\d(?:\.\d+)?|1[0-7]\d(?:\.\d+)?|180(?:\.0+)?)\u00B0?";
      public const string DegreesSuffixedEw = @"(?<LongDeg>0?\d?\d(?:\.\d+)?|1[0-7]\d(?:\.\d+)?|180(?:\.0+)?)\u00B0?\s*(?<EW>[EW])";
      public const string DegreesSignedEw = @"(?<LongDeg>\-?(?:0?\d?\d(?:\.\d+)?|1[0-7]\d(?:\.\d+)?|180(?:\.0+)?))\u00B0?";
      public const string DegreesPrefixedNs = @"(?<NS>[NS])\s*(?<LatDeg>0?[0-8]?[0-9](?:\.\d+)?|0?90(?:\.0+)?)\u00B0?";
      public const string DegreesSuffixedNs = @"(?<LatDeg>0?[0-8]?[0-9](?:\.\d+)?|0?90(?:\.0+)?)\u00B0?\s*(?<NS>[NS])";
      public const string DegreesSignedNs = @"(?<LatDeg>\-?0?[0-8]?\d(?:\.\d+)?|\-?\s* 0?90(?:\.0+)?)\u00B0?";

      public const string DegMinPrefixedEw = @"(?<EW>[EW])\s*(?:(?<LongDeg>0?\d?\d|1[0-7]\d)[\u00B0\s]\s*(?<LongMin>[0-5]?\d(?:\.\d+)?)\'?|(?<LongDeg>180)[\u00B0\s]\s*(?<LongMin>0?0(?:\.0+)?)\'?)";
      public const string DegMinSuffixedEw = @"(?:(?<LongDeg>0?\d?\d|1[0-7]\d)[\u00B0\s]\s*(?<LongMin>[0-5]?\d(?:\.\d+)?)\'?|(?<LongDeg>180)[\u00B0\s]\s*(?<LongMin>0?0(?:\.0+)?)\'?)\s*(?<EW>[EW])";
      public const string DegMinSignedEw = @"(?:(?<LongDeg>\-?(?:0?\d?\d|1[0-7]\d))[\u00B0\s]\s*(?<LongMin>\-?\s*[0-5]?\d(?:\.\d+)?)\'?|(?<LongDeg>\-?\s*180)[\u00B0\s]\s*(?<LongMin>\-?\s*0?0(?:\.0+)?)\'?)";
      public const string DegMinPrefixedNs = @"(?<NS>[NS])\s*(?:(?<LatDeg>[0-8]?[0-9])[\u00B0\s]\s*(?<LatMin>[0-5]?\d(?:\.\d+)?)\'?|(?<LatDeg>90)[\u00B0\s]\s*(?<LatMin>0?0(?:\.0+)?)\'?)";
      public const string DegMinSuffixedNs = @"(?:(?<LatDeg>[0-8]?[0-9])[\u00B0\s]\s*(?<LatMin>[0-5]?\d(?:\.\d+)?)\'?|(?<LatDeg>90)[\u00B0\s]\s*(?<LatMin>0?0(?:\.0+)?)\'?)\s*(?<NS>[NS])";
      public const string DegMinSignedNs = @"(?:(?<LatDeg>\-?0?[0-8]?\d)[\u00B0\s]\s*(?<LatMin>\-?\s*[0-5]?\d(?:\.\d+)?)\'?|(?<LatDeg>\-?\s*0?90)[\u00B0\s]\s*(?<LatMin>\-?\s*0?0(?:\.0+)?)\'?)";

      public const string DegMinSecPrefixedEw = @"(?<EW>[EW])\s*(?:(?<LongDeg>0?\d?\d|1[0-7]\d)[\u00B0\s]\s*(?<LongMin>[0-5]?\d)[\'\s]\s*(?<LongSec>[0-5]?\d(?:\.\d+)?)\""?|(?<LongDeg>180)[\u00B0\s]\s*(?<LongMin>0?0)[\'\s]\s*(?<LongSec>0?0(?:\.0+)?)\""?)";
      public const string DegMinSecSuffixedEw = @"(?:(?<LongDeg>0?\d?\d|1[0-7]\d)[\u00B0\s]\s*(?<LongMin>[0-5]?\d)[\'\s]\s*(?<LongSec>[0-5]?\d(?:\.\d+)?)\""?|(?<LongDeg>180)[\u00B0\s]\s*(?<LongMin>0?0)[\'\s]\s*(?<LongSec>0?0(?:\.0+)?)\""?)\s*(?<EW>[EW])";
      public const string DegMinSecSignedEw = @"(?:(?<LongDeg>\-?(?:0?\d?\d|1[0-7]\d))[\u00B0\s]\s*(?<LongMin>\-?\s*[0-5]?\d)[\'\s]\s*(?<LongSec>\-?\s*[0-5]?\d(?:\.\d+)?)\""?|(?<LongDeg>\-?\s*180)[\u00B0\s]\s*(?<LongMin>\-?\s*0?0)[\'\s]\s*(?<LongSec>\-?\s*0?0(?:\.0+)?)\""?)";
      public const string DegMinSecCompactEw = @"(?:(?<LongDeg>0\d\d|1[0-7]\d)(?<LongMin>[0-5]\d)(?<LongSec>[0-5]\d(?:\.\d+)?)|(?<LongDeg>180)(?<LongMin>00)(?<LongSec>00(?:\.0+)?))(?<EW>[EW])";
      public const string DegMinSecPrefixedNs = @"(?<NS>[NS])\s*(?:(?<LatDeg>[0-8]?[0-9])[\u00B0\s]\s*(?<LatMin>[0-5]?\d)[\'\s]\s*(?<LatSec>[0-5]?\d(?:\.\d+)?)\""?|(?<LatDeg>90)[\u00B0\s]\s*(?<LatMin>0?0)[\'\s]\s*(?<LatSec>0?0(?:\.0+)?)\""?)";
      public const string DegMinSecSuffixedNs = @"(?:(?<LatDeg>[0-8]?[0-9])[\u00B0\s]\s*(?<LatMin>[0-5]?\d)[\'\s]\s*(?<LatSec>[0-5]?\d(?:\.\d+)?)\""?|(?<LatDeg>90)[\u00B0\s]\s*(?<LatMin>0?0)[\'\s]\s*(?<LatSec>0?0(?:\.0+)?)\""?)\s*(?<NS>[NS])";
      public const string DegMinSecSignedNs = @"(?:(?<LatDeg>\-?0?[0-8]?\d)[\u00B0\s]\s*(?<LatMin>\-?\s*[0-5]?\d)[\'\s]\s*(?<LatSec>\-?\s*[0-5]?\d(?:\.\d+)?)\""?|(?<LatDeg>\-?\s*0?90)[\u00B0\s]\s*(?<LatMin>\-?\s*0?0)[\'\s]\s*(?<LatSec>\-?\s*0?0(?:\.0+)?)\""?)";
      public const string DegMinSecCompactNs = @"(?:(?<LatDeg>[0-8][0-9])(?<LatMin>[0-5]\d)(?<LatSec>[0-5]\d(?:\.\d+)?)|(?<LatDeg>90)(?<LatMin>00)(?<LatSec>00(?:\.0+)?))(?<NS>[NS])";

      public const string CadDegMinSecPrefixedEw = @"(?<EW>[EW])\s*(?:(?<LongDeg>0?\d?\d|1[0-7]\d)\s*\:\s*(?<LongMin>[0-5]?\d)\s*\:\s*(?<LongSec>[0-5]?\d(?:\.\d+)?)|(?<LongDeg>180)\s*\:\s*(?<LongMin>0?0)\s*\:\s*(?<LongSec>0?0(?:\.0+)?))";
      public const string CadDegMinSecSuffixedEw = @"(?:(?<LongDeg>0?\d?\d|1[0-7]\d)\s*\:\s*(?<LongMin>[0-5]?\d)\s*\:\s*(?<LongSec>[0-5]?\d(?:\.\d+)?)|(?<LongDeg>180)\s*\:\s*(?<LongMin>0?0)\s*\:\s*(?<LongSec>0?0(?:\.0+)?))\s*(?<EW>[EW])";
      public const string CadDegMinSecSignedEw = @"(?:(?<LongDeg>\-?(?:0?\d?\d|1[0-7]\d))\s*\:\s*(?<LongMin>\-?\s*[0-5]?\d)\s*\:\s*(?<LongSec>\-?\s*[0-5]?\d(?:\.\d+)?)|(?<LongDeg>\-?\s*180)\s*\:\s*(?<LongMin>\-?\s*0?0)\s*\:\s*(?<LongSec>\-?\s*0?0(?:\.0+)?))";
      public const string CadDegMinSecPrefixedNs = @"(?<NS>[NS])\s*(?:(?<LatDeg>[0-8]?[0-9])\s*\:\s*(?<LatMin>[0-5]?\d)\s*\:\s*(?<LatSec>[0-5]?\d(?:\.\d+)?)|(?<LatDeg>90)\s*\:\s*(?<LatMin>0?0)\s*\:\s*(?<LatSec>0?0(?:\.0+)?))";
      public const string CadDegMinSecSuffixedNs = @"(?:(?<LatDeg>[0-8]?[0-9])\s*\:\s*(?<LatMin>[0-5]?\d)\s*\:\s*(?<LatSec>[0-5]?\d(?:\.\d+)?)|(?<LatDeg>90)\s*\:\s*(?<LatMin>0?0)\s*\:\s*(?<LatSec>0?0(?:\.0+)?))\s*(?<NS>[NS])";
      public const string CadDegMinSecSignedNs = @"(?:(?<LatDeg>\-?0?[0-8]?\d)\s*\:\s*(?<LatMin>\-?\s*[0-5]?\d)\s*\:\s*(?<LatSec>\-?\s*[0-5]?\d(?:\.\d+)?)|(?<LatDeg>\-?\s*0?90)\s*\:\s*(?<LatMin>\-?\s*0?0)\s*\:\s*(?<LatSec>\-?\s*0?0(?:\.0+)?))";

      private static Regex[] _DegRegexes;
      private static Regex[] _DdmRegexes;
      private static Regex[] _DmsRegexes;
      private static Regex[] _CadRegexes;
      private static int _NumberDecimalDigitsForDegrees = NumberFormatInfo.CurrentInfo.NumberDecimalDigits;
      private static int _NumberDecimalDigitsForSeconds = NumberFormatInfo.CurrentInfo.NumberDecimalDigits;

      private decimal _Value;        /* Always held in decimal degrees */
      private int _Degrees;
      private int _Minutes;
      private decimal _Seconds;
      private AngularFormat _Format;
      private bool _HasBeenSet;

      static Angle()
      {
         string[] degreesFormats = new string[] {DegreesPrefixedEw,
                                                 DegreesSuffixedEw,
                                                 DegreesSignedEw,
                                                 DegreesPrefixedNs,
                                                 DegreesSuffixedNs,
                                                 DegreesSignedNs
                                                };

         string[] degMinFormats = new string[] {DegMinPrefixedEw,
                                                DegMinSuffixedEw,
                                                DegMinSignedEw,
                                                DegMinPrefixedNs,
                                                DegMinSuffixedNs,
                                                DegMinSignedNs
                                               };

         string[] degMinSecFormats = new string[] {DegMinSecPrefixedEw,
                                                   DegMinSecSuffixedEw,
                                                   DegMinSecSignedEw,
                                                   DegMinSecCompactEw,
                                                   DegMinSecPrefixedNs,
                                                   DegMinSecSuffixedNs,
                                                   DegMinSecSignedNs,
                                                   DegMinSecCompactNs
                                                  };

         string[] cadDegMinSecFormats = new string[] {CadDegMinSecPrefixedEw,
                                                      CadDegMinSecSuffixedEw,
                                                      CadDegMinSecSignedEw,
                                                      CadDegMinSecPrefixedNs,
                                                      CadDegMinSecSuffixedNs,
                                                      CadDegMinSecSignedNs
                                                     };

         _DegRegexes = BuildRegexArray(degreesFormats);
         _DdmRegexes = BuildRegexArray(degMinFormats);
         _DmsRegexes = BuildRegexArray(degMinSecFormats);
         _CadRegexes = BuildRegexArray(cadDegMinSecFormats);

         _NumberDecimalDigitsForDegrees = 2;
         _NumberDecimalDigitsForSeconds = 2;
      }

      public static string StandardizeDegreesSymbol(string angle)
      {
         /* Whilst there's only one 'real' degrees symbol, there are a couple of others that
            look similar and might be used instead.  To keep the regexes simple, we'll
            standardise on the real degrees symbol (Google Earth uses this one, in
            particular).
         */
         angle = angle.Replace("\u00BA", Angle.DegreesSymbol);     /* Replaces Masculine Ordinal Indicator character */
         return angle.Replace("\u02DA", Angle.DegreesSymbol);      /* Replaces Ring Above character (unicode) */
      }

      /// <summary>
      /// Gets or sets the number of decimal places to display for decimal degrees (and degrees/decimal minutes) in calls to <see cref="Angle.ToString"/>.
      /// </summary>
      public static int NumberDecimalDigitsForDegrees
      {
         get
         {
            return _NumberDecimalDigitsForDegrees;
         }
         set
         {
            _NumberDecimalDigitsForDegrees = Math.Min(Math.Max(value, 0), 16);
         }
      }

      /// <summary>
      /// Gets or sets the number of decimal places to display for the seconds in calls to <see cref="Angle.ToString"/>.
      /// </summary>
      public static int NumberDecimalDigitsForSeconds
      {
         get
         {
            return _NumberDecimalDigitsForSeconds;
         }
         set
         {
            _NumberDecimalDigitsForSeconds = Math.Min(Math.Max(value, 0), 16);
         }
      }

      public Angle(double angle)
         : this((decimal)angle)
      {
      }

      public Angle(decimal angle)
      {
         _Value = angle;
         _Format = AngularFormat.DecimalDegrees;
         _HasBeenSet = true;

         /* All members must be set before calling out of the constructor so set dummy values
            so compiler is happy.
         */
         _Degrees = 0;
         _Minutes = 0;
         _Seconds = 0.0M;
         SetDmsFromDegrees(angle);
      }

      public Angle(double[] angle)
         : this(CastToDecimalArray(angle))
      {
      }

      public Angle(decimal[] angle)
      {
         /* All members must be set before calling out of the constructor so set dummy values
            so compiler is happy.
         */
         _Value = 0.0M;

         if (angle.Length == 2) {
            _Format = AngularFormat.DegreesDecimalMinutes;
            _Minutes = (int)Truncate(angle[DmsMinutes]);
            _Seconds = (angle[DmsMinutes] - (decimal)_Minutes) * 60.0M;
         }
         else if (angle.Length == 3) {
            _Format = AngularFormat.DegreesMinutesSeconds;
            _Minutes = (int)Truncate(angle[DmsMinutes]);
            _Seconds = angle[DmsSeconds];
         }
         else {
            throw new ArgumentException("Array must contain either two or three elements.", "angle");
         }

         _HasBeenSet = true;
         _Degrees = (int)Truncate(angle[DmsDegrees]);
         _Value = SetDegreesFromDms();
      }

      public Angle(int degrees, int minutes, decimal seconds)
      {
         /* All members must be set before calling out of the constructor so set dummy values
            so compiler is happy.
         */
         _Value = 0.0M;

         _Format = AngularFormat.DegreesMinutesSeconds;
         _HasBeenSet = true;
         _Degrees = degrees;
         _Minutes = minutes;
         _Seconds = seconds;
         _Value = SetDegreesFromDms();
      }

      public Angle(int degrees, int minutes, double seconds)
      {
         /* All members must be set before calling out of the constructor so set dummy values
            so compiler is happy.
         */
         _Value = 0.0M;

         _Format = AngularFormat.DegreesMinutesSeconds;
         _HasBeenSet = true;
         _Degrees = degrees;
         _Minutes = minutes;
         _Seconds = (decimal)seconds;
         _Value = SetDegreesFromDms();
      }

      public Angle(string angle)
      {
         /* All members must be set before calling out of the constructor so set dummy values
            so compiler is happy.
         */
         _Value = 0.0M;
         _Degrees = 0;
         _Minutes = 0;
         _Seconds = 0.0M;
         _Format = AngularFormat.NotSpecified;
         _HasBeenSet = false;

         angle = Angle.StandardizeDegreesSymbol(angle);

         /* The 'CAD' format is checked first against the InvariantCulture; it uses a comma as
            the lat/long delimiter, so a period must be used as the decimal point.
         */
         foreach (Regex regex in _CadRegexes) {
            Match match = regex.Match(angle);
            if (match.Success) {
               Direction direction = CheckMatchForDirection(match);
               if (direction == Direction.North
                   || direction == Direction.South) {
                  _Degrees = Convert.ToInt32(match.Groups["LatDeg"].Value, CultureInfo.InvariantCulture);
                  _Minutes = Convert.ToInt32(match.Groups["LatMin"].Value, CultureInfo.InvariantCulture);
                  _Seconds = Convert.ToDecimal(match.Groups["LatSec"].Value, CultureInfo.InvariantCulture);
               }
               else {
                  _Degrees = Convert.ToInt32(match.Groups["LongDeg"].Value, CultureInfo.InvariantCulture);
                  _Minutes = Convert.ToInt32(match.Groups["LongMin"].Value, CultureInfo.InvariantCulture);
                  _Seconds = Convert.ToDecimal(match.Groups["LongSec"].Value, CultureInfo.InvariantCulture);
               }

               if (direction == Direction.South
                   || direction == Direction.West) {
                  SetDmsToNegative();
               }

               _Value = SetDegreesFromDms();
               _Format = AngularFormat.CadDegreesMinutesSeconds;
               _HasBeenSet = true;
               break;
            }
         }

         /* The remaining formats can be locale-specific, but the regex patterns have to be
            hardcoded with a period as the decimal point.  So to keep things simple, we'll
            just replace the locale-specific separator with the invariant one.  (Don't need
            to worry about grouping characters, etc, since the numbers shouldn't be that big.)
         */
         angle = angle.Replace(CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator, ".");

         if (!_HasBeenSet) {
            foreach (Regex regex in _DegRegexes) {
               Match match = regex.Match(angle);
               if (match.Success) {
                  Direction direction = CheckMatchForDirection(match);
                  if (direction == Direction.North
                      || direction == Direction.South) {
                     _Value = Convert.ToDecimal(match.Groups["LatDeg"].Value, CultureInfo.InvariantCulture);
                  }
                  else {
                     _Value = Convert.ToDecimal(match.Groups["LongDeg"].Value, CultureInfo.InvariantCulture);
                  }

                  if (direction == Direction.South
                      || direction == Direction.West) {
                     _Value *= -1.0M;
                  }

                  SetDmsFromDegrees(_Value);

                  _Format = AngularFormat.DecimalDegrees;
                  _HasBeenSet = true;
                  break;
               }
            }
         }

         if (!_HasBeenSet) {
            foreach (Regex regex in _DdmRegexes) {
               Match match = regex.Match(angle);
               if (match.Success) {
                  decimal minutes = 0.0M;
                  Direction direction = CheckMatchForDirection(match);

                  if (direction == Direction.North
                      || direction == Direction.South) {
                     _Degrees = Convert.ToInt32(match.Groups["LatDeg"].Value, CultureInfo.InvariantCulture);
                     minutes = Convert.ToDecimal(match.Groups["LatMin"].Value, CultureInfo.InvariantCulture);
                  }
                  else {
                     _Degrees = Convert.ToInt32(match.Groups["LongDeg"].Value, CultureInfo.InvariantCulture);
                     minutes = Convert.ToDecimal(match.Groups["LongMin"].Value, CultureInfo.InvariantCulture);
                  }

                  _Minutes = (int)Truncate(minutes);
                  _Seconds = (minutes - (decimal)_Minutes) * 60.0M;

                  if (direction == Direction.South
                      || direction == Direction.West) {
                     SetDmsToNegative();
                  }

                  _Value = SetDegreesFromDms();
                  _Format = AngularFormat.DegreesDecimalMinutes;
                  _HasBeenSet = true;
                  break;
               }
            }
         }

         if (!_HasBeenSet) {
            foreach (Regex regex in _DmsRegexes) {
               Match match = regex.Match(angle);
               if (match.Success) {
                  Direction direction = CheckMatchForDirection(match);
                  if (direction == Direction.North
                      || direction == Direction.South) {
                     _Degrees = Convert.ToInt32(match.Groups["LatDeg"].Value, CultureInfo.InvariantCulture);
                     _Minutes = Convert.ToInt32(match.Groups["LatMin"].Value, CultureInfo.InvariantCulture);
                     _Seconds = Convert.ToDecimal(match.Groups["LatSec"].Value, CultureInfo.InvariantCulture);
                  }
                  else {
                     _Degrees = Convert.ToInt32(match.Groups["LongDeg"].Value, CultureInfo.InvariantCulture);
                     _Minutes = Convert.ToInt32(match.Groups["LongMin"].Value, CultureInfo.InvariantCulture);
                     _Seconds = Convert.ToDecimal(match.Groups["LongSec"].Value, CultureInfo.InvariantCulture);
                  }

                  if (direction == Direction.South
                      || direction == Direction.West) {
                     SetDmsToNegative();
                  }

                  _Value = SetDegreesFromDms();
                  _Format = AngularFormat.DegreesMinutesSeconds;
                  _HasBeenSet = true;
                  break;
               }
            }
         }

         if (!_HasBeenSet) {
            throw new FormatException("Invalid angle format.");
         }
      }

      /// <summary>
      /// Gets or sets the value of the <see cref="Angle"/> in decimal degrees.
      /// </summary>
      [DefaultValue(0.0)]
#if !__MOBILE__
      [Browsable(false)]
      [Category("Data")]
      [DisplayName("Value")]
      [Description("The value of the angle in degrees.")]
#endif
      public double Value
      {
         get
         {
            return (double)_Value;
         }
         set
         {
            _Value = (decimal)value;
            _HasBeenSet = true;
            SetDmsFromDegrees(_Value);
         }
      }

      /// <summary>
      /// Gets or sets the value of the <see cref="Angle"/> in radians.
      /// </summary>
      [DefaultValue(0.0)]
      [Browsable(false)]
      [Category("Data")]
      [DisplayName("Radians")]
      [Description("The value of the angle in radians.")]
      public double Radians
      {
         get
         {
            return Angle.DegreesToRadians((double)_Value);
         }
         set
         {
            _Value = (decimal)Angle.RadiansToDegrees(value);
            _HasBeenSet = true;
            SetDmsFromDegrees(_Value);
         }
      }

      [DefaultValue(0)]
      [DisplayName("Degrees")]
      [Description("")]
      public int Degrees
      {
         get
         {
            return _Degrees;
         }
         set
         {
            _Degrees = value;
            MatchDmsSigns(value);

            _Value = DmsToDegrees(_Degrees, _Minutes, _Seconds);
            _HasBeenSet = true;
         }
      }

      [DefaultValue(0)]
      [DisplayName("Minutes")]
      [Description("")]
      public int Minutes
      {
         get
         {
            return _Minutes;
         }
         set
         {
            if (value < -60
                || value > 60) {
               throw new ArgumentException("Minutes must be between -60 and 60", "value");
            }

            _Minutes = value;
            MatchDmsSigns(value);

            _Value = DmsToDegrees(_Degrees, _Minutes, _Seconds);
            _HasBeenSet = true;
         }
      }

      [DefaultValue(0.0)]
      [DisplayName("Seconds")]
      [Description("")]
      public double Seconds
      {
         get
         {
            return (double)_Seconds;
         }
         set
         {
            if (value < -60.0
                || value > 60.0) {
               throw new ArgumentException("Seconds must be between -60.0 and 60.0", "value");
            }

            _Seconds = (decimal)value;
            MatchDmsSigns(value);

            _Value = DmsToDegrees(_Degrees, _Minutes, _Seconds);
            _HasBeenSet = true;
         }
      }

      [DefaultValue(0.0)]
      [Browsable(false)]
      [DisplayName("Total Seconds")]
      [Description("The value of the Angle expressed in seconds.")]
      public double TotalSeconds
      {
         get
         {
            return (_Degrees * 3600.0) + (_Minutes * 60.0) + (double)_Seconds;
         }
         set
         {
            _Degrees = (int)Truncate(value / 3600.0);
            value %= 3600.0;
            _Minutes = (int)Truncate(value / 60.0);
            _Seconds = (decimal)(value % 60.0);

            _Value = DmsToDegrees(_Degrees, _Minutes, _Seconds);
            _HasBeenSet = true;
         }
      }

      /// <summary>
      /// Gets the absolute value of the <see cref="Angle"/>.
      /// </summary>
      [Browsable(false)]
      [Category("Data")]
      [Description("The absolute value of the angle.")]
      public Angle Abs
      {
         get
         {
            /* Use the individual elements to avoid rounding errors */
            return new Angle(Math.Abs(_Degrees), Math.Abs(_Minutes), (double)Math.Abs(_Seconds));
         }
      }

      [DefaultValue(AngularFormat.NotSpecified)]
      [Browsable(false)]
      [DisplayName("Format")]
      [Description("")]
      public AngularFormat Format
      {
         get
         {
            return _Format;
         }
         set
         {
            _Format = value;
         }
      }

      /// <summary>
      /// Gets a value indicating whether the value has been set.
      /// </summary>
      /// <value>
      /// <b>True</b> if no value has been set; otherwise, <b>False</b>.
      /// </value>
      [Browsable(false)]
      public bool IsNull
      {
         get
         {
            return !_HasBeenSet;
         }
      }

      public static implicit operator Angle(double angle)
      {
         return new Angle(angle);
      }

      public static implicit operator double(Angle angle)
      {
         return (double)angle._Value;
      }

      public static implicit operator Angle(decimal angle)
      {
         return new Angle(angle);
      }

      public static implicit operator decimal(Angle angle)
      {
         return angle._Value;
      }

      public static implicit operator Angle(string angle)
      {
         return new Angle(angle);
      }

      public static implicit operator string(Angle angle)
      {
         return angle.ToString();
      }

      /// <summary>
      /// Compares the two specified angles, using the default delta value.
      /// </summary>
      public static bool operator ==(Angle angle1, Angle angle2)
      {
         /* Just in case of rounding errors... */
         return (Math.Abs(angle1._Value - angle2._Value) <= Angle.DefaultDegreesDelta
                 || (angle1._Degrees == angle2._Degrees
                     && angle1._Minutes == angle2._Minutes
                     && Math.Abs(angle1._Seconds - angle2._Seconds) <= (Angle.DefaultDegreesDelta * 3600M)));
      }

      public static bool operator !=(Angle angle1, Angle angle2)
      {
         return !(angle1 == angle2);
      }

      public static bool operator <(Angle angle1, Angle angle2)
      {
         return (angle1.Format == AngularFormat.DecimalDegrees
                 || angle1.Format == AngularFormat.DecimalDegreesWestEast
                     ? (angle1._Value < angle2._Value)
                     : (angle1.TotalSeconds < angle2.TotalSeconds));
      }

      public static bool operator <=(Angle angle1, Angle angle2)
      {
         return (angle1.Format == AngularFormat.DecimalDegrees
                 || angle1.Format == AngularFormat.DecimalDegreesWestEast
                     ? (angle1._Value <= angle2._Value)
                     : (angle1.TotalSeconds <= angle2.TotalSeconds));
      }

      public static bool operator >(Angle angle1, Angle angle2)
      {
         return (angle1.Format == AngularFormat.DecimalDegrees
                 || angle1.Format == AngularFormat.DecimalDegreesWestEast
                     ? (angle1._Value > angle2._Value)
                     : (angle1.TotalSeconds > angle2.TotalSeconds));
      }

      public static bool operator >=(Angle angle1, Angle angle2)
      {
         return (angle1.Format == AngularFormat.DecimalDegrees
                 || angle1.Format == AngularFormat.DecimalDegreesWestEast
                     ? (angle1._Value >= angle2._Value)
                     : (angle1.TotalSeconds >= angle2.TotalSeconds));
      }

      public static Angle operator +(Angle angle1, Angle angle2)
      {
         if (angle1.Format == AngularFormat.DecimalDegrees
             || angle1.Format == AngularFormat.DecimalDegreesWestEast) {
            angle1.Value += angle2.Value;    /* Use Value property to ensure DMS properties are also updated properly */
         }
         else {
            double seconds = angle1.TotalSeconds + angle2.TotalSeconds;
            angle1 = FromSeconds(seconds);
         }

         return angle1;
      }

      public static Angle operator -(Angle angle1, Angle angle2)
      {
         if (angle1.Format == AngularFormat.DecimalDegrees
             || angle1.Format == AngularFormat.DecimalDegreesWestEast) {
            angle1.Value -= angle2.Value;    /* Use Value property to ensure DMS properties are also updated properly */
         }
         else {
            double seconds = angle1.TotalSeconds - angle2.TotalSeconds;
            angle1 = FromSeconds(seconds);
         }

         return angle1;
      }

      public static Angle operator *(Angle angle, double factor)
      {
         if (angle.Format == AngularFormat.DecimalDegrees
             || angle.Format == AngularFormat.DecimalDegreesWestEast) {
            angle.Value *= factor;  /* Use Value property to ensure DMS properties are also updated properly */
         }
         else {
            double seconds = angle.TotalSeconds * factor;
            angle = FromSeconds(seconds);
         }

         return angle;
      }

      public static Angle operator /(Angle angle, double factor)
      {
         if (angle.Format == AngularFormat.DecimalDegrees
             || angle.Format == AngularFormat.DecimalDegreesWestEast) {
            angle.Value /= factor;     /* Use Value property to ensure DMS properties are also updated properly */
         }
         else {
            double seconds = angle.TotalSeconds / factor;
            angle = FromSeconds(seconds);
         }

         return angle;
      }

      public static double operator /(Angle angle1, Angle angle2)
      {
         return (angle2.Format == AngularFormat.DecimalDegrees
                 || angle2.Format == AngularFormat.DecimalDegreesWestEast
                     ? (double)(angle1._Value / angle2._Value)
                     : angle1.TotalSeconds / angle2.TotalSeconds);
      }

      public override int GetHashCode()
      {
         return _Value.GetHashCode();
      }

      public override bool Equals(object obj)
      {
         return (obj is Angle
                 && this == (Angle)obj);
      }

      public double[] ToDms()
      {
         return new double[] {(double)_Degrees,
                              (double)_Minutes,
                              (double)_Seconds,
                             };
      }

      public double Delta(Angle angle)
      {
         double delta = angle.Value - (double)_Value;
         return (Math.Abs(delta) <= 180.0 ? delta
                                          : (360.0 - Math.Abs(delta)) * (angle.Value > (double)_Value ? -1.0 : 1.0));

      }

      public override string ToString()
      {
         return ToString(_Format);
      }

      public string ToString(string format)
      {
         return _Value.ToString(format);
      }

      public string ToString(AngularFormat format, bool asLongitude = true)
      {
         string text = string.Empty;
         int degrees;
         int minutes;
         decimal decimalMinutes;
         decimal seconds;
         int increment;

         switch (format) {
            case AngularFormat.DecimalDegrees:
               text = CustomFormat.ToString(Resources.AngleInDecimalDegrees, _NumberDecimalDigitsForDegrees, _Value);
               break;

            case AngularFormat.DegreesDecimalMinutes:
               increment = (_Value >= 0.0M ? 1 : -1);
               decimalMinutes = decimal.Round((decimal)_Minutes + (_Seconds / 60.0M), _NumberDecimalDigitsForDegrees);
               if (Math.Abs(decimalMinutes) < 60.0M) {
                  degrees = _Degrees;
               }
               else {
                  decimalMinutes = 0.0M;
                  degrees = _Degrees + increment;
               }

               text = CustomFormat.ToString(Resources.AngleInDegreesDecimalMinutes, _NumberDecimalDigitsForDegrees,
                                            degrees, decimalMinutes);
               break;

            case AngularFormat.DegreesMinutesSeconds:
            case AngularFormat.CadDegreesMinutesSeconds:
            case AngularFormat.CompactDegreesMinutesSeconds:
               increment = (_Value >= 0.0M ? 1 : -1);
               seconds = decimal.Round(_Seconds, format != AngularFormat.CompactDegreesMinutesSeconds ? _NumberDecimalDigitsForSeconds
                                                                                                      : Angle.NumberDecimalDigitsForCompactSeconds);

               if (Math.Abs(seconds) < 60.0M) {
                  minutes = _Minutes;
               }
               else {
                  seconds = 0.0M;
                  minutes = _Minutes + increment;
               }

               if (Math.Abs(minutes) < 60) {
                  degrees = _Degrees;
               }
               else {
                  minutes = 0;
                  degrees = _Degrees + increment;
               }

               if (format == AngularFormat.DegreesMinutesSeconds) {
                  text = CustomFormat.ToString(Resources.AngleInDegreesMinutesSeconds, _NumberDecimalDigitsForSeconds,
                                               degrees, minutes, seconds);
               }
               else if (format == AngularFormat.CompactDegreesMinutesSeconds) {
                  string stringFormat = asLongitude ? Resources.LongitudeInCompactDegreesMinutesSeconds
                                                    : Resources.LatitudeInCompactDegreesMinutesSeconds;
                  text = string.Format(stringFormat, Math.Abs(degrees), Math.Abs(minutes), Math.Abs(seconds),
                                       asLongitude ? (_Value >= 0.0M ? "E" : "W")
                                                   : (_Value >= 0.0M ? "N" : "S"));
               }
               else {
                  /* Because 'CAD' coordinates use a comma as the lat/long delimiter, a period must
                     be used as the decimal point, hence the use of the InvariantCulture.
                  */
                  text = CustomFormat.ToString(CultureInfo.InvariantCulture,
                                               Resources.AngleInCadDegreesMinutesSeconds, _NumberDecimalDigitsForSeconds,
                                               degrees, minutes, seconds);
               }
               break;

            case AngularFormat.DecimalDegreesWestEast:
               decimal angle = Angle.Normalize(_Value);
               string direction = "E";

               if (angle > 180.0M) {
                  angle = 360.0M - angle;
                  direction = "W";
               }

               text = CustomFormat.ToString(Resources.AngleInDecimalDegreesWestEast, _NumberDecimalDigitsForDegrees, angle, direction);
               break;

            case AngularFormat.DecimalDegreesPlusMinus:
               decimal angle180 = Angle.NormalizeTo180(_Value);
               text = CustomFormat.ToString(Resources.AngleInDecimalDegreesPlusMinus, _NumberDecimalDigitsForDegrees, angle180);
               break;

            default:
               Debug.Assert(format == AngularFormat.NotSpecified, "Unrecognised AngularFormat value - " + format.ToString());
               text = CustomFormat.ToString(Resources.AngleWithNoFormat, _NumberDecimalDigitsForDegrees, _Value);
               break;
         }

         return text;
      }

      public void Normalize()
      {
         _Value = Angle.Normalize(_Value);
         _Degrees = (int)Angle.Normalize((decimal)_Degrees);
      }

      /// <summary>
      /// Converts an angle to the range 0.0 to 360.0 degrees
      /// </summary>
      public static double Normalize(double angle)
      {
         return (double)Angle.Normalize((decimal)angle);
      }

      /// <summary>
      /// Converts an angle to the range 0.0 to 360.0 degrees
      /// </summary>
      public static decimal Normalize(decimal angle)
      {
         angle %= 360.0M;

         if (angle < 0.0M) {
            angle += 360.0M;
         }

         return angle;
      }

      /// <summary>
      /// Converts the angle to the range 180.0 to -180.0 degrees
      /// </summary>
      public void NormalizeTo180()
      {
         _Value = Angle.NormalizeTo180(_Value);
         SetDmsFromDegrees(_Value);
      }

      /// <summary>
      /// Converts the angle to the range 180.0 to -180.0 degrees
      /// </summary>
      public static double NormalizeTo180(double angle)
      {
         return (double)Angle.NormalizeTo180((decimal)angle);
      }

      /// <summary>
      /// Converts the angle to the range 180.0 to -180.0 degrees
      /// </summary>
      public static decimal NormalizeTo180(decimal angle)
      {
         angle %= 360.0M;   /* Need it in the standard range first */

         if (angle > 180.0M) {
            angle -= 360.0M;
         }
         else if (angle < -180.0M) {
            angle += 360.0M;
         }

         return angle;
      }

      // TODO: this is now redundant (use angle.TotalSeconds = value)
      private static Angle FromSeconds(double seconds)
      {
         int degrees = (int)Truncate(seconds / 3600.0);
         seconds %= 3600.0;
         int minutes = (int)Truncate(seconds / 60.0);
         seconds = seconds % 60.0;

         return new Angle(degrees, minutes, seconds);
      }

      private static decimal DmsToDegrees(int degrees, int minutes, decimal seconds)
      {
         Debug.Assert((degrees >= 0 && minutes >= 0 && seconds >= 0.0M)
                      || (degrees <= 0 && minutes <= 0 && seconds <= 0.0M),
                      "Degrees/minutes/seconds don't have consistent signs.");

         return (decimal)degrees + ((decimal)minutes / 60.0M) + (seconds / 3600.0M);
      }

      public static double RadiansToDegrees(double radians)
      {
         return radians * (180.0 / Math.PI);
      }

      public static double DegreesToRadians(double degrees)
      {
         return degrees / (180.0 / Math.PI);
      }

      public static double CompassToCartesianDegrees(double angle)
      {
         return RadiansToDegrees(CompassToCartesianRadians(DegreesToRadians(angle)));
      }

      public static double CartesianToCompassDegrees(double angle)
      {
         return RadiansToDegrees(CartesianToCompassRadians(DegreesToRadians(angle)));
      }

      public static double CompassToCartesianRadians(double radians)
      {
         return CartesianToCompassRadians(radians) % (2.0 * Math.PI);
      }

      public static double CartesianToCompassRadians(double radians)
      {
         double angle = (2.5 * Math.PI) - radians;

         if (angle >= (2.0 * Math.PI)) {
            angle = angle % (2.0 * Math.PI);
         }

         return angle;
      }

      public static decimal GradientToDegrees(double gradient)
      {
         return NormalizeTo180((decimal)Angle.RadiansToDegrees(Math.Atan(gradient)));   /* NormalizeTo180() because a gradient can be negative */
      }

      public static double CompassAngleFrom2Points(double x1, double y1, double x2, double y2)
      {
         const double fc_zero = 0.0;
         const double fc_90 = 90.0;
         const double fc_180 = 180.0;
         const double fc_360 = 360.0;
         double angle;
         double adjacent;
         double opposite;
         double temp;
         double rad;

         if (x1 > x2) {
            adjacent = x1 - x2;
         }
         else {
            adjacent = x2 - x1;
         }

         if (y1 > y2) {
            opposite = y1 - y2;
         }
         else {
            opposite = y2 - y1;
         }

         /* swap a and o over  */
         if ((x1 > x2 && y1 < y2) || (x1 < x2 && y1 > y2)) {
            temp = adjacent;
            adjacent = opposite;
            opposite = temp;
         }

         if (opposite == fc_zero) {
            opposite = 0.0000001;
         }

         if (adjacent == fc_zero) {
            adjacent = 0.0000001;
         }

         rad = Math.Atan(opposite / adjacent);
         angle = (rad * fc_180) / Math.PI;

         /*
          angle returned is always less than 90 degrees set angle to correct
          angle between 0 -360 in MCE angle
         */
         if ((x1 > x2 && y1 < y2)) {
            angle += 90.0;
         }
         /* extra OR's needed for 180 degree and 270 degree MCE angles */
         if ((x1 > x2 && y1 > y2) || (x1 > x2 && y1 == y2) || (x1 == x2 && y1 > y2)) {
            angle += 180.0;
         }

         if ((x1 < x2 && y1 > y2)) {
            angle += 270.0;
         }

         angle = (fc_360 - angle) + fc_90;
         if (angle >= fc_360) {
            angle -= fc_360;
         }
         return angle;
      }

      internal static Regex[] BuildRegexArray(string[] regexPatterns)
      {
         Regex[] regexes = new Regex[regexPatterns.Length];

         for (int i = 0; i < regexPatterns.Length; i++) {
            regexes[i] = new Regex(@"^\s*" + regexPatterns[i] + @"\s*$",
#if !__MOBILE__
                                   RegexOptions.Compiled | RegexOptions.IgnoreCase);
#else
                                   RegexOptions.IgnoreCase);
#endif
         }

         return regexes;
      }

      private static decimal[] CastToDecimalArray(double[] source)
      {
         decimal[] target = new decimal[source.Length];

         for (int i = 0; i < source.Length; i++) {
            target[i] = (decimal)source[i];
         }

         return target;
      }

      private void SetDmsFromDegrees(decimal angle)
      {
         _Degrees = (int)Truncate(angle);
         angle = (angle - _Degrees) * 60.0M;
         _Minutes = (int)Truncate(angle);
         _Seconds = (angle - _Minutes) * 60.0M;
      }

      private decimal SetDegreesFromDms()
      {
         if (_Degrees < 0 || _Minutes < 0 || _Seconds < 0.0M) {
            SetDmsToNegative();
         }

         return (decimal)_Degrees + ((decimal)_Minutes / 60.0M) + (_Seconds / 3600.0M);
      }

      private void MatchDmsSigns(double value)
      {
         /* If the value is zero, no sign can be inferred */
         if (value > 0.0) {
            SetDmsToPositive();
         }
         else if (value < 0.0) {
            SetDmsToNegative();
         }
      }

      private void SetDmsToPositive()
      {
         if (_Degrees < 0) {
            _Degrees *= -1;
         }

         if (_Minutes < 0) {
            _Minutes *= -1;
         }

         if (_Seconds < 0.0M) {
            _Seconds *= -1.0M;
         }
      }

      private void SetDmsToNegative()
      {
         if (_Degrees > 0) {
            _Degrees *= -1;
         }

         if (_Minutes > 0) {
            _Minutes *= -1;
         }

         if (_Seconds > 0.0M) {
            _Seconds *= -1.0M;
         }
      }

      internal static Direction CheckMatchForDirection(Match match)
      {
         Direction direction = Direction.None;
         Group group = match.Groups["EW"];

         if (group.Success) {
            direction = (group.Value.IndexOfAny(new char[] { 'W', 'w' }) >= 0 ? Direction.West : Direction.East);
         }
         else {
            group = match.Groups["NS"];
            if (group.Success) {
               direction = (group.Value.IndexOfAny(new char[] { 'S', 's' }) >= 0 ? Direction.South : Direction.North);
            }
         }

         return direction;
      }

      #region IComparable Members

      public int CompareTo(object obj)
      {
         int result = 0;

         if (obj == null
             || !(obj is Angle)) {
            result = 1;
         }
         else {
            Angle that = (Angle)obj;
            result = _Value.CompareTo(that._Value);
         }

         return result;
      }

      #endregion

      public static decimal Truncate(decimal value)
      {
         return Math.Truncate(value);
      }

      public static double Truncate(double value)
      {
         return Math.Truncate(value);
      }

   }
}
