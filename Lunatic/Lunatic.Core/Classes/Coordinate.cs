
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Lunatic.Core
{
   public enum CoordinateFormat
   {
      [Description("(Not specified)")]
      /// <summary>CoordinateFormat not specified</summary>
      NotSpecified = 0,

      [Description("UTM (Cartesian)")]
      /// <summary>UTM (Cartesian)</summary>
      Cartesian = 1,

      [Description("DD.dd\u00B0")]
      /// <summary>Decimal Degrees</summary>
      DecimalDegrees = 2,

      [Description("DD\u00B0 MM.mm'")]
      /// <summary>Degrees and decimal minutes</summary>
      DegreesDecimalMinutes = 3,

      [Description("DD\u00B0 MM' SS.ss\"")]
      /// <summary>Degrees, minutes and seconds</summary>
      DegreesMinutesSeconds = 4,

      [Description("DD:MM:SS.ss")]
      /// <summary>Degrees, minutes and seconds (colon-delimited)</summary>
      CadDegreesMinutesSeconds = 5,

      [Description("DDMMSS.ssss")]
      /// <summary>Degrees, minutes and seconds (compact format)</summary>
      CompactDegreesMinutesSeconds = 6
   }

   public struct Coordinate
      : IComparable
   {
      /* HACK ALERT  It would seem sensible to define these indices as an enum (since that's what enums
         are for, right?).  But .NET forces them to be explicitly cast back to ints before they can be
         used, which seems to defeat the point of them.  So, since there's only the three of them, define
         them as integer constants instead.
      */
      public const int Degrees = 0;
      public const int Minutes = 1;
      public const int Seconds = 2;

      /* Hardcoded with period as decimal point and comma as X/Y delimiter, i.e. InvariantCulture format */
      private const string CartesianFormat = @"(?<X>\-?\d+\.?\d*)\s*\,\s*(?<Y>\-?\d+\.?\d*)";
      private const double DefaultDelta = 0.0001;    /* For both metric and imperial units, this is still pretty small for a cartesian coordinate! */

      private static Regex[] _CartesianRegexes;
      private static Regex[] _DegRegexes;
      private static Regex[] _DdmRegexes;
      private static Regex[] _DmsRegexes;
      private static Regex[] _CadDmsRegexes;
      private static CoordinateFormat _DefaultGlobalFormat = CoordinateFormat.NotSpecified;

      private Angle _Latitude;      /* in degrees */
      private Angle _Longitude;     /* in degrees */
      /* In everyday use, the projection and zone are specified in a single value, e.g. UTM84-30U.  However, to avoid
         the Projection enum containing 100s of values for every possible zone, we're splitting it into separate
         Projection and Zone values.
      */
      private bool _LatLongHaveBeenSet;
      private CoordinateFormat _DefaultFormat;

      static Coordinate()
      {
         string[] degreesFormats = new string[] {Angle.DegreesPrefixedNs + @"\s+" + Angle.DegreesPrefixedEw,
                                                 Angle.DegreesSuffixedNs + @"\s+" + Angle.DegreesSuffixedEw,
                                                 Angle.DegreesSignedNs   + @"\s+" + Angle.DegreesSignedEw
                                                };

         string[] degMinFormats = new string[] {Angle.DegMinPrefixedNs + @"\s+" + Angle.DegMinPrefixedEw,
                                                Angle.DegMinSuffixedNs + @"\s+" + Angle.DegMinSuffixedEw,
                                                Angle.DegMinSignedNs   + @"\s+" + Angle.DegMinSignedEw
                                               };

         string[] degMinSecFormats = new string[] {Angle.DegMinSecPrefixedNs + @"\s+" + Angle.DegMinSecPrefixedEw,
                                                   Angle.DegMinSecSuffixedNs + @"\s+" + Angle.DegMinSecSuffixedEw,
                                                   Angle.DegMinSecSignedNs   + @"\s+" + Angle.DegMinSecSignedEw,
                                                   Angle.DegMinSecCompactNs  + @"\s+" + Angle.DegMinSecCompactEw
                                                  };

         string[] cadDegMinSecFormats = new string[] {Angle.CadDegMinSecPrefixedNs + @"\s*\,?\s*" + Angle.CadDegMinSecPrefixedEw,
                                                      Angle.CadDegMinSecSuffixedNs + @"\s*\,?\s*" + Angle.CadDegMinSecSuffixedEw,
                                                      Angle.CadDegMinSecSignedNs   + @"\s*\,?\s*" + Angle.CadDegMinSecSignedEw
                                                     };

         _DegRegexes = BuildRegexArray(degreesFormats);
         _DdmRegexes = BuildRegexArray(degMinFormats);
         _DmsRegexes = BuildRegexArray(degMinSecFormats);
         _CadDmsRegexes = BuildRegexArray(cadDegMinSecFormats);
      }



      public Coordinate(Angle latitude, Angle longitude)
      {
         if (latitude.Value > 90.0
             || latitude.Value < -90.0) {
            throw new ArgumentOutOfRangeException("latitude", latitude, "Latitude must be between +90.0 and -90.0 degrees.");
         }

         if (longitude.Value > 180.0
             || longitude.Value < -180.0) {
            throw new ArgumentOutOfRangeException("longitude", longitude, "Longitude must be between +180.0 and -180.0 degrees.");
         }

         _Latitude = latitude;
         _Longitude = longitude;
         _DefaultFormat = CoordinateFormat.DecimalDegrees;
         _LatLongHaveBeenSet = true;


      }

      public Coordinate(double[] latitude, double[] longitude)
         : this(new Angle(latitude), new Angle(longitude))
      {
         _DefaultFormat = CoordinateFormat.DegreesMinutesSeconds;
      }

      public Coordinate(string coordinate)
      {
         bool isValid = false;

         /* All members must be set before calling out of the constructor */
         _Latitude = 0.0;
         _Longitude = 0.0;
         _DefaultFormat = CoordinateFormat.CompactDegreesMinutesSeconds;
         _LatLongHaveBeenSet = false;

         /* Whilst there's only one 'real' degrees symbol, there are a couple of others that
            look similar and might be used instead.  To keep the regexes simple, we'll
            standardise on the real degrees symbol (Google Earth uses this one, in
            particular).
         */
         coordinate = Angle.StandardizeDegreesSymbol(coordinate);


         foreach (Regex regex in _CadDmsRegexes) {
            Match match = regex.Match(coordinate);
            if (match.Success) {
               decimal[] dms = new decimal[] {Convert.ToDecimal(match.Groups["LatDeg"].Value, CultureInfo.InvariantCulture),
                                                 Convert.ToDecimal(match.Groups["LatMin"].Value, CultureInfo.InvariantCulture),
                                                 Convert.ToDecimal(match.Groups["LatSec"].Value, CultureInfo.InvariantCulture)
                                                };
               _Latitude = new Angle(dms);
               if (match.Groups["NS"].Value.IndexOfAny(new char[] { 'S', 's' }) >= 0) {
                  _Latitude *= -1.0;
               }

               dms = new decimal[] {Convert.ToDecimal(match.Groups["LongDeg"].Value, CultureInfo.InvariantCulture),
                                       Convert.ToDecimal(match.Groups["LongMin"].Value, CultureInfo.InvariantCulture),
                                       Convert.ToDecimal(match.Groups["LongSec"].Value, CultureInfo.InvariantCulture)
                                      };
               _Longitude = new Angle(dms);
               if (match.Groups["EW"].Value.IndexOfAny(new char[] { 'W', 'w' }) >= 0) {
                  _Longitude *= -1.0;
               }

               _DefaultFormat = CoordinateFormat.CadDegreesMinutesSeconds;
               _LatLongHaveBeenSet = true;

               isValid = true;
               break;
            }
         }

         if (!isValid) {
            /* The remaining formats can be locale-specific, but the regex patterns have to be
               hardcoded with a period as the decimal point.  So to keep things simple, we'll
               just replace the locale-specific separator with the invariant one.  (Don't need
               to worry about grouping characters, etc, since the numbers shouldn't be that big.)
            */
            coordinate = coordinate.Replace(CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator, ".");

            foreach (Regex regex in _DegRegexes) {
               Match match = regex.Match(coordinate);
               if (match.Success) {
                  _Latitude = Convert.ToDecimal(match.Groups["LatDeg"].Value, CultureInfo.InvariantCulture);
                  if (match.Groups["NS"].Value.IndexOfAny(new char[] { 'S', 's' }) >= 0) {
                     _Latitude *= -1.0;
                  }

                  _Longitude = Convert.ToDecimal(match.Groups["LongDeg"].Value, CultureInfo.InvariantCulture);
                  if (match.Groups["EW"].Value.IndexOfAny(new char[] { 'W', 'w' }) >= 0) {
                     _Longitude *= -1.0;
                  }

                  _DefaultFormat = CoordinateFormat.DecimalDegrees;
                  _LatLongHaveBeenSet = true;

                  isValid = true;
                  break;
               }
            }
         }

         if (!isValid) {
            foreach (Regex regex in _DdmRegexes) {
               Match match = regex.Match(coordinate);
               if (match.Success) {
                  decimal[] ddm = new decimal[] {Convert.ToDecimal(match.Groups["LatDeg"].Value, CultureInfo.InvariantCulture),
                                                 Convert.ToDecimal(match.Groups["LatMin"].Value, CultureInfo.InvariantCulture)
                                                };
                  _Latitude = new Angle(ddm);
                  if (match.Groups["NS"].Value.IndexOfAny(new char[] { 'S', 's' }) >= 0) {
                     _Latitude *= -1.0;
                  }

                  ddm = new decimal[] {Convert.ToDecimal(match.Groups["LongDeg"].Value, CultureInfo.InvariantCulture),
                                       Convert.ToDecimal(match.Groups["LongMin"].Value, CultureInfo.InvariantCulture)
                                      };
                  _Longitude = new Angle(ddm);
                  if (match.Groups["EW"].Value.IndexOfAny(new char[] { 'W', 'w' }) >= 0) {
                     _Longitude *= -1.0;
                  }

                  _DefaultFormat = CoordinateFormat.DegreesDecimalMinutes;
                  _LatLongHaveBeenSet = true;

                  isValid = true;
                  break;
               }
            }
         }

         if (!isValid) {
            foreach (Regex regex in _DmsRegexes) {
               Match match = regex.Match(coordinate);
               if (match.Success) {
                  decimal[] dms = new decimal[] {Convert.ToDecimal(match.Groups["LatDeg"].Value, CultureInfo.InvariantCulture),
                                                 Convert.ToDecimal(match.Groups["LatMin"].Value, CultureInfo.InvariantCulture),
                                                 Convert.ToDecimal(match.Groups["LatSec"].Value, CultureInfo.InvariantCulture)
                                                };
                  _Latitude = new Angle(dms);
                  if (match.Groups["NS"].Value.IndexOfAny(new char[] { 'S', 's' }) >= 0) {
                     _Latitude *= -1.0;
                  }

                  dms = new decimal[] {Convert.ToDecimal(match.Groups["LongDeg"].Value, CultureInfo.InvariantCulture),
                                       Convert.ToDecimal(match.Groups["LongMin"].Value, CultureInfo.InvariantCulture),
                                       Convert.ToDecimal(match.Groups["LongSec"].Value, CultureInfo.InvariantCulture)
                                      };
                  _Longitude = new Angle(dms);
                  if (match.Groups["EW"].Value.IndexOfAny(new char[] { 'W', 'w' }) >= 0) {
                     _Longitude *= -1.0;
                  }

                  _DefaultFormat = CoordinateFormat.DegreesMinutesSeconds;
                  _LatLongHaveBeenSet = true;

                  isValid = true;
                  break;
               }
            }
         }

         if (!isValid) {
            throw new FormatException("Invalid coordinate format.");
         }

      }

      #region Properties ...
      public Angle Latitude
      {
         get
         {
            return _Latitude;
         }
         set
         {
            if (value > 90.0
                || value < -90.0) {
               throw new ArgumentOutOfRangeException("value", value, "Latitude must be between +90.0 and -90.0 degrees.");
            }

            if (value != _Latitude) {
               _Latitude = value;
            }

            _LatLongHaveBeenSet = true;   /* Assumes that the longitude is/will be set before accessing X/Y */
         }
      }

      public Angle Longitude
      {
         get
         {
            return _Longitude;
         }
         set
         {
            if (value > 180.0
                || value < -180.0) {
               throw new ArgumentOutOfRangeException("value", value, "Longitude must be between +180.0 and -180.0 degrees.");
            }

            if (value != _Longitude) {
               _Longitude = value;
            }

            _LatLongHaveBeenSet = true;   /* Assumes that the latitude is/will be set before accessing X/Y */
         }
      }


      /// <remarks>
      /// The idea behind this method is to provide an efficient way to set the lat/long and X/Y values on a
      /// coordinate from another.  This ensures that the current coordinate's projection, etc, remain set
      /// and that the process takes into account the fact that one of the value pairs may not have been set
      /// yet.
      /// </remarks>
      public void Update(Coordinate coordinate)
      {
            _Latitude = coordinate._Latitude;
            _Longitude = coordinate._Longitude;
      }

      /// <summary>
      /// Sets the <see cref="Latitude"/> and <see cref="Longitude"/> properties, retaining the existing <see cref="Projection"/>, etc.
      /// </summary>
      public void Update(Angle latitude, Angle longitude)
      {
         if (latitude != _Latitude) {
            if (latitude > 90.0
                || latitude < -90.0) {
               throw new ArgumentOutOfRangeException("value", latitude, "Latitude must be between +90.0 and -90.0 degrees.");
            }

            _Latitude = latitude;
         }

         if (longitude != _Longitude) {
            if (longitude > 180.0
                || longitude < -180.0) {
               throw new ArgumentOutOfRangeException("value", longitude, "Longitude must be between +180.0 and -180.0 degrees.");
            }

            _Longitude = longitude;
         }

         _LatLongHaveBeenSet = true;
      }



      #endregion

      /// <summary>
      /// Gets a value indicating whether the X/Y or lat/long values have been set.
      /// </summary>
      /// <value>
      /// <b>True</b> if neither the <see cref="X"/>/<see cref="Y"/> nor <see cref="Latitude"/>/<see cref="Longitude"/> properties
      /// have been set or are all zero; otherwise, <b>False</b>.
      /// </value>
      [Browsable(false)]
      [ReadOnly(true)]
      public bool IsNull
      {
         get
         {
            return (_Latitude.Value == 0.0 && _Longitude.Value == 0.0);
         }
      }

      public static CoordinateFormat DefaultFormat
      {
         get
         {
            return _DefaultGlobalFormat;
         }
         set
         {
            _DefaultGlobalFormat = value;
         }
      }


      /// <summary>
      /// Compares the two specified coordinates, using the default delta value.
      /// </summary>
      public static bool operator ==(Coordinate coordinate1, Coordinate coordinate2)
      {
         /* Hack alert!  Currently, the conversion between X/Y and lat/longs is not automatically
            done until the corresponding values are accessed.  Until then, one pair of values will
            remain as zero.  Since we don't know which pair that will be (and it could be different
            for the two coordinates), we have to check both pairs of values to be sure.

            Note that if both coords have the same value pairs set, the comparisons are using the
            underlying member variables directly; accessing the values via the property methods
            instead would always trigger the X/Y-lat/long translation, killing performance.
         */
         bool areEqual = false;

         bool isCoordinate1Null = coordinate1.IsNull;
         bool isCoordinate2Null = coordinate2.IsNull;

         /* First, some easy shortcuts */
         if (isCoordinate1Null
             && isCoordinate2Null) {
            areEqual = true;
         }
         else if (isCoordinate1Null != isCoordinate2Null) {
            areEqual = false;
         }
         else if (coordinate1._LatLongHaveBeenSet
                  && coordinate2._LatLongHaveBeenSet) {
            areEqual = (coordinate1._Latitude == coordinate2._Latitude
                        && coordinate1._Longitude == coordinate2._Longitude);
         }
         else {
            /* Use the public properties here since the values will have to be converted if not
               available anyway in order to do the comparison.
            */
            areEqual = (coordinate1.Latitude == coordinate2.Latitude
                        && coordinate1.Longitude == coordinate2.Longitude);
         }

         return areEqual;
      }

      public static bool operator !=(Coordinate coordinate1, Coordinate coordinate2)
      {
         return !(coordinate1 == coordinate2);
      }

      public override int GetHashCode()
      {
         /* Just create a string that is unique for the coordinate's value and get the hashcode from that */
         return _Latitude.GetHashCode() ^ _Longitude.GetHashCode();
      }

      public override bool Equals(object obj)
      {
         return (obj is Coordinate
                 && this == (Coordinate)obj);
      }

      public bool Equals(Coordinate obj, double delta)
      {
         bool areEqual = false;
         delta = Math.Abs(delta);      /* Just in case! */

         if (this._LatLongHaveBeenSet
             && obj._LatLongHaveBeenSet) {
            areEqual = (Math.Abs(this._Latitude.Value - obj._Latitude.Value) <= delta
                        && Math.Abs(this._Longitude.Value - obj._Longitude.Value) <= delta);
         }
         else {
            /* Use the public properties here since the values will have to be converted if not
               available anyway in order to do the comparison.
            */
            areEqual = (Math.Abs(this.Latitude.Value - obj.Latitude.Value) <= delta
                        && Math.Abs(this.Longitude.Value - obj.Longitude.Value) <= delta);
         }

         return areEqual;
      }

      public override string ToString()
      {
         return ToString(CoordinateFormat.NotSpecified);
      }

      public string ToString(CoordinateFormat format)
      {
         string coordinate = string.Empty;

         format = GetActiveCoordinateFormat(format);

         switch (format) {
            case CoordinateFormat.DecimalDegrees:
               coordinate = string.Format(CultureInfo.CurrentCulture, "{0}{1} {2}{3}",
                                          _Latitude.Abs.ToString(AngularFormat.DecimalDegrees),
                                          _Latitude >= 0.0 ? "N" : "S",
                                          _Longitude.Abs.ToString(AngularFormat.DecimalDegrees),
                                          _Longitude >= 0.0 ? "E" : "W");
               break;

            case CoordinateFormat.DegreesDecimalMinutes:
               coordinate = string.Format(CultureInfo.CurrentCulture, "{0}{1} {2}{3}",
                                          _Latitude.Abs.ToString(AngularFormat.DegreesDecimalMinutes),
                                          _Latitude >= 0.0 ? "N" : "S",
                                          _Longitude.Abs.ToString(AngularFormat.DegreesDecimalMinutes),
                                          _Longitude >= 0.0 ? "E" : "W");
               break;

            case CoordinateFormat.DegreesMinutesSeconds:
               coordinate = string.Format(CultureInfo.CurrentCulture, "{0}{1} {2}{3}",
                                          _Latitude.Abs.ToString(AngularFormat.DegreesMinutesSeconds),
                                          _Latitude >= 0.0 ? "N" : "S",
                                          _Longitude.Abs.ToString(AngularFormat.DegreesMinutesSeconds),
                                          _Longitude >= 0.0 ? "E" : "W");
               break;

            case CoordinateFormat.CompactDegreesMinutesSeconds:
               coordinate = string.Format(CultureInfo.CurrentCulture, "{0} {1}",
                                          _Latitude.ToString(AngularFormat.CompactDegreesMinutesSeconds, false),
                                          _Longitude.ToString(AngularFormat.CompactDegreesMinutesSeconds, true));
               break;

            case CoordinateFormat.CadDegreesMinutesSeconds:
               /* Because 'CAD' coordinates use a comma as the lat/long delimiter, a period must
                  be used as the decimal point, hence the use of the InvariantCulture.
               */
               coordinate = string.Format(CultureInfo.InvariantCulture, "{0}, {1}",
                                          _Latitude.ToString(AngularFormat.CadDegreesMinutesSeconds),
                                          _Longitude.ToString(AngularFormat.CadDegreesMinutesSeconds));
               break;

            default:
               throw new InvalidEnumArgumentException("format", Convert.ToInt32(format),
                                                      typeof(CoordinateFormat));
         }

         return coordinate;
      }

      private CoordinateFormat GetActiveCoordinateFormat(CoordinateFormat format)
      {
         return (format != CoordinateFormat.NotSpecified
                     ? format
                     : (_DefaultGlobalFormat != CoordinateFormat.NotSpecified
                           ? _DefaultGlobalFormat
                           : _DefaultFormat != CoordinateFormat.NotSpecified ? _DefaultFormat
                                                                             : CoordinateFormat.CompactDegreesMinutesSeconds));
      }


      private static Regex[] BuildRegexArray(string[] regexPatterns)
      {
         Regex[] regexes = new Regex[regexPatterns.Length];

         for (int i = 0; i < regexPatterns.Length; i++) {
            regexes[i] = new Regex(@"^\s*" + regexPatterns[i] + @"\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
         }

         return regexes;
      }

      #region IComparable Members

      public int CompareTo(object obj)
      {
         int result = 0;

         if (obj == null
             || !(obj is Coordinate)) {
            result = 1;
         }
         else {
            Coordinate that = (Coordinate)obj;
            result = _Latitude.CompareTo(that._Latitude);

            if (result == 0) {
               result = _Longitude.CompareTo(that._Longitude);
            }
         }

         return result;
      }

      #endregion

   }
}
