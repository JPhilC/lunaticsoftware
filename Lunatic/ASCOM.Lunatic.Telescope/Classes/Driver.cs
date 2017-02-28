//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Telescope driver for Synta mounts by Lunatic Software
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM Telescope interface version: V3
// Author:		(JPC) Phil Crompton <phil@lunaticsoftware.org>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	---------------------------------------------------------------------------------------
// dd-mmm-yyyy	XXX	6.2.0	Initial edit, created from ASCOM driver template merged with opensource Skywatcher code
// ---------------------------------------------------------------------------------------------------------------
//


// NOTE this is a partial class. The other module is Driver_SyntaExtensions which contains the code form the Skywatcher_Open sources.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using Lunatic.Core;
using System.Runtime.CompilerServices;
using System.Threading;
using Lunatic.SyntaController;

namespace ASCOM.Lunatic.Telescope
{
   //
   // Your driver's DeviceID is ASCOM.Lunatic.Telescope
   //
   // The Guid attribute sets the CLSID for ASCOM.Lunatic.Telescope
   // The ClassInterface/None addribute prevents an empty interface called
   // _Winforms from being created and used as the [default] interface
   //
   // TODO Replace the not implemented exceptions with code to implement the function or
   // throw the appropriate ASCOM exception.
   //

   /// <summary>
   /// ASCOM Telescope Driver for Winforms.
   /// </summary>
   [Guid("C21225C0-EF9A-43C7-A7FA-F172501F0357")]
   [ProgId("ASCOM.Lunatic.Telescope")]
   [ServedClassName("Lunatic ASCOM Driver for Synta Telescopes")]
   [ClassInterface(ClassInterfaceType.None)]
   public partial class Telescope : SyntaMountBase, ITelescopeV3
   {
      /// <summary>
      /// ASCOM DeviceID (COM ProgID) for this driver.
      /// The DeviceID is used by ASCOM applications to load the driver at runtime.
      /// </summary>
      internal static string DRIVER_ID = "ASCOM.Lunatic.Telescope";
      /// <summary>
      /// Driver description that displays in the ASCOM Chooser.
      /// </summary>
      private static string INSTRUMENT_DESCRIPTION = "Lunatic ASCOM Driver for Synta Telescopes";  // Was Driver description before moving to hub
      private static string INSTRUMENT_NAME = "Lunatic HEQ5/6";   // Was DRIVER_ID before moving to hub
      internal static string comPortProfileName = "COM Port"; // Constants used for Profile persistence
      internal static string comPortDefault = "COM1";
      internal static string traceStateProfileName = "Trace Level";
      internal static string traceStateDefault = "true";


      /// <summary>
      /// Private variable to hold an ASCOM Utilities object
      /// </summary>
      private Util utilities;

      /// <summary>
      /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
      /// </summary>
      private AstroUtils astroUtilities;

      /// <summary>
      /// Private variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
      /// </summary>
      protected TraceLogger _Logger;

      private MountController _Mount;


      #region Internal properties ...
      private const int RAAxisIndex = 0;
      private const int DECAxisIndex = 1;
      /// <summary>
      /// The axis positions in Radians
      /// </summary>
      private double[] AxisPositionRadians = new double[2] { 0, 0 }; 

      #endregion


      /// <summary>
      /// Initializes a new instance of the <see cref="Winforms"/> class.
      /// Must be public for COM registration.
      /// </summary>
      public Telescope() : base()
      {
         // Check the current settings are loaded
         if (Settings == null) {
            throw new ASCOM.DriverException("Unable to load configuration settings");
         }

         _Logger = new TraceLogger("", "Winforms");
         _Logger.Enabled = Settings.IsTracing;       /// NOTE: This line triggers a load of the current settings
         _Logger.LogMessage("Telescope", "Starting initialisation");

         DRIVER_ID = Marshal.GenerateProgIdForType(this.GetType());

         utilities = new Util(); //Initialise util object
         astroUtilities = new AstroUtils(); // Initialise astro utilities object
                                            //TODO: Implement your additional construction here

         _AlignmentMode = AlignmentModes.algGermanPolar;
         _TrackingRates = new TrackingRates();

         _Mount = SharedResources.Controller;

         _Logger.LogMessage("Telescope", "Completed initialisation");
      }

     

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
            SettingsManager.SaveSettings(); // Persist device configuration values Lunatic Settings
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
                  // Need to send current axis position
                  _Mount.MCSetAxisPosition(AxisId.Axis1_RA, AxisPositionRadians[RAAxisIndex]);
                  _Mount.MCSetAxisPosition(AxisId.Axis2_DEC, AxisPositionRadians[DECAxisIndex]);

