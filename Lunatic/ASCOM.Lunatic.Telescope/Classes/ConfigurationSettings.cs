using GalaSoft.MvvmLight;
using Lunatic.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Collections;
using System.Collections.Generic;

namespace ASCOM.Lunatic.Telescope
{

   /// <summary>
   /// Class for alignment data
   /// </summary>
   public class AlignmentData : DataObjectBase
   {
      public double OriginalTargetRA { get; set; }
      public double OriginalTargetDEC { get; set; }
      public double TargetRA { get; set; }
      public double TargetDEC { get; set; }
      public double EncoderRA { get; set; }
      public double EncoderDEC { get; set; }
      public DateTime AlignmentTime { get; set; }
   }

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

      private bool _IsSlewing;
      public bool IsSlewing
      {
         get
         {
            return _IsSlewing;
         }
         set
         {
            Set<bool>(ref _IsSlewing, value);
         }
      }

      // gAscomCompatibility.AllowPulseGuide
      public PulseGuidingOption PulseGuidingMode { get; set; }

      public AscomCompliance AscomCompliance { get; set; }

      public bool ThreeStarEnable { get; set; }

      public SyncAlignmentModeOptions SyncAlignmentMode { get; set; }

      public bool DisableSyncLimit { get; set; }

      /// <summary>
      /// The RA Axis position in Radians
      /// </summary>
      public double RAAxisPosition { get; set; }

      /// <summary>
      /// The DEC Axis position in Radians
      /// </summary>
      public double DECAxisPosition { get; set; }

      /// <summary>
      /// Initial RA sync adjustment (radians)
      /// </summary>
      public double RASync01 { get; set; }

      /// <summary>
      /// Initial DEC sync adjustment (radians)
      /// </summary>
      public double DECSync01 { get; set; }

      /// <summary>
      /// Initial RA Alignment adjustment (radians)
      /// </summary>
      public double RA1Star { get; set; }

      /// <summary>
      /// Initial DEC Alignment adjustment (radians)
      /// </summary>
      public double DEC1Star { get; set; }

      /// <summary>
      /// Max Sync Diff (EQ_MAXSYNC)
      /// </summary>
      public double MaxSync { get; set; }


         /// <summary>
         /// Total Common RA-Encoder Steps
         /// </summary>
      public double Tot_step { get; set; }

      /// <summary>
      /// otal RA Encoder Steps
      /// </summary>
      public double Tot_RA { get; set; }

      /// <summary>
      /// Total DEC Encoder Steps
      /// </summary>
      public double Tot_DEC { get; set; }


      public bool EmulOneShot { get; set; }

      public List<AlignmentData> AlignmentStars { get; set; }

      public bool SaveAlignmentStarsOnAppend { get; set; }

      public bool UseAffineTakiAndPolar { get; set; }    // Formally HC.PolarEnable

      /// <summary>
      /// Alignment proximity in degrees
      /// </summary>
      public int AlignmentProximity { get; set; }      // ProximityRA


      public Settings()
      {
         this.AscomCompliance = new AscomCompliance();
         this.AlignmentStars = new List<AlignmentData>();
         SetDefaults();
      }


      private void SetDefaults()
      {
         this.SaveAlignmentStarsOnAppend = true;
      }
   }
}
