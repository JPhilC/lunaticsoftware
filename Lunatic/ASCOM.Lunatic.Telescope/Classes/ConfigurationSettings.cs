using GalaSoft.MvvmLight;
using Lunatic.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Collections;

namespace ASCOM.Lunatic.Telescope
{


   public class Settings : DataObjectBase
   {
      public bool ASCOMCompatibilityStrict { get; set; }
      //FriendlyName=
      //ProcessPrioirty=0

      public RetryOption Retry { get; set; }
      //Timeout=1000
      public TimeOutOption Timeout { get; set; }
      //Baud=9600
      public BaudRate BaudRate { get; set; }

      //Port=COM4
      private string _COMPort = string.Empty;
      public string COMPort
      {
         get
         {
            return _COMPort;
         }
         set
         {
            if (value == _COMPort) {
               return;
            }
            _COMPort = value;
            RaisePropertyChanged();
         }
      }

      // TRACE_STATE
      private bool _IsTracing = false;
      public bool IsTracing
      {
         get
         {
            return _IsTracing;
         }
         set
         {
            if (value == _IsTracing) {
               return;
            }
            _IsTracing = value;
            RaisePropertyChanged();
         }
      }

      //EQPARKSTATUS=parked
      private ParkStatus _ParkStatus;
      public ParkStatus ParkStatus
      {
         get
         {
            return _ParkStatus;
         }
         set
         {
            if (value == _ParkStatus) {
               return;
            }
            _ParkStatus = value;
            RaisePropertyChanged();
         }
      }

      // gAscomCompatibility.AllowPulseGuide
      public PulseGuidingOption PulseGuidingMode { get; set; }

      public AscomCompliance AscomCompliance { get; set; }

      public Settings()
      {
         this.AscomCompliance = new AscomCompliance();
         SetDefaults();
      }

      private void SetDefaults()
      {
      }
   }
}
