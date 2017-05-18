using GalaSoft.MvvmLight;
using Lunatic.Core;
using Core = Lunatic.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Collections;
using System.Collections.Generic;
using Lunatic.SyntaController;
using Lunatic.Core.Classes;
using Lunatic.Core.Geometry;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace ASCOM.Lunatic.Telescope
{
   public class DevelopmentOptions
   {
      public bool ShowAdvancedOptions { get; set; }
      public bool ShowPolarAlign { get; set; }

      public bool Use3PointAlgorithm { get; set; }

      [Description("Maximum Goto Iterations")]
      public int MaximumSlewCount { get; set; }

      public int GotoResolution { get; set; }

      [Description("Goto RA Compensation")]
      public int GotoRACompensation { get; set; }

      public bool ListDisplayMode { get; set; }
   }

   public class CustomMount
   {
      //CUSTOM_TRACKING_OFFSET_DEC=0
      public int TrackingDecOffset { get; set; }
      //CUSTOM_TRACKING_OFFSET_RA=0
      public int TrackingRAOffset { get; set; }
      //CUSTOM_DEC_STEPS_WORM=50133
      public int DecWormSteps { get; set; }
      //CUSTOM_RA_STEPS_WORM=50133
      public int RAWormSteps { get; set; }
      //CUSTOM_DEC_STEPS_360=9024000
      public int DecStepsPer360 { get; set; }
      //CUSTOM_RA_STEPS_360=9024000
      public int RAStepsPer360 { get; set; }
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum MountOptions
   {
      [Description("Auto detect")]
      AutoDetect,
      [Description("Custom")]
      Custom
   }

   public class Settings 
   {
      /// <summary>
      /// The encoder timer interval in milliseconds. Controls
      /// how ofen the mount is queried for it's current position.
      /// </summary>
      public int EncoderTimerInterval { get; set; }

      /// <summary>
      /// The pulse timer interval in milliseconds.
      /// </summary>
      public int PulseTimerInterval { get; set; }

      //CUSTOM_MOUNT=0
      public CustomMount CustomMount { get; set; }
      public MountOptions MountOption { get; set; }


      public DevelopmentOptions DevelopmentOptions { get; set; }

      public bool ASCOMCompatibilityStrict { get; set; }
      //FriendlyName=
      //ProcessPrioirty=0

      public RetryOption Retry { get; set; }
      //Timeout=1000
      public TimeOutOption Timeout { get; set; }
      //Baud=9600
      public BaudRate BaudRate { get; set; }

      //Port=COM4
      public string COMPort { get; set; }

      // TRACE_STATE
      public bool IsTracing { get; set; }

      //EQPARKSTATUS=parked
      public ParkStatus ParkStatus { get; set; }

      public AxisPosition AxisUnparkPosition { get; set; }


      [Obsolete("Use AxisUnparkPosition"), JsonIgnore()]
      public int RAEncoderUnparkPosition { get; set; }
      [Obsolete("Use AxisUnparkPosition"), JsonIgnore()]
      public int DECEncoderUnparkPosition { get; set; }

      public AxisPosition AxisParkPosition { get; set; }

      [Obsolete("Use AxisParkPosition"), JsonIgnore()]
      public int RAEncoderParkPosition { get; set; }
      [Obsolete("Use AxisParkPosition"), JsonIgnore()]
      public int DECEncoderParkPosition { get; set; }

      public AutoguiderPortRate RAAutoGuiderPortRate { get; set; }
      public AutoguiderPortRate DECAutoGuiderPortRate { get; set; }

      public bool CheckRASync { get; set; }

      // gAscomCompatibility.AllowPulseGuide
      public PulseGuidingOption PulseGuidingMode { get; set; }

      public AscomCompliance AscomCompliance { get; set; }

      public bool ThreeStarEnable { get; set; }

      public SyncAlignmentModeOptions SyncAlignmentMode { get; set; }

      public bool DisableSyncLimit { get; set; }


      public AxisPosition AxisHomePosition { get; set; }


      public MountCoordinate CurrentMountPosition { get; set; }
      
      /// <summary>
      /// The RA Axis position in Radians
      /// </summary>
      [Obsolete("Use CurrentMountPosition instead"), JsonIgnore()]
      public double RAAxisPosition { get; set; }

      /// <summary>
      /// The DEC Axis position in Radians
      /// </summary>
      [Obsolete("Use CurrentMountPosition instead"), JsonIgnore()]
      public double DECAxisPosition { get; set; }

      /// <summary>
      /// Initial RA sync adjustment (Radians)
      /// </summary>
      [Obsolete("Use InitialAxisSyncAdjustment instead"), JsonIgnore()]
      public double RASync01 { get; set; }

      /// <summary>
      /// Initial DEC sync adjustment (Radians)
      /// </summary>
      [Obsolete("Use InitialAxisSyncAdjustment instead"), JsonIgnore()]
      public double DECSync01 { get; set; }


      /// <summary>
      /// Initial sync adjustment (Radians)
      /// </summary>
      public AxisPosition InitialAxisSyncAdjustment { get; set; }

      /// <summary>
      /// Initial RA Alignment adjustment (Radians)
      /// </summary>
      [Obsolete("Use InitialAxisAlignmentAdjustment instead"), JsonIgnore()]
      public double RA1Star { get; set; }

      /// <summary>
      /// Initial DEC Alignment adjustment (Radians)
      /// </summary>
      [Obsolete("Use InitialAxisAlignmentAdjustment instead"), JsonIgnore()]
      public double DEC1Star { get; set; }

      /// <summary>
      /// Initial alignment adjustment
      /// </summary>
      public AxisPosition InitialAxisAlignmentAdjustment { get; set; }
      /// <summary>
      /// Max Sync Diff (EQ_MAXSYNC)
      /// </summary>
      public double MaxSync { get; set; }


      /// <summary>
      /// Total Common RA-Encoder Steps
      /// </summary>
      public int Tot_step { get; set; }

      public bool EmulOneShot { get; set; }

      public bool CommsErrorStop { get; set; }

      public AlignmentPointCollection AlignmentPoints { get; set; }

      public bool SaveAlignmentStarsOnAppend { get; set; }

      public PointFilterOption AlignmentPointFilter { get; set; }

      public bool UseAffineTakiAndPolar { get; set; }    // Formally HC.PolarEnable

      /// <summary>
      /// Alignment proximity in degrees
      /// </summary>
      public int AlignmentProximity { get; set; }      // ProximityRA


      public bool TrackUsingPEC { get; set; }            // Track using PEC (HC.CheckPEC.Value)

      public string CustomTrackName { get; set; }

      public Settings()
      {
         this.AscomCompliance = new AscomCompliance();
         this.AlignmentPoints = new AlignmentPointCollection();
         this.DevelopmentOptions = new DevelopmentOptions();
         SetDefaults();
      }


      private void SetDefaults()
      {
         this.SaveAlignmentStarsOnAppend = true;
         this.DevelopmentOptions.MaximumSlewCount = 5;
         this.DevelopmentOptions.GotoResolution = 20;
         this.DevelopmentOptions.GotoRACompensation = 40;
         this.EncoderTimerInterval = 200; // Default to 200 milliseconds.
         this.PulseTimerInterval = 1000;  // Default to 1000 milliseconds.
         // Default the current mount position to NCP from Greenwich Observatory (as good as anywhere).
         this.CurrentMountPosition = new MountCoordinate(new AltAzCoordinate(51.4769, 0.0));
         // Default the axis home position to 0 RA, 90 degrees DEC (but in radians)
         this.AxisHomePosition = new AxisPosition(0, Core.Constants.HALF_PI);
      }
   }
}
