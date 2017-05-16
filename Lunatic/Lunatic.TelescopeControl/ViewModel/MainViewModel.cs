// Uncomment the line below to instantiate the driver directly
// rather than using the ASCOM Local Server (i.e. while developing).
// Look at the PopSettings() method to see what is affected.
// #define INSTANTIATE_DIRECT    // NOTE: When commenting this line out you can
// also remove the project reference to 
// ASCOM.Lunatic.TelescopeServer
// NOTE: The above is now handled via Visual Studio configurations.
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Windows;
using Lunatic.TelescopeControl;
using Lunatic.Core;
using System.Windows.Threading;
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Lunatic.TelescopeControl.Controls;
using ASCOM.Lunatic.Telescope;


namespace Lunatic.TelescopeControl.ViewModel
{
   /// <summary>
   /// This class contains properties that the main View can data bind to.
   /// <para>
   /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
   /// </para>
   /// <para>
   /// You can also use Blend to data bind with the tool's support.
   /// </para>
   /// <para>
   /// See http://www.galasoft.ch/mvvm
   /// </para>
   /// </summary>
   [CategoryOrder("Mount Options", 1)]
   [CategoryOrder("Site Information", 2)]
   [CategoryOrder("Gamepad", 3)]
   [CategoryOrder("General", 4)]
   public class MainViewModel : LunaticViewModelBase, IDisposable
   {

      #region Properties ....

      #region Settings ...
      ISettingsProvider<TelescopeControlSettings> _SettingsProvider;

      private TelescopeControlSettings _Settings;
      public TelescopeControlSettings Settings
      {
         get
         {
            return _Settings;
         }
      }

      #region Mount options ...
      private MountOptions _MountOption;
      [Category("Mount Options")]
      [DisplayName("Mount")]
      [Description("Choose the type of mount")]
      [PropertyOrder(0)]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public MountOptions MountOption
      {
         get
         {
            return _MountOption;
         }
         set
         {
            if (value == _MountOption) {
               return;
            }
            _MountOption = value;
            RaisePropertyChanged();
         }
      }


      #endregion

      #region Site information ...
      [Category("Site Information")]
      [DisplayName("Current site")]
      [Description("The currently selected telescope site.")]
      [PropertyOrder(0)]
      public Site CurrentSite
      {
         get
         {
            return _Settings.Sites.CurrentSite;
         }
      }

      [Category("Site Information")]
      [DisplayName("Available sites")]
      [Description("Manage the available sites.")]
      [PropertyOrder(1)]
      public SiteCollection Sites
      {
         get
         {
            return _Settings.Sites;
         }
      }
      #endregion

      #endregion

      #region Telescope driver selection etc ...
#if INSTANTIATE_DIRECT
      private ASCOM.DeviceInterface.ITelescopeV3 _Driver;
      private ASCOM.DeviceInterface.ITelescopeV3 Driver
      {
         get
         {
            return _Driver;
         }
         set
         {
            if (ReferenceEquals(_Driver, value)) {
               return;
            }
            if (_Driver != null) {
               _Driver.Dispose();
            }
            _Driver = value;
         }
      }

#else
      private ASCOM.DriverAccess.Telescope _Driver;

      private ASCOM.DriverAccess.Telescope Driver
      {
         get
         {
            return _Driver;
         }
         set
         {
            if (ReferenceEquals(_Driver, value)) {
               return;
            }
            if (_Driver != null) {
               _Driver.Dispose();
            }
            _Driver = value;
         }
      }
#endif

      public bool IsConnected
      {
         get
         {
            return ((_Driver != null) && (_Driver.Connected == true));
         }
      }

      public bool IsParked
      {
         get
         {
            if (IsInDesignMode) {
               // Code runs in Blend --> create design time data.
               return true;
            }
            else {
               return ((_Driver != null) && _Driver.AtPark);
            }
         }
      }

      private ParkStatus _ParkStatus;

      public ParkStatus ParkStatus
      {
         get
         {
            return _ParkStatus;
         }
         set
         {
            Set<ParkStatus>("ParkStatus", ref _ParkStatus, value);
         }
      }

      private string _ParkCaption;

