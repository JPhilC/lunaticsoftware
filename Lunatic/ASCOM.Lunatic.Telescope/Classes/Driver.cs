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


   }

}
