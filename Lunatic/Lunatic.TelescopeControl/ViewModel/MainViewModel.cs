using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;

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
      }

      #region Relay commands ...
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
      #endregion


      private void RaiseCanExecuteChanged()
      {
         ChooseCommand.RaiseCanExecuteChanged();
         ConnectCommand.RaiseCanExecuteChanged();
      }
   }
}