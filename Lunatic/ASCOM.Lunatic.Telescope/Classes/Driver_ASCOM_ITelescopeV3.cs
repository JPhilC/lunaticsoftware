using System;
using System.Text;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using Lunatic.Core;
using Core = Lunatic.Core;
using System.Threading;
using ASCOM.Lunatic.Telescope;
using ASCOM.Lunatic.Telescope.Classes;
using Lunatic.Core.Geometry;
using Lunatic.SyntaController;
using Lunatic.Core.Classes;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// The ASCOM ITelescopeV3 implimentation for the driver.
/// </summary>
namespace ASCOM.Lunatic.Telescope
{
   partial class Telescope
   {
      #region Private members ...
      /// <summary>
      /// 
      /// </summary>
      AxisPosition AxisZeroPosition;  // Constants for zero Encoder positions.
      /// <summary>
      /// Current polled positions (Radians)
      /// </summary>
      private AxisPosition CurrentAxisPosition;             // 
      private MountSpeed[] MoveAxisRate = new MountSpeed[2] { MountSpeed.LowSpeed, MountSpeed.LowSpeed };
      private double[] LastGotoTarget = new double[2] { 0.0, 0.0 };
      private int[] PulseDuration = new int[2] { 0, 0 };




      private int[] TotalStepsPer360 = new int[2];
      private double[] RateAdjustment = new double[2] { 0.0, 0.0 };                 // Rate adjustment (RA and DEC)
      private double MeridianWest;
      private double MeridianEast;
      private double MaximumSyncDifference;

      private int[] MotorStatus = new int[2] { 0, 0 };

      /// <summary>
      /// Radians
      /// </summary>
      private AxisPosition EmulatorAxisPosition;
      /// <summary>
      /// Radians
      /// </summary>
      private AxisPosition EmulatorAxisInitialPosition;

      private bool EmulatorOneShot;
      private bool EmulatorNudge;
      private double EmulatorLastReadTime;
      private double EmulatorRaRate;

      private AlignmentPoint SelectedAlignmentPoint = null;

      #endregion

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

      /// <summary>
      /// Returns the list of action names supported by this driver.
      /// </summary>
      /// <value>An ArrayList of strings (SafeArray collection) containing the names of supported actions.</value>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks><p style="color:red"><b>Must be implemented</b></p> This method must return an empty arraylist if no actions are supported. Please do not throw a 
      /// <see cref="ASCOM.PropertyNotImplementedException" />.
      /// <para>This is an aid to client authors and testers who would otherwise have to repeatedly poll the driver to determine its capabilities. 
      /// Returned action names may be in mixed case to enhance presentation but  will be recognised case insensitively in 
      /// the <see cref="Action">Action</see> method.</para>
      ///<para>An array list collection has been selected as the vehicle for  action names in order to make it easier for clients to
      /// determine whether a particular action is supported. This is easily done through the Contains method. Since the
      /// collection is also ennumerable it is easy to use constructs such as For Each ... to operate on members without having to be concerned 
      /// about hom many members are in the collection. </para>
      /// <para>Collections have been used in the Telescope specification for a number of years and are known to be compatible with COM. Within .NET
      /// the ArrayList is the correct implementation to use as the .NET Generic methods are not compatible with COM.</para>
      /// </remarks>
      public ArrayList SupportedActions
      {
         get
         {
            _Logger.LogMessage("SupportedActions", "Get - ");
            return new ArrayList(_SupportedActions);
         }
      }

      /// <summary>
      /// Invokes the specified device-specific action.
      /// </summary>
      /// <param name="ActionName">
      /// A well known name agreed by interested parties that represents the action to be carried out. 
      /// </param>
      /// <param name="ActionParameters">List of required parameters or an <see cref="String.Empty">Empty String</see> if none are required.
      /// </param>
      /// <returns>A string response. The meaning of returned strings is set by the driver author.</returns>
      /// <exception cref="ASCOM.MethodNotImplementedException">Throws this exception if no actions are suported.</exception>
      /// <exception cref="ASCOM.ActionNotImplementedException">It is intended that the SupportedActions method will inform clients 
      /// of driver capabilities, but the driver must still throw an ASCOM.ActionNotImplemented exception if it is asked to 
      /// perform an action that it does not support.</exception>
      /// <exception cref="NotConnectedException">If the driver is not connected.</exception>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <example>Suppose filter wheels start to appear with automatic wheel changers; new actions could 
      /// be “FilterWheel:QueryWheels” and “FilterWheel:SelectWheel”. The former returning a 
      /// formatted list of wheel names and the second taking a wheel name and making the change, returning appropriate 
      /// values to indicate success or failure.
      /// </example>
      /// <remarks><p style="color:red"><b>Can throw a not implemented exception</b></p> 
      /// This method is intended for use in all current and future device types and to avoid name clashes, management of action names 
      /// is important from day 1. A two-part naming convention will be adopted - <b>DeviceType:UniqueActionName</b> where:
      /// <list type="bullet">
      /// <item><description>DeviceType is the same value as would be used by <see cref="ASCOM.Utilities.Chooser.DeviceType"/> e.g. Telescope, Camera, Switch etc.</description></item>
      /// <item><description>UniqueActionName is a single word, or multiple words joined by underscore characters, that sensibly describes the action to be performed.</description></item>
      /// </list>
      /// <para>
      /// It is recommended that UniqueActionNames should be a maximum of 16 characters for legibility.
      /// Should the same function and UniqueActionName be supported by more than one type of device, the reserved DeviceType of 
      /// “General” will be used. Action names will be case insensitive, so FilterWheel:SelectWheel, filterwheel:selectwheel 
      /// and FILTERWHEEL:SELECTWHEEL will all refer to the same action.</para>
      /// <para>The names of all supported actions must be returned in the <see cref="SupportedActions"/> property.</para>
      /// </remarks>
      public string Action(string actionName, string actionParameters)
      {
         _Logger.LogMessage("Action", string.Format("({0}, {1})", actionName, actionParameters));
         return ProcessCustomAction(actionName, actionParameters);
      }

      /// <summary>
      /// Transmits an arbitrary string to the device and does not wait for a response.
      /// Optionally, protocol framing characters may be added to the string before transmission.
      /// </summary>
      /// <param name="Command">The literal command string to be transmitted.</param>
      /// <param name="Raw">
      /// if set to <c>true</c> the string is transmitted 'as-is'.
      /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
      /// </param>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented</exception>
      /// <exception cref="NotConnectedException">If the driver is not connected.</exception>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks><p style="color:red"><b>Can throw a not implemented exception</b></p> </remarks>
      public void CommandBlind(string command, bool raw)
      {
         //CheckConnected("CommandBlind");
         //// Call CommandString and return as soon as it finishes
         //this.CommandString(command, raw);
         // or
         throw new ASCOM.MethodNotImplementedException("CommandBlind");
         // DO NOT have both these sections!  One or the other
      }

      /// <summary>
      /// Transmits an arbitrary string to the device and waits for a boolean response.
      /// Optionally, protocol framing characters may be added to the string before transmission.
      /// </summary>
      /// <param name="Command">The literal command string to be transmitted.</param>
      /// <param name="Raw">
      /// if set to <c>true</c> the string is transmitted 'as-is'.
      /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
      /// </param>
      /// <returns>
      /// Returns the interpreted boolean response received from the device.
      /// </returns>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented</exception>
      /// <exception cref="NotConnectedException">If the driver is not connected.</exception>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks><p style="color:red"><b>Can throw a not implemented exception</b></p> </remarks>
      public bool CommandBool(string command, bool raw)
      {
         _Logger.LogMessage("CommandBool", string.Format("({0}, {1})", command, raw));
         return ProcessCommandBool(command, raw);
      }

      /// <summary>
      /// Transmits an arbitrary string to the device and waits for a string response.
      /// Optionally, protocol framing characters may be added to the string before transmission.
      /// </summary>
      /// <param name="Command">The literal command string to be transmitted.</param>
      /// <param name="Raw">
      /// if set to <c>true</c> the string is transmitted 'as-is'.
      /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
      /// </param>
      /// <returns>
      /// Returns the string response received from the device.
      /// </returns>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented</exception>
      /// <exception cref="NotConnectedException">If the driver is not connected.</exception>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks><p style="color:red"><b>Can throw a not implemented exception</b></p> </remarks>
      public string CommandString(string command, bool raw)
      {
         _Logger.LogMessage("CommandString", string.Format("({0}, {1})", command, raw));
         // it's a good idea to put all the low level communication with the device here,
         // then all communication calls this function
         // you need something to ensure that only one command is in progress at a time
         return ProcessCommandString(command, raw);
      }