      public string ParkCaption
      {
         get
         {
            return _ParkCaption;
         }
         set
         {
            Set<string>("ParkCaption", ref _ParkCaption, value);
         }
      }

      private string _ParkStatusPosition;

      public string ParkStatusPosition
      {
         get
         {
            return _ParkStatusPosition;
         }
         set
         {
            Set<string>("ParkStatusPosition", ref _ParkStatusPosition, value);
         }
      }

      public bool IsSlewing
      {
         get
         {
            return ((_Driver != null) && _Driver.Slewing);
         }
      }

      public bool DriverSelected
      {
         get
         {
            return (_Driver != null);
         }
      }

      public string DriverName
      {
         get
         {
            return (_Driver != null ? _Driver.Description : "Telescope driver not selected.");
         }
      }

      private string _DriverId;
      public string DriverId
      {
         get
         {
            return _DriverId;
         }
         set
         {
            if (Set<string>(ref _DriverId, value)) {
               OnDriverChanged();
            }
         }
      }


      public string SetupMenuHeader
      {
         get
         {
            return (DriverSelected ? "Setup " + DriverName + "..." : "Setup");
         }
      }

      public string DisconnectMenuHeader
      {
         get
         {
            return (DriverSelected ? "Disconnect from " + DriverName : "Disconnect");
         }
      }
      public string ConnectMenuHeader
      {
         get
         {
            return (DriverSelected ? "Connect to " + DriverName : "Connect ...");
         }
      }

      private void OnDriverChanged(bool saveSettings = true)
      {
         if (saveSettings) {
            _Settings.DriverId = DriverId;
            _SettingsProvider.SaveSettings();
         }
         RaisePropertyChanged("DriverName");
         RaisePropertyChanged("DriverSelected");
         RaisePropertyChanged("SetupMenuHeader");
         RaisePropertyChanged("DisconnectMenuHeader");
         RaisePropertyChanged("ConnectMenuHeader");
         StatusMessage = (DriverSelected ? DriverName + " selected." : "Telescope driver not selected");
      }

      #endregion

      private string _StatusMessage = "Not connected.";
      public string StatusMessage
      {
         get
         {
            return _StatusMessage;
         }
         private set
         {
            Set<string>(ref _StatusMessage, value);
         }
      }

      #endregion

      #region Visibility display properties ...

      /// <summary>
      /// Used to control the main form component visiblity
      /// </summary>

      private DisplayMode _DisplayMode = DisplayMode.MountPosition;
      public DisplayMode DisplayMode
      {
         get
         {
            return _DisplayMode;
         }
         set
         {
            if (Set<DisplayMode>(ref _DisplayMode, value)) {
               _Settings.DisplayMode = DisplayMode;
               _SettingsProvider.SaveSettings();
               RaiseVisiblitiesChanged();
            }
         }
      }

