//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Telescope driver for Synta mounts by Lunatic Software
//
// Description:	
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
using System.Runtime.InteropServices;

using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using Lunatic.Core;
using Lunatic.SyntaController;
using System.Threading.Tasks;
using Core = Lunatic.Core;

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
      private int _MountVersion;
      private int[] UnparkEncoderPosition = new int[2];

      private double[] GotoResolution = new double[2];      // Goto resolutions.
      private int[] WormSteps = new int[2];
      private int[] WormPeriod = new int[2];
      private double[] LowSpeedSlewRate = new double[2];         // [0] = LowSpeed, [1] = HighSpeed.

      private bool RAStatusSlew = false;

      #region Internal properties ...


      // private const int RAAxisIndex = 0;
      // private const int DECAxisIndex = 1;
      /// <summary>
      /// The axis positions in Radians
      /// </summary>
      // private double[] AxisPositionRadians = new double[2] { 0, 0 };

      private Settings Settings
      {
         get
         {
            return SettingsProvider.Current.Settings;
         }
      }

      //EQPARKSTATUS=parked
      private ParkStatus _ParkStatus;
      private ParkStatus ParkStatus
      {
         get
         {
            return _ParkStatus;
         }
         set
         {
            if (value == _ParkStatus) {
               return;
            }
            _ParkStatus = value;
            RaisePropertyChanged();
         }
      }

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

         MaximumSyncDifference = (2 * Math.PI) / 8.0;    // Allow a 45.0 (360/8) but in degrees discrepancy in Radians.
      }



      private TrackingStatus TrackingState { get; set; }

      private HemisphereOption Hemisphere
      {
         get
         {
            return (SiteLatitude >= 0.0 ? HemisphereOption.Northern : HemisphereOption.Southern);
         }

      }

      private SyncModeOption SyncMode { get; set; }

      private SyncAlignmentModeOptions SyncAlignmentMode { get; set; }


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

      #region Initialisation ...
      /// <summary>
      /// Initialise the encoder positions for the RA meridians, the DEC encoder
      /// home position and the maximum synchronisation parameters.
      /// </summary>
      private void InitialiseMeridians()
      {
         TotalStepsPer360[0] = _Mount.EQ_GetTotal360microstep(AxisId.Axis1_RA);
         TotalStepsPer360[1] = _Mount.EQ_GetTotal360microstep(AxisId.Axis2_DEC);
         MeridianWest = EncoderZeroPosition[0] + (TotalStepsPer360[0] / 4);
         MeridianEast = EncoderZeroPosition[0] - (TotalStepsPer360[0] / 4);
         EncoderHomePosition[1] = (TotalStepsPer360[1] / 4) + EncoderZeroPosition[1];    // totstep/4 + Homepos
         MaximumSyncDifference = TotalStepsPer360[0] / 16;             // totalstep /16 = 22.5 degree field
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

      #region Parking and Unparking ...
      private void ParkScope()
      {
      }

      private void ParkScopeAsync()
      {
      }

      private void UnparkScope()
      {
         if (_Mount.EQ_GetMountStatus() == 1) {     // Make sure that we unpark only if the mount is online


            if (ParkStatus == ParkStatus.Parked) {


               // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(178)



               // Load Alignment if required


               // Call AlignmentStarsUnpark



               // Just make sure motors are not moving


               // Call PEC_StopTracking

               _Mount.EQ_MotorStop(AxisId.Both_Axes);



               //            eqres = EQ_MotorStop(0)
               //            eqres = EQ_MotorStop(1)




               _Mount.EQ_SetMotorValues(AxisId.Axis1_RA, UnparkEncoderPosition[(int)AxisId.Axis1_RA]);
               _Mount.EQ_SetMotorValues(AxisId.Axis2_DEC, UnparkEncoderPosition[(int)AxisId.Axis2_DEC]);


               //  set status as unparked

               // Set status as unparked 
               ParkStatus = ParkStatus.Unparked;
               // Persist the current Park status to disk
               Settings.ParkStatus = ParkStatus;
               SettingsProvider.Current.SaveSettings();
            }
         }
         else {

            // HC.Add_Message(oLangDll.GetLangString(5037))


         }


      }


      private void UnparkScopeAsync()
      {
         var t = Task.Run(() => UnparkScope());
         t.Wait();
      }
      #endregion


      #region Moving and slewing ...
      private void InternalMoveAxis(AxisId axis, double rate)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("InternalMoveAxis({0}, {1})", axis, rate));
         double j;
         double currentRate;
         bool moveRAAxisSlewing = false;
         bool moveDecAxisSlewing = false;

         if (rate != 0.0) {
            // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(189)
         }

         j = rate * 3600; // Convert to Arcseconds

         if (axis == AxisId.Axis1_RA) {

            if (rate == 0 && DeclinationRate == 0) {
               // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(178)
            }



            if (Hemisphere == HemisphereOption.Southern) {
               j = -1 * j;
               currentRate = RightAscensionRate * -1;
            }
            else {
               currentRate = RightAscensionRate;
            }


            // check for change of direction
            if ((currentRate * j) <= 0) {
               StartRAByRate(j);
            }
            else {
               ChangeRAByRate(j);
            }


            RightAscensionRate = j;



            if (rate == 0) {
               moveRAAxisSlewing = false;
            }
            else {
               TrackingState = TrackingStatus.Custom;
               moveRAAxisSlewing = true;
            }
         }


         if (axis == AxisId.Axis2_DEC) {

            if (rate == 0 && RightAscensionRate == 0) {
               // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(178)
            }


            // check for change of direction
            if ((DeclinationRate * j) <= 0) {
               StartDECByRate(j);
            }
            else {
               ChangeDECByRate(j);
            }


            DeclinationRate = j;
            if (rate == 0) {
               moveDecAxisSlewing = false;
            }
            else {
               TrackingState = TrackingStatus.Custom;
               moveDecAxisSlewing = true;
            }
         }

         _IsMoveAxisSlewing = (moveRAAxisSlewing || moveDecAxisSlewing);
      }

      private void StartRAByRate(double raRate)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("StartRAByRate({0})", raRate));
         double i;
         double j;
         MountSpeed mountSpeed = MountSpeed.LowSpeed;
         int highSpeedRatio = 1;
         i = Math.Abs(raRate);
         int eqResult;

         if (_MountVersion > 0x301) {
            if (i > 1000) {
               mountSpeed = MountSpeed.HighSpeed;
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);
            }
         }
         else {
            if (i > 3000) {
               mountSpeed = MountSpeed.HighSpeed;
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);
            }
         }

         // HC.Add_Message (oLangDll.GetLangString(117) & " " & str(m) & " , " & str(RA_RATE) & " arcsec/sec")

         eqResult = _Mount.EQ_MotorStop(AxisId.Axis1_RA);          // Stop RA Motor
         if (eqResult != Core.Constants.MOUNT_SUCCESS) {
            return;
         }

         if (raRate == 0) {
            _IsSlewing = false;
            RAStatusSlew = false;
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis1_RA);
            MoveAxisRate[0] = MountSpeed.LowSpeed;
            return;
         }

         i = raRate;
         j = Math.Abs(i);              // Get the absolute value for parameter passing
         if (_MountVersion == 0x301) {
            if ((j > 1350) && (j <= 3000)) {
               if (j < 2175) {
                  j = 1350;
               }
               else {
                  j = 3001;
                  mountSpeed = MountSpeed.HighSpeed;
                  highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);
               }
            }
         }


         MoveAxisRate[0] = mountSpeed;    // Save Speed Settings

         // HC.Add_FileMessage ("StartRARate=" & FormatNumber(RA_RATE, 5))
         j = ((highSpeedRatio * LowSpeedSlewRate[0] / j) + 0.5) + 30000; // Compute for the rate

         if (i >= 0) {
            eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis1_RA, TrackMode.Initial, (int)j, mountSpeed, Hemisphere, AxisDirection.Forward);
         }
         else {
            eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis1_RA, TrackMode.Initial, (int)j, mountSpeed, Hemisphere, AxisDirection.Reverse);
         }

      }

      // Change RA motor rate based on an input rate of arcsec per Second

      private void ChangeRAByRate(double raRate)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("ChangeRAByRate({0})", raRate));

         double j;
         MountSpeed mountSpeed = MountSpeed.LowSpeed;
         int highSpeedRatio;
         AxisDirection dir;
         int eqResult;
         TrackMode init = TrackMode.Update;


         if (raRate >= 0) {
            dir = AxisDirection.Forward;
         }
         else {
            dir = AxisDirection.Reverse;
         }


         if (raRate == 0) {
            // rate = 0 so stop motors
            _IsSlewing = false;
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis1_RA);
            RAStatusSlew = false;
            MoveAxisRate[0] = 0;
            return;
         }

         highSpeedRatio = 1;   // Speed multiplier = 1
         j = Math.Abs(raRate);



         if (_MountVersion > 0x301) {
            // if above high speed theshold
            if (j > 1000) {
               mountSpeed = MountSpeed.HighSpeed;               // HIGH SPEED
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);  // GET HIGH SPEED MULTIPLIER
            }
         }
         else {
            // who knows what Mon is up to here - a special for his mount perhaps?
            if (_MountVersion == 0x301) {
               if ((j > 1350) && (j <= 3000)) {
                  if (j < 2175) {
                     j = 1350;
                  }
                  else {
                     j = 3001;
                     mountSpeed = MountSpeed.HighSpeed;
                     highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);
                  }
               }
            }
            // if above high speed theshold
            if (j > 3000) {
               mountSpeed = MountSpeed.HighSpeed;          // HIGH SPEED
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10003);  // GET HIGH SPEED MULTIPLIER
            }
         }


         // HC.Add_FileMessage ("ChangeRARate=" & FormatNumber(rate, 5))

         // if there// s a switch between high/low speed or if operating at high speed
         // we ned to do additional initialisation
         if (mountSpeed != MountSpeed.LowSpeed || mountSpeed != MoveAxisRate[0]) {
            init = TrackMode.Initial;
         }
         if (init == TrackMode.Initial) {
            // Stop Motor
            // HC.Add_FileMessage ("Direction or High/Low speed change")
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis1_RA);
            if (eqResult != Core.Constants.MOUNT_SUCCESS) {
               return;
            }

         }
         MoveAxisRate[0] = mountSpeed;

         // Compute for the rate
         j = ((highSpeedRatio * LowSpeedSlewRate[0] / j) + 0.5) + 30000;

         eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis1_RA, init, (int)j, mountSpeed, Hemisphere, dir);
         // HC.Add_FileMessage ("EQ_SetCustomTrackRate=0," & CStr(init) & "," & CStr(j) & "," & CStr(k) & "," & CStr(gHemisphere) & "," & CStr(dir))
         // HC.Add_Message (oLangDll.GetLangString(117) & "=" & str(rate) & " arcsec/sec" & "," & CStr(eqres))
      }


      // Start DEC motor based on an input rate of arcsec per Second

      private void StartDECByRate(double decRate)
      {
         System.Diagnostics.Debug.WriteLine(string.Format("StartDECByRate({0})", decRate));
         double i;
         double j;
         MountSpeed mountSpeed = MountSpeed.LowSpeed;
         int highSpeedRatio = 1;
         i = Math.Abs(decRate);
         int eqResult;


         if (_MountVersion > 0x301) {
            if (i > 1000) {
               mountSpeed = MountSpeed.HighSpeed;
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);
            }
         }
         else {
            if (i > 3000) {
               mountSpeed = MountSpeed.HighSpeed;
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);
            }
         }


         // HC.Add_Message (oLangDll.GetLangString(118) & " " & str(m) & " , " & str(DEC_RATE) & " arcsec/sec")

         eqResult = _Mount.EQ_MotorStop(AxisId.Axis2_DEC);          // Stop RA Motor
         if (eqResult != Core.Constants.MOUNT_SUCCESS) {
            return;
         }

         if (decRate == 0) {
            _IsSlewing = false;
            RAStatusSlew = false;
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
            MoveAxisRate[1] = MountSpeed.LowSpeed;
            return;
         }
         i = decRate;
         j = Math.Abs(i);              // Get the absolute value for parameter passing
         if (_MountVersion == 0x301) {
            if ((j > 1350) && (j <= 3000)) {
               if (j < 2175) {
                  j = 1350;
               }
               else {
                  j = 3001;
                  mountSpeed = MountSpeed.HighSpeed;
                  highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);
               }
            }
         }

         MoveAxisRate[1] = mountSpeed;      // Save Speed Settings

         // HC.Add_FileMessage ("StartDecRate=" & FormatNumber(DEC_RATE, 5))
         //    j = Int((m * 9325.46154 / j) + 0.5) + 30000 // Compute for the rate
         j = ((highSpeedRatio * LowSpeedSlewRate[1] / j) + 0.5) + 30000; // Compute for the rate

         if (i >= 0) {
            eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis2_DEC, TrackMode.Initial, (int)j, mountSpeed, Hemisphere, AxisDirection.Forward);
         }
         else {
            eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis2_DEC, TrackMode.Initial, (int)j, mountSpeed, Hemisphere, AxisDirection.Reverse);
         }
      }



      // Change DEC motor rate based on an input rate of arcsec per Second

      private void ChangeDECByRate(double decRate)
      {

         double j;
         MountSpeed mountSpeed = MountSpeed.LowSpeed;
         int highSpeedRatio = 1;
         AxisDirection dir;
         TrackMode init = TrackMode.Update;
         int eqResult;


         if (decRate >= 0) {
            dir = AxisDirection.Forward;
         }
         else {
            dir = AxisDirection.Forward;
         }


         if (decRate == 0) {
            // rate = 0 so stop motors
            _IsSlewing = false;
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
            //        gRAStatus_slew = False
            MoveAxisRate[1] = MountSpeed.LowSpeed;
            return;
         }

         j = Math.Abs(decRate);

         if (_MountVersion > 0x301) {
            // if above high speed theshold
            if (j > 1000) {
               mountSpeed = MountSpeed.HighSpeed;               // HIGH SPEED
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);  // GET HIGH SPEED MULTIPLIER
            }
         }
         else {
            // who knows what Mon is up to here - a special for his mount perhaps?
            if (_MountVersion == 0x301) {
               if ((j > 1350) && (j <= 3000)) {
                  if (j < 2175) {
                     j = 1350;
                  }
                  else {
                     j = 3001;
                     mountSpeed = MountSpeed.HighSpeed;
                     highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);
                  }
               }
            }
            // if above high speed theshold
            if (j > 3000) {
               mountSpeed = MountSpeed.HighSpeed;               // HIGH SPEED
               highSpeedRatio = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10003);  // GET HIGH SPEED MULTIPLIER
            }
         }


         // HC.Add_FileMessage ("ChangeDECRate=" & FormatNumber(rate, 5))

         // if there// s a switch between high/low speed or if operating at high speed
         // we need to do additional initialisation
         if (mountSpeed != MountSpeed.LowSpeed || mountSpeed != MoveAxisRate[1]) {
            init = TrackMode.Initial;
         }


         if (init == TrackMode.Initial) {
            // Stop Motor
            // HC.Add_FileMessage ("Direction or High/Low speed change")
            eqResult = _Mount.EQ_MotorStop(AxisId.Axis2_DEC);
            if (eqResult != Core.Constants.MOUNT_SUCCESS) {
               return;
            }
         }
         MoveAxisRate[1] = mountSpeed;

         // Compute for the rate
         //    j = Int((m * 9325.46154 / j) + 0.5) + 30000
         j = ((highSpeedRatio * LowSpeedSlewRate[1] / j) + 0.5) + 30000;

         eqResult = _Mount.EQ_SetCustomTrackRate(AxisId.Axis2_DEC, init, (int)j, mountSpeed, Hemisphere, dir);
         // HC.Add_FileMessage ("EQ_SetCustomTrackRate=1," & CStr(init) & "," & CStr(j) & "," & CStr(k) & "," & CStr(gHemisphere) & "," & CStr(dir))
         // HC.Add_Message (oLangDll.GetLangString(118) & "=" & str(rate) & " arcsec/sec" & "," & CStr(eqres))

      }

      private void InternalStopAxis(AxisId axis)
      {
         _Mount.EQ_MotorStop(axis);
      }

      #endregion
   }

}
