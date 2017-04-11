using Lunatic.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Collections.Specialized;
using System.Windows;
using System.Runtime.Serialization;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace Lunatic.TelescopeControl
{
   public enum PolarReticuleType
   {

      Generic,
      SyntaJ2000
   }
   public class PolarAlignment : DataObjectBase
   {


      public int HomeReticuleStart { get; set; }
      public int HomeGotoDec { get; set; }
      public int HomeGotoRa { get; set; }
      public bool Disable { get; set; }

      public double ReticuleD1 { get; set; }
      public double ReticuleD2 { get; set; }
      public int ReticuleEpoch { get; set; }
      public PolarReticuleType ReticuleType { get; set; }
      public int ReticuleStart { get; set; }

   }

   public class ParkPosition : DataObjectBase
   {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public int DecCount { get; set; }
      public int RACount { get; set; }

   }

   public class CustomMount : DataObjectBase
   {
      //CUSTOM_TRACKING_OFFSET_DEC=0
      public int TrackingDecOffset { get; set; }
      //CUSTOM_TRACKING_OFFSET_RA=0
      public int TrackingRAOffset { get; set; }
      //CUSTOM_DEC_STEPS_WORM=50133
      public int DecWormSteps { get; set; }
      //CUSTOM_RA_STEPS_WORM=50133
      public int RAWorkSteps { get; set; }
      //CUSTOM_DEC_STEPS_360=9024000
      public int DecStepsPer360 { get; set; }
      //CUSTOM_RA_STEPS_360=9024000
      public int RAStepsPer360 { get; set; }
   }

   public class SlewRatePreset : ObservableObject
   {
      public int Rate { get; private set; }

      private int _RARate;
      public int RARate
      {
         get
         {
            return _RARate;
         }
         set
         {
            Set<int>(ref _RARate, value);
         }
      }

      private int _DecRate;
      public int DecRate
      {
         get
         {
            return _DecRate;
         }
         set
         {
            Set<int>(ref _DecRate, value);
         }
      }

      public SlewRatePreset(int rate, int raRate, int decRate)
      {
         Rate = rate;
         RARate = raRate;
         DecRate = decRate;
      }
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum MountOptions
   {
      [Description("Auto detect")]
      AutoDetect,
      [Description("Custom")]
      Custom
   }





   [TypeConverter(typeof(EnumTypeConverter))]
   public enum PierSide
   {
      [Description("Unknown")]
      Unknown = -1,
      [Description("East pointing west")]
      East = 0,
      [Description("West pointing east")]
      West = 1
   }

   public enum ThreePointAlgorithm
   {
      [Description("Best Center")]
      BestCenter,
      [Description("Closest Points")]
      ClosestPoints
   }


   public class Site : DataObjectBase
   {
      public Site()
      {
         this.Id = Guid.NewGuid();
      }
      public Site(Guid id)
      {
         this.Id = id;
      }

      public Guid Id { get; private set; }
      //SiteName=Lime Grove

      private string _SiteName;
      [DisplayName("Site name")]
      [Description("Enter a name for this site.")]
      [PropertyOrder(0)]
      public string SiteName
      {
         get
         {
            return _SiteName;
         }
         set
         {
            Set<string>(ref _SiteName, value);
         }
      }

      //Elevation=173

      private double _Elevation;
      [DisplayName("Elevation (m)")]
      [Description("Enter the elevation of the site in metres.")]
      [PropertyOrder(3)]

      public double Elevation
      {
         get
         {
            return _Elevation;
         }
         set
         {
            Set<double>(ref _Elevation, value);
         }
      }
      //HemisphereNS=0
      private HemisphereOption _Hemisphere;
      [DisplayName("Hemisphere")]
      [Description("The hemisphere in which the site is located.")]
      [PropertyOrder(4)]
      public HemisphereOption Hemisphere
      {
         get
         {
            return _Hemisphere;
         }
         private set
         {
            Set<HemisphereOption>(ref _Hemisphere, value);
         }
      }

      //LatitudeNS=0
      //LatitudeDeg=52
      //LatitudeMin=40
      //LatitudeSec=6.0
      private const string LatitudePropertyName = "Latitude";
      private double _Latitude;
      [DisplayName("Latitude")]
      [Description("Enter the site latitude in the format DD MM SS(W/E) (e.g. 52 40 7N)")]
      [PropertyOrder(1)]
      public double Latitude
      {
         get
         {
            return _Latitude;
         }
         set
         {
            if (value != 0.0 && _Latitude == value) {
               return;
            }
            _Latitude = value;
            if (_Latitude == 0.0) {
               AddError(LatitudePropertyName, "Please enter a valid latitude.");
            }
            else {
               RemoveError(LatitudePropertyName);
            }
            RaisePropertyChanged();
            if (_Latitude < 0) {
               Hemisphere = HemisphereOption.Southern;
            }
            else {
               Hemisphere = HemisphereOption.Northern;
            }
         }
      }

      //LongitudeEW=1
      //LongitudeSec=21.0
      //LongitudeMin=20
      //LongitudeDeg=1

      private const string LongitudePropertyName = "Longitude";
      private double _Longitude;


      [DisplayName("Longitude")]
      [Description("Enter the site longitude in the format DD MM SS(N/S) (e.g. 1 20 21W)")]
      [PropertyOrder(2)]
      public double Longitude
      {
         get
         {
            return _Longitude;
         }
         set
         {
            if (value != 0.0 && _Longitude == value) {
               return;
            }
            _Longitude = value;
            if (_Longitude == 0.0) {
               AddError(LongitudePropertyName, "Please enter a valid longitude.");
            }
            else {
               RemoveError(LongitudePropertyName);
            }
            RaisePropertyChanged();
         }
      }

      private bool _IsCurrentSite;
      [DisplayName("Current site")]
      [Description("Tick this box if this is the current site")]
      [PropertyOrder(0)]
      public bool IsCurrentSite
      {
         get
         {
            return _IsCurrentSite;
         }
         set
         {
            Set<bool>(ref _IsCurrentSite, value);
         }
      }

   }

   public class SiteCollection : ObservableCollection<Site>
   {

      public event EventHandler<EventArgs> CurrentSiteChanged;
      public new event EventHandler<PropertyChangedEventArgs> PropertyChanged;

      private Site _CurrentSite;
      [DisplayName("Current site")]
      [Description("The currently selected site")]
      public Site CurrentSite
      {
         get
         {
            return _CurrentSite;
         }
         private set
         {
            if (ReferenceEquals(_CurrentSite, value)) {
               return;
            }
            _CurrentSite = value;
            RaisePropertyChanged("CurrentSite");
         }
      }

      private bool resetingCurrentSite = false;
      public SiteCollection() : base() { }


      [OnDeserialized]
      private void Deserialized(StreamingContext context)
      {
         this.CurrentSite = this.Items.Where(s => s.IsCurrentSite).FirstOrDefault();
         // WeakEventManager<SiteCollection, EventArgs>.AddHandler(this.Sites, "CurrentSiteChanged", Sites_CurrentSiteChanged);
      }


      protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
      {
         if (e.Action == NotifyCollectionChangedAction.Add) {
            foreach (Site site in e.NewItems) {
               // First remove the event because OnCollection changed is called twice when deserialising.
               WeakEventManager<Site, PropertyChangedEventArgs>.RemoveHandler(site, "PropertyChanged", Site_PropertyChanged);
               WeakEventManager<Site, PropertyChangedEventArgs>.AddHandler(site, "PropertyChanged", Site_PropertyChanged);
            }
         }
         else if (e.Action == NotifyCollectionChangedAction.Remove) {
            foreach (Site site in e.OldItems) {
               WeakEventManager<Site, PropertyChangedEventArgs>.RemoveHandler(site, "PropertyChanged", Site_PropertyChanged);
            }
         }
         base.OnCollectionChanged(e);

      }

      private void Site_PropertyChanged(object sender, PropertyChangedEventArgs e)
      {
         Site site = sender as Site;
         if (e.PropertyName == "IsCurrentSite") {
            if (!resetingCurrentSite) {
               if (site.IsCurrentSite) {
                  resetingCurrentSite = true;
                  foreach (Site switchoff in Items.Where(s => s.Id != site.Id && s.IsCurrentSite)) {
                     switchoff.IsCurrentSite = false;
                  }
                  CurrentSite = site;
                  OnCurrentSiteChanged();
                  resetingCurrentSite = false;
               }
            }
         }
         if (site.IsCurrentSite) {
            RaisePropertyChanged("CurrentSite." + e.PropertyName);
         }
      }


      private void OnCurrentSiteChanged()
      {
         CurrentSiteChanged?.Invoke(this, EventArgs.Empty);
      }

      private void RaisePropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }

   public class TelescopeControlSettings : DataObjectBase
   {
      #region Properties ...

      public string DriverId { get; set; }

      public DisplayMode DisplayMode { get; set; }

      private SiteCollection _Sites;
      public SiteCollection Sites
      {
         get
         {
            return _Sites;
         }
         private set
         {
            _Sites = value;
         }
      }

      public Site CurrentSite { get; set; }
      //Retry=1

      public bool OnTop { get; set; }
      // ON_TOP1=0
      // LIMIT_FILE=
      public string LimitFile { get; set; }
      // FILE_HIDDEN_DIR=0
      public bool FileHiddenDir { get; set; }

      public PolarAlignment PolarAlignment { get; set; }


      //DEC_REVERSE=0
      private bool _ReverseDec;
      public bool ReverseDec
      {
         get
         {
            return _ReverseDec;
         }
         set
         {
            Set<bool>(ref _ReverseDec, value);
         }
      }

      //RA_REVERSE=1
      private bool _ReverseRA = true;
      public bool ReverseRA
      {
         get
         {
            return _ReverseRA;
         }
         set
         {
            Set<bool>(ref _ReverseRA, value);
         }
      }

      //DSYNC01=0
      public int DecSync { get; set; }
      //RSYNC01=0
      public int RASync { get; set; }

      //DALIGN01=0 gDEC1Star
      public double Dec1Star { get; set; }

      //RALIGN01=0 gRA1Star
      public double RA1Star { get; set; }

      //BAR03_2=809  // From Mouse SlewPad
      //BAR03_1=809  // From Mouse SlewPad
      //SlewPadHeight=7875
      //SlewPadWidth=7470

      //UNPARK_DEC=9003010 gDECEncoderUNPark
      public int DecEncoderUNParkPos { get; set; }
      //UNPARK_RA=8388570 gRAEncoderUNPark
      public int RAEncoderUNParkPos { get; set; }
      //LASTPOS_DEC=8375645 gDECEncoderlastpos
      public int DECEncoderLastPos { get; set; }
      //LASTPOS_RA=9150089 gRAEncoderlastpos
      public int RAEncoderLastPos { get; set; }
      //TimeDelta= 0 gEQTimeDelta
      public double TimeDelta { get; set; }


      //DEFAULT_UNPARK_MODE = 0
      public ParkPosition DefaultUnpark { get; set; }
      public ObservableCollection<ParkPosition> UNParkPositions { get; private set; }
      //DEFAULT_PARK_MODE=2
      public ParkPosition DefaultPark { get; set; }
      public ObservableCollection<ParkPosition> ParkPositions { get; private set; }

      //CUSTOM_MOUNT=0
      public CustomMount CustomMount { get; set; }
      public MountOptions MountOption { get; set; }


      //PULSEGUIDE_TIMER_INTERVAL=20
      public int PulseGuidingTimeInterval { get; set; }
      //AUTOSYNCRA=1 RAAutoSync

      public bool RAAutoSync { get; set; }

      //BAR01_6=1 DecOverrideRate
      public int DecOverrideRate { get; set; }
      //BAR01_5=1  RAOverrideRate
      public int RAOverrideRate { get; set; }
      //BAR01_4=1  DecRate
      public int DecRate { get; set; }
      //BAR01_3=1  RARate
      public int RARate { get; set; }
      //BAR01_2=17 DecSlewRate

      public ObservableCollection<SlewRatePreset> SlewRatePresets { get; private set; }

      private SlewRatePreset _SlewRatePreset;

      /// <summary>
      /// The currently selected slew preset. Not stored in the config settings as
      /// always starts off on the lowest setting.
      /// </summary>
      [JsonIgnore]
      public SlewRatePreset SlewRatePreset
      {
         get
         {
            return _SlewRatePreset;
         }
         set
         {
            Set<SlewRatePreset>(ref _SlewRatePreset, value);
         }
      }

      //FLIP_AUTO_ENABLED = False
      //FLIP_AUTO_ALLOWED=False
      //LIMIT_SLEWS = 1
      //LIMIT_PARK=0
      //LIMIT_HORIZON_ALGORITHM=0
      //LIMIT_ENABLE=0
      //RA_LIMIT_WEST=9009016
      //RA_LIMIT_EAST=7768200
      //COORD_TYPE=0
      //MOUNT_TYPE=1
      //SIDEREAL_RATE=15.041067
      //ASCOM_COMPAT_SWAP_SOP=0
      //ASCOM_COMPAT_SWAP_PSOP=0
      //ASCOM_COMPAT_SOP=0
      //ASCOM_COMPAT_EPOCH=0
      //ASCOM_COMPAT_SITEWRITES=False
      //ASCOM_COMPAT_BLOCK_PARK = False
      //ASCOM_COMPAT_PG_EXCEPTIONS=True
      //ASCOM_COMPAT_EXCEPTIONS = True
      //UpdateMode=0
      //UpdateTestUrl=http://tech.groups.yahoo.com/group/EQMOD/
      //UpdateReleaseUrl=http://sourceforge.net/projects/eq-mod/files/
      //UpdateFileUrl=http://eq-mod.sourceforge.net/versions/versions.txt
      //AUTOGUIDER_DEC=External
      //AUTOGUIDER_RA = External
      //PoleStarId=0
      //PolarReticuleEpoch=2000
      //ALIGN_LOCALTOPIER=1
      //ALIGN_SELECTION=0
      //ALIGN_PROXIMITY=0
      //NSTAR_MAXCOMBINATION=50
      //APPENDSYNCNSTAR=1
      //SYNCNSTAR=0
      //DISABLE_FLIPGOTO_RESET=0
      //GOTO_RATE=0
      //CUSTOM_TRACKFILE=
      //CUSTOM_DEC=0
      //CUSTOM_RA=150.41067
      //SND_ENABLE_REVERSE=1
      //SND_ENABLE_MONITOR=1
      //SND_ENABLE_GPL=1
      //SND_ENABLE_DMS=1
      //SND_ENABLE_POLAR=1
      //SND_ENABLE_ALIGN=1
      //SND_ENABLE_TRACKING=1
      //SND_ENABLE_STOP=1
      //SND_ENABLE_GOTOSTART=1
      //SND_ENABLE_GOTO=1
      //SND_ENABLE_PARKED=1
      //SND_ENABLE_UNPARK=1
      //SND_ENABLE_PARK=1
      //SND_ENABLE_RATE=1
      //SND_ENABLE_ALARM=1
      //SND_ENABLE_CLICK=0
      //SND_ENABLE_BEEP=0
      //SND_MODE=1
      //SND_WAV_RATE10=EQMOD_click.wav
      //SND_WAV_RATE9 = EQMOD_click.wav
      //SND_WAV_RATE8=EQMOD_click.wav
      //SND_WAV_RATE7 = EQMOD_click.wav
      //SND_WAV_RATE6=EQMOD_click.wav
      //SND_WAV_RATE5 = C:\Program Files\Common Files\ASCOM\Telescope\rate5.wav
      //  SND_WAV_RATE4 = C:\Program Files\Common Files\ASCOM\Telescope\rate4.wav
      //  SND_WAV_RATE3 = C:\Program Files\Common Files\ASCOM\Telescope\rate3.wav
      //  SND_WAV_RATE2 = C:\Program Files\Common Files\ASCOM\Telescope\rate2.wav
      //  SND_WAV_RATE1 = C:\Program Files\Common Files\ASCOM\Telescope\rate1.wav
      //  SND_WAV_DECREVERSEON = C:\Program Files\Common Files\ASCOM\Telescope\decreverseon.wav
      //  SND_WAV_DECREVERSEOFF = C:\Program Files\Common Files\ASCOM\Telescope\decreverseoff.wav
      //  SND_WAV_RAREVERSEON = C:\Program Files\Common Files\ASCOM\Telescope\rareverseon.wav
      //  SND_WAV_RAREVERSEOFF = C:\Program Files\Common Files\ASCOM\Telescope\rareverseoff.wav
      //  SND_WAV_MONITOROFF = C:\Program Files\Common Files\ASCOM\Telescope\monitoroff.wav
      //  SND_WAV_MONITORON = C:\Program Files\Common Files\ASCOM\Telescope\monitoron.wav
      //  SND_WAV_GPLOFF = C:\Program Files\Common Files\ASCOM\Telescope\GamepadUnlocked.wav
      //  SND_WAV_GPLON = C:\Program Files\Common Files\ASCOM\Telescope\GamepadLocked.wav
      //  SND_WAV_DMS2 = C:\Program Files\Common Files\ASCOM\Telescope\DMSDisarmed.wav
      //  SND_WAV_DMS = C:\Program Files\Common Files\ASCOM\Telescope\DMSArmed.wav
      //  SND_WAV_PALIGNED = C:\Program Files\Common Files\ASCOM\Telescope\polarscopealigned.wav
      //  SND_WAV_PALIGN = C:\Program Files\Common Files\ASCOM\Telescope\aligningpolarscope.wav
      //  SND_WAV_PHOME = C:\Program Files\Common Files\ASCOM\Telescope\polarhome.wav
      //  SND_WAV_END = C:\Program Files\Common Files\ASCOM\Telescope\end.wav
      //  SND_WAV_CANCEL = C:\Program Files\Common Files\ASCOM\Telescope\cancel.wav
      //  SND_WAV_ACCEPT = C:\Program Files\Common Files\ASCOM\Telescope\accept.wav
      //  SND_WAV_CUSTOM = C:\Program Files\Common Files\ASCOM\Telescope\custom.wav
      //  SND_WAV_SOLAR = C:\Program Files\Common Files\ASCOM\Telescope\solar.wav
      //  SND_WAV_LUNAR = C:\Program Files\Common Files\ASCOM\Telescope\lunar.wav
      //  SND_WAV_SIDEREAL = C:\Program Files\Common Files\ASCOM\Telescope\sidereal.wav
      //  SND_WAV_STOP = C:\Program Files\Common Files\ASCOM\Telescope\stop.wav
      //  SND_WAV_GOTOSTART = C:\Program Files\Common Files\ASCOM\Telescope\slewstart.wav
      //  SND_WAV_GOTO = C:\Program Files\Common Files\ASCOM\Telescope\slewcomplete.wav
      //  SND_WAV_PARKED = C:\Program Files\Common Files\ASCOM\Telescope\parked.wav
      //  SND_WAV_UNPARK = C:\Program Files\Common Files\ASCOM\Telescope\unparked.wav
      //  SND_WAV_PARK = C:\Program Files\Common Files\ASCOM\Telescope\parking.wav
      //  SND_WAV_SYNC = C:\Program Files\Common Files\ASCOM\Telescope\sync.wav
      //  SND_WAV_BEEP = EQMOD_beep.wav
      //SND_WAV_CLICK=EQMOD_click.wav
      //SND_WAV_ALARM = EQMOD_klaxton.wav
      //LST_DISPLAY_MODE=0
      //COMMS_ERROR_STOP=0
      //GOTO_RA_COMPENSATE=40
      //GOTO_RESOLUTION=20
      //MAX_GOTO_INTERATIONS=5
      //3POINT_ALGORITHM=0
      //POLAR_ALIGNMENT=0
      //Advanced=0
      //LANG_DLL=
      //form_left=3420
      //form_top=1785
      //form_height=8640
      //ASCOM_COMPAT_PULSEGUIDE=True
      //ASCOM_COMPAT_SLEWTRACKOFF = True

      public bool ThreePointAlignment { get; set; }

      #endregion

      public TelescopeControlSettings()
      {
         this.DriverId = string.Empty;
         this.DisplayMode = DisplayMode.MountPosition;
         this.CustomMount = new CustomMount();
         this.ParkPositions = new ObservableCollection<ParkPosition>();
         this.UNParkPositions = new ObservableCollection<ParkPosition>();
         this.Sites = new SiteCollection();
         this.SlewRatePresets = new ObservableCollection<SlewRatePreset>();
      }

      [OnDeserialized]
      private void Deserialized(StreamingContext context)
      {
         this.CurrentSite = this.Sites.Where(s => s.IsCurrentSite).FirstOrDefault();
         if (this.SlewRatePresets.Count == 0) {
            this.SlewRatePresets.Add(new SlewRatePreset(1, 1, 1));
            this.SlewRatePresets.Add(new SlewRatePreset(2, 8, 8));
            this.SlewRatePresets.Add(new SlewRatePreset(3, 64, 64));
            this.SlewRatePresets.Add(new SlewRatePreset(4, 400, 400));
            this.SlewRatePresets.Add(new SlewRatePreset(5, 800, 800));
         }
         // Always start with the lowest rate selected.
         this.SlewRatePreset = this.SlewRatePresets.OrderBy(p => p.Rate).FirstOrDefault();
      }
   }
}
