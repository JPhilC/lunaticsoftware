using ASCOM.Lunatic.Interfaces;
using Lunatic.Core;
using Lunatic.Core.Services;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
   [CategoryOrder("Mount Options", 1)]
   [CategoryOrder("Port Details", 2)]
   [CategoryOrder("Site Information", 3)]
   [CategoryOrder("Gamepad", 5)]
   [CategoryOrder("ASCOM Options", 6)]
   [CategoryOrder("General", 7)]
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

      private PulseGuidingOption _PulseGuidingMode;
      [Category("Mount Options")]
      [DisplayName("Guiding")]
      [Description("Choose type of guiding used.")]
      [PropertyOrder(1)]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public PulseGuidingOption PulseGuidingMode
      {
         get
         {
            return _PulseGuidingMode;
         }
         set
         {
            if (value == _PulseGuidingMode) {
               return;
            }
            _PulseGuidingMode = value;
            RaisePropertyChanged();
         }
      }


      #endregion

      #region Port details ...
      private COMPortInfo _SelectedCOMPort;

      [Category("Port Details")]
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
      [Category("Port Details")]
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
      [Category("Port Details")]
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
      [Category("Port Details")]
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

      #region General options ...

      #endregion

      #region Site information ...
      #endregion

      #region ASCOM Options ...
      private bool _IsTraceOn;
      [Category("ASCOM Options")]
      [DisplayName("Trace On")]
      [Description("Put a tick in here to switch on ASCOM tracing")]
      [PropertyOrder(0)]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
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

      private bool _StrictAscom;
      [Category("ASCOM Options")]
      [DisplayName("Strict ASCOM conformance")]
      [Description("Put a tick in here to switch on strict conformance of the ASCOM interface.")]
      [PropertyOrder(1)]
      [RefreshProperties(RefreshProperties.All)]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public bool StrictAscom
      {
         get
         {
            return _StrictAscom;
         }
         set
         {
            if (value == _StrictAscom) {
               return;
            }
            _StrictAscom = value;
            SetStrictAscom(_StrictAscom);
            RaisePropertyChanged();
         }
      }


      private bool _AllowPulseGuidingExceptions;
      [Category("ASCOM Options")]
      [DisplayName("Pulseguide exceptions")]
      [Description("Put a tick in here to allow exceptions to be raised when pulse guiding.")]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public bool AllowPulseGuidingExceptions
      {
         get
         {
            return _AllowPulseGuidingExceptions;
         }
         set
         {
            if (value == _AllowPulseGuidingExceptions) {
               return;
            }
            _AllowPulseGuidingExceptions = value;
            RaisePropertyChanged();
         }
      }

      private bool _UseSynchronousParking;
      [Category("ASCOM Options")]
      [DisplayName("Synchronous Park")]
      [Description("Put a tick in here to for a synchrononus park (i.e. the park command must finish before any other commands can be sent).")]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public bool UseSynchronousParking
      {
         get
         {
            return _UseSynchronousParking;
         }
         set
         {
            if (value == _UseSynchronousParking) {
               return;
            }
            _UseSynchronousParking = value;
            RaisePropertyChanged();
         }
      }

      private bool _AllowSiteWrites;
      [Category("ASCOM Options")]
      [DisplayName("Allow site writes")]
      [Description("Put a tick in here to allow updates to site information to be saved.")]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public bool AllowSiteWrites
      {
         get
         {
            return _AllowSiteWrites;
         }
         set
         {
            if (value == _AllowSiteWrites) {
               return;
            }
            _AllowSiteWrites = value;
            RaisePropertyChanged();
         }
      }

      private bool _AllowExceptions;
      [Category("ASCOM Options")]
      [DisplayName("Allow exceptions")]
      [Description("Put a tick in here to allow ASCOM exceptions to be raised.")]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public bool AllowExceptions
      {
         get
         {
            return _AllowExceptions;
         }
         set
         {
            if (value == _AllowExceptions) {
               return;
            }
            _AllowExceptions = value;
            RaisePropertyChanged();
         }
      }

      private bool _SlewWithTrackingOff;
      [Category("ASCOM Options")]
      [DisplayName("Slew with tracking off")]
      [Description("Put a tick in here to switch off tracking whilst slewing.")]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public bool SlewWithTrackingOff
      {
         get
         {
            return _SlewWithTrackingOff;
         }
         set
         {
            if (value == _SlewWithTrackingOff) {
               return;
            }
            _SlewWithTrackingOff = value;
            RaisePropertyChanged();
         }
      }

      private SideOfPierOption _SideOfPier;
      [Category("ASCOM Options")]
      [DisplayName("Side of pier")]
      [Description("Choose a side of pier option")]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public SideOfPierOption SideOfPier
      {
         get
         {
            return _SideOfPier;
         }
         set
         {
            if (value == _SideOfPier) {
               return;
            }
            _SideOfPier = value;
            RaisePropertyChanged();
         }
      }

      private bool _SwapPointingSideOfPier;
      [Category("ASCOM Options")]
      [DisplayName("Swap pointing side of pier")]
      [Description("Put a tick in here to swap over the pointing side of pier.")]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public bool SwapPointingSideOfPier
      {
         get
         {
            return _SwapPointingSideOfPier;
         }
         set
         {
            if (value == _SwapPointingSideOfPier) {
               return;
            }
            _SwapPointingSideOfPier = value;
            RaisePropertyChanged();
         }
      }

      private bool _SwapPhysicalSideOfPier;
      [Category("ASCOM Options")]
      [DisplayName("Swap physical side of pier")]
      [Description("Put a tick in here to swap over the physical side of pier.")]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public bool SwapPhysicalSideOfPier
      {
         get
         {
            return _SwapPhysicalSideOfPier;
         }
         set
         {
            if (value == _SwapPhysicalSideOfPier) {
               return;
            }
            _SwapPhysicalSideOfPier = value;
            RaisePropertyChanged();
         }
      }

      private EpochOption _Epoch;
      [Category("ASCOM Options")]
      [DisplayName("Epoch")]
      [Description("Choose an epoch option")]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public EpochOption Epoch
      {
         get
         {
            return _Epoch;
         }
         set
         {
            if (value == _Epoch) {
               return;
            }
            _Epoch = value;
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
         MountOption = _Settings.MountOption;
         PulseGuidingMode = _Settings.PulseGuidingMode;

         SelectedCOMPort = COMPortService.GetCOMPortsInfo().Where(port => port.Name == Settings.COMPort).FirstOrDefault();
         TimeOut = _Settings.Timeout;
         Retry = _Settings.Retry;
         BaudRate = _Settings.BaudRate;

         IsTraceOn = _Settings.IsTracing;

         SlewWithTrackingOff = _Settings.AscomCompliance.SlewWithTrackingOff;
         AllowExceptions = _Settings.AscomCompliance.AllowExceptions;
         AllowPulseGuidingExceptions = _Settings.AscomCompliance.AllowPulseGuidingExceptions;
         UseSynchronousParking = _Settings.AscomCompliance.UseSynchronousParking;
         AllowSiteWrites = _Settings.AscomCompliance.AllowSiteWrites;
         Epoch = _Settings.AscomCompliance.Epoch;
         SideOfPier = _Settings.AscomCompliance.SideOfPier;
         SwapPointingSideOfPier = _Settings.AscomCompliance.SwapPointingSideOfPier;
         SwapPhysicalSideOfPier = _Settings.AscomCompliance.SwapPhysicalSideOfPier;
         StrictAscom = _Settings.AscomCompliance.Strict;

      }

      public void PushProperties()
      {
         _Settings.MountOption = MountOption;
         _Settings.PulseGuidingMode = PulseGuidingMode;

         if (SelectedCOMPort != null) {
            _Settings.COMPort = SelectedCOMPort.Name;
         }

         _Settings.Timeout = TimeOut;
         _Settings.Retry = Retry;
         _Settings.BaudRate = BaudRate;

         _Settings.IsTracing = IsTraceOn;

         _Settings.AscomCompliance.SlewWithTrackingOff = SlewWithTrackingOff;
         _Settings.AscomCompliance.AllowExceptions = AllowExceptions;
         _Settings.AscomCompliance.AllowPulseGuidingExceptions = AllowPulseGuidingExceptions;
         _Settings.AscomCompliance.UseSynchronousParking = UseSynchronousParking;
         _Settings.AscomCompliance.AllowSiteWrites = AllowSiteWrites;
         _Settings.AscomCompliance.Epoch = Epoch;
         _Settings.AscomCompliance.SideOfPier = SideOfPier;
         _Settings.AscomCompliance.SwapPointingSideOfPier = SwapPointingSideOfPier;
         _Settings.AscomCompliance.SwapPhysicalSideOfPier = SwapPhysicalSideOfPier;
         _Settings.AscomCompliance.Strict = StrictAscom;

      }

      private void SetStrictAscom(bool strictAscom)
      {
         if (strictAscom) {
            AllowPulseGuidingExceptions = false;
            AllowExceptions = false;
            SlewWithTrackingOff = false;
            AllowExceptions = false;
            AllowPulseGuidingExceptions = false;
            SideOfPier = SideOfPierOption.None;
            SwapPointingSideOfPier = false;
            SwapPhysicalSideOfPier = false;
         }
         this.SetBrowsableProperty("AllowPulseGuidingExceptions", !strictAscom);
         this.SetBrowsableProperty("AllowExceptions", !strictAscom);
         this.SetBrowsableProperty("SlewWithTrackingOff", !strictAscom);
         this.SetBrowsableProperty("AllowExceptions", !strictAscom);
         this.SetBrowsableProperty("AllowPulseGuidingExceptions", !strictAscom);
         this.SetBrowsableProperty("SideOfPier", !strictAscom);
         this.SetBrowsableProperty("SwapPointingSideOfPier", !strictAscom);
         this.SetBrowsableProperty("SwapPhysicalSideOfPier", !strictAscom);
      }

   }
}