                  IsConnected = true;
               }
               else if (connectionResult == 1) {
                  // Was already connected to GET the current axis positions
                  AxisPositionRadians[RAAxisIndex] = _Mount.MCGetAxisPosition(AxisId.Axis1_RA);
                  AxisPositionRadians[DECAxisIndex] = _Mount.MCGetAxisPosition(AxisId.Axis2_DEC);
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

      #endregion

      #region ITelescope Implementation
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

      private ParkStatus _ParkStatus;
      public bool AtPark
      {
         get
         {
            //  ASCOM has no means of detecting a parking state
            // However some folks will be closing their roofs based upon the stare of AtPark
            // So we must respond with a false!
            bool atPark = (_ParkStatus == ParkStatus.Parked);
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
            _Logger.LogMessage("DeclinationRate", "Set" + utilities.DegreesToDMS(_DeclinationRate, ":", ":"));
            throw new ASCOM.PropertyNotImplementedException("DeclinationRate", true);
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

      public EquatorialCoordinateType EquatorialSystem
      {
         get
         {
            EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equLocalTopocentric;
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

      public double GuideRateDeclination
      {
         get
         {
            _Logger.LogMessage("GuideRateDeclination Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
         }
         set
         {
            _Logger.LogMessage("GuideRateDeclination Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
         }
      }

      public double GuideRateRightAscension
      {
         get
         {
            _Logger.LogMessage("GuideRateRightAscension Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", false);
         }
         set
         {
            _Logger.LogMessage("GuideRateRightAscension Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", true);
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

      public void MoveAxis(TelescopeAxes Axis, double Rate)
      {
         _Logger.LogMessage("MoveAxis", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("MoveAxis");
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

      public double RightAscensionRate
      {
         get
         {
            double rightAscensionRate = 0.0;
            _Logger.LogMessage("RightAscensionRate", "Get - " + rightAscensionRate.ToString());
            return rightAscensionRate;
         }
         set
         {
            _Logger.LogMessage("RightAscensionRate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("RightAscensionRate", true);
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
            double siderealTime = 0.0;
            using (var novas = new ASCOM.Astrometry.NOVAS.NOVAS31()) {
               var jd = utilities.DateUTCToJulian(DateTime.UtcNow);
               novas.SiderealTime(jd, 0, novas.DeltaT(jd),
                   ASCOM.Astrometry.GstType.GreenwichApparentSiderealTime,
                   ASCOM.Astrometry.Method.EquinoxBased,
                   ASCOM.Astrometry.Accuracy.Reduced, ref siderealTime);
            }
            // allow for the longitude
            siderealTime += SiteLongitude / 360.0 * 24.0;
            // reduce to the range 0 to 24 hours
            siderealTime = siderealTime % 24.0;
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
            switch (_ParkStatus) {
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

      public void SyncToCoordinates(double RightAscension, double Declination)
      {
         _Logger.LogMessage("SyncToCoordinates", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SyncToCoordinates");
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

      #region Private properties and methods
      // here are some useful properties and methods that can be used as required
      // to help with driver development

      #region ASCOM Registration

      //// Register or unregister driver for ASCOM. This is harmless if already
      //// registered or unregistered. 
      ////
      ///// <summary>
      ///// Register or unregister the driver with the ASCOM Platform.
      ///// This is harmless if the driver is already registered/unregistered.
      ///// </summary>
      ///// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
      //private static void RegUnregASCOM(bool bRegister)
      //{
      //   using (var P = new ASCOM.Utilities.Profile()) {
      //      P.DeviceType = "Telescope";
      //      if (bRegister) {
      //         P.Register(DRIVER_ID, DRIVER_DESCRIPTION);
      //      }
      //      else {
      //         P.Unregister(DRIVER_ID);
      //      }
      //   }
      //}

      ///// <summary>
      ///// This function registers the driver with the ASCOM Chooser and
      ///// is called automatically whenever this class is registered for COM Interop.
      ///// </summary>
      ///// <param name="t">Type of the class being registered, not used.</param>
      ///// <remarks>
      ///// This method typically runs in two distinct situations:
      ///// <list type="numbered">
      ///// <item>
      ///// In Visual Studio, when the project is successfully built.
      ///// For this to work correctly, the option <c>Register for COM Interop</c>
      ///// must be enabled in the project settings.
      ///// </item>
      ///// <item>During setup, when the installer registers the assembly for COM Interop.</item>
      ///// </list>
      ///// This technique should mean that it is never necessary to manually register a driver with ASCOM.
      ///// </remarks>
      //[ComRegisterFunction]
      //public static void RegisterASCOM(Type t)
      //{
      //   RegUnregASCOM(true);
      //}

      ///// <summary>
      ///// This function unregisters the driver from the ASCOM Chooser and
      ///// is called automatically whenever this class is unregistered from COM Interop.
      ///// </summary>
      ///// <param name="t">Type of the class being registered, not used.</param>
      ///// <remarks>
      ///// This method typically runs in two distinct situations:
      ///// <list type="numbered">
      ///// <item>
      ///// In Visual Studio, when the project is cleaned or prior to rebuilding.
      ///// For this to work correctly, the option <c>Register for COM Interop</c>
      ///// must be enabled in the project settings.
      ///// </item>
      ///// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
      ///// </list>
      ///// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
      ///// </remarks>
      //[ComUnregisterFunction]
      //public static void UnregisterASCOM(Type t)
      //{
      //   RegUnregASCOM(false);
      //}

      #endregion


      /// <summary>
      /// Use this function to throw an exception if we aren't connected to the hardware
      /// </summary>
      /// <param name="message"></param>
      private void CheckConnected(string message)
      {
         if (!IsConnected) {
            throw new ASCOM.NotConnectedException(message);
         }
      }

      /// <summary>
      /// Read the device configuration from the ASCOM Profile store
      /// </summary>
      internal void ReadProfile()
      {
         lock (_Lock) {
            using (Profile driverProfile = new Profile()) {
               driverProfile.DeviceType = this.GetType().Name;
            }
         }
      }

      /// <summary>
      /// Write the device configuration to the  ASCOM  Profile store
      /// </summary>
      internal void WriteProfile()
      {
         lock (_Lock) {
            using (Profile driverProfile = new Profile()) {
               driverProfile.DeviceType = this.GetType().Name;
               //driverProfile.WriteValue(DRIVER_ID, traceStateProfileName, TRACE_STATE.ToString());
               driverProfile.WriteValue(DRIVER_ID, comPortProfileName, Settings.COMPort);
            }
         }
      }

      #endregion

      #region Setup dialogue threading classes ...
      public class SetupThread
      {
         private SetupViewModel _SetupViewModel;
         private TraceLogger _Logger;
         private SetupCallback _CallBackDelegate;

         public SetupThread(SetupViewModel vm, TraceLogger logger, SetupCallback callBackDelegate)
         {
            _SetupViewModel = vm;
            _Logger = logger;
            _CallBackDelegate = callBackDelegate;
         }

         public void ThreadProc()
         {
            SetupWindow setupWindow = new SetupWindow(_SetupViewModel);
            bool? result = setupWindow.ShowDialog();
            setupWindow.Dispatcher.InvokeShutdown();
            if (result.HasValue && result.Value) {
               _Logger.Enabled = _SetupViewModel.Settings.IsTracing;
            }
            _CallBackDelegate(result);
         }
      }

      public delegate void SetupCallback(bool? saveChanges);
      #endregion

   }
}
