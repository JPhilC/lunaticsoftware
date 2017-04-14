using System;
using System.Text;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using Lunatic.Core;
using System.Threading;
using ASCOM.Lunatic.Telescope;
using ASCOM.Lunatic.Telescope.Classes;
using Lunatic.Core.Geometry;

/// <summary>
/// The ASCOM ITelescopeV3 implimentation for the driver.
/// </summary>
namespace ASCOM.Lunatic.Telescope
{
   partial class Telescope
   {
      #region PUBLIC COM INTERFACE ITelescopeV3 IMPLEMENTATION


      /// <summary>
      /// Displays the Setup Dialog form.
      /// If the user clicks the OK button to dismiss the form, then
      /// the new settings are saved, otherwise the old values are reloaded.
      /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
      /// </summary>
      public void SetupDialog()
      {
         // Increment the server lock to prevent it disappearing while the Setup dialog is open
         TelescopeServer.CountLock();
         SetupViewModel setupVm = ViewModelLocator.Current.Setup;
         SetupThread setupThread = new SetupThread(setupVm, _Logger, new SetupCallback(ResultCallback));
         Thread thread = new Thread(new ThreadStart(setupThread.ThreadProc));
         thread.SetApartmentState(ApartmentState.STA);
         thread.Start();
      }

      private void ResultCallback(bool? result)
      {
         if (result.HasValue && result.Value) {
            SettingsProvider.Current.SaveSettings(); // Persist device configuration values Lunatic Settings
            WriteProfile(); // Persist device configuration values to the ASCOM Profile store
         }
         TelescopeServer.UncountLock();
         TelescopeServer.ExitIf();
      }

      public ArrayList SupportedActions
      {
         get
         {
            _Logger.LogMessage("SupportedActions Get", "Returning empty arraylist");
            return new ArrayList();
         }
      }

      public string Action(string actionName, string actionParameters)
      {
         throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
      }

      public void CommandBlind(string command, bool raw)
      {
         //CheckConnected("CommandBlind");
         //// Call CommandString and return as soon as it finishes
         //this.CommandString(command, raw);
         // or
         throw new ASCOM.MethodNotImplementedException("CommandBlind");
         // DO NOT have both these sections!  One or the other
      }

      public bool CommandBool(string command, bool raw)
      {
         //CheckConnected("CommandBool");
         //string ret = CommandString(command, raw);
         // or
         throw new ASCOM.MethodNotImplementedException("CommandBool");
         // DO NOT have both these sections!  One or the other
      }

      public string CommandString(string command, bool raw)
      {
         CheckConnected("CommandString");
         // it's a good idea to put all the low level communication with the device here,
         // then all communication calls this function
         // you need something to ensure that only one command is in progress at a time

         throw new ASCOM.MethodNotImplementedException("CommandString");
      }

      public new void Dispose()
      {
         System.Diagnostics.Trace.WriteLine("ASCOM.Lunatic.Telescope.Dispose() called.");
         // Clean up the tracelogger and util objects
         _Logger.Enabled = false;
         _Logger.Dispose();
         _Logger = null;
         utilities.Dispose();
         utilities = null;
         astroUtilities.Dispose();
         astroUtilities = null;
         base.Dispose();
      }

      public bool Connected
      {
         get
         {
            _Logger.LogMessage("Connected Get", IsConnected.ToString());
            return IsConnected;
         }
         set
         {
            _Logger.LogMessage("Connected", "Set - " + value.ToString());
            if (value == IsConnected)
               return;

            if (value) {
               _Logger.LogMessage("Connected", "Set - Connecting to port " + Settings.COMPort);
               int connectionResult = _Mount.Connect(Settings.COMPort, (int)Settings.BaudRate, (int)Settings.Timeout, (int)Settings.Retry);
               if (connectionResult == 0) {
                  // Need to send current axis position (E)
                  _Mount.MCSetAxisPosition(AxisId.Axis1_RA, Settings.RAAxisPosition);
                  _Mount.MCSetAxisPosition(AxisId.Axis2_DEC, Settings.DECAxisPosition);

                  IsConnected = true;
               }
               else if (connectionResult == 1) {
                  // Was already connected to GET the current axis positions
                  Settings.RAAxisPosition = _Mount.MCGetAxisPosition(AxisId.Axis1_RA);
                  Settings.DECAxisPosition = _Mount.MCGetAxisPosition(AxisId.Axis2_DEC);
                  IsConnected = true;
               }
               else {
                  // Something went wrong so not connected.
                  IsConnected = false;
               }
            }
            else {
               _Mount.Disconnect();
               _Logger.LogMessage("Connected", "Set - Disconnecting from port " + Settings.COMPort);
            }
         }
      }

      public string Description
      {
         get
         {
            _Logger.LogMessage("Description", "Get - " + INSTRUMENT_DESCRIPTION);
            return INSTRUMENT_DESCRIPTION;
         }
      }

