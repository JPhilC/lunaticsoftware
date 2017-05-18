using GalaSoft.MvvmLight.Command;
using Lunatic.Core;
using Lunatic.Core.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace ASCOM.Lunatic.Telescope
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
   [CategoryOrder("Port Details", 1)]
   [CategoryOrder("ASCOM Options", 2)]
   [ComVisible(false)]
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

      [Category("Port Details")]
      [DisplayName("Port")]
      [Description("The COM port that is connected to the telescope")]
      [PropertyOrder(0)]
      public COMPortInfo SelectedCOMPort
      {
         get
         {
            return _SelectedCOMPort;
         }
         set
         {
            Set<COMPortInfo>("SelectedCOMPort", ref _SelectedCOMPort, value);
         }
      }

      private BaudRate _BaudRate;
      [Category("Port Details")]
      [DisplayName("Baud rate")]
      [Description("The data transfer rate used with the COM port.")]
      [PropertyOrder(1)]
      public BaudRate BaudRate
      {
         get
         {
            return _BaudRate;
         }
         set
         {
            Set<BaudRate>("BaudRate", ref _BaudRate, value);
         }
      }

      private TimeOutOption _TimeOut;
      [Category("Port Details")]
      [DisplayName("Timeout")]
      [Description("The length of time to wait for a response from the telescope (milliseconds)")]
      [PropertyOrder(2)]
      public TimeOutOption TimeOut
      {
         get
         {
            return _TimeOut;
         }
         set
         {
            Set<TimeOutOption>("TimeOut", ref _TimeOut, value);
         }
      }

      private RetryOption _Retry;
      [Category("Port Details")]
      [DisplayName("Retry")]
      [Description("How many times to try connecting to the COM port.")]
      [PropertyOrder(3)]
      public RetryOption Retry
      {
         get
         {
            return _Retry;
         }
         set
         {
            Set<RetryOption>("Retry", ref _Retry, value);
         }
      }


      #endregion

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
            Set<PulseGuidingOption>("PulseGuidingMode", ref _PulseGuidingMode, value);
         }
      }


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
            Set<bool>("IsTraceOn", ref _IsTraceOn, value);
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
            if (Set<bool>("StrictAscom", ref _StrictAscom, value)) {
               SetStrictAscom(_StrictAscom);
            }
         }
      }

      private bool _AllowPulseGuide;
      [Category("ASCOM Options")]
      [DisplayName("Allow pulse guiding")]
      [Description("Put a tick in here to allow pulse guiding.")]
      [Browsable(true)]    // Need to allow this property to be hidden at runtime
      public bool AllowPulseGuide
      {
         get
         {
            return _AllowPulseGuide;
         }
         set
         {
            Set<bool>("AllowPulseGuide", ref _AllowPulseGuide, value);
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
            Set<bool>("AllowPulseGuidingExceptions", ref _AllowPulseGuidingExceptions, value);
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
            Set<bool>("UseSynchronousParking", ref _UseSynchronousParking, value);
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
            Set<bool>("AllowExceptions", ref _AllowExceptions, value);
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
            Set<bool>("SlewWithTrackingOff", ref _SlewWithTrackingOff, value);
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
            Set<SideOfPierOption>("SideOfPier", ref _SideOfPier, value);
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
            Set<bool>("SwapPointingSideOfPier", ref _SwapPointingSideOfPier, value);
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
            Set<bool>("SwapPhysicalSideOfPier", ref _SwapPhysicalSideOfPier, value);
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
            Set<EpochOption>("Epoch", ref _Epoch, value);
         }
      }
      #endregion
      #endregion

      /// <summary>
      /// Initializes a new instance of the MainViewModel class.
      /// </summary>
      public SetupViewModel()
      {
         _Settings = SettingsProvider.Current.Settings;
         PopProperties();
      }

      protected override bool OnSaveCommand()
      {
         PushProperties();
         return base.OnSaveCommand();
      }

      public void PopProperties()
      {

         _SelectedCOMPort = COMPortService.GetCOMPortsInfo().Where(port => port.Name == Settings.COMPort).FirstOrDefault();
         _TimeOut = _Settings.Timeout;
         _Retry = _Settings.Retry;
         _BaudRate = _Settings.BaudRate;
         _PulseGuidingMode = _Settings.PulseGuidingMode;

         _IsTraceOn = _Settings.IsTracing;

         _SlewWithTrackingOff = _Settings.AscomCompliance.SlewWithTrackingOff;
         _AllowExceptions = _Settings.AscomCompliance.AllowExceptions;
         _AllowPulseGuide = _Settings.AscomCompliance.AllowPulseGuide;
         _AllowPulseGuidingExceptions = _Settings.AscomCompliance.AllowPulseGuidingExceptions;
         _UseSynchronousParking = _Settings.AscomCompliance.UseSynchronousParking;
         _Epoch = _Settings.AscomCompliance.Epoch;
         _SideOfPier = _Settings.AscomCompliance.SideOfPier;
         _SwapPointingSideOfPier = _Settings.AscomCompliance.SwapPointingSideOfPier;
         _SwapPhysicalSideOfPier = _Settings.AscomCompliance.SwapPhysicalSideOfPier;
         _StrictAscom = _Settings.AscomCompliance.Strict;

      }

      public void PushProperties()
      {
         if (SelectedCOMPort != null) {
            _Settings.COMPort = SelectedCOMPort.Name;
         }

         _Settings.Timeout = TimeOut;
         _Settings.Retry = Retry;
         _Settings.BaudRate = BaudRate;

         _Settings.PulseGuidingMode = PulseGuidingMode;

         _Settings.IsTracing = IsTraceOn;

         _Settings.AscomCompliance.SlewWithTrackingOff = SlewWithTrackingOff;
         _Settings.AscomCompliance.AllowExceptions = AllowExceptions;
         _Settings.AscomCompliance.AllowPulseGuide = AllowPulseGuide;
         _Settings.AscomCompliance.AllowPulseGuidingExceptions = AllowPulseGuidingExceptions;
         _Settings.AscomCompliance.UseSynchronousParking = UseSynchronousParking;
         _Settings.AscomCompliance.Epoch = Epoch;
         _Settings.AscomCompliance.SideOfPier = SideOfPier;
         _Settings.AscomCompliance.SwapPointingSideOfPier = SwapPointingSideOfPier;
         _Settings.AscomCompliance.SwapPhysicalSideOfPier = SwapPhysicalSideOfPier;
         _Settings.AscomCompliance.Strict = StrictAscom;

      }

      private void SetStrictAscom(bool strictAscom)
      {
         if (strictAscom) {
            AllowPulseGuide = false;
            AllowExceptions = false;
            SlewWithTrackingOff = false;
            AllowExceptions = false;
            AllowPulseGuidingExceptions = false;
            SideOfPier = SideOfPierOption.None;
            SwapPointingSideOfPier = false;
            SwapPhysicalSideOfPier = false;
         }
         this.SetBrowsableProperty("AllowPulseGuide", !strictAscom);
         this.SetBrowsableProperty("AllowExceptions", !strictAscom);
         this.SetBrowsableProperty("SlewWithTrackingOff", !strictAscom);
         this.SetBrowsableProperty("AllowExceptions", !strictAscom);
         this.SetBrowsableProperty("AllowPulseGuidingExceptions", !strictAscom);
         this.SetBrowsableProperty("SideOfPier", !strictAscom);
         this.SetBrowsableProperty("SwapPointingSideOfPier", !strictAscom);
         this.SetBrowsableProperty("SwapPhysicalSideOfPier", !strictAscom);
      }

      #region Relay commands ...
      #endregion
   }
}