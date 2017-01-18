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

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using Microsoft.Practices.ServiceLocation;
using Lunatic.Core.Classes;

namespace ASCOM.Lunatic.TelescopeDriver
{
   //
   // Your driver's DeviceID is ASCOM.Lunatic.TelescopeDriver.SyntaTelescope
   //
   // The Guid attribute sets the CLSID for ASCOM.Lunatic.TelescopeDriver.SyntaTelescope
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
   [ClassInterface(ClassInterfaceType.None)]
   public partial class SyntaTelescope :SyntaMountBase, ITelescopeV3
   {
      /// <summary>
      /// ASCOM DeviceID (COM ProgID) for this driver.
      /// The DeviceID is used by ASCOM applications to load the driver at runtime.
      /// </summary>
      internal static string DRIVER_ID = "ASCOM.Lunatic.TelescopeDriver.SyntaTelescope";
      /// <summary>
      /// Driver description that displays in the ASCOM Chooser.
      /// </summary>
      private static string DRIVER_DESCRIPTION = "Lunatic ASCOM Synta Telescope Driver";

      internal static string comPortProfileName = "COM Port"; // Constants used for Profile persistence
      internal static string comPortDefault = "COM1";
      internal static string traceStateProfileName = "Trace Level";
      internal static string traceStateDefault = "true";

      internal static string COM_PORT; // Variables to hold the currrent device configuration
      internal static bool TRACE_STATE;

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
      private TraceLogger tl;

      /// <summary>
      /// Initializes a new instance of the <see cref="Winforms"/> class.
      /// Must be public for COM registration.
      /// </summary>
      public SyntaTelescope():base()
      {
         ReadProfile(); // Read device configuration from the ASCOM Profile store

         tl = new TraceLogger("", "Winforms");
         tl.Enabled = TRACE_STATE;
         tl.LogMessage("Telescope", "Starting initialisation");

         utilities = new Util(); //Initialise util object
         astroUtilities = new AstroUtils(); // Initialise astro utilities object
                                            //TODO: Implement your additional construction here

         tl.LogMessage("Telescope", "Completed initialisation");
      }


      //
      // PUBLIC COM INTERFACE ITelescopeV3 IMPLEMENTATION
      //

      #region Common properties and methods.

      /// <summary>
      /// Displays the Setup Dialog form.
      /// If the user clicks the OK button to dismiss the form, then
      /// the new settings are saved, otherwise the old values are reloaded.
      /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
      /// </summary>
      public void SetupDialog()
      {
         // consider only showing the setup dialog if not connected
         // or call a different dialog if connected
         //if (IsConnected)
         //   System.Windows.Forms.MessageBox.Show("Already connected, just press OK");
         SetupViewModel setupVm = ViewModelLocator.Current.Setup;
         // Refresh the com ports
         setupVm.RefreshCOMPorts();

         SetupWindow setupWindow = new SetupWindow(setupVm);
         var result = setupWindow.ShowDialog();
         if (result.HasValue && result.Value) {
            WriteProfile(); // Persist device configuration values to the ASCOM Profile store
         }

      }

      public ArrayList SupportedActions
      {
         get
         {
            tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
            return new ArrayList();
         }
      }

      public string Action(string actionName, string actionParameters)
      {
         throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
      }

      public void CommandBlind(string command, bool raw)
      {
         CheckConnected("CommandBlind");
         // Call CommandString and return as soon as it finishes
         this.CommandString(command, raw);
         // or
         throw new ASCOM.MethodNotImplementedException("CommandBlind");
         // DO NOT have both these sections!  One or the other
      }

