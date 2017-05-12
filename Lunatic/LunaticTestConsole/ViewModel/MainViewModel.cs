using ASCOM.Lunatic.Telescope;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Windows.Threading;

namespace LunaticTestConsole.ViewModel
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
      private const double Elevation = 200.0;
      private const int Hemispher = 0;
      private const double Latitude = 52.66842473829653;
      private const double Longitude = -1.3393039268281037;

      private DispatcherTimer _DisplayTimer;
      private bool _ProcessingDisplayTimerTick = false;
      private ASCOM.Utilities.Util _Util = new ASCOM.Utilities.Util();

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

      private string _CommandText = "Connect";
      public string CommandText
      {
         get
         {
            return _CommandText;
         }
         private set
         {
            Set<string>("CommandText", ref _CommandText, value);
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
            Set<string>(ref _StatusMessage, value);
         }
      }

      /// <summary>
      /// Initializes a new instance of the MainViewModel class.
      /// </summary>
      public MainViewModel()
      {
         if (IsInDesignMode) {
            // Code runs in Blend --> create design time data.
         }
         else {
            // Code runs "for real"
            _DisplayTimer = new DispatcherTimer();
            _DisplayTimer.Interval = TimeSpan.FromMilliseconds(500);
            _DisplayTimer.Tick += _DisplayTimer_Tick;
         }
      }

      private void _DisplayTimer_Tick(object sender, EventArgs e)
      {
         if (Driver!= null && !_ProcessingDisplayTimerTick) {
            _ProcessingDisplayTimerTick = true;
            string ra =  _Util.HoursToHMS(Driver.RightAscension, "h", "m", "s", 2);
            string dec = _Util.DegreesToDMS(Driver.Declination, "°", "'","\"", 1);
            string lst = _Util.HoursToHMS(Driver.SiderealTime, "h", "m", "s", 2);
            string alt = _Util.DegreesToDMS(Driver.Altitude, "°", "'", "\"", 1);
            string az = _Util.DegreesToDMS(Driver.Azimuth, "°", "'", "\"", 1);
            StatusMessage = string.Format("LST={0}\nRA={1}\nDec={2}\nAlt={3}\nAz={4}", lst, ra, dec, alt, az);
            _ProcessingDisplayTimerTick = false;
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
                     _DisplayTimer.Stop();
                     Disconnect();
                     StatusMessage = "Not Connected.";
                     CommandText = "Connect";
                  }
                  else {
                     Connect();
                     StatusMessage = "Connected.";
                     CommandText = "Disconnect";
                     _ProcessingDisplayTimerTick = false;
                     _DisplayTimer.Start();
                  }
                  RaisePropertyChanged("IsConnected");
                  RaisePropertyChanged("IsParked");
               }));
         }
      }

      // Perform the logic when connecting.
      private void Connect()
      {
         try {
            Driver = new Telescope();
            // Check to see if the driver is already connected
            bool initialiseNeeded = !Driver.CommandBool("Lunatic:IsInitialised", false);
            Driver.Connected = true;
            // Start the timer.  Note that this call can be made from any thread.
            if (initialiseNeeded) {
               // Transfer location any other initialisation needed.
               Driver.SiteElevation = Elevation;
               Driver.SiteLatitude = Latitude;
               Driver.SiteLongitude = Longitude;
            }
         }
         catch (Exception ex) {
            StatusMessage = ex.Message;
         }
      }

      private void Disconnect()
      {
         Driver.Connected = false;
         Driver = null;
      }


   }
}