      public string DriverInfo
      {
         get
         {
            //TODO: See if the same version information exists in versionInfo as version.
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}, Version {1}.{2}\n", INSTRUMENT_DESCRIPTION,
               SettingsProvider.MajorVersion,
               SettingsProvider.MinorVersion);
            sb.AppendLine(SettingsProvider.CompanyName);
            sb.AppendLine(SettingsProvider.Copyright);
            sb.AppendLine(SettingsProvider.Comments);
            string driverInfo = sb.ToString();
            _Logger.LogMessage("DriverInfo", "Get - " + driverInfo);
            return driverInfo;
         }
      }

      public string DriverVersion
      {
         get
         {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            _Logger.LogMessage("DriverVersion", "Get - " + driverVersion);
            return driverVersion;
         }
      }

      public short InterfaceVersion
      {
         // set by the driver wizard
         get
         {
            _Logger.LogMessage("InterfaceVersion", "Get - 3");
            return Convert.ToInt16("3");
         }
      }

      public string Name
      {
         get
         {
            _Logger.LogMessage("Name", "Get - " + INSTRUMENT_NAME);
            return INSTRUMENT_NAME;
         }
      }

      public void AbortSlew()
      {
         _Logger.LogMessage("AbortSlew", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("AbortSlew");
         /*
    If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 6, ("COMMAND AbortSlew")
    If gEQparkstatus <> 0 Then
        ' no move axis if parked or parking!
        RaiseError SCODE_INVALID_WHILST_PARKED, ERR_SOURCE, "AbortSlew() " & MSG_SCOPE_PARKED
        Exit Sub
    End If

    If gSlewStatus Then
        gSlewStatus = False
        ' stop the slew if already slewing
'        eqres = EQ_MotorStop(0)
'        eqres = EQ_MotorStop(1)
        eqres = EQ_MotorStop(2)
        gRAStatus_slew = False
    
        ' restart tracking
        RestartTracking

    End If

End Sub
          */
      }

      private AlignmentModes _AlignmentMode;
      public AlignmentModes AlignmentMode
      {
         get
         {
            _Logger.LogMessage("AlignmentMode", "Get - " + _AlignmentMode.ToString());
            return _AlignmentMode;     // Set in Constructor
         }
      }

      private double _Altitude;
      public double Altitude
      {
         get
         {
            _Logger.LogMessage("Altitude", "Get - " + _Altitude.ToString());
            return _Altitude;
         }
      }

      public double ApertureArea
      {
         get
         {
            _Logger.LogMessage("ApertureArea Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("ApertureArea", false);
         }
      }

      public double ApertureDiameter
      {
         get
         {
            _Logger.LogMessage("ApertureDiameter Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("ApertureDiameter", false);
         }
      }

      public bool AtHome
      {
         get
         {
            _Logger.LogMessage("AtHome", "Get - " + false.ToString());
            return false;
         }
      }

      public bool AtPark
      {
         get
         {
            //  ASCOM has no means of detecting a parking state
            // However some folks will be closing their roofs based upon the stare of AtPark
            // So we must respond with a false!
            bool atPark = (Settings.ParkStatus == ParkStatus.Parked);
            _Logger.LogMessage("AtPark", "Get - " + atPark.ToString());
            return atPark;
         }
      }

      public IAxisRates AxisRates(TelescopeAxes Axis)
      {
         _Logger.LogMessage("AxisRates", "Get - " + Axis.ToString());
         return new AxisRates(Axis);
      }

      private double _Azimuth;
      public double Azimuth
      {
         get
         {
            _Logger.LogMessage("Azimuth", "Get - " + _Azimuth.ToString());
            return _Azimuth;
         }
      }

      public bool CanFindHome
      {
         get
         {
            _Logger.LogMessage("CanFindHome", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanMoveAxis(TelescopeAxes Axis)
      {
         _Logger.LogMessage("CanMoveAxis", "Get - " + Axis.ToString());
         switch (Axis) {
            case TelescopeAxes.axisPrimary: return false;
            case TelescopeAxes.axisSecondary: return false;
            case TelescopeAxes.axisTertiary: return false;
            default: throw new InvalidValueException("CanMoveAxis", Axis.ToString(), "0 to 2");
         }
      }

      public bool CanPark
      {
         get
         {
            _Logger.LogMessage("CanPark", "Get - " + true.ToString());
            return true;
         }
      }


      public bool CanPulseGuide
      {
         get
         {
            bool canPulseGuide = (Settings.PulseGuidingMode == PulseGuidingOption.ASCOM);
            _Logger.LogMessage("CanPulseGuide", "Get - " + canPulseGuide.ToString());
            return canPulseGuide;
         }
      }

      public bool CanSetDeclinationRate
      {
         get
         {
            _Logger.LogMessage("CanSetDeclinationRate", "Get - " + true.ToString());
            return true;
         }
      }

      public bool CanSetGuideRates
      {
         get
         {
            bool canSetGuideRates = (Settings.PulseGuidingMode == PulseGuidingOption.ASCOM);
            _Logger.LogMessage("CanSetGuideRates", "Get - " + canSetGuideRates.ToString());
            return canSetGuideRates;
         }
      }

      public bool CanSetPark
      {
         get
         {
            _Logger.LogMessage("CanSetPark", "Get - " + true.ToString());
            return true;
         }
      }

      public bool CanSetPierSide
      {
         get
         {
            _Logger.LogMessage("CanSetPierSide", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetRightAscensionRate
      {
         get
         {
            _Logger.LogMessage("CanSetRightAscensionRate", "Get - " + true.ToString());
            return true;
         }
      }

      public bool CanSetTracking
      {
         get
         {
            _Logger.LogMessage("CanSetTracking", "Get - " + true.ToString());
            return true;
         }
      }

      public bool CanSlew
      {
         get
         {
            _Logger.LogMessage("CanSlew", "Get - " + true.ToString());
            return true;
         }
      }

      public bool CanSlewAltAz
      {
         get
         {
            _Logger.LogMessage("CanSlewAltAz", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSlewAltAzAsync
      {
         get
         {
            _Logger.LogMessage("CanSlewAltAzAsync", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSlewAsync
      {
         get
         {
            _Logger.LogMessage("CanSlewAsync", "Get - " + true.ToString());
            return true;
         }
      }

      public bool CanSync
      {
         get
         {
            _Logger.LogMessage("CanSync", "Get - " + true.ToString());
            return true;
         }
      }

      public bool CanSyncAltAz
      {
         get
         {
            _Logger.LogMessage("CanSyncAltAz", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanUnpark
      {
         get
         {
            _Logger.LogMessage("CanUnpark", "Get - " + true.ToString());
            return true;
         }
      }

      private double _Declination = 0.0;
      public double Declination
      {
         get
         {
            _Logger.LogMessage("Declination", "Get - " + utilities.DegreesToDMS(_Declination, ":", ":"));
            return _Declination;
         }
      }

      private double _DecRateAdjust = 0.0;
      private double _DeclinationRate = 0.0;
      public double DeclinationRate
      {
         get
         {
            _Logger.LogMessage("DeclinationRate", "Get - " + utilities.DegreesToDMS(_DeclinationRate, ":", ":"));
            return _DeclinationRate;
         }
         set
         {
            _Logger.LogMessage("DeclinationRate", "Set" + utilities.DegreesToDMS(value, ":", ":"));
            _DecRateAdjust = value;
            if (Settings.ParkStatus == ParkStatus.Parked) {
               if (value == 0 && _RaRateAdjust == 0) {
                  // TODO: Call EQStartSidereal2
               }
               else {
                  if (TrackingState != TrackingStatus.Off) {
                     if ((_DeclinationRate * value) <= 0) {
                        // TODO: Call StartDEC_By_Rate(value)
                     }
                     else {
                        // TODO: Call ChangeDEC_By_Rate(value)
                     }
                     TrackingState = TrackingStatus.Custom;
                     // TODO: HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(189)
                  }
               }
               _DeclinationRate = value;
            }
            else {
               throw new ASCOM.InvalidOperationException("Invalid while the telescope is parked");
            }
         }
      }

      public DeviceInterface.PierSide DestinationSideOfPier(double RightAscension, double Declination)
      {
         _Logger.LogMessage("DestinationSideOfPier Get", "Not implemented");
         throw new ASCOM.PropertyNotImplementedException("DestinationSideOfPier", false);
      }

      public bool DoesRefraction
      {
         get
         {
            _Logger.LogMessage("DoesRefraction Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DoesRefraction", false);
         }
         set
         {
            _Logger.LogMessage("DoesRefraction Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DoesRefraction", true);
         }
      }

      /// <summary>
      /// Equatorial coordinate system used by this telescope (e.g. Topocentric or J2000).
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// Most amateur telescopes use local topocentric coordinates. This coordinate system is simply the apparent position in the sky
      /// (possibly uncorrected for atmospheric refraction) for "here and now", thus these are the coordinates that one would use with digital setting
      /// circles and most amateur scopes. More sophisticated telescopes use one of the standard reference systems established by professional astronomers.
      /// The most common is the Julian Epoch 2000 (J2000). These instruments apply corrections for precession,nutation, abberration, etc. to adjust the coordinates 
      /// from the standard system to the pointing direction for the time and location of "here and now". 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      public EquatorialCoordinateType EquatorialSystem
      {
         get
         {
            EquatorialCoordinateType equatorialSystem;
            switch (Settings.AscomCompliance.Epoch) {
               case EpochOption.Unknown:
                  equatorialSystem = EquatorialCoordinateType.equOther;
                  break;
               case EpochOption.JNow:
                  equatorialSystem = EquatorialCoordinateType.equLocalTopocentric;
                  break;
               case EpochOption.J2000:
                  equatorialSystem = EquatorialCoordinateType.equJ2000;
                  break;
               case EpochOption.J2050:
                  equatorialSystem = EquatorialCoordinateType.equJ2050;
                  break;
               case EpochOption.B1950:
                  equatorialSystem = EquatorialCoordinateType.equB1950;
                  break;
               default:
                  equatorialSystem = EquatorialCoordinateType.equOther;
                  break;
            }
            _Logger.LogMessage("DeclinationRate", "Get - " + equatorialSystem.ToString());
            return equatorialSystem;
         }
      }

      public void FindHome()
      {
         _Logger.LogMessage("FindHome", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("FindHome");
      }

      public double FocalLength
      {
         get
         {
            _Logger.LogMessage("FocalLength Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("FocalLength", false);
         }
      }

      private double _GuideRateDeclination;
      public double GuideRateDeclination
      {
         get
         {
            _Logger.LogMessage("GuideRateDeclination", "Get - " + _GuideRateDeclination.ToString());
            return _GuideRateDeclination;
         }

         set
         {
            Set<double>(ref _GuideRateDeclination, value);
            _Logger.LogMessage("GuideRateDeclination", "Set - " + _GuideRateDeclination.ToString());
         }
      }

      private double _GuideRateRightAscension;
      public double GuideRateRightAscension
      {
         get
         {
            _Logger.LogMessage("GuideRateRightAscension", "Get - " + _GuideRateRightAscension.ToString());
            return _GuideRateRightAscension;
         }
         set
         {
            Set<double>(ref _GuideRateRightAscension, value);
            _Logger.LogMessage("GuideRateRightAscension", "Set - " + _GuideRateRightAscension.ToString());
         }
      }

      private long _RAPulseDuration;
      private long _DecPulseDuration;
      public bool IsPulseGuiding
      {
         get
         {
            if (Settings.PulseGuidingMode == PulseGuidingOption.ASCOM) {
               bool isPulseGuiding = false;
               if (_RAPulseDuration + _DecPulseDuration != 0) {
                  isPulseGuiding = true;
               }
               _Logger.LogMessage("IsPulseGuiding", "Get - " + isPulseGuiding.ToString());
               return isPulseGuiding;
            }
            else {
               _Logger.LogMessage("IsPulseGuiding Get", "Not implemented");
               throw new ASCOM.PropertyNotImplementedException("IsPulseGuiding", false);
            }
         }
      }


      /// <summary>
      /// Move the telescope in one axis at the given rate.
      /// </summary>
      /// <param name="Axis">The physical axis about which movement is desired</param>
      /// <param name="Rate">The rate of motion (deg/sec) about the specified axis</param>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented.</exception>
      /// <exception cref="InvalidValueException">If an invalid axis or rate is given.</exception>
      /// <remarks>
      /// This method supports control of the mount about its mechanical axes.
      /// The telescope will start moving at the specified rate about the specified axis and continue indefinitely.
      /// This method can be called for each axis separately, and have them all operate concurrently at separate rates of motion. 
      /// Set the rate for an axis to zero to restore the motion about that axis to the rate set by the <see cref="Tracking"/> property.
      /// Tracking motion (if enabled, see note below) is suspended during this mode of operation. 
      /// <para>
      /// Raises an error if <see cref="AtPark" /> is true. 
      /// This must be implemented for the if the <see cref="CanMoveAxis" /> property returns True for the given axis.</para>
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// <para>
      /// <b>NOTES:</b>
      /// <list type="bullet">
      /// <item><description>The movement rate must be within the value(s) obtained from a <see cref="IRate" /> object in the 
      /// the <see cref="AxisRates" /> collection. This is a signed value with negative rates moving in the oposite direction to positive rates.</description></item>
      /// <item><description>The values specified in <see cref="AxisRates" /> are absolute, unsigned values and apply to both directions, determined by the sign used in this command.</description></item>
      /// <item><description>The value of <see cref="Slewing" /> must be True if the telescope is moving about any of its axes as a result of this method being called. 
      /// This can be used to simulate a handbox by initiating motion with the MouseDown event and stopping the motion with the MouseUp event.</description></item>
      /// <item><description>When the motion is stopped by setting the rate to zero the scope will be set to the previous <see cref="TrackingRate" /> or to no movement, depending on the state of the <see cref="Tracking" /> property.</description></item>
      /// <item><description>It may be possible to implement satellite tracking by using the <see cref="MoveAxis" /> method to move the scope in the required manner to track a satellite.</description></item>
      /// </list>
      /// </para>
      /// </remarks>
      public void MoveAxis(TelescopeAxes Axis, double Rate)
      {
         System.Diagnostics.Debug.Write(String.Format("MoveAxis({0}, {1})", Axis, Rate));
         if (AtPark) {
            throw new ASCOM.ParkedException("Method MoveAxis");
         }
         _Logger.LogMessage("MoveAxis", string.Format("({0}, {1})", Axis, Rate));
         if (Axis == TelescopeAxes.axisPrimary) {
            if (Rate == 0.0) {
               if (_IsMoveAxisSlewing) {
                  _Mount.MCAxisStop(AxisId.Axis1_RA);
                  _IsMoveAxisSlewing = false;
               }
            }
            else {
               if (!Slewing) {
                  _Mount.MCAxisSlew(AxisId.Axis1_RA, Rate);
                  _IsMoveAxisSlewing = true;
               }
            }
         }
         else if (Axis == TelescopeAxes.axisSecondary) {
            if (Rate == 0.0) {
               if (_IsMoveAxisSlewing) {
                  _Mount.MCAxisStop(AxisId.Axis2_DEC);
                  _IsMoveAxisSlewing = false;
               }
            }
            else {
               if (!Slewing) {
                  _Mount.MCAxisSlew(AxisId.Axis2_DEC, Rate);
                  _IsMoveAxisSlewing = true;
               }
            }
         }
         else {
            throw new ASCOM.InvalidValueException("Driver does not third axis.");
         }
      }

      public void Park()
      {
         _Logger.LogMessage("Park", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("Park");
      }

      public void PulseGuide(GuideDirections Direction, int Duration)
      {
         _Logger.LogMessage("PulseGuide", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("PulseGuide");
      }

      public double RightAscension
      {
         get
         {
            double rightAscension = 0.0;
            _Logger.LogMessage("RightAscension", "Get - " + utilities.HoursToHMS(rightAscension));
            return rightAscension;
         }
      }

      private double _RaRateAdjust = 0.0;
      private double _RightAscensionRate;

      /// <summary>
      /// 
      /// </summary>
      public double RightAscensionRate
      {
         get
         {
            double value;
            if (Hemisphere == HemisphereOption.Northern) {
               value = _RightAscensionRate - global::Lunatic.Core.Constants.SIDEREAL_RATE_ARCSECS;
            }
            else {
               value = _RightAscensionRate + global::Lunatic.Core.Constants.SIDEREAL_RATE_ARCSECS;
            }
            _Logger.LogMessage("RightAscensionRate", "Get - " + value.ToString());
            return value;
         }
         set
         {
            _Logger.LogMessage("RightAscensionRate", "Set - " + utilities.DegreesToDMS(value, ":", ":"));
            _RaRateAdjust = value;
            // don't action this if we're parked!
            if (Settings.ParkStatus == ParkStatus.Parked) {
               if (value == 0 && _DecRateAdjust == 0) {
                  // TODO: Call EQStartSidereal2
               }
               else {
                  if (Hemisphere == HemisphereOption.Northern) {
                     value = global::Lunatic.Core.Constants.SIDEREAL_RATE_ARCSECS + value;      // Treat newval as an offset
                  }
                  else {
                     value = value - global::Lunatic.Core.Constants.SIDEREAL_RATE_ARCSECS;      // Treat newval as an offset
                  }
                  // if we're already tracking then apply the new rate.
                  if (TrackingState != TrackingStatus.Off) {
                     if ((_RightAscensionRate * value) <= 0) {
                        // TODO: Call StartRA_by_Rate(value)
                     }
                     else {
                        // TODO: Call ChangeRA_by_Rate(value)
                     }
                     TrackingState = TrackingStatus.Custom;
                     // ' Custom tracking!
                     // TODO: HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(189)
                  }
               }
               _RightAscensionRate = value;
            }
            else {
               _RaRateAdjust = 0;
               throw new ASCOM.InvalidOperationException("Invalid operation while the telescope is parked");
            }
         }
      }

      public void SetPark()
      {
         _Logger.LogMessage("SetPark", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SetPark");
      }

      public DeviceInterface.PierSide SideOfPier
      {
         get
         {
            _Logger.LogMessage("SideOfPier Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SideOfPier", false);
         }
         set
         {
            _Logger.LogMessage("SideOfPier Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SideOfPier", true);
         }
      }

      public double SiderealTime
      {
         get
         {
            // get greenwich sidereal time: https://en.wikipedia.org/wiki/Sidereal_time
            //double siderealTime = (18.697374558 + 24.065709824419081 * (utilities.DateUTCToJulian(DateTime.UtcNow) - 2451545.0));

            // alternative using NOVAS 3.1
            //double siderealTime = 0.0;
            //using (var novas = new ASCOM.Astrometry.NOVAS.NOVAS31()) {
            //   var jd = utilities.DateUTCToJulian(DateTime.UtcNow);
            //   novas.SiderealTime(jd, 0, novas.DeltaT(jd),
            //       ASCOM.Astrometry.GstType.GreenwichApparentSiderealTime,
            //       ASCOM.Astrometry.Method.EquinoxBased,
            //       ASCOM.Astrometry.Accuracy.Reduced, ref siderealTime);
            //}
            //// allow for the longitude
            //siderealTime += SiteLongitude / 360.0 * 24.0;
            //// reduce to the range 0 to 24 hours
            //siderealTime = siderealTime % 24.0;
            double siderealTime = AstroConvert.LocalApparentSiderealTime(SiteLongitude);
            _Logger.LogMessage("SiderealTime", "Get - " + siderealTime.ToString());
            return siderealTime;
         }
      }

      private double _SiteElevation;
      public double SiteElevation
      {
         get
         {
            _Logger.LogMessage("SiteElevation", "Get - " + _SiteElevation.ToString());
            return _SiteElevation;
         }
         set
         {
            if (Settings.AscomCompliance.AllowSiteWrites) {
               _Logger.LogMessage("SiteElevation", "Set - " + value.ToString());
               if (value == _SiteElevation) {
                  return;
               }
               if (value < -300 || value > 10000) {
                  throw new ASCOM.InvalidValueException("SiteElevation Get", value.ToString(), "-300 to 10000");
               }
               else {
                  _SiteElevation = value;
                  RaisePropertyChanged();
               }
            }
            else {
               _Logger.LogMessage("SiteElevation Set", "Not implemented");
               throw new ASCOM.PropertyNotImplementedException("SiteElevation", true);
            }
         }
      }

      private double _SiteLatitude;
      public double SiteLatitude
      {
         get
         {
            _Logger.LogMessage("SiteLatitude", "Get - " + _SiteLatitude.ToString());
            return _SiteLatitude;
         }
         set
         {
            if (Settings.AscomCompliance.AllowSiteWrites) {
               _Logger.LogMessage("SiteLatitude", "Set - " + value.ToString());
               if (value == _SiteLatitude) {
                  return;
               }
               if (value < -90 || value > 90) {
                  throw new ASCOM.InvalidValueException("SiteLatitude Get", value.ToString(), "-90  to 90");
               }
               else {
                  _SiteLatitude = value;
                  Hemisphere = (_SiteLatitude >= 0 ? HemisphereOption.Northern : HemisphereOption.Southern);
                  RaisePropertyChanged();
               }
            }
            else {
               _Logger.LogMessage("SiteLatitude Set", "Not implemented");
               throw new ASCOM.PropertyNotImplementedException("SiteLatitude", true);
            }
         }
      }

      private double _SiteLongitude;
      public double SiteLongitude
      {
         get
         {
            _Logger.LogMessage("SiteLongitude", "Get - " + _SiteLongitude.ToString());
            return _SiteLongitude;
         }
         set
         {
            if (Settings.AscomCompliance.AllowSiteWrites) {
               _Logger.LogMessage("SiteLongitude", "Set - " + value.ToString());
               if (value == _SiteLongitude) {
                  return;
               }
               if (value < -180 || value > 180) {
                  throw new ASCOM.InvalidValueException("SiteLongitude Get", value.ToString(), "-180 to 180");
               }
               else {
                  _SiteLongitude = value;
                  RaisePropertyChanged();
               }
            }
            else {
               _Logger.LogMessage("SiteLongitude Set", "Not implemented");
               throw new ASCOM.PropertyNotImplementedException("SiteLongitude", true);
            }
         }
      }

      private short _SlewSettleTime = 0;
      public short SlewSettleTime
      {
         get
         {
            _Logger.LogMessage("SlewSettleTime", "Get - " + _SlewSettleTime.ToString());
            return _SlewSettleTime;
         }
         set
         {
            _Logger.LogMessage("SlewSettleTime", "Set - " + value.ToString());
            if (_SlewSettleTime == value) {
               return;
            }
            if (value < 0 || value > 100) {
               throw new ASCOM.InvalidValueException("SiteLongitude Get", value.ToString(), "-180 to 180");
            }
            _SlewSettleTime = value;
         }
      }

      public void SlewToAltAz(double Azimuth, double Altitude)
      {
         _Logger.LogMessage("SlewToAltAz", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToAltAz");
      }

      public void SlewToAltAzAsync(double Azimuth, double Altitude)
      {
         _Logger.LogMessage("SlewToAltAzAsync", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToAltAzAsync");
      }

      public void SlewToCoordinates(double RightAscension, double Declination)
      {
         _Logger.LogMessage("SlewToCoordinates", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToCoordinates");
      }

      public void SlewToCoordinatesAsync(double RightAscension, double Declination)
      {
         _Logger.LogMessage("SlewToCoordinatesAsync", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToCoordinatesAsync");
      }

      public void SlewToTarget()
      {
         _Logger.LogMessage("SlewToTarget", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToTarget");
      }

      public void SlewToTargetAsync()
      {
         _Logger.LogMessage("SlewToTargetAsync", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToTargetAsync");
      }

      bool _IsSlewing;
      bool _IsMoveAxisSlewing;
      public bool Slewing
      {
         get
         {
            bool isSlewing = false;
            switch (Settings.ParkStatus) {
               case ParkStatus.Unparked:
                  isSlewing = _IsSlewing;
                  if (!isSlewing) {
                     isSlewing = _IsMoveAxisSlewing;
                  }
                  break;
               case ParkStatus.Parked:
               case ParkStatus.Unparking:
                  isSlewing = false;
                  break;
               case ParkStatus.Parking:
                  isSlewing = true;
                  break;
            }
            _Logger.LogMessage("Slewing", "Get - " + isSlewing.ToString());
            return isSlewing;
         }
      }

      public void SyncToAltAz(double Azimuth, double Altitude)
      {
         _Logger.LogMessage("SyncToAltAz", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SyncToAltAz");
      }

      /// <summary>
      /// Matches the scope's equatorial coordinates to the given equatorial coordinates.
      /// </summary>
      /// <param name="rightAscension">The corrected right ascension (hours). Copied to the <see cref="TargetRightAscension" /> property.</param>
      /// <param name="declination">The corrected declination (degrees, positive North). Copied to the <see cref="TargetDeclination" /> property.</param>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanSync" /> is False</exception>
      /// <exception cref="InvalidValueException">If an invalid right ascension or declination is given.</exception>
      /// <remarks>
      /// This must be implemented if the <see cref="CanSync" /> property is True. Raises an error if matching fails. 
      /// Raises an error if <see cref="AtPark" /> AtPark is True, or if <see cref="Tracking" /> is False. 
      /// The way that Sync is implemented is mount dependent and it should only be relied on to improve pointing for positions close to
      /// the position at which the sync is done.
      /// </remarks>
      public void SyncToCoordinates(double rightAscension, double declination)
      {
         _Logger.LogMessage("COMMAND - ", string.Format("SyncToCoordinate({0},{1})", rightAscension, declination));
         if (Settings.ParkStatus == ParkStatus.Unparked) {
            if (TrackingState == TrackingStatus.Off) {
               throw new ASCOM.InvalidOperationException("RaDec sync is not permitted if moumt is not Tracking.");
            }
            else {
               if (ValidateRADEC(rightAscension, declination)) {
                  // TODO: HC.Add_Message("SynCoor: " & oLangDll.GetLangString(105) & "[ " & FmtSexa(RightAscension, False) & "] " & oLangDll.GetLangString(106) & "[ " & FmtSexa(Declination, True) & " ]")
                  if (SyncToRADEC(rightAscension, declination, SiteLongitude, Hemisphere)) {
                     // EQ_Beep(4)
                  }
               }
               else {
                  throw new ASCOM.InvalidValueException("Invalid value passed to SyncToCoordinates()");
               }
            }
         }
         else {
            throw new ASCOM.InvalidOperationException("SyncToCoordinates() is not valid whilst the scope is parked.");
         }
      }


      public void SyncToTarget()
      {
         _Logger.LogMessage("SyncToTarget", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SyncToTarget");
      }

      private double? _TargetDeclination;
      public double TargetDeclination
      {
         get
         {
            _Logger.LogMessage("TargetDeclination", "Get - " + _TargetDeclination.ToString());
            if (_TargetDeclination.HasValue) {
               return _TargetDeclination.Value;
            }
            else {
               throw new ASCOM.InvalidOperationException("TargetDeclination value has not be set");
            }
         }
         set
         {
            _Logger.LogMessage("TargetDeclination", "Set - " + value.ToString());
            if (_TargetDeclination.HasValue && _TargetDeclination.Value == value) {
               return;
            }
            if (value < -90 || value > 90) {
               throw new ASCOM.InvalidValueException("TargetDeclination", value.ToString(), "-90 to 90");
            }
            _TargetDeclination = value;
            RaisePropertyChanged();
         }
      }

      private double? _TargetRightAscension;
      public double TargetRightAscension
      {
         get
         {
            _Logger.LogMessage("TargetRightAscension", "Get - " + _TargetRightAscension.ToString());
            if (_TargetRightAscension.HasValue) {
               return _TargetRightAscension.Value;
            }
            else {
               throw new ASCOM.InvalidOperationException("TargetRightAscension value has not be set");
            }
         }
         set
         {
            _Logger.LogMessage("TargetRightAscension", "Set - " + value.ToString());
            if (_TargetRightAscension.HasValue && _TargetRightAscension.Value == value) {
               return;
            }
            if (value < 0 || value > 24) {
               throw new ASCOM.InvalidValueException("TargetRightAscension", value.ToString(), "0 to 24");
            }
            _TargetRightAscension = value;
            RaisePropertyChanged();
         }
      }

      public bool Tracking
      {
         get
         {
            bool tracking = true;
            _Logger.LogMessage("Tracking", "Get - " + tracking.ToString());
            return tracking;
         }
         set
         {
            _Logger.LogMessage("Tracking Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Tracking", true);
         }
      }

      private DriveRates _TrackingRate;
      public DriveRates TrackingRate
      {
         get
         {
            _Logger.LogMessage("TrackingRate", "Get - " + _TrackingRate.ToString());
            return _TrackingRate;
         }
         set
         {
            _Logger.LogMessage("TrackingRate", "Set - " + value.ToString());
            if (value == _TrackingRate) {
               return;
            }
            _TrackingRate = value;
            RaisePropertyChanged();
         }
      }

      private ITrackingRates _TrackingRates;
      public ITrackingRates TrackingRates
      {
         get
         {
            _Logger.LogMessage("TrackingRates", "Get - ");
            foreach (DriveRates driveRate in _TrackingRates) {
               _Logger.LogMessage("TrackingRates", "Get - " + driveRate.ToString());
            }
            return _TrackingRates;
         }
      }

      public DateTime UTCDate
      {
         get
         {
            DateTime utcDate = DateTime.UtcNow;
            _Logger.LogMessage("TrackingRates", "Get - " + String.Format("MM/dd/yy HH:mm:ss", utcDate));
            return utcDate;
         }
         set
         {
            _Logger.LogMessage("UTCDate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("UTCDate", true);
         }
      }

      public void Unpark()
      {
         _Logger.LogMessage("Unpark", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("Unpark");
      }

      #endregion

      #region Validation methods ...
      private bool ValidateRADEC(double ra, double dec)
      {
         bool isValid = false;
         if (ra >= 0 && ra <= 24) {
            if (dec >= -90 && dec <= 90) {
               isValid = true;
            }
         }
         return isValid;
      }

      #endregion


      #region Helper methods ...
      public bool SyncToRADEC(double rightAscension, double declination, double longitude, HemisphereOption hemisphere)
      {
         double targetRAEncoder;
         double targetDECEncoder;
         double currentRAEncoder;
         double currentDECEncoder;
         double saveRASync;
         double saveDECSync;


         double tRA;
         double tHA;
         int tPier;
         CarteseanCoordinate tmpCoord;

         bool result = true;

         // If HC.ListSyncMode.ListIndex = 1 Then
         if (SyncMode == SyncModeOption.AppendOnSync) {
            //' Append via sync mode!
            result = Alignment.EQ_NPointAppend(rightAscension, declination, longitude, hemisphere);
            //Exit Function
         }
         else {
            //' its an ascom sync - shift whole model
            saveDECSync = Settings.DECSync01;
            saveRASync = Settings.RASync01;
            Settings.RASync01 = 0;
            Settings.DECSync01 = 0;

            //TODO: HC.EncoderTimer.Enabled = False
            double raAxisPositon = SharedResources.Controller.MCGetAxisPosition(AxisId.Axis1_RA);
            double decAxisPosition = SharedResources.Controller.MCGetAxisPosition(AxisId.Axis2_DEC);
            if (!Settings.ThreeStarEnable) {
               currentRAEncoder = raAxisPositon + Settings.RA1Star;
               currentDECEncoder = decAxisPosition + Settings.DEC1Star;
            }
            else {
               switch (SyncAlignmentMode) {

                  case SyncAlignmentModeOptions.NearestStar:
                     //   Case 2
                     // ' nearest
                     tmpCoord = AstroConvert.DeltaSyncMatrixMap(raAxisPositon, decAxisPosition);
                     currentRAEncoder = tmpCoord.X;
                     currentDECEncoder = tmpCoord.Y;
                     break;

                  default:
                     // 'n-star+nearest
                     tmpCoord = AstroConvert.DeltaMatrixReverseMap(raAxisPositon, decAxisPosition);
                     currentRAEncoder = tmpCoord.X;
                     currentDECEncoder = tmpCoord.Y;

                     if (!tmpCoord.Flag) {
                        tmpCoord = AstroConvert.DeltaSyncMatrixMap(raAxisPositon, decAxisPosition);
                        currentRAEncoder = tmpCoord.X;
                        currentDECEncoder = tmpCoord.Y;
                     }
                     break;
               }
            }


            //TODO: HC.EncoderTimer.Enabled = True
            tHA = AstroConvert.RangeHA(rightAscension - AstroConvert.LocalApparentSiderealTime(longitude));


            if (tHA < 0) {
               if (hemisphere == HemisphereOption.Northern) {
                  tPier = 1;
               }
               else {
                  tPier = 0;
               }
               tRA = AstroConvert.Range24(rightAscension - 12);
            }
            else {
               if (hemisphere == HemisphereOption.Northern) {
                  tPier = 0;
               }
               else {
                  tPier = 1;
               }
               tRA = rightAscension;
            }

            //'Compute for Sync RA/DEC Encoder Values


            targetRAEncoder = AstroConvert.RAAxisPositionFromRA(tRA, 0, longitude, global::Lunatic.Core.Constants.RAEncoder_Zero_pos, hemisphere);
            targetDECEncoder = AstroConvert.DECAxisPositionFromDEC(declination, tPier, global::Lunatic.Core.Constants.DECEncoder_Zero_pos, hemisphere);


            if (Settings.DisableSyncLimit) {
               Settings.RASync01 = targetRAEncoder - currentRAEncoder;
               Settings.DECSync01 = targetDECEncoder - currentDECEncoder;
            }
            else {
               if ((Math.Abs(targetRAEncoder - currentRAEncoder) > Settings.MaxSync) || (Math.Abs(targetDECEncoder - currentDECEncoder) > Settings.MaxSync)) {
                  //TODO: Call HC.Add_Message(oLangDll.GetLangString(6004))
                  Settings.DECSync01 = saveDECSync;
                  Settings.RASync01 = saveRASync;
                  //TODO: HC.Add_Message ("RA=" & FmtSexa(gRA, False) & " " & CStr(currentRAEncoder))
                  //TODO: HC.Add_Message ("SyncRA=" & FmtSexa(RightAscension, False) & " " & CStr(targetRAEncoder))
                  //TODO: HC.Add_Message ("DEC=" & FmtSexa(gDec, True) & " " & CStr(currentDECEncoder))
                  //TODO: HC.Add_Message ("Sync   DEC=" & FmtSexa(Declination, True) & " " & CStr(targetDECEncoder))
                  result = false;
               }
               else {
                  Settings.RASync01 = targetRAEncoder - currentRAEncoder;
                  Settings.DECSync01 = targetDECEncoder - currentDECEncoder;
               }
            }


            //WriteSyncMap(); ==> Persist the values of RASync01 && RaSync02
            SettingsProvider.Current.SaveSettings();
            Settings.EmulOneShot = true;    // Re Sync Display
            //TODO: HC.DxSalbl.Caption = Format$(str(gRASync01), "000000000")
            //TODO: HC.DxSblbl.Caption = Format$(str(gDECSync01), "000000000")
         }
         return result;
      }


      #endregion
   }
}
