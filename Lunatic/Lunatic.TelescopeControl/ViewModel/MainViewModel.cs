using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Windows;
using Lunatic.TelescopeControl;

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
   public class MainViewModel : ViewModelBase
   {
      private ASCOM.DriverAccess.Telescope _Driver;

      #region Properties ....
      public bool IsConnected
      {
         get
         {
            return ((_Driver != null) && (_Driver.Connected == true));
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
            if (value == _DriverId) {
               return;
            }
            _DriverId = value;
            RaisePropertyChanged();
         }
      }

      private string _StatusMessage = "Not connected.";
      public string StatusMessage
      {
         get
         {
            return _StatusMessage;
         }
         private set
         {
            if (_StatusMessage == value) {
               return;
            }
            _StatusMessage = value;
            RaisePropertyChanged();
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
      public MainViewModel()
      {
         ////if (IsInDesignMode)
         ////{
         ////    // Code runs in Blend --> create design time data.
         ////}
         ////else
         ////{
         ////    // Code runs "for real"
         ////}
         DisplayMode = DisplayMode.MountPosition;

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

      private RelayCommand _ChooseCommand;

      public RelayCommand ChooseCommand
      {
         get
         {
            return _ChooseCommand
               ?? (_ChooseCommand = new RelayCommand(() => {
                  DriverId = ASCOM.DriverAccess.Telescope.Choose(Properties.Settings.Default.DriverId);
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
                     if (_Driver != null) {
                        _Driver.Connected = false;
                     }
                  }
                  else {
                     _Driver = new ASCOM.DriverAccess.Telescope(DriverId);
                     try {
                        _Driver.Connected = true;
                     }
                     catch (Exception ex) {
                        StatusMessage = ex.Message; 
                     }
                  }
                  RaisePropertyChanged("IsConnected");
                  RaiseCanExecuteChanged();
               }, () => { return !string.IsNullOrEmpty(DriverId); }));
         }
      }

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