using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Windows;
using Lunatic.TelescopeControl;
using Lunatic.Core;

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
   public class MainViewModel : LunaticViewModelBase
   {

      #region Properties ....
      #region Settings ...
      ISettingsProvider<TelescopeControlSettings> _SettingsProvider;

      private TelescopeControlSettings _Settings;

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
      
      /// <summary>
      /// Initializes a new instance of the MainViewModel class.
      /// </summary>
      public MainViewModel(ISettingsProvider<TelescopeControlSettings> settingsProvider)
      {
         _SettingsProvider = settingsProvider;
         _Settings = settingsProvider.CurrentSettings;
         PopSettings();
         
         ////if (IsInDesignMode)
         ////{
         ////    // Code runs in Blend --> create design time data.
         ////}
         ////else
         ////{
         ////    // Code runs "for real"
         ////}
         

      }

      public override void Cleanup()
      {
         // Release the reference to the driver.
         Driver = null;
         base.Cleanup();
      }

      private void PopSettings()
      {
         _DriverId = _Settings.DriverId;
         DisplayMode = _Settings.DisplayMode;

         // Better try to instantiate the driver as well if we have a driver ID
         if (!string.IsNullOrWhiteSpace(_DriverId)) {
            try {
               Driver = new ASCOM.DriverAccess.Telescope(_DriverId);
               OnDriverChanged(false);   // Update menu options and stuff for telescope.
            }
            catch (Exception ex) {
               _DriverId = string.Empty;
               StatusMessage = "Failed select previous telescope driver";
            }
         }
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
                     if (Driver != null) {
                        Driver.Connected = false;
                     }
                  }
                  else {
                     try {
                        Driver.Connected = true;
                     }
                     catch (Exception ex) {
                        StatusMessage = ex.Message; 
                     }
                  }
                  RaisePropertyChanged("IsConnected");
                  RaiseCanExecuteChanged();
               }, () => { return Driver != null; }));
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

      private RelayCommand<SlewDirection> _SlewCommand;

      public RelayCommand<SlewDirection> SlewCommand
      {
         get
         {
            return _SlewCommand
               ?? (_SlewCommand = new RelayCommand<SlewDirection>((direction) => {
                  //TODO: Add body of SlewCommand
               }, (direction) => { return !IsConnected; }));
         }
      }
      #endregion


      private void RaiseCanExecuteChanged()
      {
         ChooseCommand.RaiseCanExecuteChanged();
         ConnectCommand.RaiseCanExecuteChanged();
         SlewCommand.RaiseCanExecuteChanged();
      }
   }
}