      public bool CommandBool(string command, bool raw)
      {
         CheckConnected("CommandBool");
         string ret = CommandString(command, raw);
         // TODO decode the return string and return true or false
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

      public void Dispose()
      {
         // Clean up the tracelogger and util objects
         tl.Enabled = false;
         tl.Dispose();
         tl = null;
         utilities.Dispose();
         utilities = null;
         astroUtilities.Dispose();
         astroUtilities = null;
      }

      public bool Connected
      {
         get
         {
            tl.LogMessage("Connected Get", IsConnected.ToString());
            return IsConnected;
         }
         set
         {
            tl.LogMessage("Connected Set", value.ToString());
            if (value == IsConnected)
               return;

            if (value) {
               tl.LogMessage("Connected Set", "Connecting to port " + COM_PORT);
               Connect_COM(SyntaTelescope.COM_PORT);      //TODO: Sort out the comPort
            }
            else {
               Disconnect_COM();
               tl.LogMessage("Connected Set", "Disconnecting from port " + COM_PORT);
            }
            RaisePropertyChanged();
         }
      }

      public string Description
      {
         // TODO customise this device description
         get
         {
            tl.LogMessage("Description Get", DRIVER_DESCRIPTION);
            return DRIVER_DESCRIPTION;
         }
      }

      public string DriverInfo
      {
         get
         {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            // TODO customise this driver description
            string driverInfo = "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            tl.LogMessage("DriverInfo Get", driverInfo);
            return driverInfo;
         }
      }

      public string DriverVersion
      {
         get
         {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            tl.LogMessage("DriverVersion Get", driverVersion);
            return driverVersion;
         }
      }

      public short InterfaceVersion
      {
         // set by the driver wizard
         get
         {
            tl.LogMessage("InterfaceVersion Get", "3");
            return Convert.ToInt16("3");
         }
      }

      public string Name
      {
         get
         {
            string name = "Short driver name - please customise";
            tl.LogMessage("Name Get", name);
            return name;
         }
      }

      #endregion

      #region ITelescope Implementation
      public void AbortSlew()
      {
         tl.LogMessage("AbortSlew", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("AbortSlew");
      }

      public AlignmentModes AlignmentMode
      {
         get
         {
            tl.LogMessage("AlignmentMode Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("AlignmentMode", false);
         }
      }

      public double Altitude
      {
         get
         {
            tl.LogMessage("Altitude", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Altitude", false);
         }
      }

      public double ApertureArea
      {
         get
         {
            tl.LogMessage("ApertureArea Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("ApertureArea", false);
         }
      }

      public double ApertureDiameter
      {
         get
         {
            tl.LogMessage("ApertureDiameter Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("ApertureDiameter", false);
         }
      }

      public bool AtHome
      {
         get
         {
            tl.LogMessage("AtHome", "Get - " + false.ToString());
            return false;
         }
      }

      public bool AtPark
      {
         get
         {
            tl.LogMessage("AtPark", "Get - " + false.ToString());
            return false;
         }
      }

      public IAxisRates AxisRates(TelescopeAxes Axis)
      {
         tl.LogMessage("AxisRates", "Get - " + Axis.ToString());
         return new AxisRates(Axis);
      }

      public double Azimuth
      {
         get
         {
            tl.LogMessage("Azimuth Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Azimuth", false);
         }
      }

      public bool CanFindHome
      {
         get
         {
            tl.LogMessage("CanFindHome", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanMoveAxis(TelescopeAxes Axis)
      {
         tl.LogMessage("CanMoveAxis", "Get - " + Axis.ToString());
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
            tl.LogMessage("CanPark", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanPulseGuide
      {
         get
         {
            tl.LogMessage("CanPulseGuide", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetDeclinationRate
      {
         get
         {
            tl.LogMessage("CanSetDeclinationRate", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetGuideRates
      {
         get
         {
            tl.LogMessage("CanSetGuideRates", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetPark
      {
         get
         {
            tl.LogMessage("CanSetPark", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetPierSide
      {
         get
         {
            tl.LogMessage("CanSetPierSide", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetRightAscensionRate
      {
         get
         {
            tl.LogMessage("CanSetRightAscensionRate", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetTracking
      {
         get
         {
            tl.LogMessage("CanSetTracking", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSlew
      {
         get
         {
            tl.LogMessage("CanSlew", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSlewAltAz
      {
         get
         {
            tl.LogMessage("CanSlewAltAz", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSlewAltAzAsync
      {
         get
         {
            tl.LogMessage("CanSlewAltAzAsync", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSlewAsync
      {
         get
         {
            tl.LogMessage("CanSlewAsync", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSync
      {
         get
         {
            tl.LogMessage("CanSync", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSyncAltAz
      {
         get
         {
            tl.LogMessage("CanSyncAltAz", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanUnpark
      {
         get
         {
            tl.LogMessage("CanUnpark", "Get - " + false.ToString());
            return false;
         }
      }

      public double Declination
      {
         get
         {
            double declination = 0.0;
            tl.LogMessage("Declination", "Get - " + utilities.DegreesToDMS(declination, ":", ":"));
            return declination;
         }
      }

      public double DeclinationRate
      {
         get
         {
            double declination = 0.0;
            tl.LogMessage("DeclinationRate", "Get - " + declination.ToString());
            return declination;
         }
         set
         {
            tl.LogMessage("DeclinationRate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DeclinationRate", true);
         }
      }

      public PierSide DestinationSideOfPier(double RightAscension, double Declination)
      {
         tl.LogMessage("DestinationSideOfPier Get", "Not implemented");
         throw new ASCOM.PropertyNotImplementedException("DestinationSideOfPier", false);
      }

      public bool DoesRefraction
      {
         get
         {
            tl.LogMessage("DoesRefraction Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DoesRefraction", false);
         }
         set
         {
            tl.LogMessage("DoesRefraction Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DoesRefraction", true);
         }
      }

      public EquatorialCoordinateType EquatorialSystem
      {
         get
         {
            EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equLocalTopocentric;
            tl.LogMessage("DeclinationRate", "Get - " + equatorialSystem.ToString());
            return equatorialSystem;
         }
      }

      public void FindHome()
      {
         tl.LogMessage("FindHome", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("FindHome");
      }

      public double FocalLength
      {
         get
         {
            tl.LogMessage("FocalLength Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("FocalLength", false);
         }
      }

      public double GuideRateDeclination
      {
         get
         {
            tl.LogMessage("GuideRateDeclination Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
         }
         set
         {
            tl.LogMessage("GuideRateDeclination Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
         }
      }

      public double GuideRateRightAscension
      {
         get
         {
            tl.LogMessage("GuideRateRightAscension Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", false);
         }
         set
         {
            tl.LogMessage("GuideRateRightAscension Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", true);
         }
      }

      public bool IsPulseGuiding
      {
         get
         {
            tl.LogMessage("IsPulseGuiding Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("IsPulseGuiding", false);
         }
      }

      public void MoveAxis(TelescopeAxes Axis, double Rate)
      {
         tl.LogMessage("MoveAxis", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("MoveAxis");
      }

      public void Park()
      {
         tl.LogMessage("Park", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("Park");
      }

      public void PulseGuide(GuideDirections Direction, int Duration)
      {
         tl.LogMessage("PulseGuide", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("PulseGuide");
      }

      public double RightAscension
      {
         get
         {
            double rightAscension = 0.0;
            tl.LogMessage("RightAscension", "Get - " + utilities.HoursToHMS(rightAscension));
            return rightAscension;
         }
      }

      public double RightAscensionRate
      {
         get
         {
            double rightAscensionRate = 0.0;
            tl.LogMessage("RightAscensionRate", "Get - " + rightAscensionRate.ToString());
            return rightAscensionRate;
         }
         set
         {
            tl.LogMessage("RightAscensionRate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("RightAscensionRate", true);
         }
      }

      public void SetPark()
      {
         tl.LogMessage("SetPark", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SetPark");
      }

      public PierSide SideOfPier
      {
         get
         {
            tl.LogMessage("SideOfPier Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SideOfPier", false);
         }
         set
         {
            tl.LogMessage("SideOfPier Set", "Not implemented");
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
            tl.LogMessage("SiderealTime", "Get - " + siderealTime.ToString());
            return siderealTime;
         }
      }

      public double SiteElevation
      {
         get
         {
            tl.LogMessage("SiteElevation Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SiteElevation", false);
         }
         set
         {
            tl.LogMessage("SiteElevation Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SiteElevation", true);
         }
      }

      public double SiteLatitude
      {
         get
         {
            tl.LogMessage("SiteLatitude Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SiteLatitude", false);
         }
         set
         {
            tl.LogMessage("SiteLatitude Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SiteLatitude", true);
         }
      }

      public double SiteLongitude
      {
         get
         {
            tl.LogMessage("SiteLongitude Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SiteLongitude", false);
         }
         set
         {
            tl.LogMessage("SiteLongitude Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SiteLongitude", true);
         }
      }

      public short SlewSettleTime
      {
         get
         {
            tl.LogMessage("SlewSettleTime Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", false);
         }
         set
         {
            tl.LogMessage("SlewSettleTime Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", true);
         }
      }

      public void SlewToAltAz(double Azimuth, double Altitude)
      {
         tl.LogMessage("SlewToAltAz", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToAltAz");
      }

      public void SlewToAltAzAsync(double Azimuth, double Altitude)
      {
         tl.LogMessage("SlewToAltAzAsync", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToAltAzAsync");
      }

      public void SlewToCoordinates(double RightAscension, double Declination)
      {
         tl.LogMessage("SlewToCoordinates", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToCoordinates");
      }

      public void SlewToCoordinatesAsync(double RightAscension, double Declination)
      {
         tl.LogMessage("SlewToCoordinatesAsync", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToCoordinatesAsync");
      }

      public void SlewToTarget()
      {
         tl.LogMessage("SlewToTarget", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToTarget");
      }

      public void SlewToTargetAsync()
      {
         tl.LogMessage("SlewToTargetAsync", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToTargetAsync");
      }

      public bool Slewing
      {
         get
         {
            tl.LogMessage("Slewing Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Slewing", false);
         }
      }

      public void SyncToAltAz(double Azimuth, double Altitude)
      {
         tl.LogMessage("SyncToAltAz", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SyncToAltAz");
      }

      public void SyncToCoordinates(double RightAscension, double Declination)
      {
         tl.LogMessage("SyncToCoordinates", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SyncToCoordinates");
      }

      public void SyncToTarget()
      {
         tl.LogMessage("SyncToTarget", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SyncToTarget");
      }

      public double TargetDeclination
      {
         get
         {
            tl.LogMessage("TargetDeclination Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TargetDeclination", false);
         }
         set
         {
            tl.LogMessage("TargetDeclination Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TargetDeclination", true);
         }
      }

      public double TargetRightAscension
      {
         get
         {
            tl.LogMessage("TargetRightAscension Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TargetRightAscension", false);
         }
         set
         {
            tl.LogMessage("TargetRightAscension Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TargetRightAscension", true);
         }
      }

      public bool Tracking
      {
         get
         {
            bool tracking = true;
            tl.LogMessage("Tracking", "Get - " + tracking.ToString());
            return tracking;
         }
         set
         {
            tl.LogMessage("Tracking Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Tracking", true);
         }
      }

      public DriveRates TrackingRate
      {
         get
         {
            tl.LogMessage("TrackingRate Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TrackingRate", false);
         }
         set
         {
            tl.LogMessage("TrackingRate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TrackingRate", true);
         }
      }

      public ITrackingRates TrackingRates
      {
         get
         {
            ITrackingRates trackingRates = new TrackingRates();
            tl.LogMessage("TrackingRates", "Get - ");
            foreach (DriveRates driveRate in trackingRates) {
               tl.LogMessage("TrackingRates", "Get - " + driveRate.ToString());
            }
            return trackingRates;
         }
      }

      public DateTime UTCDate
      {
         get
         {
            DateTime utcDate = DateTime.UtcNow;
            tl.LogMessage("TrackingRates", "Get - " + String.Format("MM/dd/yy HH:mm:ss", utcDate));
            return utcDate;
         }
         set
         {
            tl.LogMessage("UTCDate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("UTCDate", true);
         }
      }

      public void Unpark()
      {
         tl.LogMessage("Unpark", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("Unpark");
      }

      #endregion

      #region Private properties and methods
      // here are some useful properties and methods that can be used as required
      // to help with driver development

      #region ASCOM Registration

      // Register or unregister driver for ASCOM. This is harmless if already
      // registered or unregistered. 
      //
      /// <summary>
      /// Register or unregister the driver with the ASCOM Platform.
      /// This is harmless if the driver is already registered/unregistered.
      /// </summary>
      /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
      private static void RegUnregASCOM(bool bRegister)
      {
         using (var P = new ASCOM.Utilities.Profile()) {
            P.DeviceType = "Telescope";
            if (bRegister) {
               P.Register(DRIVER_ID, DRIVER_DESCRIPTION);
            }
            else {
               P.Unregister(DRIVER_ID);
            }
         }
      }

      /// <summary>
      /// This function registers the driver with the ASCOM Chooser and
      /// is called automatically whenever this class is registered for COM Interop.
      /// </summary>
      /// <param name="t">Type of the class being registered, not used.</param>
      /// <remarks>
      /// This method typically runs in two distinct situations:
      /// <list type="numbered">
      /// <item>
      /// In Visual Studio, when the project is successfully built.
      /// For this to work correctly, the option <c>Register for COM Interop</c>
      /// must be enabled in the project settings.
      /// </item>
      /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
      /// </list>
      /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
      /// </remarks>
      [ComRegisterFunction]
      public static void RegisterASCOM(Type t)
      {
         RegUnregASCOM(true);
      }

      /// <summary>
      /// This function unregisters the driver from the ASCOM Chooser and
      /// is called automatically whenever this class is unregistered from COM Interop.
      /// </summary>
      /// <param name="t">Type of the class being registered, not used.</param>
      /// <remarks>
      /// This method typically runs in two distinct situations:
      /// <list type="numbered">
      /// <item>
      /// In Visual Studio, when the project is cleaned or prior to rebuilding.
      /// For this to work correctly, the option <c>Register for COM Interop</c>
      /// must be enabled in the project settings.
      /// </item>
      /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
      /// </list>
      /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
      /// </remarks>
      [ComUnregisterFunction]
      public static void UnregisterASCOM(Type t)
      {
         RegUnregASCOM(false);
      }

      #endregion

      /// <summary>
      /// Returns true if there is a valid connection to the driver hardware
      /// </summary>
      private bool IsConnected
      {
         get
         {
            return (mConnection != null);
         }
      }

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
         using (Profile driverProfile = new Profile()) {
            driverProfile.DeviceType = "Telescope";
            TRACE_STATE = Convert.ToBoolean(driverProfile.GetValue(DRIVER_ID, traceStateProfileName, string.Empty, traceStateDefault));
            COM_PORT = driverProfile.GetValue(DRIVER_ID, comPortProfileName, string.Empty, comPortDefault);
         }
      }

      /// <summary>
      /// Write the device configuration to the  ASCOM  Profile store
      /// </summary>
      internal void WriteProfile()
      {
         using (Profile driverProfile = new Profile()) {
            driverProfile.DeviceType = "Telescope";
            driverProfile.WriteValue(DRIVER_ID, traceStateProfileName, TRACE_STATE.ToString());
            driverProfile.WriteValue(DRIVER_ID, comPortProfileName, COM_PORT.ToString());
         }
      }

      #endregion

   }
}