      public Visibility ReducedSlewVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.ReducedSlew) == (long)Modules.ReducedSlew ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility SlewVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.Slew) == (long)Modules.Slew ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility MountPositionVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.MountPosition) == (long)Modules.MountPosition ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility TrackingVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.Tracking) == (long)Modules.Tracking ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility ParkStatusVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.ParkStatus) == (long)Modules.ParkStatus ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility ExpanderVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.Expander) == (long)Modules.Expander ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility AxisPositionVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.AxisPosition) == (long)Modules.AxisPosition ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility MessageCentreVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.MessageCentre) == (long)Modules.MessageCentre ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility PECVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.PEC) == (long)Modules.PEC ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility PulseGuidingVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.PulseGuide) == (long)Modules.PulseGuide ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      private void RaiseVisiblitiesChanged()
      {
         RaisePropertyChanged("ReducedSlewVisibility");
         RaisePropertyChanged("SlewVisibility");
         RaisePropertyChanged("MountPositionVisibility");
         RaisePropertyChanged("TrackingVisibility");
         RaisePropertyChanged("ParkStatusVisibility");
         RaisePropertyChanged("ExpanderVisibility");
         RaisePropertyChanged("AxisPositionVisibility");
         RaisePropertyChanged("MessageCentreVisibility");
         RaisePropertyChanged("PECVisibility");
         RaisePropertyChanged("PulseGuidingVisibility");
      }

      #endregion

      #region Telescope driver properties
      private double _LocalSiderealTime;

      public double LocalSiderealTime
      {
         get
         {
            return _LocalSiderealTime;
         }
         set
         {
            Set<double>(ref _LocalSiderealTime, value);
         }
      }

      private double _RightAscension;

      public double RightAscension
      {
         get
         {
            return _RightAscension;
         }
         set
         {
            Set<double>("RightAscension", ref _RightAscension, value);
         }
      }

      private double _Declination;

      public double Declination
      {
         get
         {
            return _Declination;
         }
         set
         {
            Set<double>("Declination", ref _Declination, value);
         }
      }

      private double _Altitude;

      public double Altitude
      {
         get
         {
            return _Altitude;
         }
         set
         {
            Set<double>("Altitude", ref _Altitude, value);
         }
      }

      private double _Azimuth;

      public double Azimuth
      {
         get
         {
            return _Azimuth;
         }
         set
         {
            Set<double>("Azimuth", ref _Azimuth, value);
         }
      }

      #region GuideRateDeclination ...
      // TODO: Migrate GuideRateDeclination and just pass the value to the driver
      private double _GuideRateDeclination;
      public double GuideRateDeclination
      {
         get
         {
            return _GuideRateDeclination;
         }
         set
         {
            _GuideRateDeclination = value;
         }
      }
      /*
      private double _GuideRateDeclination;
      public double GuideRateDeclination
      {
         get
         {
            if (Settings.AscomCompliance.AllowPulseGuide) {
               // movement rate offset in degress/sec
               // TODO: _GuideRateDeclination = (HC.HScrollDecRate.Value * 0.1 * SID_RATE) / 3600
            _Logger.LogMessage("GuideRateDeclination", "Get - " + _GuideRateDeclination.ToString());
                  }
            else {
               // RaiseError SCODE_NOT_IMPLEMENTED, ERR_SOURCE, "Property Get GuideRateDeclination" & MSG_NOT_IMPLEMENTED
               Select Case HC.DECGuideRateList.ListIndex
                   Case 1
                        GuideRateDeclination = (0.5 * SID_RATE) / 3600
                    Case 2
                        GuideRateDeclination = (0.75 * SID_RATE) / 3600
                    Case 3
                        GuideRateDeclination = (SID_RATE) / 3600
                    Case 4
                        RaiseError SCODE_NOT_IMPLEMENTED, ERR_SOURCE, "Property Get GuideRateDeclination" & MSG_NOT_IMPLEMENTED
                    Case Else
                        GuideRateDeclination = (0.25 * SID_RATE) / 3600
                End Select
                If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "GET GuideRateDEC :" & CStr(GuideRateDeclination)
            }

            throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
         }
         set
         {
            _Logger.LogMessage("GuideRateDeclination Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
         }
      }
       */
      #endregion


      #region GuideRateRightAscension ...
      // TODO: Migrate GuideRateRightAscension and just pass the value to the driver
      private double _GuideRateRightAscension;
      public double GuideRateRightAscension
      {
         get
         {
            return _GuideRateRightAscension;
         }
         set
         {
            _GuideRateRightAscension = value;
         }
      }
      /*
Public Property Get GuideRateRightAscension() As Double
 If gAscomCompatibility.AllowPulseGuide Then
     ' movement rate offset in degrees/sec
     GuideRateRightAscension = (HC.HScrollRARate.Value * 0.1 * SID_RATE) / 3600
     If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "GET GuideRateRA :" & CStr(GuideRateRightAscension)
 Else
     ' RaiseError SCODE_NOT_IMPLEMENTED, ERR_SOURCE, "Property Get GuideRateRightAscension" & MSG_NOT_IMPLEMENTED
     Select Case HC.RAGuideRateList.ListIndex
         Case 1
             GuideRateRightAscension = (0.5 * SID_RATE) / 3600
         Case 2
             GuideRateRightAscension = (0.75 * SID_RATE) / 3600
         Case 3
             GuideRateRightAscension = (SID_RATE) / 3600
         Case 4
             RaiseError SCODE_NOT_IMPLEMENTED, ERR_SOURCE, "Property Get GuideRateRightAscension" & MSG_NOT_IMPLEMENTED
         Case Else
             GuideRateRightAscension = (0.25 * SID_RATE) / 3600
     End Select
     If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "GET GuideRateRA :" & CStr(GuideRateRightAscension)
 End If

End Property

Public Property Let GuideRateRightAscension(ByVal newval As Double)
 ' We can't support properly beacuse the ASCOM spec does not distinquish between ST4 and Pulseguiding
 ' and states that this property relates to both - crazy!
 If gAscomCompatibility.AllowPulseGuide Then
     If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "LET GuideRateRA(" & newval & ")"
     newval = newval * 3600 / (0.1 * SID_RATE)
     If newval < HC.HScrollRARate.min Then
         newval = HC.HScrollRARate.min
     Else
         If newval > HC.HScrollRARate.max Then
             newval = HC.HScrollRARate.max
         End If
     End If
     HC.HScrollRARate.Value = CInt(newval)
 Else
     If HC.RAGuideRateList.ListIndex = 4 Then
         If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "LET GuideRateRA(" & newval & ") :NOT_SUPPORTED"
         RaiseError SCODE_NOT_IMPLEMENTED, ERR_SOURCE, "Property Let GuideRateRightAscension" & MSG_NOT_IMPLEMENTED
     Else
         newval = newval * 3600 / SID_RATE
         If newval > 0.75 Then
             HC.RAGuideRateList.ListIndex = 3
         Else
             If newval > 0.5 Then
                 HC.RAGuideRateList.ListIndex = 2
             Else
                 If newval > 0.25 Then
                     HC.RAGuideRateList.ListIndex = 1
                 Else
                     HC.RAGuideRateList.ListIndex = 0
                 End If
             End If
         End If
         If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "LET GuideRateRA(" & newval & ")"
     End If
 End If
End Property
       */
      #endregion


      #endregion

      /// <summary>
      /// Initializes a new instance of the MainViewModel class.
      /// </summary>
      public MainViewModel(ISettingsProvider<TelescopeControlSettings> settingsProvider)
      {

         if (IsInDesignMode) {
            // Code runs in Blend --> create design time data.
         }
         else {
            // Code runs "for real"
            _SettingsProvider = settingsProvider;
            _Settings = settingsProvider.Settings;
            PopSettings();
            _DisplayTimer = new DispatcherTimer();
            _DisplayTimer.Interval = TimeSpan.FromMilliseconds(500);
            _DisplayTimer.Tick += new EventHandler(this.DisplayTimer_Tick);
         }

      }

      public override void Cleanup()
      {
         // Release the reference to the driver.
         Driver = null;
         base.Cleanup();
      }

      #region Timers ...
      // This code creates a new DispatcherTimer with an interval of 15 seconds.
      private DispatcherTimer _DisplayTimer;
      private bool _ProcessingDisplayTimerTick = false;
      private void DisplayTimer_Tick(object state, EventArgs e)
      {
         if (Driver != null && !_ProcessingDisplayTimerTick) {
            _ProcessingDisplayTimerTick = true;
            LocalSiderealTime = Driver.SiderealTime;
            RightAscension = Driver.RightAscension;
            Declination = Driver.Declination;
            Altitude = Driver.Altitude;
            Azimuth = Driver.Azimuth;

            if (Driver.AtPark != IsParked) {
               RaisePropertyChanged("IsParked");
               ParkCommand.RaiseCanExecuteChanged();
            }
            RefreshParkStatus();

            RaisePropertyChanged("IsSlewing");
            _ProcessingDisplayTimerTick = false;
         }
      }

      private void RefreshParkStatus()
      {
         try {
            string result = Driver.CommandString("Lunatic:GetParkStatus", false);
            int parkStatus;
            if (int.TryParse(result, out parkStatus)) {
               ParkStatus = (ParkStatus)parkStatus;
               if (ParkStatus == ParkStatus.Parked) {
                  ParkCaption = "Unpark";
                  ParkStatusPosition = "HOME";
               }
               else {
                  ParkCaption = "Park: HOME";
                  ParkStatusPosition = "";
               }
            }
            else {
               StatusMessage = "Invalid park status returned.";
            }
         }
         catch (Exception ex) {
            // TODO: Sort out better error message display
            StatusMessage = ex.Message;
         }
      }

      #endregion

      #region Settings ...
      private void PopSettings()
      {
         _DriverId = _Settings.DriverId;
         _DisplayMode = _Settings.DisplayMode;

         _Settings.Sites.PropertyChanged += Sites_PropertyChanged;
#if INSTANTIATE_DIRECT
         _Driver = new Telescope();
#else
         // Better try to instantiate the driver as well if we have a driver ID
         if (!string.IsNullOrWhiteSpace(_DriverId)) {
            try {
               _Driver = new ASCOM.DriverAccess.Telescope(_DriverId);
            }
            catch (Exception) {
               _DriverId = string.Empty;
               _StatusMessage = "Failed select previous telescope driver";
            }
         }
#endif
      }

      private void Sites_PropertyChanged(object sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "CurrentSite") {
            RaisePropertyChanged("CurrentSite");
            SaveSettings();
            UpdateDriverSiteDetails();
         }
      }

      private void Sites_CurrentSiteChanged(object sender, EventArgs e)
      {
      }

      private void PushSettings()
      {
         _Settings.DriverId = this.DriverId;
         _Settings.DisplayMode = this.DisplayMode;
      }

      public void SaveSettings()
      {
         PushSettings();
         _SettingsProvider.SaveSettings();
      }

      #endregion

      #region Relay commands ...
      private RelayCommand<DisplayMode> _DisplayModeCommand;

      /// <summary>
      /// Command to cycle through the display modes.
      /// </summary>
      public RelayCommand<DisplayMode> DisplayModeCommand
      {
         get
         {
            return _DisplayModeCommand
               ?? (_DisplayModeCommand = new RelayCommand<DisplayMode>((mode) => {
                  DisplayMode = mode;
               }));
         }
      }

      #region Site Relay commands ...
      private RelayCommand<SiteCollection> _AddSiteCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<SiteCollection> AddSiteCommand
      {
         get
         {
            return _AddSiteCommand
                ?? (_AddSiteCommand = new RelayCommand<SiteCollection>(
                                      (collection) => {
                                         collection.Add(new Site(Guid.NewGuid()) { SiteName = "<Site name>" });
                                      }

                                      ));
         }
      }


      private RelayCommand<Site> _RemoveSiteCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<Site> RemoveSiteCommand
      {
         get
         {
            return _RemoveSiteCommand
                ?? (_RemoveSiteCommand = new RelayCommand<Site>(
                                      (site) => {
                                         Sites.Remove(site);
                                      }

                                      ));
         }
      }

      private RelayCommand<Site> _GetSiteCoordinateCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<Site> GetSiteCoordinateCommand
      {
         get
         {
            return _GetSiteCoordinateCommand
                ?? (_GetSiteCoordinateCommand = new RelayCommand<Site>(
                                      (site) => {
                                         MapViewModel vm = new MapViewModel(site);
                                         MapWindow map = new MapWindow(vm);
                                         var result = map.ShowDialog();
                                      }

                                      ));
         }
      }

      private void UpdateDriverSiteDetails()
      {
         if (Driver != null) {
            // Transfer location any other initialisation needed.
            Driver.SiteElevation = Settings.CurrentSite.Elevation;
            Driver.SiteLatitude = Settings.CurrentSite.Latitude;
            Driver.SiteLongitude = Settings.CurrentSite.Longitude;
         }
      }
      #endregion


      #region Choose, Connect, Disconnect etc ...
      private RelayCommand _ChooseCommand;

      public RelayCommand ChooseCommand
      {
         get
         {
            return _ChooseCommand
               ?? (_ChooseCommand = new RelayCommand(() => {
#if INSTANTIATE_DIRECT
                  Driver = new Telescope();

#else
                  string driverId = ASCOM.DriverAccess.Telescope.Choose(DriverId);
                  if (driverId != null) {
                     Driver = new ASCOM.DriverAccess.Telescope(driverId);
                     DriverId = driverId; // Triggers a refresh of menu options etc
                  }
#endif
                  RaiseCanExecuteChanged();
               }, () => { return !IsConnected; }));
         }
      }

      private RelayCommand _ConnectCommand;

      public RelayCommand ConnectCommand
      {
         get
         {
            return _ConnectCommand
               ?? (_ConnectCommand = new RelayCommand(() => {
                  if (IsConnected) {
                     Disconnect();
                  }
                  else {
                     Connect();
                  }
                  RaisePropertyChanged("IsConnected");
                  RaisePropertyChanged("IsParked");
                  RaiseCanExecuteChanged();
               }, () => { return Driver != null; }));
         }
      }

      // Perform the logic when connecting.
      private void Connect()
      {
         try {
            // Check to see if the driver is already connected
            bool initialiseNeeded = !Driver.CommandBool("Lunatic:IsInitialised", false);
            Driver.Connected = true;
            if (initialiseNeeded) {
               UpdateDriverSiteDetails();
            }
            _ProcessingDisplayTimerTick = false;
            _DisplayTimer.Start();
         }
         catch (Exception ex) {
            StatusMessage = ex.Message;
         }
      }

      private void Disconnect()
      {
         _DisplayTimer.Stop();
         _ProcessingDisplayTimerTick = false;
         if (Driver != null) {
            Driver.Connected = false;
            Driver = null;
         }
      }

      private RelayCommand _SetupCommand;

      public RelayCommand SetupCommand
      {
         get
         {
            return _SetupCommand
               ?? (_SetupCommand = new RelayCommand(() => {
                  Driver.SetupDialog();
               }, () => { return Driver != null; }));
         }
      }

      #endregion

      #region Slewing commands ...
      private RelayCommand<SlewButton> _StartSlewCommand;

      public RelayCommand<SlewButton> StartSlewCommand
      {
         get
         {
            return _StartSlewCommand
               ?? (_StartSlewCommand = new RelayCommand<SlewButton>((button) => {
                  double rate;      // 10 x Sidereal;
                  switch (button) {
                     case SlewButton.North:
                     case SlewButton.South:
                        rate = Settings.SlewRatePreset.DecRate * Core.Constants.SIDEREAL_RATE_DEGREES;
                        if (button == SlewButton.South) {
                           rate = -rate;
                        }
                        if (Settings.ReverseDec) {
                           rate = -rate;
                        }
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, rate);
                        break;
                     case SlewButton.East:
                     case SlewButton.West:
                        rate = Settings.SlewRatePreset.RARate * Core.Constants.SIDEREAL_RATE_DEGREES;
                        if (button == SlewButton.East) {
                           rate = -rate;
                        }
                        if (Settings.ReverseRA) {
                           rate = -rate;
                        }
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, rate);
                        break;
                  }


               }, (button) => { return (IsConnected); }));   // Check that we are connected
         }
      }

      private RelayCommand<SlewButton> _StopSlewCommand;

      public RelayCommand<SlewButton> StopSlewCommand
      {
         get
         {
            return _StopSlewCommand
               ?? (_StopSlewCommand = new RelayCommand<SlewButton>((button) => {
                  switch (button) {
                     case SlewButton.Stop:
                        Driver.AbortSlew();
                        break;
                     case SlewButton.North:
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0.0);
                        break;
                     case SlewButton.South:
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0.0);
                        break;
                     case SlewButton.East:
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0.0);
                        break;
                     case SlewButton.West:
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0.0);
                        break;
                  }
               }, (button) => { return (IsConnected); }));   // Check that we are connected
         }
      }
      #endregion

      #region Parking and unparking commands ...
      private RelayCommand _ParkCommand;

      public RelayCommand ParkCommand
      {
         get
         {
            return _ParkCommand
               ?? (_ParkCommand = new RelayCommand(() => {
                  if (IsParked) {
                     Driver.Unpark();
                  }
                  else {
                     Driver.Park();
                  }
               }, () => { return (IsConnected && !IsSlewing); }));   // Check that we are connected
         }
      }

      #endregion

      #endregion


      private void RaiseCanExecuteChanged()
      {
         ChooseCommand.RaiseCanExecuteChanged();
         ConnectCommand.RaiseCanExecuteChanged();
         StartSlewCommand.RaiseCanExecuteChanged();
         StopSlewCommand.RaiseCanExecuteChanged();
         ParkCommand.RaiseCanExecuteChanged();
      }

      #region IDisposable ...
      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (disposing) {
            if (_Driver != null) {
               _Driver.Dispose();
            }
         }
      }
      #endregion
   }
}