      /// <summary>
      /// Set True to connect to the device hardware. Set False to disconnect from the device hardware.
      /// You can also read the property to check whether it is connected. This reports the current hardware state.
      /// </summary>
      /// <value><c>true</c> if connected to the hardware; otherwise, <c>false</c>.</value>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented</b></p>Do not use a NotConnectedException here, that exception is for use in other methods that require a connection in order to succeed.
      /// <para>The Connected property sets and reports the state of connection to the device hardware.
      /// For a hub this means that Connected will be true when the first driver connects and will only be set to false
      /// when all drivers have disconnected.  A second driver may find that Connected is already true and
      /// setting Connected to false does not report Connected as false.  This is not an error because the physical state is that the
      /// hardware connection is still true.</para>
      /// <para>Multiple calls setting Connected to true or false will not cause an error.</para>
      /// </remarks>
      public bool Connected
      {
         get
         {
            _Logger.LogMessage("Connected Get", IsConnected.ToString());
            return IsConnected;
         }
         set
         {
            lock (_Lock) {
               _Logger.LogMessage("Connected", "Set - " + value.ToString());
               if (value == IsConnected)
                  return;

               if (value) {
                  _Logger.LogMessage("Connected", "Set - Connecting to port " + Settings.COMPort);

                  //gpl_interval = 50


                  //Call readRASyncCheckVal ' RA Sync Auto
                  //Call readPulseguidepwidth ' Read Pulseguide interval

                  int connectionResult = _Mount.Connect(Settings.COMPort, (int)Settings.BaudRate, (int)Settings.Timeout, (int)Settings.Retry);
                  if (connectionResult == Core.Constants.MOUNT_SUCCESS) {
                     #region Initialise new mount instance ...
                     _MountVersion = _Mount.EQ_GetMountVersion();

                     // Initialise some settings
                     LastPECRate = 0;
                     // gCurrent_time = 0;
                     // gLast_time = 0;
                     EmulatorAxisInitialPosition[RA_AXIS] = 0;

                     // gAlignmentStars_count = 0


                     PulseDuration[RA_AXIS] = 0;
                     PulseDuration[DEC_AXIS] = 0;
                     ProcessingPulseTimerTick = true;
                     LastGotoTarget[RA_AXIS] = 0;
                     LastGotoTarget[DEC_AXIS] = 0;

                     MoveAxisRate[RA_AXIS] = MountSpeed.LowSpeed;
                     MoveAxisRate[DEC_AXIS] = MountSpeed.LowSpeed;

                     // Initialise Meridian settings etc
                     InitialiseMeridians();


                     // Call readRALimit
                     // Call readCustomMount

                     if (TotalStepsPer360[RA_AXIS] != Core.Constants.EQ_ERROR) {
                        GotoResolution[RA_AXIS] = Settings.DevelopmentOptions.GotoResolution * 1296000 / TotalStepsPer360[RA_AXIS];  // 1296000 = seconds per 360 degrees
                        WormSteps[RA_AXIS] = _Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10006);
                        // HC.Add_Message CStr(gRAWormSteps) & " RAWormSteps read"
                        if (Settings.MountOption == MountOptions.AutoDetect) {
                           switch (WormSteps[RA_AXIS]) {
                              case 0:
                                 if (TotalStepsPer360[0] == 5184000) {   // AZEQ5GT detected, worm steps need fixing!
                                    WormSteps[RA_AXIS] = 38400;
                                    // HC.Add_Message "AZEQ5GT:RAWormSteps=38400"
                                 }
                                 else {
                                    // prevent divide by 0 later
                                    WormSteps[RA_AXIS] = 1;
                                 }
                                 break;

                              case 61866:
                                 if (TotalStepsPer360[RA_AXIS] == 11136000) {  //EQ8 detected, worm steps need fixing!
                                    WormSteps[RA_AXIS] = 25600;
                                    // HC.Add_Message "EQ8:RAWormSteps=25600"
                                 }
                                 break;
                              case 51200:    // AZEQ6GT
                              case 50133:    // EQ6Pro
                              case 66844:    // HEQ5
                              case 35200:    // EQ3
                              case 31288:    // EQ4/EQ5
                                             // Do nothing.
                                 break;
                           }
                        }
                        else {
                           // Custom mount so read values from settings.
                           TotalStepsPer360[RA_AXIS] = Settings.CustomMount.RAStepsPer360;
                           WormSteps[RA_AXIS] = Settings.CustomMount.RAWormSteps;
                        }

                        WormPeriod[RA_AXIS] = (int)((Core.Constants.SECONDS_PER_SIDERIAL_DAY * WormSteps[RA_AXIS] / TotalStepsPer360[RA_AXIS]) + 0.5);
                     }



                     if (TotalStepsPer360[DEC_AXIS] != Core.Constants.EQ_ERROR) {
                        GotoResolution[DEC_AXIS] = Settings.DevelopmentOptions.GotoResolution * 1296000 / TotalStepsPer360[DEC_AXIS];  // 1296000 = seconds per 360 degrees
                        WormSteps[DEC_AXIS] = _Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10006);
                        // HC.Add_Message CStr(gDECWormSteps) & " DECWormSteps read"

                        if (Settings.MountOption == MountOptions.AutoDetect) {
                           switch (WormSteps[DEC_AXIS]) {

                              case 0:

                                 if (TotalStepsPer360[DEC_AXIS] == 5184000) {  //AZEQ5GT detected, worm steps need fixing!
                                    WormSteps[DEC_AXIS] = 38400;
                                    // HC.Add_Message "AZEQ5GT:DECWormSteps=38400"
                                 }
                                 else {
                                    // prevent divide by 0 later
                                    WormSteps[DEC_AXIS] = 1;
                                 }
                                 break;
                              case 61866:

                                 if (TotalStepsPer360[DEC_AXIS] == 11136000) {  // EQ8 detected, worm steps need fixing!
                                    WormSteps[DEC_AXIS] = 25600;
                                    // HC.Add_Message "EQ8:DECWormSteps=25600"
                                 }
                                 break;
                              case 51200:                        // AZEQ6GT
                              case 50133:                        // EQ6Pro
                              case 66844:                        // HEQ5
                              case 35200:                        // EQ3
                              case 31288:                        // EQ4/EQ5
                                                                 // Nothing to do.
                                 break;
                           }
                        }
                        else {
                           // Custom mount so read values from settings.
                           TotalStepsPer360[DEC_AXIS] = Settings.CustomMount.DecStepsPer360;
                           WormSteps[DEC_AXIS] = Settings.CustomMount.DecWormSteps;
                        }

                     }


                     LowSpeedSlewRate[0] = ((double)_Mount.EQ_GetMountParameter(AxisId.Axis1_RA, 10004)) * Core.Constants.SIDEREAL_RATE_ARCSECS; // Low Speed tracking rate
                     LowSpeedSlewRate[1] = ((double)_Mount.EQ_GetMountParameter(AxisId.Axis2_DEC, 10004)) * Core.Constants.SIDEREAL_RATE_ARCSECS; // Low speed tracking rate


                     _Mount.EQ_MotorStop(AxisId.Both_Axes);
                     // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(178)

                     // Get state of at least one of the motors
                     int motorStatus = _Mount.EQ_GetMotorStatus(AxisId.Axis1_RA);
                     // If its an error then Initialize it


                     // Initialise the axes at the home position
                     if (motorStatus == Core.Constants.MOUNT_MOTORINACTIVE) {
                        // Initialise the current mount position using Greenwich Observatory position
                        InitialiseCurrentMountPosition();

                        CurrentAxisPosition = Settings.AxisHomePosition;
                        int result = _Mount.MCInitialiseAxes(CurrentAxisPosition);
                        if (result != Core.Constants.MOUNT_SUCCESS) {
                           throw new ASCOM.DriverException("There was an error initialising the mount axes. The mount returned: " + result.ToString());
                        };
                     }
                     else {
                        CurrentAxisPosition = _Mount.MCGetAxisPosition();
                        // Work out the current position
                        //System.Diagnostics.Debug.WriteLine("Mount axis position: " + CurrentAxisPosition.ToString());
                        //System.Diagnostics.Debug.WriteLine("Current axis position: " + Settings.CurrentMountPosition.ObservedAxes.ToString());
                     }

                     // set up rates collection
                     // MaxRate = Core.Constants.SIDEREAL_RATE_ARCSECS * 800 / 3600;      
                     // g_RAAxisRates.Add MaxRate, 0#
                     // g_DECAxisRates.Add MaxRate, 0#

                     // Make sure we get the latest data from the registry

                     //HC.Add_Message(oLangDll.GetLangString(5132) & " " & gPort & ":" & str(gBaud))
                     //HC.Add_Message(oLangDll.GetLangString(5133) & " " & printhex(EQ_GetMountVersion()) & " DLL Version:" & printhex(EQ_DriverVersion()))
                     //HC.Add_Message "Using " & CStr(gRAWormSteps) & "RAWormSteps"
                     //HC.EncoderTimer.Enabled = True
                     ProcessingEncoderTimerTick = false;
                     _EncoderTimer.Start();
                     //HC.EncoderTimerFlag = True
                     //gEQPulsetimerflag = True
                     //HC.Pulseguide_Timer.Enabled = False     'Enabled only during pulseguide session


                     //Call readParkModes
                     //Call readAlignProximity

                     //gEQparkstatus = readparkStatus()


                     if (Settings.ParkStatus == ParkStatus.Parked) {
                        //  currently parked
                        // HC.Frame15.Caption = oLangDll.GetLangString(146) & " " & oLangDll.GetLangString(177)
                        // Read Park position

                        //  Preset the Encoder values to Park position
                        _Mount.MCSetAxisPosition(Settings.AxisUnparkPosition);
                     }
                     else {
                        // HC.Frame15.Caption = oLangDll.GetLangString(146) & " " & oLangDll.GetLangString(179)
                     }
                     // Call SetParkCaption

                     // Call readportrate ' Read Autoguider port settings from registry and send to mount
                     _Mount.EQ_SetAutoguiderPortRate(AxisId.Axis1_RA, Settings.RAAutoGuiderPortRate);
                     _Mount.EQ_SetAutoguiderPortRate(AxisId.Axis2_DEC, Settings.DECAutoGuiderPortRate);

                     // TODO: Sort out PEC
                     // Call PEC_Initialise   ' only initialise PEc when we've defaults for worm

                     IsConnected = true;
                     #endregion
                  }
                  else if (connectionResult == Core.Constants.MOUNT_COMCONNECTED) {
                     // Was already connected to GET the current axis positions
                     //Settings.RAAxisPosition = _Mount.MCGetAxisPosition(AxisId.Axis1_RA);
                     //Settings.DECAxisPosition = _Mount.MCGetAxisPosition(AxisId.Axis2_DEC);
                     IsConnected = true;
                  }
                  else {
                     // Something went wrong so not connected.
                     IsConnected = false;
                  }
               }
               else {
                  _EncoderTimer.Stop();
                  _Mount.Disconnect();
                  _Logger.LogMessage("Connected", "Set - Disconnecting from port " + Settings.COMPort);
                  IsConnected = false;
               }
            }
         }
      }

      /// <summary>
      /// Returns a description of the device, such as manufacturer and modelnumber. Any ASCII characters may be used. 
      /// </summary>
      /// <value>The description.</value>
      /// <exception cref="NotConnectedException">If the device is not connected and this information is only available when connected.</exception>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks><p style="color:red"><b>Must be implemented</b></p> </remarks>
      public string Description
      {
         get
         {
            _Logger.LogMessage("Description", "Get - " + INSTRUMENT_DESCRIPTION);
            return INSTRUMENT_DESCRIPTION;
         }
      }

      /// <summary>
      /// Descriptive and version information about this ASCOM driver.
      /// </summary>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented</b></p> This string may contain line endings and may be hundreds to thousands of characters long.
      /// It is intended to display detailed information on the ASCOM driver, including version and copyright data.
      /// See the <see cref="Description" /> property for information on the device itself.
      /// To get the driver version in a parseable string, use the <see cref="DriverVersion" /> property.
      /// </remarks>
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

      /// <summary>
      /// A string containing only the major and minor version of the driver.
      /// </summary>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks><p style="color:red"><b>Must be implemented</b></p> This must be in the form "n.n".
      /// It should not to be confused with the <see cref="InterfaceVersion" /> property, which is the version of this specification supported by the 
      /// driver.
      /// </remarks>
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

      /// <summary>
      /// The interface version number that this device supports. Should return 3 for this interface version.
      /// </summary>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks><p style="color:red"><b>Must be implemented</b></p> Clients can detect legacy V1 drivers by trying to read ths property.
      /// If the driver raises an error, it is a V1 driver. V1 did not specify this property. A driver may also return a value of 1. 
      /// In other words, a raised error or a return value of 1 indicates that the driver is a V1 driver.
      /// </remarks>
      public short InterfaceVersion
      {
         // set by the driver wizard
         get
         {
            _Logger.LogMessage("InterfaceVersion", "Get - 3");
            return Convert.ToInt16("3");
         }
      }

      /// <summary>
      /// The short name of the driver, for display purposes
      /// </summary>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks><p style="color:red"><b>Must be implemented</b></p> </remarks>
      public string Name
      {
         get
         {
            _Logger.LogMessage("Name", "Get - " + INSTRUMENT_NAME);
            return INSTRUMENT_NAME;
         }
      }

      /// <summary>
      /// Stops a slew in progress.
      /// </summary>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented</exception>
      /// <remarks>
      /// Effective only after a call to <see cref="SlewToTargetAsync" />, <see cref="SlewToCoordinatesAsync" />, <see cref="SlewToAltAzAsync" />, or <see cref="MoveAxis" />.
      /// Does nothing if no slew/motion is in progress. Tracking is returned to its pre-slew state. Raises an error if <see cref="AtPark" /> is true. 
      /// </remarks>
      public void AbortSlew()
      {
         System.Diagnostics.Debug.WriteLine("AbortSlew()");
         _Logger.LogMessage("Command", "AbortSlew");
         if (Settings.ParkStatus == ParkStatus.Parked) {
            // no move axis if parked or parking!
            throw new ASCOM.ParkedException("AbortSlew");
         }

         if (Slewing) {
            _IsSlewing = false;
            _IsMoveAxisSlewing = false;
            _Mount.EQ_MotorStop(AxisId.Both_Axes);
            RAAxisSlewing = false;

            // restart tracking
            // TODO: RestartTracking
         }
      }

      private AlignmentModes _AlignmentMode;
      /// <summary>
      /// The alignment mode of the mount (Alt/Az, Polar, German Polar).
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented</exception>
      /// <remarks>
      /// This is only available for telescope InterfaceVersions 2 and 3
      /// </remarks>
      public AlignmentModes AlignmentMode
      {
         get
         {
            _Logger.LogMessage("AlignmentMode", "Get - " + _AlignmentMode.ToString());
            return _AlignmentMode;     // Set in Constructor
         }
      }

      /// <summary>
      /// The Altitude above the local horizon of the telescope's current position(degrees, positive up)
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented</exception>
      public double Altitude
      {
         get
         {
            double altitude = Settings.CurrentMountPosition.AltAzimuth.Altitude.Value;
            _Logger.LogMessage("Altitude", "Get - " + AscomTools.Util.DegreesToDMS(altitude));
            return altitude;
         }
      }

      /// <summary>
      /// The area of the telescope's aperture, taking into account any obstructions(square meters)
      /// </summary>
      /// <remarks>
      /// This is only available for telescope InterfaceVersions 2 and 3
      /// </remarks>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented</exception>
      [SuppressMessage("Microsoft.Design", "CA1065: Do not raise exceptions in unexpected locations")]
      public double ApertureArea
      {
         get
         {
            _Logger.LogMessage("ApertureArea Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("ApertureArea", false);
         }
      }

      /// <summary>
      /// The telescope's effective aperture diameter(meters)
      /// </summary>
      /// <remarks>
      /// This is only available for telescope InterfaceVersions 2 and 3
      /// </remarks>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented</exception>
      [SuppressMessage("Microsoft.Design", "CA1065: Do not raise exceptions in unexpected locations")]
      public double ApertureDiameter
      {
         get
         {
            _Logger.LogMessage("ApertureDiameter Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("ApertureDiameter", false);
         }
      }

      /// <summary>
      /// True if the telescope is stopped in the Home position. Set only following a <see cref="FindHome"></see> operation,
      ///  and reset with any slew operation. This property must be False if the telescope does not support homing. 
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// This is only available for telescope InterfaceVersions 2 and 3
      /// </remarks>
      public bool AtHome
      {
         get
         {
            _Logger.LogMessage("AtHome", "Get - " + false.ToString());
            return false;
         }
      }

      /// <summary>
      /// True if the telescope has been put into the parked state by the seee <see cref="Park" /> method. Set False by calling the Unpark() method.
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// <para>AtPark is True when the telescope is in the parked state. This is achieved by calling the <see cref="Park" /> method. When AtPark is true, 
      /// the telescope movement is stopped (or restricted to a small safe range of movement) and all calls that would cause telescope 
      /// movement (e.g. slewing, changing Tracking state) must not do so, and must raise an error.</para>
      /// <para>The telescope is taken out of parked state by calling the <see cref="UnPark" /> method. If the telescope cannot be parked, 
      /// then AtPark must always return False.</para>
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
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

      /// <summary>
      /// Determine the rates at which the telescope may be moved about the specified axis by the <see cref="MoveAxis" /> method.
      /// </summary>
      /// <param name="Axis">The axis about which rate information is desired (TelescopeAxes value)</param>
      /// <returns>Collection of <see cref="IRate" /> rate objects</returns>
      /// <exception cref="InvalidValueException">If an invalid Axis is specified.</exception>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a MethodNotImplementedException.</b></p>
      /// See the description of <see cref="MoveAxis" /> for more information. This method must return an empty collection if <see cref="MoveAxis" /> is not supported. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// <para>
      /// Please note that the rate objects must contain absolute non-negative values only. Applications determine the direction by applying a
      /// positive or negative sign to the rates provided. This obviates the need for the driver to to present a duplicate set of negative rates 
      /// as well as the positive rates.</para>
      /// </remarks>
      public IAxisRates AxisRates(TelescopeAxes Axis)
      {
         _Logger.LogMessage("AxisRates", "Get - " + Axis.ToString());
         return new AxisRates(Axis);
      }

      /// <summary>
      /// The azimuth at the local horizon of the telescope's current position(degrees, North-referenced, positive East/clockwise).
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented</exception>
      public double Azimuth
      {
         get
         {
            double azimuth = Settings.CurrentMountPosition.AltAzimuth.Azimuth.Value;
            _Logger.LogMessage("Azimuth", "Get - " + AscomTools.Util.DegreesToDMS(azimuth));
            return azimuth;
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

      /// <summary>
      /// True if this telescope can move the requested axis
      /// </summary>
      /// <param name="Axis">Primary, Secondary or Tertiary axis</param>
      /// <returns>Boolean indicating can or can not move the requested axis</returns>
      /// <exception cref="InvalidValueException">If an invalid Axis is specified.</exception>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a MethodNotImplementedException.</b></p>
      /// This is only available for telescope InterfaceVersions 2 and 3
      /// </remarks>
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

      /// <summary>
      /// True if this telescope is capable of programmed parking (<see cref="Park" />method)
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// May raise an error if the telescope is not connected. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      public bool CanPark
      {
         get
         {
            _Logger.LogMessage("CanPark", "Get - " + true.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return true;
         }
      }


      /// <summary>
      /// True if this telescope is capable of software-pulsed guiding (via the <see cref="PulseGuide" /> method)
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// May raise an error if the telescope is not connected. 
      /// </remarks>
      public bool CanPulseGuide
      {
         get
         {
            bool canPulseGuide = (Settings.PulseGuidingMode == PulseGuidingOption.ASCOM);
            _Logger.LogMessage("CanPulseGuide", "Get - " + canPulseGuide.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return canPulseGuide;
         }
      }

      /// <summary>
      /// True if the <see cref="DeclinationRate" /> property can be changed to provide offset tracking in the declination axis.
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// May raise an error if the telescope is not connected. 
      /// </remarks>
      public bool CanSetDeclinationRate
      {
         get
         {
            _Logger.LogMessage("CanSetDeclinationRate", "Get - " + true.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return true;
         }
      }

      /// <summary>
      /// True if the guide rate properties used for <see cref="PulseGuide" /> can ba adjusted.
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// May raise an error if the telescope is not connected. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      public bool CanSetGuideRates
      {
         get
         {
            bool canSetGuideRates = (Settings.PulseGuidingMode == PulseGuidingOption.ASCOM);
            _Logger.LogMessage("CanSetGuideRates", "Get - " + canSetGuideRates.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return canSetGuideRates;
         }
      }

      /// <summary>
      /// True if this telescope is capable of programmed setting of its park position (<see cref="SetPark" /> method)
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// May raise an error if the telescope is not connected. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      public bool CanSetPark
      {
         get
         {
            _Logger.LogMessage("CanSetPark", "Get - " + true.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return true;
         }
      }

      /// <summary>
      /// True if the <see cref="SideOfPier" /> property can be set, meaning that the mount can be forced to flip.
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// This will always return False for non-German-equatorial mounts that do not have to be flipped. 
      /// May raise an error if the telescope is not connected. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      public bool CanSetPierSide
      {
         get
         {
            _Logger.LogMessage("CanSetPierSide", "Get - " + false.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return false;
         }
      }

      /// <summary>
      /// True if the <see cref="RightAscensionRate" /> property can be changed to provide offset tracking in the right ascension axis.
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// May raise an error if the telescope is not connected. 
      /// </remarks>
      public bool CanSetRightAscensionRate
      {
         get
         {
            _Logger.LogMessage("CanSetRightAscensionRate", "Get - " + true.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return true;
         }
      }

      /// <summary>
      /// True if the <see cref="Tracking" /> property can be changed, turning telescope sidereal tracking on and off.
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// May raise an error if the telescope is not connected. 
      /// </remarks>
      public bool CanSetTracking
      {
         get
         {
            _Logger.LogMessage("CanSetTracking", "Get - " + true.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return true;
         }
      }

      /// <summary>
      /// True if this telescope is capable of programmed slewing (synchronous or asynchronous) to equatorial coordinates
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// If this is true, then only the synchronous equatorial slewing methods are guaranteed to be supported.
      /// See the <see cref="CanSlewAsync" /> property for the asynchronous slewing capability flag. 
      /// May raise an error if the telescope is not connected. 
      /// </remarks>
      public bool CanSlew
      {
         get
         {
            _Logger.LogMessage("CanSlew", "Get - " + true.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return true;
         }
      }

      /// <summary>
      /// True if this telescope is capable of programmed slewing (synchronous or asynchronous) to local horizontal coordinates
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// If this is true, then only the synchronous local horizontal slewing methods are guaranteed to be supported.
      /// See the <see cref="CanSlewAltAzAsync" /> property for the asynchronous slewing capability flag. 
      /// May raise an error if the telescope is not connected. 
      /// </remarks>
      public bool CanSlewAltAz
      {
         get
         {
            _Logger.LogMessage("CanSlewAltAz", "Get - " + false.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return false;
         }
      }

      /// <summary>
      /// True if this telescope is capable of programmed asynchronous slewing to local horizontal coordinates
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// This indicates the the asynchronous local horizontal slewing methods are supported.
      /// If this is True, then <see cref="CanSlewAltAz" /> will also be true. 
      /// May raise an error if the telescope is not connected. 
      /// </remarks>
      public bool CanSlewAltAzAsync
      {
         get
         {
            _Logger.LogMessage("CanSlewAltAzAsync", "Get - " + false.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return false;
         }
      }

      /// <summary>
      /// True if this telescope is capable of programmed asynchronous slewing to equatorial coordinates.
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// This indicates the the asynchronous equatorial slewing methods are supported.
      /// If this is True, then <see cref="CanSlew" /> will also be true.
      /// May raise an error if the telescope is not connected. 
      /// </remarks>
      public bool CanSlewAsync
      {
         get
         {
            _Logger.LogMessage("CanSlewAsync", "Get - " + true.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return true;
         }
      }

      /// <summary>
      /// True if this telescope is capable of programmed synching to equatorial coordinates.
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// May raise an error if the telescope is not connected. 
      /// </remarks>
      public bool CanSync
      {
         get
         {
            _Logger.LogMessage("CanSync", "Get - " + true.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return true;
         }
      }

      /// <summary>
      /// True if this telescope is capable of programmed synching to local horizontal coordinates
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// May raise an error if the telescope is not connected. 
      /// </remarks>
      public bool CanSyncAltAz
      {
         get
         {
            _Logger.LogMessage("CanSyncAltAz", "Get - " + false.ToString());
            if (!IsConnected) {
               throw new ASCOM.NotConnectedException();
            }
            return false;
         }
      }

      /// <summary>
      /// True if this telescope is capable of programmed unparking (<see cref="Unpark" /> method).
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// If this is true, then <see cref="CanPark" /> will also be true. May raise an error if the telescope is not connected.
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      public bool CanUnpark
      {
         get
         {
            _Logger.LogMessage("CanUnpark", "Get - " + true.ToString());
            return true;
         }
      }

      /// <summary>
      /// The declination (degrees) of the telescope's current equatorial coordinates, in the coordinate system given by the<see cref= "EquatorialSystem" /> property.
      /// Reading the property will raise an error if the value is unavailable. 
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// </remarks>
      public double Declination
      {
         get
         {
            // Note: If the CurrentMountPosition has not yet been calculated default
            // to NCP/SCP
            double declination = 90.0;
            if (Settings.CurrentMountPosition != null) {
               declination = Settings.CurrentMountPosition.Equatorial.Declination.Value;
            }
            else {
               if (Hemisphere == HemisphereOption.Southern) {
                  declination = -90.0;
               }
            }
            _Logger.LogMessage("Declination", "Get - " + AscomTools.Util.DegreesToDMS(declination, ":", ":"));
            return declination;
         }
      }

      private double _DecRateAdjust = 0.0;
      private double _DeclinationRate = 0.0;
      /// <summary>
      /// The declination tracking rate (arcseconds per second, default = 0.0)
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If DeclinationRate Write is not implemented.</exception>
      /// <remarks>
      /// <p style="color:red;margin-bottom:0"><b>DeclinationRate Read must be implemented and must not throw a PropertyNotImplementedException. </b></p>
      /// <p style="color:red;margin-top:0"><b>DeclinationRate Write can throw a PropertyNotImplementedException.</b></p>
      /// This property, together with <see cref="RightAscensionRate" />, provides support for "offset tracking".
      /// Offset tracking is used primarily for tracking objects that move relatively slowly against the equatorial coordinate system.
      /// It also may be used by a software guiding system that controls rates instead of using the <see cref="PulseGuide">PulseGuide</see> method. 
      /// <para>
      /// <b>NOTES:</b>
      /// <list type="bullet">
      /// <list></list>
      /// <item><description>The property value represents an offset from zero motion.</description></item>
      /// <item><description>If <see cref="CanSetDeclinationRate" /> is False, this property will always return 0.</description></item>
      /// <item><description>To discover whether this feature is supported, test the <see cref="CanSetDeclinationRate" /> property.</description></item>
      /// <item><description>The supported range of this property is telescope specific, however, if this feature is supported,
      /// it can be expected that the range is sufficient to allow correction of guiding errors caused by moderate misalignment 
      /// and periodic error.</description></item>
      /// <item><description>If this property is non-zero when an equatorial slew is initiated, the telescope should continue to update the slew 
      /// destination coordinates at the given offset rate.</description></item>
      /// <item><description>This will allow precise slews to a fast-moving target with a slow-slewing telescope.</description></item>
      /// <item><description>When the slew completes, the <see cref="TargetRightAscension" /> and <see cref="TargetDeclination" /> properties should reflect the final (adjusted) destination.</description></item>
      /// </list>
      /// </para>
      /// <para>
      ///This is not a required feature of this specification, however it is desirable. 
      /// </para>
      /// </remarks>
      public double DeclinationRate
      {
         get
         {
            _Logger.LogMessage("DeclinationRate", "Get - " + AscomTools.Util.DegreesToDMS(_DeclinationRate, ":", ":"));
            return _DeclinationRate;
         }
         set
         {
            _Logger.LogMessage("DeclinationRate", "Set" + AscomTools.Util.DegreesToDMS(value, ":", ":"));
            _DecRateAdjust = value;
            if (Settings.ParkStatus == ParkStatus.Unparked) {
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

      /// <summary>
      /// Predict side of pier for German equatorial mounts
      /// </summary>
      /// <param name="RightAscension">The destination right ascension (hours).</param>
      /// <param name="Declination">The destination declination (degrees, positive North).</param>
      /// <returns>The side of the pier on which the telescope would be on if a slew to the given equatorial coordinates is performed at the current instant of time.</returns>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented</exception>
      /// <exception cref="InvalidValueException">If an invalid RightAscension or Declination is specified.</exception>
      /// <remarks>
      /// This is only available for telescope InterfaceVersions 2 and 3
      /// </remarks>
      public DeviceInterface.PierSide DestinationSideOfPier(double RightAscension, double Declination)
      {
         _Logger.LogMessage("DestinationSideOfPier Get", "Not implemented");
         throw new ASCOM.PropertyNotImplementedException("DestinationSideOfPier", false);
      }

      /// <summary>
      /// True if the telescope or driver applies atmospheric refraction to coordinates.
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">Either read or write or both properties can throw PropertyNotImplementedException if not implemented</exception>
      /// <remarks>
      /// If this property is True, the coordinates sent to, and retrieved from, the telescope are unrefracted. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// <para>
      /// <b>NOTES:</b>
      /// <list type="bullet">
      /// <item><description>If the driver does not know whether the attached telescope does its own refraction, and if the driver does not itself calculate 
      /// refraction, this property (if implemented) must raise an error when read.</description></item>
      /// <item><description>Writing to this property is optional. Often, a telescope (or its driver) calculates refraction using standard atmospheric parameters.</description></item>
      /// <item><description>If the client wishes to calculate a more accurate refraction, then this property could be set to False and these 
      /// client-refracted coordinates used.</description></item>
      /// <item><description>If disabling the telescope or driver's refraction is not supported, the driver must raise an error when an attempt to set 
      /// this property to False is made.</description></item> 
      /// <item><description>Setting this property to True for a telescope or driver that does refraction, or to False for a telescope or driver that 
      /// does not do refraction, shall not raise an error. It shall have no effect.</description></item> 
      /// </list>
      /// </para>
      /// </remarks>
      [SuppressMessage("Microsoft.Design", "CA1065: Do not raise exceptions in unexpected locations")]
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

      /// <summary>
      /// True if this telescope is capable of programmed finding its home position (<see cref="FindHome" /> method).
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// May raise an error if the telescope is not connected. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      public void FindHome()
      {
         _Logger.LogMessage("FindHome", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("FindHome");
      }

      /// <summary>
      /// The telescope's focal length, meters
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented</exception>
      /// <remarks>
      /// This property may be used by clients to calculate telescope field of view and plate scale when combined with detector pixel size and geometry. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      [SuppressMessage("Microsoft.Design", "CA1065: Do not raise exceptions in unexpected locations")]
      public double FocalLength
      {
         get
         {
            _Logger.LogMessage("FocalLength Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("FocalLength", false);
         }
      }

      private double _GuideRateDeclination;
      /// <summary>
      /// The current Declination movement rate offset for telescope guiding (degrees/sec)
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented</exception>
      /// <exception cref="InvalidValueException">If an invalid guide rate is set.</exception>
      /// <remarks> 
      /// This is the rate for both hardware/relay guiding and the PulseGuide() method. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// <para>
      /// <b>NOTES:</b>
      /// <list type="bullet">
      /// <item><description>To discover whether this feature is supported, test the <see cref="CanSetGuideRates" /> property.</description></item> 
      /// <item><description>The supported range of this property is telescope specific, however, if this feature is supported, it can be expected that the range is sufficient to
      /// allow correction of guiding errors caused by moderate misalignment and periodic error.</description></item> 
      /// <item><description>If a telescope does not support separate guiding rates in Right Ascension and Declination, then it is permissible for <see cref="GuideRateRightAscension" /> and GuideRateDeclination to be tied together.
      /// In this case, changing one of the two properties will cause a change in the other.</description></item> 
      /// <item><description>Mounts must start up with a known or default declination guide rate, and this property must return that known/default guide rate until changed.</description></item> 
      /// </list>
      /// </para>
      /// </remarks>
      public double GuideRateDeclination
      {
         get
         {
            _Logger.LogMessage("GuideRateDeclination", "Get - " + _GuideRateDeclination.ToString());
            return _GuideRateDeclination;
         }

         set
         {
            _GuideRateDeclination = value;
            _Logger.LogMessage("GuideRateDeclination", "Set - " + _GuideRateDeclination.ToString());
         }
      }

      private double _GuideRateRightAscension;
      /// <summary>
      /// The current Right Ascension movement rate offset for telescope guiding (degrees/sec)
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented</exception>
      /// <exception cref="InvalidValueException">If an invalid guide rate is set.</exception>
      /// <remarks>
      /// This is the rate for both hardware/relay guiding and the PulseGuide() method. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// <para>
      /// <b>NOTES:</b>
      /// <list type="bullet">
      /// <item><description>To discover whether this feature is supported, test the <see cref="CanSetGuideRates" /> property.</description></item>  
      /// <item><description>The supported range of this property is telescope specific, however, if this feature is supported, it can be expected that the range is sufficient to allow correction of guiding errors caused by moderate
      /// misalignment and periodic error.</description></item>  
      /// <item><description>If a telescope does not support separate guiding rates in Right Ascension and Declination, then it is permissible for GuideRateRightAscension and <see cref="GuideRateDeclination" /> to be tied together. 
      /// In this case, changing one of the two properties will cause a change in the other.</description></item>  
      ///<item><description> Mounts must start up with a known or default right ascension guide rate, and this property must return that known/default guide rate until changed.</description></item>  
      /// </list>
      /// </para>
      /// </remarks>
      public double GuideRateRightAscension
      {
         get
         {
            _Logger.LogMessage("GuideRateRightAscension", "Get - " + _GuideRateRightAscension.ToString());
            return _GuideRateRightAscension;
         }
         set
         {
            _GuideRateRightAscension = value;
            _Logger.LogMessage("GuideRateRightAscension", "Set - " + _GuideRateRightAscension.ToString());
         }
      }

      private long _RAPulseDuration;
      private long _DecPulseDuration;
      /// <summary>
      /// True if a <see cref="PulseGuide" /> command is in progress, False otherwise
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If <see cref="CanPulseGuide" /> is False</exception>
      /// <remarks>
      /// Raises an error if the value of the <see cref="CanPulseGuide" /> property is false (the driver does not support the <see cref="PulseGuide" /> method). 
      /// </remarks>
      [SuppressMessage("Microsoft.Design", "CA1065: Do not raise exceptions in unexpected locations")]
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
      public void MoveAxis(TelescopeAxes axis, double rate)
      {
         lock (_Lock) {
            System.Diagnostics.Debug.WriteLine(String.Format("MoveAxis({0}, {1})", axis, rate));
            if (AtPark) {
               throw new ASCOM.ParkedException("Method MoveAxis");
            }
            if (rate > MountController.MAX_SLEW_SPEED) {
               throw new ASCOM.InvalidValueException("Method MoveAxis() rate exceed maximum allowed.");
            }
            _Logger.LogMessage("MoveAxis", string.Format("({0}, {1})", axis, rate));
            switch (axis) {
               case TelescopeAxes.axisPrimary:
                  InternalMoveAxis(AxisId.Axis1_RA, rate);
                  break;
               case TelescopeAxes.axisSecondary:
                  InternalMoveAxis(AxisId.Axis2_DEC, rate);
                  break;
               default:
                  throw new ASCOM.InvalidValueException("Tertiary axis is not supported by MoveAxis command");
            }
         }
      }

      /// <summary>
      /// Move the telescope to its park position, stop all motion (or restrict to a small safe range), and set <see cref="AtPark" /> to True.
      /// </summary>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanPark" /> is False</exception>
      /// <remarks>
      /// Raises an error if there is a problem communicating with the telescope or if parking fails. Parking should put the telescope into a state where its pointing accuracy 
      /// will not be lost if it is power-cycled (without moving it).Some telescopes must be power-cycled before unparking. Others may be unparked by simply calling the <see cref="UnPark" /> method.
      /// Calling this with <see cref="AtPark" /> = True does nothing (harmless) 
      /// </remarks>
      public void Park()
      {
         _Logger.LogMessage("Command", "Park");
         if (Settings.ParkStatus == ParkStatus.Unparked) {
            lock (_Lock) {
               Settings.ParkStatus = ParkStatus.Parking;
               if (Settings.AscomCompliance.UseSynchronousParking) {
                  ParkScope();
               }
               else {
                  ParkScopeAsync();
               }
               Settings.ParkStatus = ParkStatus.Parked;
            }
         }

         // TODO: Properly implement park
      }

      /// <summary>
      /// Moves the scope in the given direction for the given interval or time at 
      /// the rate given by the corresponding guide rate property 
      /// </summary>
      /// <param name="Direction">The direction in which the guide-rate motion is to be made</param>
      /// <param name="Duration">The duration of the guide-rate motion (milliseconds)</param>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanPulseGuide" /> is False</exception>
      /// <exception cref="InvalidValueException">If an invalid direction or duration is given.</exception>
      /// <remarks>
      /// This method returns immediately if the hardware is capable of back-to-back moves,
      /// i.e. dual-axis moves. For hardware not having the dual-axis capability,
      /// the method returns only after the move has completed. 
      /// <para>
      /// <b>NOTES:</b>
      /// <list type="bullet">
      /// <item><description>Raises an error if <see cref="AtPark" /> is true.</description></item>
      /// <item><description>The <see cref="IsPulseGuiding" /> property must be be True during pulse-guiding.</description></item>
      /// <item><description>The rate of motion for movements about the right ascension axis is 
      /// specified by the <see cref="GuideRateRightAscension" /> property. The rate of motion
      /// for movements about the declination axis is specified by the 
      /// <see cref="GuideRateDeclination" /> property. These two rates may be tied together
      /// into a single rate, depending on the driver's implementation
      /// and the capabilities of the telescope.</description></item>
      /// </list>
      /// </para>
      /// </remarks>
      public void PulseGuide(GuideDirections Direction, int Duration)
      {
         _Logger.LogMessage("PulseGuide", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("PulseGuide");
      }

      /// <summary>
      /// The right ascension (hours) of the telescope's current equatorial coordinates,
      /// in the coordinate system given by the EquatorialSystem property
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// Reading the property will raise an error if the value is unavailable. 
      /// </remarks>
      public double RightAscension
      {
         get
         {
            double rightAscension = 0.0;
            if (Settings.CurrentMountPosition != null) {
               rightAscension = Settings.CurrentMountPosition.Equatorial.RightAscension.Value;
            }
            else {
               // Assume we are looking at the NCP or SCP.
               if (Hemisphere == HemisphereOption.Northern) {
                  rightAscension = SiderealTime + 12.0;
               }
               else {
                  rightAscension = SiderealTime - 12.0;
               }
            }
            _Logger.LogMessage("RightAscension", "Get - " + AscomTools.Util.HoursToHMS(rightAscension));
            return rightAscension;
         }
      }

      private double _RaRateAdjust = 0.0;
      private double _RightAscensionRate;

      /// <summary>
      /// The right ascension tracking rate offset from sidereal (seconds per sidereal second, default = 0.0)
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If RightAscensionRate Write is not implemented.</exception>
      /// <exception cref="InvalidValueException">If an invalid drive rate is set.</exception>
      /// <exception cref="InvalidValueException">If an invalid rate is set.</exception>
      /// <remarks>
      /// <p style="color:red;margin-bottom:0"><b>RightAscensionRate Read must be implemented and must not throw a PropertyNotImplementedException. </b></p>
      /// <p style="color:red;margin-top:0"><b>RightAscensionRate Write can throw a PropertyNotImplementedException.</b></p>
      /// This property, together with <see cref="DeclinationRate" />, provides support for "offset tracking". Offset tracking is used primarily for tracking objects that move relatively slowly
      /// against the equatorial coordinate system. It also may be used by a software guiding system that controls rates instead of using the <see cref="PulseGuide">PulseGuide</see> method.
      /// <para>
      /// <b>NOTES:</b>
      /// The property value represents an offset from the currently selected <see cref="TrackingRate" />. 
      /// <list type="bullet">
      /// <item><description>If this property is zero, tracking will be at the selected <see cref="TrackingRate" />.</description></item>
      /// <item><description>If <see cref="CanSetRightAscensionRate" /> is False, this property must always return 0.</description></item> 
      /// To discover whether this feature is supported, test the <see cref="CanSetRightAscensionRate" />property. 
      /// <item><description>The property value is in in seconds of right ascension per sidereal second.</description></item> 
      /// <item><description>To convert a given rate in (the more common) units of sidereal seconds per UTC (clock) second, multiply the value by 0.9972695677 
      /// (the number of UTC seconds in a sidereal second) then set the property. Please note that these units were chosen for the Telescope V1 standard,
      /// and in retrospect, this was an unfortunate choice. However, to maintain backwards compatibility, the units cannot be changed.
      /// A simple multiplication is all that's needed, as noted.The supported range of this property is telescope specific, however,
      /// if this feature is supported, it can be expected that the range is sufficient to allow correction of guiding errors
      /// caused by moderate misalignment and periodic error. </description></item>
      /// <item><description>If this property is non-zero when an equatorial slew is initiated, the telescope should continue to update the slew destination coordinates 
      /// at the given offset rate. This will allow precise slews to a fast-moving target with a slow-slewing telescope. When the slew completes, 
      /// the <see cref="TargetRightAscension" /> and <see cref="TargetDeclination" /> properties should reflect the final (adjusted) destination. This is not a required
      /// feature of this specification, however it is desirable. </description></item>
      /// <item><description>Use the <see cref="Tracking" /> property to enable and disable sidereal tracking (if supported). </description></item>
      /// </list>
      /// </para>
      /// </remarks>
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
            _Logger.LogMessage("RightAscensionRate", "Set - " + AscomTools.Util.DegreesToDMS(value, ":", ":"));
            _RaRateAdjust = value;
            // don't action this if we're parked!
            if (Settings.ParkStatus == ParkStatus.Unparked) {
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

      /// <summary>
      /// Sets the telescope's park position to be its current position.
      /// </summary>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanPark" /> is False</exception>
      public void SetPark()
      {
         _Logger.LogMessage("SetPark", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SetPark");
      }

      /// <summary>
      /// Indicates the pointing state of the mount.
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented.</exception>
      /// <exception cref="InvalidValueException">If an invalid side of pier is set.</exception>
      /// <remarks>
      /// <para>For historical reasons, this property's name does not reflect its true meaning.The name will not be changed(so as to preserve 
      /// compatibility), but the meaning has since become clear. All conventional mounts have two pointing states for a given equatorial (sky) position. 
      /// Mechanical limitations often make it impossible for the mount to position the optics at given HA/Dec in one of the two pointing 
      /// states, but there are places where the same point can be reached sensibly in both pointing states (e.g. near the pole and 
      /// close to the meridian). In order to understand these pointing states, consider the following (thanks to Patrick Wallace for this info):</para>
      /// <para>All conventional telescope mounts have two axes nominally at right angles. For an equatorial, the longitude axis is mechanical 
      /// hour angle and the latitude axis is mechanical declination. Sky coordinates and mechanical coordinates are two completely separate arenas. 
      /// This becomes rather more obvious if your mount is an altaz, but it's still true for an equatorial. Both mount axes can in principle
      /// move over a range of 360 deg. This is distinct from sky HA/Dec, where Dec is limited to a 180 deg range (+90 to -90).  Apart from 
      /// practical limitations, any point in the sky can be seen in two mechanical orientations. To get from one to the other the HA axis 
      /// is moved 180 deg and the Dec axis is moved through the pole a distance twice the sky codeclination (90 - sky declination).</para>
      /// <para>Mechanical zero HA/Dec will be one of the two ways of pointing at the intersection of the celestial equator and the local meridian. 
      /// In order to support Dome slaving, where it is important to know which side of the pier the mount is actually on, ASCOM has adopted the 
      /// convention that the Normal pointing state will be the state where a German Equatorial mount is on the East side of the pier, looking West, with the 
      /// counterweights below the optical assembly and that <see cref="PierSide.pierEast"></see> will represent this pointing state.</para>
      /// <para>Move your scope to this position and consider the two mechanical encoders zeroed. The two pointing states are, then:
      /// <list type="table">
      /// <item><term><b>Normal (<see cref="PierSide.pierEast"></see>)</b></term><description>Where the mechanical Dec is in the range -90 deg to +90 deg</description></item>
      /// <item><term><b>Beyond the pole (<see cref="PierSide.pierWest"></see>)</b></term><description>Where the mechanical Dec is in the range -180 deg to -90 deg or +90 deg to +180 deg.</description></item>
      /// </list>
      /// </para>
      /// <para>"Side of pier" is a "consequence" of the former definition, not something fundamental. 
      /// Apart from mechanical interference, the telescope can move from one side of the pier to the other without the mechanical Dec 
      /// having changed: you could track Polaris forever with the telescope moving from west of pier to east of pier or vice versa every 12h. 
      /// Thus, "side of pier" is, in general, not a useful term (except perhaps in a loose, descriptive, explanatory sense). 
      /// All this applies to a fork mount just as much as to a GEM, and it would be wrong to make the "beyond pole" state illegal for the 
      /// former. Your mount may not be able to get there if your camera hits the fork, but it's possible on some mounts.Whether this is useful
      /// depends on whether you're in Hawaii or Finland.</para>
      /// <para>To first order, the relationship between sky and mechanical HA/Dec is as follows:</para>
      /// <para><b>Normal state:</b>
      /// <list type="bullet">
      /// <item><description>HA_sky  = HA_mech</description></item>
      /// <item><description>Dec_sky = Dec_mech</description></item>
      /// </list>
      /// </para>
      /// <para><b>Beyond the pole</b>
      /// <list type="bullet">
      /// <item><description>HA_sky  = HA_mech + 12h, expressed in range ± 12h</description></item>
      /// <item><description>Dec_sky = 180d - Dec_mech, expressed in range ± 90d</description></item>
      /// </list>
      /// </para>
      /// <para>Astronomy software often needs to know which which pointing state the mount is in. Examples include setting guiding polarities 
      /// and calculating dome opening azimuth/altitude. The meaning of the SideOfPier property, then is:
      /// <list type="table">
      /// <item><term><b>pierEast</b></term><description>Normal pointing state</description></item>
      /// <item><term><b>pierWest</b></term><description>Beyond the pole pointing state</description></item>
      /// </list>
      /// </para>
      /// <para>If the mount hardware reports neither the true pointing state (or equivalent) nor the mechanical declination axis position 
      /// (which varies from -180 to +180), a driver cannot calculate the pointing state, and *must not* implement SideOfPier.
      /// If the mount hardware reports only the mechanical declination axis position (-180 to +180) then a driver can calculate SideOfPier as follows:
      /// <list type="bullet">
      /// <item><description>pierEast = abs(mechanical dec) &lt;= 90 deg</description></item>
      /// <item><description>pierWest = abs(mechanical Dec) &gt; 90 deg</description></item>
      /// </list>
      /// </para>
      /// <para>It is allowed (though not required) that this property may be written to force the mount to flip. Doing so, however, may change 
      /// the right ascension of the telescope. During flipping, Telescope.Slewing must return True.</para>
      /// <para>This property is only available in telescope InterfaceVersions 2 and 3.</para>
      /// <para><b>Pointing State and Side of Pier - Help for Driver Developers</b></para>
      /// <para>A further document, "Pointing State and Side of Pier", is installed in the Developer Documentation folder by the ASCOM Developer 
      /// Components installer. This further explains the pointing state concept and includes diagrams illustrating how it relates 
      /// to physical side of pier for German equatorial telescopes. It also includes details of the tests performed by Conform to determine whether 
      /// the driver correctly reports the pointing state as defined above.</para>
      /// </remarks>
      public DeviceInterface.PierSide SideOfPier
      {
         get
         {
            PierSide value = PierSide.pierUnknown;
            switch (Settings.AscomCompliance.SideOfPier) {
               case SideOfPierOption.Pointing:
                  value = SOP_Pointing(Settings.CurrentMountPosition.ObservedAxes.DecAxis.Radians);
                  break;
               case SideOfPierOption.Physical:
                  value = SOP_Physical(Settings.CurrentMountPosition.Equatorial.RightAscension);
                  break;
               case SideOfPierOption.None:
                  value = PierSide.pierUnknown;
                  break;
               case SideOfPierOption.V124g:
                  value = SOP_Dec(Settings.CurrentMountPosition.ObservedAxes.DecAxis.Radians);
                  break;
            }
            _Logger.LogMessage("SideOfPier", "Get - " + value.ToString());
            return value;
         }
         set
         {
            _Logger.LogMessage("SideOfPier Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SideOfPier", true);
         }
      }

      /// <summary>
      /// The local apparent sidereal time from the telescope's internal clock(hours, sidereal)
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented, must not throw a PropertyNotImplementedException.</b></p>
      /// It is required for a driver to calculate this from the system clock if the telescope 
      /// has no accessible source of sidereal time. Local Apparent Sidereal Time is the sidereal 
      /// time used for pointing telescopes, and thus must be calculated from the Greenwich Mean
      /// Sidereal time, longitude, nutation in longitude and true ecliptic obliquity. 
      /// </remarks>
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

      private double _SiteElevation = double.MinValue;
      /// <summary>
      /// The elevation above mean sea level (meters) of the site at which the telescope is located
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented.</exception>
      /// <exception cref="InvalidValueException">If an invalid elevation is set.</exception>
      /// <exception cref="InvalidOperationException">If the application must set the elevation before reading it, but has not.</exception>
      /// <remarks>
      /// Setting this property will raise an error if the given value is outside the range -300 through +10000 metres.
      /// Reading the property will raise an error if the value has never been set or is otherwise unavailable. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      [SuppressMessage("Microsoft.Design", "CA1065: Do not raise exceptions in unexpected locations")]
      public double SiteElevation
      {
         get
         {
            _Logger.LogMessage("SiteElevation", "Get - " + _SiteElevation.ToString());
            if (_SiteElevation == double.MinValue) {
               throw new ASCOM.InvalidOperationException("SiteElevation must be set before it can be read.");
            }
            return _SiteElevation;
         }
         set
         {
            _Logger.LogMessage("SiteElevation", "Set - " + value.ToString());
            if (value == _SiteElevation) {
               return;
            }
            if (value < -300 || value > 10000) {
               throw new ASCOM.InvalidValueException("SiteElevation Get", value.ToString(), "-300 to 10000");
            }
            else {
               _SiteElevation = value;
               AscomTools.Transform.SiteElevation = value;
               if (AscomTools.Transform.IsInitialised()) {
                  Settings.CurrentMountPosition.Refresh(AscomTools.Transform, AscomTools.LocalJulianTimeUTC);
               }
            }
         }
      }

      private double _SiteLatitude = double.MinValue;
      /// <summary>
      /// The geodetic(map) latitude (degrees, positive North, WGS84) of the site at which the telescope is located.
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented.</exception>
      /// <exception cref="InvalidValueException">If an invalid latitude is set.</exception>
      /// <exception cref="InvalidOperationException">If the application must set the latitude before reading it, but has not.</exception>
      /// <remarks>
      /// Setting this property will raise an error if the given value is outside the range -90 to +90 degrees.
      /// Reading the property will raise an error if the value has never been set or is otherwise unavailable. 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      [SuppressMessage("Microsoft.Design", "CA1065: Do not raise exceptions in unexpected locations")]
      public double SiteLatitude
      {
         get
         {
            _Logger.LogMessage("SiteLatitude", "Get - " + _SiteLatitude.ToString());
            if (_SiteLatitude == double.MinValue) {
               throw new ASCOM.InvalidOperationException("SiteLatitude must be set before it can be read.");
            }
            return _SiteLatitude;
         }
         set
         {
            _Logger.LogMessage("SiteLatitude", "Set - " + value.ToString());
            if (value == _SiteLatitude) {
               return;
            }
            if (value < -90 || value > 90) {
               throw new ASCOM.InvalidValueException("SiteLatitude Get", value.ToString(), "-90  to 90");
            }
            else {
               _SiteLatitude = value;
               AscomTools.Transform.SiteLatitude = value;
               if (AscomTools.Transform.IsInitialised()) {
                  Settings.CurrentMountPosition.Refresh(AscomTools.Transform, AscomTools.LocalJulianTimeUTC);
               }
            }
         }
      }

      private double _SiteLongitude = double.MinValue;
      /// <summary>
      /// The longitude (degrees, positive East, WGS84) of the site at which the telescope is located.
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented.</exception>
      /// <exception cref="InvalidValueException">If an invalid longitude is set.</exception>
      /// <exception cref="InvalidOperationException">If the application must set the longitude before reading it, but has not.</exception>
      /// <remarks>
      /// Setting this property will raise an error if the given value is outside the range -180 to +180 degrees.
      /// Reading the property will raise an error if the value has never been set or is otherwise unavailable.
      /// Note that West is negative! 
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      [SuppressMessage("Microsoft.Design", "CA1065: Do not raise exceptions in unexpected locations")]
      public double SiteLongitude
      {
         get
         {
            _Logger.LogMessage("SiteLongitude", "Get - " + _SiteLongitude.ToString());
            if (_SiteLongitude == double.MinValue) {
               throw new ASCOM.InvalidOperationException("SiteLongitude must be set before it can be read.");
            }
            return _SiteLongitude;
         }
         set
         {
            _Logger.LogMessage("SiteLongitude", "Set - " + value.ToString());
            if (value == _SiteLongitude) {
               return;
            }
            if (value < -180 || value > 180) {
               throw new ASCOM.InvalidValueException("SiteLongitude Get", value.ToString(), "-180 to 180");
            }
            else {
               _SiteLongitude = value;
               AscomTools.Transform.SiteLongitude = value;
               if (AscomTools.Transform.IsInitialised()) {
                  Settings.CurrentMountPosition.Refresh(AscomTools.Transform, AscomTools.LocalJulianTimeUTC);
               }
            }
         }
      }

      private short _SlewSettleTime = 0;
      /// <summary>
      /// Specifies a post-slew settling time (sec.).
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented.</exception>
      /// <exception cref="InvalidValueException">If an invalid settle time is set.</exception>
      /// <remarks>
      /// Adds additional time to slew operations. Slewing methods will not return, 
      /// and the <see cref="Slewing" /> property will not become False, until the slew completes and the SlewSettleTime has elapsed.
      /// This feature (if supported) may be used with mounts that require extra settling time after a slew. 
      /// </remarks>
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

      /// <summary>
      /// Move the telescope to the given local horizontal coordinates, return when slew is complete
      /// </summary>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanSlewAltAz" /> is False</exception>
      /// <exception cref="InvalidValueException">If an invalid azimuth or elevation is given.</exception>
      /// <remarks>
      /// This Method must be implemented if <see cref="CanSlewAltAz" /> returns True. Raises an error if the slew fails. The slew may fail if the target coordinates are beyond limits imposed within the driver component.
      /// Such limits include mechanical constraints imposed by the mount or attached instruments, building or dome enclosure restrictions, etc.
      /// <para>The <see cref="TargetRightAscension" /> and <see cref="TargetDeclination" /> properties are not changed by this method. 
      /// Raises an error if <see cref="AtPark" /> is True, or if <see cref="Tracking" /> is True. This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      /// <param name="Azimuth">Target azimuth (degrees, North-referenced, positive East/clockwise).</param>
      /// <param name="Altitude">Target altitude (degrees, positive up)</param>
      public void SlewToAltAz(double Azimuth, double Altitude)
      {
         _Logger.LogMessage("SlewToAltAz", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToAltAz");
      }

      /// <summary>
      /// This Method must be implemented if <see cref="CanSlewAltAzAsync" /> returns True.
      /// </summary>
      /// <param name="Azimuth">Azimuth to which to move</param>
      /// <param name="Altitude">Altitude to which to move to</param>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanSlewAltAzAsync" /> is False</exception>
      /// <exception cref="InvalidValueException">If an invalid azimuth or elevation is given.</exception>
      /// <remarks>
      /// This method should only be implemented if the properties <see cref="Altitude" />, <see cref="Azimuth" />,
      /// <see cref="RightAscension" />, <see cref="Declination" /> and <see cref="Slewing" /> can be read while the scope is slewing. Raises an error if starting the slew fails. Returns immediately after starting the slew.
      /// The client may monitor the progress of the slew by reading the <see cref="Azimuth" />, <see cref="Altitude" />, and <see cref="Slewing" /> properties during the slew. When the slew completes, Slewing becomes False. 
      /// The slew may fail if the target coordinates are beyond limits imposed within the driver component. Such limits include mechanical constraints imposed by the mount or attached instruments, building or dome enclosure restrictions, etc. 
      /// The <see cref="TargetRightAscension" /> and <see cref="TargetDeclination" /> properties are not changed by this method. 
      /// <para>Raises an error if <see cref="AtPark" /> is True, or if <see cref="Tracking" /> is True.</para>
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
      public void SlewToAltAzAsync(double Azimuth, double Altitude)
      {
         _Logger.LogMessage("SlewToAltAzAsync", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToAltAzAsync");
      }

      /// <summary>
      /// Move the telescope to the given equatorial coordinates, return when slew is complete
      /// </summary>
      /// <exception cref="InvalidValueException">If an invalid right ascension or declination is given.</exception>
      /// <param name="RightAscension">The destination right ascension (hours). Copied to <see cref="TargetRightAscension" />.</param>
      /// <param name="Declination">The destination declination (degrees, positive North). Copied to <see cref="TargetDeclination" />.</param>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanSlew" /> is False</exception>
      /// <remarks>
      /// This Method must be implemented if <see cref="CanSlew" /> returns True. Raises an error if the slew fails. 
      /// The slew may fail if the target coordinates are beyond limits imposed within the driver component.
      /// Such limits include mechanical constraints imposed by the mount or attached instruments,
      /// building or dome enclosure restrictions, etc. The target coordinates are copied to
      /// <see cref="TargetRightAscension" /> and <see cref="TargetDeclination" /> whether or not the slew succeeds. 
      /// <para>Raises an error if <see cref="AtPark" /> is True, or if <see cref="Tracking" /> is False.</para>
      /// </remarks>
      public void SlewToCoordinates(double RightAscension, double Declination)
      {
         _Logger.LogMessage("SlewToCoordinates", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToCoordinates");
      }

      /// <summary>
      /// Move the telescope to the given equatorial coordinates, return immediately after starting the slew.
      /// </summary>
      /// <param name="RightAscension">The destination right ascension (hours). Copied to <see cref="TargetRightAscension" />.</param>
      /// <param name="Declination">The destination declination (degrees, positive North). Copied to <see cref="TargetDeclination" />.</param>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanSlewAsync" /> is False</exception>
      /// <exception cref="InvalidValueException">If an invalid right ascension or declination is given.</exception>
      /// <remarks>
      /// This method must be implemented if <see cref="CanSlewAsync" /> returns True. Raises an error if starting the slew failed. 
      /// Returns immediately after starting the slew. The client may monitor the progress of the slew by reading
      /// the <see cref="RightAscension" />, <see cref="Declination" />, and <see cref="Slewing" /> properties during the slew. When the slew completes,
      /// <see cref="Slewing" /> becomes False. The slew may fail to start if the target coordinates are beyond limits
      /// imposed within the driver component. Such limits include mechanical constraints imposed
      /// by the mount or attached instruments, building or dome enclosure restrictions, etc. 
      /// <para>The target coordinates are copied to <see cref="TargetRightAscension" /> and <see cref="TargetDeclination" />
      /// whether or not the slew succeeds. 
      /// Raises an error if <see cref="AtPark" /> is True, or if <see cref="Tracking" /> is False.</para>
      /// </remarks>
      public void SlewToCoordinatesAsync(double RightAscension, double Declination)
      {
         _Logger.LogMessage("Command", String.Format("SlewToCoordinatesAsync({0}, {1})", RightAscension, Declination));
         if (Settings.ParkStatus == ParkStatus.Unparked) {
            if (!Settings.AscomCompliance.SlewWithTrackingOff && !Tracking) {
               throw new ASCOM.InvalidOperationException("SlewToCoordinateAsyc() RaDec slew is not permittted if mount is not Tracking.");
            }
            else {
               if (ValidateRADEC(RightAscension, Declination)) {
                  RADecAsyncSlew(RightAscension, Declination);
                  // TODO: EQ_Beep(20)
               }
               else {
                  throw new ASCOM.InvalidValueException("SlewToCoordinates() a property value is out of range.");
               }
            }
         }
         else {
            // TODO: HC.Add_Message(oLangDll.GetLangString(5000))
            throw new ASCOM.InvalidOperationException("SlewToCoordinateAsyc() is not permitted while mount is parked or parking.");
         }
      }

      /// <summary>
      /// Move the telescope to the <see cref="TargetRightAscension" /> and <see cref="TargetDeclination" /> coordinates, return when slew complete.
      /// </summary>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanSlew" /> is False</exception>
      /// <remarks>
      /// This Method must be implemented if <see cref="CanSlew" /> returns True. Raises an error if the slew fails. 
      /// The slew may fail if the target coordinates are beyond limits imposed within the driver component.
      /// Such limits include mechanical constraints imposed by the mount or attached
      /// instruments, building or dome enclosure restrictions, etc. 
      /// Raises an error if <see cref="AtPark" /> is True, or if <see cref="Tracking" /> is False. 
      /// </remarks>
      public void SlewToTarget()
      {
         _Logger.LogMessage("SlewToTarget", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToTarget");
      }

      /// <summary>
      /// Move the telescope to the <see cref="TargetRightAscension" /> and <see cref="TargetDeclination" />  coordinates,
      /// returns immediately after starting the slew.
      /// </summary>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanSlewAsync" /> is False</exception>
      /// <remarks>
      /// This Method must be implemented if  <see cref="CanSlewAsync" /> returns True.
      /// Raises an error if starting the slew failed. Returns immediately after starting the slew. The client may monitor the progress of the slew by reading the RightAscension, Declination,
      /// and Slewing properties during the slew. When the slew completes,  <see cref="Slewing" /> becomes False. The slew may fail to start if the target coordinates are beyond limits imposed within 
      /// the driver component. Such limits include mechanical constraints imposed by the mount or attached instruments, building or dome enclosure restrictions, etc. 
      /// Raises an error if <see cref="AtPark" /> is True, or if <see cref="Tracking" /> is False. 
      /// </remarks>
      public void SlewToTargetAsync()
      {
         _Logger.LogMessage("SlewToTargetAsync", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToTargetAsync");
      }

      bool _IsSlewing;
      bool _IsMoveAxisSlewing;
      /// <summary>
      /// True if telescope is currently moving in response to one of the
      /// Slew methods or the <see cref="MoveAxis" /> method, False at all other times.
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented.</exception>
      /// <remarks>
      /// Reading the property will raise an error if the value is unavailable. If the telescope is not capable of asynchronous slewing, this property will always be False. 
      /// The definition of "slewing" excludes motion caused by sidereal tracking, <see cref="PulseGuide">PulseGuide</see>, <see cref="RightAscensionRate" />, and <see cref="DeclinationRate" />.
      /// It reflects only motion caused by one of the Slew commands, flipping caused by changing the <see cref="SideOfPier" /> property, or <see cref="MoveAxis" />. 
      /// </remarks>
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

      /// <summary>
      /// Matches the scope's local horizontal coordinates to the given local horizontal coordinates.
      /// </summary>
      /// <param name="Azimuth">Target azimuth (degrees, North-referenced, positive East/clockwise)</param>
      /// <param name="Altitude">Target altitude (degrees, positive up)</param>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanSyncAltAz" /> is False</exception>
      /// <exception cref="InvalidValueException">If an invalid azimuth or altitude is given.</exception>
      /// <remarks>
      /// This must be implemented if the <see cref="CanSyncAltAz" /> property is True. Raises an error if matching fails. 
      /// <para>Raises an error if <see cref="AtPark" /> is True, or if <see cref="Tracking" /> is True.</para>
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
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


      /// <summary>
      /// Matches the scope's equatorial coordinates to the given equatorial coordinates.
      /// </summary>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanSync" /> is False</exception>
      /// <remarks>
      /// This must be implemented if the <see cref="CanSync" /> property is True. Raises an error if matching fails. 
      /// Raises an error if <see cref="AtPark" /> AtPark is True, or if <see cref="Tracking" /> is False. 
      /// The way that Sync is implemented is mount dependent and it should only be relied on to improve pointing for positions close to
      /// the position at which the sync is done.
      /// </remarks>
      public void SyncToTarget()
      {
         _Logger.LogMessage("SyncToTarget", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SyncToTarget");
      }

      private double? _TargetDeclination;
      /// <summary>
      /// The declination (degrees, positive North) for the target of an equatorial slew or sync operation
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented.</exception>
      /// <exception cref="InvalidValueException">If an invalid declination is set.</exception>
      /// <exception cref="InvalidOperationException">If the property is read before being set for the first time.</exception>
      /// <remarks>
      /// Setting this property will raise an error if the given value is outside the range -90 to +90 degrees. Reading the property will raise an error if the value has never been set or is otherwise unavailable. 
      /// </remarks>
      [SuppressMessage("Microsoft.Design", "CA1065: Do not raise exceptions in unexpected locations")]
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
         }
      }

      private double? _TargetRightAscension;
      /// <summary>
      /// The right ascension (hours) for the target of an equatorial slew or sync operation
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If the property is not implemented.</exception>
      /// <exception cref="InvalidValueException">If an invalid right ascension is set.</exception>
      /// <exception cref="InvalidOperationException">If the property is read before being set for the first time.</exception>
      /// <remarks>
      /// Setting this property will raise an error if the given value is outside the range 0 to 24 hours. Reading the property will raise an error if the value has never been set or is otherwise unavailable. 
      /// </remarks>
      [SuppressMessage("Microsoft.Design", "CA1065: Do not raise exceptions in unexpected locations")]
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
         }
      }

      /// <summary>
      /// The state of the telescope's sidereal tracking drive.
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If Tracking Write is not implemented.</exception>
      /// <remarks>
      /// <p style="color:red;margin-bottom:0"><b>Tracking Read must be implemented and must not throw a PropertyNotImplementedException. </b></p>
      /// <p style="color:red;margin-top:0"><b>Tracking Write can throw a PropertyNotImplementedException.</b></p>
      /// Changing the value of this property will turn the sidereal drive on and off.
      /// However, some telescopes may not support changing the value of this property
      /// and thus may not support turning tracking on and off.
      /// See the <see cref="CanSetTracking" /> property. 
      /// </remarks>
      public bool Tracking
      {
         get
         {
            bool tracking = (TrackingState != TrackingStatus.Off);
            _Logger.LogMessage("Tracking", "Get - " + tracking.ToString());
            return tracking;
         }
         set
         {
            _Logger.LogMessage("Tracking", "Set - " + value.ToString());
            if (Settings.ParkStatus == ParkStatus.Unparked || (Settings.ParkStatus == ParkStatus.Parked && value)) {
               if (value) {
                  if (RateAdjustment[0] == 0 && RateAdjustment[1] == 0) {
                     // track at sidereal
                     StartSiderealTracking(true);
                     EmulatorOneShot = true;                 //  Get One shot cap
                  }
                  else {
                     // track at custom rate
                     LastPECRate = 0;
                     _DeclinationRate = RateAdjustment[1];
                     _RightAscensionRate = Core.Constants.SIDEREAL_RATE_ARCSECS + RateAdjustment[0];
                     if (PECEnabled) {
                        PECStopTracking();
                     }
                     // Call CustomMoveAxis(0, gRightAscensionRate, True, oLangDll.GetLangString(189))
                     // Call CustomMoveAxis(1, gDeclinationRate, True, oLangDll.GetLangString(189))
                  }
               }
               else {
                  _Mount.EQ_MotorStop(AxisId.Both_Axes);
                  // EQ_Beep(7)
                  TrackingState = TrackingStatus.Off;
                  // not sure that we should be clearing the rate offests ASCOM Spec is no help
                  _DeclinationRate = 0;
                  _RightAscensionRate = 0;
                  // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(178)
               }
            }
            else {
               // HC.Add_Message(oLangDll.GetLangString(5013))
               throw new ASCOM.ParkedException("Tracking change not allowed when mount is parked.");
            }
         }
      }

      private DriveRates _TrackingRate;
      /// <summary>
      /// The current tracking rate of the telescope's sidereal drive
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If TrackingRate Write is not implemented.</exception>
      /// <exception cref="InvalidValueException">If an invalid drive rate is set.</exception>
      /// <remarks>
      /// <p style="color:red;margin-bottom:0"><b>TrackingRate Read must be implemented and must not throw a PropertyNotImplementedException. </b></p>
      /// <p style="color:red;margin-top:0"><b>TrackingRate Write can throw a PropertyNotImplementedException.</b></p>
      /// Supported rates (one of the <see cref="DriveRates" />  values) are contained within the <see cref="TrackingRates" /> collection.
      /// Values assigned to TrackingRate must be one of these supported rates. If an unsupported value is assigned to this property, it will raise an error. 
      /// The currently selected tracking rate can be further adjusted via the <see cref="RightAscensionRate" /> and <see cref="DeclinationRate" /> properties. These rate offsets are applied to the currently 
      /// selected tracking rate. Mounts must start up with a known or default tracking rate, and this property must return that known/default tracking rate until changed.
      /// <para>If the mount's current tracking rate cannot be determined(for example, it is a write-only property of the mount's protocol), 
      /// it is permitted for the driver to force and report a default rate on connect. In this case, the preferred default is Sidereal rate.</para>
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
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
            if (Tracking && value == _TrackingRate) {
               return;
            }
            switch (value) {
               case DriveRates.driveSidereal:
                  StartSiderealTracking(false);
                  break;
               case DriveRates.driveLunar:
                  StartLunarTracking(false);
                  break;
               case DriveRates.driveSolar:
                  StartSolarTracking(false);
                  break;
               default:
                  throw new ASCOM.InvalidValueException("TrackingRate");
            }
         }
      }

      private ITrackingRates _TrackingRates;
      /// <summary>
      /// Returns a collection of supported <see cref="DriveRates" /> values that describe the permissible
      /// values of the <see cref="TrackingRate" /> property for this telescope type.
      /// </summary>
      /// <remarks>
      /// <p style="color:red"><b>Must be implemented and must not throw a PropertyNotImplementedException.</b></p>
      /// At a minimum, this must contain an item for <see cref="DriveRates.driveSidereal" />.
      /// <para>This is only available for telescope InterfaceVersions 2 and 3</para>
      /// </remarks>
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

      /// <summary>
      /// The UTC date/time of the telescope's internal clock
      /// </summary>
      /// <exception cref="PropertyNotImplementedException">If UTCDate Write is not implemented.</exception>
      /// <exception cref="InvalidValueException">If an invalid <see cref="DateTime" /> is set.</exception>
      /// <exception cref="InvalidOperationException">When UTCDate is read and the mount cannot provide this property itslef and a value has not yet be established by writing to the property.</exception>
      /// <remarks>
      /// <p style="color:red;margin-bottom:0"><b>UTCDate Read must be implemented and must not throw a PropertyNotImplementedException. </b></p>
      /// <p style="color:red;margin-top:0"><b>UTCDate Write can throw a PropertyNotImplementedException.</b></p>
      /// The driver must calculate this from the system clock if the telescope has no accessible source of UTC time. In this case, the property must not be writeable (this would change the system clock!) and will instead raise an error.
      /// However, it is permitted to change the telescope's internal UTC clock if it is being used for this property.This allows clients to adjust the telescope's UTC clock as needed for accuracy. Reading the property
      /// will raise an error if the value has never been set or is otherwise unavailable. 
      /// </remarks>
      public DateTime UTCDate
      {
         get
         {
            DateTime utcDate = DateTime.UtcNow;
            _Logger.LogMessage("TrackingRates", "Get - " + String.Format("{0:MM/dd/yy HH:mm:ss}", utcDate));
            return utcDate;
         }
         set
         {
            _Logger.LogMessage("UTCDate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("UTCDate", true);
         }
      }

      /// <summary>
      /// Takes telescope out of the Parked state.
      /// </summary>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented and <see cref="CanUnpark" /> is False</exception>
      /// <remarks>
      /// The state of <see cref="Tracking" /> after unparking is undetermined. Valid only after <see cref="Park" />. Applications must check and change Tracking as needed after unparking. 
      /// Raises an error if unparking fails. Calling this with <see cref="AtPark" /> = False does nothing (harmless) 
      /// NOTE: Unpark encoder positions should be set prior to calling this method using the Action "Lunatic:SetUnparkPosition"
      /// </remarks>
      public void Unpark()
      {
         _Logger.LogMessage("COMMAND", "Unpark");
         if (Settings.ParkStatus == ParkStatus.Parked) {
            lock (_Lock) {
               Settings.ParkStatus = ParkStatus.Unparking;
               // ASCOM, in their wisdom (or lack of it), require that park blocks the client until completion.
               // This is rather poor and we have chosen to ignor that part of the spec believing that
               // non blocking asynchronous methods are a much better solution. However some clients may
               // require a blocking function so we've provided an option to allow this.
               if (Settings.AscomCompliance.UseSynchronousParking) {
                  UnparkScope();
               }
               else {
                  UnparkScopeAsync();
               }
               Settings.ParkStatus = ParkStatus.Unparked;
            }
         }
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
         //AxisPosition controllerAxisPosition;
         //AxisPosition currentAxisPosition;
         //AxisPosition targetAxisPosition;
         //AxisPosition axisAdjustment;
         //double saveRASync;
         //double saveDECSync;


         //double tRA;
         //double tHA;
         //int tPier;
         //CarteseanCoordinate tmpCoord;

         bool result = true;

         //// If HC.ListSyncMode.ListIndex = 1 Then
         //if (SyncMode == SyncModeOption.AppendOnSync) {
         //   //' Append via sync mode!
         //   if (!_IsSlewing) {
         //      result = Alignment.EQ_NPointAppend(rightAscension, declination, longitude, hemisphere);
         //   }
         //   else {
         //      result = false;
         //   }
         //   //Exit Function
         //}
         //else {
         //   //' its an ascom sync - shift whole model
         //   saveDECSync = Settings.DECSync01;
         //   saveRASync = Settings.RASync01;
         //   Settings.RASync01 = 0;
         //   Settings.DECSync01 = 0;

         //   //TODO: HC.EncoderTimer.Enabled = False
         //   controllerAxisPosition = new AxisPosition(SharedResources.Controller.MCGetAxisPosition(AxisId.Axis1_RA),
         //                                                SharedResources.Controller.MCGetAxisPosition(AxisId.Axis2_DEC));
         //   if (!Settings.ThreeStarEnable) {
         //      currentAxisPosition = controllerAxisPosition + new AxisPosition(Settings.RA1Star, Settings.DEC1Star);
         //   }
         //   else {
         //      switch (SyncAlignmentMode) {

         //         case SyncAlignmentModeOptions.NearestStar:
         //            //   Case 2
         //            // ' nearest
         //            tmpCoord = DeltaSyncMatrixMap(controllerAxisPosition);
         //            CurrentAxisPosition = new AxisPosition(tmpCoord.X, tmpCoord.Y);
         //            break;

         //         default:
         //            // 'n-star+nearest
         //            tmpCoord = DeltaMatrixReverseMap(raAxisPositon, decAxisPosition);
         //            currentRAEncoder = tmpCoord.X;
         //            currentDECEncoder = tmpCoord.Y;

         //            if (!tmpCoord.Flag) {
         //               tmpCoord = DeltaSyncMatrixMap(raAxisPositon, decAxisPosition);
         //               currentRAEncoder = tmpCoord.X;
         //               currentDECEncoder = tmpCoord.Y;
         //            }
         //            break;
         //      }
         //   }


         //   //TODO: HC.EncoderTimer.Enabled = True
         //   tHA = AstroConvert.RangeHA(rightAscension - AstroConvert.LocalApparentSiderealTime(longitude));


         //   if (tHA < 0) {
         //      if (hemisphere == HemisphereOption.Northern) {
         //         tPier = 1;
         //      }
         //      else {
         //         tPier = 0;
         //      }
         //      tRA = AstroConvert.Range24(rightAscension - 12);
         //   }
         //   else {
         //      if (hemisphere == HemisphereOption.Northern) {
         //         tPier = 0;
         //      }
         //      else {
         //         tPier = 1;
         //      }
         //      tRA = rightAscension;
         //   }

         //   //'Compute for Sync RA/DEC Encoder Values


         //   targetRAEncoder = AstroConvert.RAAxisPositionFromRA(tRA, 0, longitude, global::Lunatic.Core.Constants.RAEncoder_Zero_pos, hemisphere);
         //   targetDECEncoder = AstroConvert.DECAxisPositionFromDEC(declination, tPier, global::Lunatic.Core.Constants.DECEncoder_Zero_pos, hemisphere);


         //   if (Settings.DisableSyncLimit) {
         //      Settings.RASync01 = targetRAEncoder - currentRAEncoder;
         //      Settings.DECSync01 = targetDECEncoder - currentDECEncoder;
         //   }
         //   else {
         //      if ((Math.Abs(targetRAEncoder - currentRAEncoder) > Settings.MaxSync) || (Math.Abs(targetDECEncoder - currentDECEncoder) > Settings.MaxSync)) {
         //         //TODO: Call HC.Add_Message(oLangDll.GetLangString(6004))
         //         Settings.DECSync01 = saveDECSync;
         //         Settings.RASync01 = saveRASync;
         //         //TODO: HC.Add_Message ("RA=" & FmtSexa(gRA, False) & " " & CStr(currentRAEncoder))
         //         //TODO: HC.Add_Message ("SyncRA=" & FmtSexa(RightAscension, False) & " " & CStr(targetRAEncoder))
         //         //TODO: HC.Add_Message ("DEC=" & FmtSexa(gDec, True) & " " & CStr(currentDECEncoder))
         //         //TODO: HC.Add_Message ("Sync   DEC=" & FmtSexa(Declination, True) & " " & CStr(targetDECEncoder))
         //         result = false;
         //      }
         //      else {
         //         Settings.RASync01 = targetRAEncoder - currentRAEncoder;
         //         Settings.DECSync01 = targetDECEncoder - currentDECEncoder;
         //      }
         //   }


         //   //WriteSyncMap(); ==> Persist the values of RASync01 && RaSync02
         //   SettingsProvider.Current.SaveSettings();
         //   Settings.EmulOneShot = true;    // Re Sync Display
         //                                   //TODO: HC.DxSalbl.Caption = Format$(str(gRASync01), "000000000")
         //                                   //TODO: HC.DxSblbl.Caption = Format$(str(gDECSync01), "000000000")
         //}
         return result;
      }


      #endregion
   }
}
