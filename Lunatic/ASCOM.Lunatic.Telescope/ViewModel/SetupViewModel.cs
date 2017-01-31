using ASCOM.Lunatic.Interfaces;
using Lunatic.Core;
using Lunatic.Core.Services;
using System.ComponentModel;
using System.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace ASCOM.Lunatic.TelescopeDriver
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
   public class SetupViewModel : LunaticViewModelBase
   {


      #region Properties ....
      private Settings _Settings;
      public Settings Settings
      {
         get
         {
            return _Settings;
         }
      }

      #region Port details ...
      private COMPortInfo _SelectedCOMPort;

      [Category("Port details")]
      [DisplayName("Port")]
      [Description("The COM port that is connected to the telescope")]
      public COMPortInfo SelectedCOMPort
      {
         get
         {
            return _SelectedCOMPort;
         }
         set
         {
            if (value == _SelectedCOMPort) {
               return;
            }
            _SelectedCOMPort = value;
            RaisePropertyChanged();
         }
      }

      private TimeOutOption _TimeOut;
      [Category("Port details")]
      [DisplayName("Timeout")]
      [Description("The length of time to wait for a response from the telescope (milliseconds)")]
      public TimeOutOption TimeOut
      {
         get
         {
            return _TimeOut;
         }
         set
         {
            if (value == _TimeOut) {
               return;
            }
            _TimeOut = value;
            RaisePropertyChanged();
         }
      }

      private RetryOption _Retry;
      [Category("Port details")]
      [DisplayName("Retry")]
      [Description("How many times to try connecting to the COM port.")]
      public RetryOption Retry
      {
         get
         {
            return _Retry;
         }
         set
         {
            if (value == _Retry) {
               return;
            }
            _Retry = value;
            RaisePropertyChanged();
         }
      }

      private BaudRate _BaudRate;
      [Category("Port details")]
      [DisplayName("Baud rate")]
      [Description("The data transfer rate used with the COM port.")]
      public BaudRate BaudRate
      {
         get
         {
            return _BaudRate;
         }
         set
         {
            if (value == _BaudRate) {
               return;
            }
            _BaudRate = value;
            RaisePropertyChanged();
         }
      }

      #endregion

      #region General ...
      private bool _IsTraceOn;
      [Category("General")]
      [DisplayName("Trace On")]
      [Description("Put a tick in here to switch on ASCOM tracing")]
      public bool IsTraceOn
      {
         get
         {
            return _IsTraceOn;
         }
         set
         {
            if (value == _IsTraceOn) {
               return;
            }
            _IsTraceOn = value;
            RaisePropertyChanged();
         }
      }

      #endregion

      #endregion

      /// <summary>
      /// Initializes a new instance of the MainViewModel class.
      /// </summary>
      public SetupViewModel(ISettingsProvider settingsProvider)
      {
         _Settings = settingsProvider.CurrentSettings;
         PopProperties();
      }

      protected override bool OnSaveCommand()
      {
         PushProperties();
         return base.OnSaveCommand();
      }

      public void PopProperties()
      {
         SelectedCOMPort = COMPortService.GetCOMPortsInfo().Where(port => port.Name == Settings.COMPort).FirstOrDefault();
         TimeOut = _Settings.Timeout;
         Retry = _Settings.Retry;
         BaudRate = _Settings.BaudRate;

         IsTraceOn = _Settings.IsTracing;
      }

      public void PushProperties()
      {
         _Settings.COMPort = SelectedCOMPort.Name;
         _Settings.Timeout = TimeOut;
         _Settings.Retry = Retry;
         _Settings.BaudRate = BaudRate;

         _Settings.IsTracing = IsTraceOn;
      }

   }
}