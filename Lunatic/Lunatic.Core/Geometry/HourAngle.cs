﻿using Lunatic.Core.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lunatic.Core.Geometry
{
   public enum HourAngleFormat
   {
      [Description("(Not specified)")]
      /// <summary>CoordinateFormat not specified</summary>
      NotSpecified = 0,

      [Description("HH.hh")]
      /// <summary>Decimal degrees</summary>
      DecimalHours = 1,

      [Description("HH MM.mm")]
      /// <summary>Hours and decimal minutes</summary>
      HoursDecimalMinutes = 2,

      [Description("HH MM SS.ss")]
      /// <summary>Hours, minutes and seconds</summary>
      HoursMinutesSeconds = 3,

      [Description("HH:MM:SS.ss")]
      /// <summary>Hours, minutes and seconds (colon-delimited)</summary>
      CadHoursMinutesSeconds = 4,

      [Description("HHMMSS.ssss")]
      /// <summary>Degrees, minutes and seconds (compact format)</summary>
      CompactHoursMinutesSeconds = 5,

      [Description("+/-HH.hh")]
      /// <summary>Decimal degrees (plus/minus)</summary>
      DecimalHoursPlusMinus = 6

   }


   public struct HourAngle
      : IComparable
   {
      private const int HmsHours = 0;
      private const int HmsMinutes = 1;
      private const int HmsSeconds = 2;

      private const double DefaultHoursDelta = 0.00000000001;    /* In decimal hours */
      private const int NumberDecimalDigitsForCompactSeconds = 4;    /* 'Compact' format for HMS uses a standard no of decimal places */

      public const string HoursRegex = @"((?<Hrs>(?:[+-])?(?:[0-9]|1[0-9]|2[0-3])(?:\.\d{1,2})?|00(?:\.\d{1,2}?))?)";

      public const string HrMin = @"((?<Hrs>(?:[+-])?(?:[0-9]|1[0-9]|2[0-3]|00))[ :h](?<Mins>(?:[0-9]|[0-5][0-9])(?:\.\d{1,2})?))";

      public const string HrMinSec = @"((?<Hrs>(?:[+-])?(?:[0-9]|1[0-9]|2[0-3]|00))(?:[ h])(?<Mins>(?:[0-9]|[0-5][0-9]))[ m](?<Secs>(?:[0-9]|[0-5][0-9])(?:\.\d{1,2})?)(?:[ s])?)";

      public const string CadHrMinSec = @"((?<Hrs>(?:[+-])?(?:[0-9]|1[0-9]|2[0-3]|00)):(?<Mins>(?:[0-9]|[0-5][0-9])):(?<Secs>(?:[0-9]|[0-5][0-9])(?:\.\d{1,2})?))";


      private static Regex[] _HrsRegexes;
      private static Regex[] _HdmRegexes;
      private static Regex[] _HmsRegexes;
      private static Regex[] _CadRegexes;

      private static int _NumberDecimalDigitsForHours = NumberFormatInfo.CurrentInfo.NumberDecimalDigits;
      private static int _NumberDecimalDigitsForSeconds = NumberFormatInfo.CurrentInfo.NumberDecimalDigits;

      private double _Value;
      private int _Hours;
      private int _Minutes;
      private double _Seconds;
      private HourAngleFormat _Format;
      private bool _HasBeenSet;

      static HourAngle()
      {

         string[] hoursFormats = new string[] { HoursRegex };

         string[] hrMinFormats = new string[] { HrMin };

         string[] hrMinSecFormats = new string[] { HrMinSec };

         string[] cadHrMinSecFormats = new string[] { CadHrMinSec };


         _HrsRegexes = BuildRegexArray(hoursFormats);
         _HdmRegexes = BuildRegexArray(hrMinFormats);
         _HmsRegexes = BuildRegexArray(hrMinSecFormats);
         _CadRegexes = BuildRegexArray(cadHrMinSecFormats);

         _NumberDecimalDigitsForHours = 2;
         _NumberDecimalDigitsForSeconds = 2;
      }

      public HourAngle(double hour, bool radians = false)
      {
         if (radians) {
            _Value = (double)HourAngle.RadiansToHours(hour);
         }
         else {
            _Value = hour;
         }
         _Format = HourAngleFormat.DecimalHours;
         _HasBeenSet = true;

         /* All members must be set before calling out of the constructor so set dummy values
            so compiler is happy.
         */
         _Hours = 0;
         _Minutes = 0;
         _Seconds = 0.0;
         SetHmsFromHours(hour);
      }


      public HourAngle(double[] hour)
      {
         /* All members must be set before calling out of the constructor so set dummy values
            so compiler is happy.
         */
         _Value = 0.0;

         if (hour.Length == 2) {
            _Format = HourAngleFormat.HoursDecimalMinutes;
            _Minutes = (int)Truncate(hour[HmsMinutes]);
            _Seconds = (hour[HmsMinutes] - (double)_Minutes) * 60.0;
         }
         else if (hour.Length == 3) {
            _Format = HourAngleFormat.HoursMinutesSeconds;
            _Minutes = (int)Truncate(hour[HmsMinutes]);
            _Seconds = hour[HmsSeconds];
         }
         else {
            throw new ArgumentException("Array must contain either two or three elements.", "angle");
         }

         _HasBeenSet = true;
         _Hours = (int)Truncate(hour[HmsHours]);
         _Value = SetHoursFromHms();
      }

      public HourAngle(int hours, int minutes, double seconds)
      {
         /* All members must be set before calling out of the constructor so set dummy values
            so compiler is happy.
         */
         _Value = 0.0;

         _Format = HourAngleFormat.HoursMinutesSeconds;
         _HasBeenSet = true;
         _Hours = hours;
         _Minutes = minutes;
         _Seconds = seconds;
         _Value = SetHoursFromHms();
      }

      public HourAngle(string hour)
      {
         /* All members must be set before calling out of the constructor so set dummy values
            so compiler is happy.
         */
         _Value = 0.0;
         _Hours = 0;
         _Minutes = 0;
         _Seconds = 0.0;
         _Format = HourAngleFormat.NotSpecified;
         _HasBeenSet = false;


         /* The 'CAD' format is checked first against the InvariantCulture; it uses a comma as
            the lat/long delimiter, so a period must be used as the double point.
         */
         foreach (Regex regex in _CadRegexes) {
            Match match = regex.Match(hour);
            if (match.Success) {
               _Hours = Convert.ToInt32(match.Groups["Hrs"].Value, CultureInfo.InvariantCulture);
               _Minutes = Convert.ToInt32(match.Groups["Mins"].Value, CultureInfo.InvariantCulture);
               _Seconds = Convert.ToDouble(match.Groups["Secs"].Value, CultureInfo.InvariantCulture);


               _Value = SetHoursFromHms();
               _Format = HourAngleFormat.CadHoursMinutesSeconds;
               _HasBeenSet = true;
               break;
            }
         }
         /* The remaining formats can be locale-specific, but the regex patterns have to be
            hardcoded with a period as the double point.  So to keep things simple, we'll
            just replace the locale-specific separator with the invariant one.  (Don't need
            to worry about grouping characters, etc, since the numbers shouldn't be that big.)
         */
         hour = hour.Replace(CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator, ".");

         if (!_HasBeenSet) {
            foreach (Regex regex in _HrsRegexes) {
               Match match = regex.Match(hour);
               if (match.Success) {
                  _Value = Convert.ToDouble(match.Groups["Hrs"].Value, CultureInfo.InvariantCulture);
                  SetHmsFromHours(_Value);
                  _Format = HourAngleFormat.DecimalHours;
                  _HasBeenSet = true;
                  break;
               }
            }
         }

         if (!_HasBeenSet) {
            foreach (Regex regex in _HdmRegexes) {
               Match match = regex.Match(hour);
               if (match.Success) {
                  double minutes = 0.0;
                  _Hours = Convert.ToInt32(match.Groups["Hrs"].Value, CultureInfo.InvariantCulture);
                  minutes = Convert.ToDouble(match.Groups["Mins"].Value, CultureInfo.InvariantCulture);

                  _Minutes = (int)Truncate(minutes);
                  _Seconds = (minutes - (double)_Minutes) * 60.0;

                  _Value = SetHoursFromHms();
                  _Format = HourAngleFormat.HoursDecimalMinutes;
                  _HasBeenSet = true;
                  break;
               }
            }
         }

         if (!_HasBeenSet) {
            foreach (Regex regex in _HmsRegexes) {
               Match match = regex.Match(hour);
               if (match.Success) {
                  _Hours = Convert.ToInt32(match.Groups["Hrs"].Value, CultureInfo.InvariantCulture);
                  _Minutes = Convert.ToInt32(match.Groups["Mins"].Value, CultureInfo.InvariantCulture);
                  _Seconds = Convert.ToDouble(match.Groups["Secs"].Value, CultureInfo.InvariantCulture);

                  _Value = SetHoursFromHms();
                  _Format = HourAngleFormat.HoursMinutesSeconds;
                  _HasBeenSet = true;
                  break;
               }
            }
         }

         if (!_HasBeenSet) {
            throw new FormatException("Invalid hour format.");
         }
      }

      /// <summary>
      /// Gets or sets the value of the <see cref="Angle"/> in double hours.
      /// </summary>
      [DefaultValue(0.0)]
      [Browsable(false)]
      [Category("Data")]
      [DisplayName("Value")]
      [Description("The value of the angle in hours.")]
      public double Value
      {
         get
         {
            return (double)_Value;
         }
         set
         {
            _Value = (double)value;
            _HasBeenSet = true;
            SetHmsFromHours(_Value);
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
            return HourAngle.HoursToRadians((double)_Value);
         }
         set
         {
            _Value = (double)HourAngle.RadiansToHours(value);
            _HasBeenSet = true;
            SetHmsFromHours(_Value);
         }
      }

      [DefaultValue(0)]
      [DisplayName("Hours")]
      [Description("")]
      public int Hours
      {
         get
         {
            return _Hours;
         }
         set
         {
            _Hours = value;
            MatchHmsSigns(value);

            _Value = HmsToHours(_Hours, _Minutes, _Seconds);
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
            MatchHmsSigns(value);

            _Value = HmsToHours(_Hours, _Minutes, _Seconds);
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

            _Seconds = (double)value;
            MatchHmsSigns(value);

            _Value = HmsToHours(_Hours, _Minutes, _Seconds);
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
            return (_Hours * 3600.0) + (_Minutes * 60.0) + (double)_Seconds;
         }
         set
         {
            _Hours = (int)Truncate(value / 3600.0);
            value %= 3600.0;
            _Minutes = (int)Truncate(value / 60.0);
            _Seconds = (double)(value % 60.0);

            _Value = HmsToHours(_Hours, _Minutes, _Seconds);
            _HasBeenSet = true;
         }
      }

      /// <summary>
      /// Gets the absolute value of the <see cref="Angle"/>.
      /// </summary>
      [Browsable(false)]
      [Category("Data")]
      [Description("The absolute value of the angle.")]
      public HourAngle Abs
      {
         get
         {
            /* Use the individual elements to avoid rounding errors */
            return new HourAngle(Math.Abs(_Hours), Math.Abs(_Minutes), (double)Math.Abs(_Seconds));
         }
      }

      [DefaultValue(HourAngleFormat.NotSpecified)]
      [Browsable(false)]
      [DisplayName("Format")]
      [Description("")]
      public HourAngleFormat Format
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

      public static implicit operator HourAngle(double hour)
      {
         return new HourAngle(hour);
      }

      public static implicit operator double(HourAngle hour)
      {
         return hour._Value;
      }


      public static implicit operator HourAngle(string hour)
      {
         return new HourAngle(hour);
      }

      public static implicit operator string(HourAngle hour)
      {
         return hour.ToString();
      }

      /// <summary>
      /// Compares the two specified hours, using the default delta value.
      /// </summary>
      public static bool operator ==(HourAngle hour1, HourAngle hour2)
      {
         /* Just in case of rounding errors... */
         return (Math.Abs(hour1._Value - hour2._Value) <= HourAngle.DefaultHoursDelta
                 || (hour1._Hours == hour2._Hours
                     && hour1._Minutes == hour2._Minutes
                     && Math.Abs(hour1._Seconds - hour2._Seconds) <= (HourAngle.DefaultHoursDelta * 3600.0)));
      }

      public static bool operator !=(HourAngle hour1, HourAngle hour2)
      {
         return !(hour1 == hour2);
      }

      public static bool operator <(HourAngle hour1, HourAngle hour2)
      {
         return (hour1.Format == HourAngleFormat.DecimalHours
                     ? (hour1._Value < hour2._Value)
                     : (hour1.TotalSeconds < hour2.TotalSeconds));
      }

      public static bool operator <=(HourAngle hour1, HourAngle hour2)
      {
         return (hour1.Format == HourAngleFormat.DecimalHours
                     ? (hour1._Value <= hour2._Value)
                     : (hour1.TotalSeconds <= hour2.TotalSeconds));
      }

      public static bool operator >(HourAngle hour1, HourAngle hour2)
      {
         return (hour1.Format == HourAngleFormat.DecimalHours
                     ? (hour1._Value > hour2._Value)
                     : (hour1.TotalSeconds > hour2.TotalSeconds));
      }

      public static bool operator >=(HourAngle hour1, HourAngle hour2)
      {
         return (hour1.Format == HourAngleFormat.DecimalHours
                     ? (hour1._Value >= hour2._Value)
                     : (hour1.TotalSeconds >= hour2.TotalSeconds));
      }

      public static HourAngle operator +(HourAngle hour1, HourAngle hour2)
      {
         if (hour1.Format == HourAngleFormat.DecimalHours) {
            hour1.Value += hour2.Value;    /* Use Value property to ensure DMS properties are also updated properly */
         }
         else {
            double seconds = hour1.TotalSeconds + hour2.TotalSeconds;
            hour1 = FromSeconds(seconds);
         }

         return hour1;
      }

      public static HourAngle operator -(HourAngle hour1, HourAngle hour2)
      {
         if (hour1.Format == HourAngleFormat.DecimalHours) {
            hour1.Value -= hour2.Value;    /* Use Value property to ensure DMS properties are also updated properly */
         }
         else {
            double seconds = hour1.TotalSeconds - hour2.TotalSeconds;
            hour1 = FromSeconds(seconds);
         }

         return hour1;
      }

      public static HourAngle operator *(HourAngle hour, double factor)
      {
         if (hour.Format == HourAngleFormat.DecimalHours) {
            hour.Value *= factor;  /* Use Value property to ensure HMS properties are also updated properly */
         }
         else {
            double seconds = hour.TotalSeconds * factor;
            hour = FromSeconds(seconds);
         }

         return hour;
      }

      public static HourAngle operator /(HourAngle hour, double factor)
      {
         if (hour.Format == HourAngleFormat.DecimalHours) {
            hour.Value /= factor;     /* Use Value property to ensure DMS properties are also updated properly */
         }
         else {
            double seconds = hour.TotalSeconds / factor;
            hour = FromSeconds(seconds);
         }

         return hour;
      }

      public static double operator /(HourAngle hour1, HourAngle hour2)
      {
         return (hour2.Format == HourAngleFormat.DecimalHours
                     ? (double)(hour1._Value / hour2._Value)
                     : hour1.TotalSeconds / hour2.TotalSeconds);
      }

      public override int GetHashCode()
      {
         return _Value.GetHashCode();
      }

      public override bool Equals(object obj)
      {
         return (obj is HourAngle
                 && this == (HourAngle)obj);
      }

      public double[] ToHms()
      {
         return new double[] {(double)_Hours,
                              (double)_Minutes,
                              (double)_Seconds,
                             };
      }

      public double Delta(HourAngle hour)
      {
         double delta = hour.Value - (double)_Value;
         return (Math.Abs(delta) <= 180.0 ? delta
                                          : (360.0 - Math.Abs(delta)) * (hour.Value > (double)_Value ? -1.0 : 1.0));

      }

      public override string ToString()
      {
         return ToString(_Format);
      }

      public string ToString(string format)
      {
         return _Value.ToString(format);
      }

      public string ToString(HourAngleFormat format)
      {
         string text = string.Empty;
         int hours;
         int minutes;
         double doubleMinutes;
         double seconds;
         int increment;

         switch (format) {
            case HourAngleFormat.DecimalHours:
               text = CustomFormat.ToString(Resources.HourAngleInDecimalHours, _NumberDecimalDigitsForHours, _Value);
               break;

            case HourAngleFormat.HoursDecimalMinutes:
               increment = (_Value >= 0.0 ? 1 : -1);
               doubleMinutes = Math.Round((double)_Minutes + (_Seconds / 60.0), _NumberDecimalDigitsForHours);
               if (Math.Abs(doubleMinutes) < 60.0) {
                  hours = _Hours;
               }
               else {
                  doubleMinutes = 0.0;
                  hours = _Hours + increment;
               }

               text = CustomFormat.ToString(Resources.HourAngleInHoursDecimalMinutes, _NumberDecimalDigitsForHours,
                                            hours, doubleMinutes);
               break;

            case HourAngleFormat.HoursMinutesSeconds:
            case HourAngleFormat.CadHoursMinutesSeconds:
            case HourAngleFormat.CompactHoursMinutesSeconds:
               increment = (_Value >= 0.0 ? 1 : -1);
               seconds = Math.Round(_Seconds, format != HourAngleFormat.CompactHoursMinutesSeconds ? _NumberDecimalDigitsForSeconds
                                                                                                      : HourAngle.NumberDecimalDigitsForCompactSeconds);

               if (Math.Abs(seconds) < 60.0) {
                  minutes = _Minutes;
               }
               else {
                  seconds = 0.0;
                  minutes = _Minutes + increment;
               }

               if (Math.Abs(minutes) < 60) {
                  hours = _Hours;
               }
               else {
                  minutes = 0;
                  hours = _Hours + increment;
               }

               if (format == HourAngleFormat.HoursMinutesSeconds) {
                  text = CustomFormat.ToString(Resources.HourAngleInHoursMinutesSeconds, _NumberDecimalDigitsForSeconds,
                                               hours, minutes, seconds);
               }
               else if (format == HourAngleFormat.CompactHoursMinutesSeconds) {
                  text = CustomFormat.ToString(Resources.HoursAngleInCompactHoursMinutesSeconds, _NumberDecimalDigitsForSeconds,
                     hours, minutes, seconds);
               }
               else {
                  /* Because 'CAD' coordinates use a comma as the lat/long delimiter, a period must
                     be used as the double point, hence the use of the InvariantCulture.
                  */
                  text = CustomFormat.ToString(CultureInfo.InvariantCulture,
                                               Resources.HourAngleInCadHoursMinutesSeconds, _NumberDecimalDigitsForSeconds,
                                               hours, minutes, seconds);
               }
               break;


            default:
               Debug.Assert(format == HourAngleFormat.NotSpecified, "Unrecognised HourAngleFormat value - " + format.ToString());
               text = CustomFormat.ToString(Resources.HourAngleWithNoFormat, _NumberDecimalDigitsForHours, _Value);
               break;
         }

         return text;
      }

      public void Normalize()
      {
         _Value = HourAngle.Normalize(_Value);
         _Hours = (int)HourAngle.Normalize((double)_Hours);
      }

      /// <summary>
      /// Converts an hour to the range 0.0 to 360.0 hours
      /// </summary>

      /// <summary>
      /// Converts an hour to the range 0.0 to 360.0 hours
      /// </summary>
      public static double Normalize(double hour)
      {
         hour %= 360.0;

         if (hour < 0.0) {
            hour += 360.0;
         }

         return hour;
      }

      /// <summary>
      /// Converts the hour to the range 180.0 to -180.0 hours
      /// </summary>
      public void NormalizeTo180()
      {
         _Value = HourAngle.NormalizeTo180(_Value);
         SetHmsFromHours(_Value);
      }


      /// <summary>
      /// Converts the hour to the range 180.0 to -180.0 hours
      /// </summary>
      public static double NormalizeTo180(double hour)
      {
         hour %= 360.0;   /* Need it in the standard range first */

         if (hour > 180.0) {
            hour -= 360.0;
         }
         else if (hour < -180.0) {
            hour += 360.0;
         }

         return hour;
      }

      // TODO: this is now redundant (use hour.TotalSeconds = value)
      private static HourAngle FromSeconds(double seconds)
      {
         int hours = (int)Truncate(seconds / 3600.0);
         seconds %= 3600.0;
         int minutes = (int)Truncate(seconds / 60.0);
         seconds = seconds % 60.0;

         return new HourAngle(hours, minutes, seconds);
      }

      private static double HmsToHours(int hours, int minutes, double seconds)
      {
         Debug.Assert((hours >= 0 && minutes >= 0 && seconds >= 0.0)
                      || (hours <= 0 && minutes <= 0 && seconds <= 0.0),
                      "Hours/minutes/seconds don't have consistent signs.");

         return (double)hours + ((double)minutes / 60.0) + (seconds / 3600.0);
      }

      public static double RadiansToHours(double radians)
      {
         return radians * (12.0 / Math.PI);
      }

      public static double HoursToRadians(double hours)
      {
         return hours / (12.0 / Math.PI);
      }


      internal static Regex[] BuildRegexArray(string[] regexPatterns)
      {
         Regex[] regexes = new Regex[regexPatterns.Length];

         for (int i = 0; i < regexPatterns.Length; i++) {
            regexes[i] = new Regex(@"^\s*" + regexPatterns[i] + @"\s*$",
                                   RegexOptions.Compiled | RegexOptions.IgnoreCase);
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

      private void SetHmsFromHours(double hour)
      {
         _Hours = (int)Truncate(hour);
         hour = (hour - _Hours) * 60.0;
         _Minutes = (int)Truncate(hour);
         _Seconds = (hour - _Minutes) * 60.0;
      }

      private double SetHoursFromHms()
      {
         if (_Hours < 0 || _Minutes < 0 || _Seconds < 0.0) {
            SetHmsToNegative();
         }

         return (double)_Hours + ((double)_Minutes / 60.0) + (_Seconds / 3600.0);
      }

      private void MatchHmsSigns(double value)
      {
         /* If the value is zero, no sign can be inferred */
         if (value > 0.0) {
            SetHmsToPositive();
         }
         else if (value < 0.0) {
            SetHmsToNegative();
         }
      }

      private void SetHmsToPositive()
      {
         if (_Hours < 0) {
            _Hours *= -1;
         }

         if (_Minutes < 0) {
            _Minutes *= -1;
         }

         if (_Seconds < 0.0) {
            _Seconds *= -1.0;
         }
      }

      private void SetHmsToNegative()
      {
         if (_Hours > 0) {
            _Hours *= -1;
         }

         if (_Minutes > 0) {
            _Minutes *= -1;
         }

         if (_Seconds > 0.0) {
            _Seconds *= -1.0;
         }
      }


      #region IComparable Members

      public int CompareTo(object obj)
      {
         int result = 0;

         if (obj == null
             || !(obj is HourAngle)) {
            result = 1;
         }
         else {
            HourAngle that = (HourAngle)obj;
            result = _Value.CompareTo(that._Value);
         }

         return result;
      }

      #endregion


      public static double Truncate(double value)
      {
         return Math.Truncate(value);
      }

   }
}