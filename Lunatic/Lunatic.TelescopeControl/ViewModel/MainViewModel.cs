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
   public class MainViewModel : LunaticViewModelBase
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
            return ((_Driver != null) && _Driver.AtPark);
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
         if (DriverSelected) {
            // Update the site settings
            // TODO: UpdateDriverSiteDetails();
         }
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
      private TimeSpan _LocalSiderealTime;

      public TimeSpan LocalSiderealTime
      {
         get
         {
            return _LocalSiderealTime;
         }
         set
         {
            Set<TimeSpan>(ref _LocalSiderealTime, value);
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
         _SettingsProvider = settingsProvider;
         _Settings = settingsProvider.Settings;
         PopSettings();

         ////if (IsInDesignMode)
         ////{
         ////    // Code runs in Blend --> create design time data.
         ////}
         ////else
         ////{
         ////    // Code runs "for real"
         ////}

         _EncoderTimer = new DispatcherTimer();
         _EncoderTimer.Interval = new TimeSpan(0, 0, 1);
         _EncoderTimer.Tick += new EventHandler(this.EncoderTime_Tick);


      }

      public override void Cleanup()
      {
         // Release the reference to the driver.
         Driver = null;
         base.Cleanup();
      }

      #region Timers ...
      // This code creates a new DispatcherTimer with an interval of 15 seconds.
      private DispatcherTimer _EncoderTimer;
      private bool _ProcessingEncoderTimerTick = false;
      private void EncoderTime_Tick(object state, EventArgs e)
      {
         if (!_ProcessingEncoderTimerTick) {
            _ProcessingEncoderTimerTick = true;
            LocalSiderealTime = TimeSpan.FromHours(_Driver.SiderealTime);

            if (_Driver.AtPark != IsParked) {
               RaisePropertyChanged("IsParked");
               UnparkCommand.RaiseCanExecuteChanged();
               ParkCommand.RaiseCanExecuteChanged();
            }

            _ProcessingEncoderTimerTick = false;
         }

         // double rightAscension;
         // double declination;
         // double altitude;
         // double azimuth;
         // int ra;
         // int dec;
         // Coordt coord;

         // if (!_ProcessingEncoderTimerTick) {
         //    _ProcessingEncoderTimerTick = true;
         //    // If(gEmulOneShot = True) Or(gEmulNudge = True) Or(gSlewStatus = True) Or(HC.CheckRASync.Value = 1) Then

         //    // Else
         //    //    ' emulate RA motor position
         //    //    gEmulRA = GetEmulRA()
         //    // End If

         //    if 
         //_ProcessingEncoderTimerTick = false;
         // }


      }
      #endregion

      #region Settings ...
      private void PopSettings()
      {
         _DriverId = _Settings.DriverId;
         DisplayMode = _Settings.DisplayMode;

         // Sites are updated directly
         // _Settings.Sites.CurrentSiteChanged += Sites_CurrentSiteChanged;
         _Settings.Sites.PropertyChanged += Sites_PropertyChanged;
         // Better try to instantiate the driver as well if we have a driver ID
         if (!string.IsNullOrWhiteSpace(_DriverId)) {
            try {
               Driver = new ASCOM.DriverAccess.Telescope(_DriverId);
               OnDriverChanged(false);   // Update menu options and stuff for telescope.

               // Update Driver properties
               // Driver.SiteElevation = 
            }
            catch (Exception ex) {
               _DriverId = string.Empty;
               StatusMessage = "Failed select previous telescope driver";
            }
         }
      }

      private void Sites_PropertyChanged(object sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "CurrentSite") {
            RaisePropertyChanged("CurrentSite");
            SaveSettings();
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

      #region Setup Relay commands ...
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
      #endregion


      #region Choose, Connect, Disconnect etc ...
      private RelayCommand _ChooseCommand;

      public RelayCommand ChooseCommand
      {
         get
         {
            return _ChooseCommand
               ?? (_ChooseCommand = new RelayCommand(() => {
                  string driverId = ASCOM.DriverAccess.Telescope.Choose(DriverId);
                  if (driverId != null) {
                     Driver = new ASCOM.DriverAccess.Telescope(driverId);
                     DriverId = driverId; // Triggers a refresh of menu options etc
                  }
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
            // Start the timer.  Note that this call can be made from any thread.
            _ProcessingEncoderTimerTick = false;
            _EncoderTimer.Start();
            if (initialiseNeeded) {
               // Transfer location any other initialisation needed.
               Driver.SiteElevation = Settings.CurrentSite.Elevation;
               Driver.SiteLatitude = Settings.CurrentSite.Latitude;
               Driver.SiteLongitude = Settings.CurrentSite.Longitude;
            }
         }
         catch (Exception ex) {
            StatusMessage = ex.Message;
         }
      }

      private void Disconnect()
      {
         _EncoderTimer.Stop();
         _ProcessingEncoderTimerTick = false;
         if (Driver != null) {
            Driver.Connected = false;
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
                  Driver.Park();
               }, () => { return (IsConnected && !IsParked && !IsSlewing); }));   // Check that we are connected
         }
      }


      private RelayCommand _UnparkCommand;

      public RelayCommand UnparkCommand
      {
         get
         {
            return _UnparkCommand
               ?? (_UnparkCommand = new RelayCommand(() => {
                  Driver.Unpark();
               }, () => { return (IsConnected && IsParked); }));   // Check that we are connected
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
      }
   }
}