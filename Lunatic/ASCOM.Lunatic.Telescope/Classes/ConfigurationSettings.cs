using ASCOM.DeviceInterface;
using ASCOM.Lunatic.Interfaces;
using GalaSoft.MvvmLight;
using Lunatic.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Lunatic.TelescopeDriver
{

   //POLARHOME_RETICULE_START=1
   //POLARHOME_GOTO_DEC=9469288
   //POLARHOME_GOTO_RA=8026995
   //POLAR_HOME_DISABLE=0
   //POLAR_RETICULE_D2=0.355
   //POLAR_RETICULE_D1=2.67
   //POLAR_RETICULE_EPOCH=2000
   //POLAR_RETICULE_TYPE=1
   //POLAR_RETICULE_START=1

   public enum PolarReticuleType
   {

      Generic,
      SyntaJ2000
   }
   public class PolarAlignment : ObservableObject
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

   public class ParkPosition : ObservableObject
   {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public int DecCount { get; set; }
      public int RACount { get; set; }

   }

   public class CustomMount : ObservableObject
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

   public class Settings : ObservableObject
   {
      public bool ASCOMCompatibilityStrict { get; set; }
      public bool OnTop { get; set; }
      // ON_TOP1=0
      // LIMIT_FILE=
      public string LimitFile { get; set; }
      // FILE_HIDDEN_DIR=0
      public bool FileHiddenDir { get; set; }

      public PolarAlignment PolarAlignment { get; set; }


      //DEC_REVERSE=0
      public bool ReverseDec { get; set; }

      //RA_REVERSE=1
      public bool ReverseRA { get; set; }

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

      //DEFAULT_UNPARK_MODE = 0
      public ParkPosition DefaultUnpark { get; set; }
      private ObservableCollection<ParkPosition> _UNParkPositions;
      public ObservableCollection<ParkPosition> UNParkPositions { get; private set; }
      //DEFAULT_PARK_MODE=2
      public ParkPosition DefaultPark { get; set; }
      private ObservableCollection<ParkPosition> _ParkPositions;
      public ObservableCollection<ParkPosition> ParkPositions { get; private set; }

      //CUSTOM_MOUNT=0
      public CustomMount CustomMount { get; set; }
      public bool IsCustomMount { get; set; }
      //PULSEGUIDE_TIMER_INTERVAL=20
      public int PulseGuidingTimeInterval { get; set; }
      //AUTOSYNCRA=1 RAAutoSync

      public bool RAAutoSync { get; set; }

      //BAR01_6=1 DecOverrideRate
      public int DecOverrideRate { get; set; }
      //BAR01_5=1    RAOverrideRate
      public int RAOverrideRate { get; set; }
      //BAR01_4=1    DecRate
      public int DecRate { get; set; }
      //BAR01_3=1    RARate
      public int RARate { get; set; }
      //BAR01_2=17   DecSlewRate
      public int DecSlewRate { get; set; }
      //BAR01_1=17   RASlewRate
      public int RASlewRate { get; set; }

      //FriendlyName=
      //ProcessPrioirty=0
      //SiteName=Lime Grove
      //Elevation=173
      //HemisphereNS=0
      //LongitudeEW=1
      //LongitudeSec=21.0
      //LongitudeMin=20
      //LongitudeDeg=1
      //LatitudeNS=0
      //LatitudeDeg=52
      //LatitudeMin=40
      //LatitudeSec=6.0
      //Retry=1
      //Timeout=1000
      //Baud=9600

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
      public bool IsTracing {
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
      //    SND_WAV_RATE3 = C:\Program Files\Common Files\ASCOM\Telescope\rate3.wav
      //      SND_WAV_RATE2 = C:\Program Files\Common Files\ASCOM\Telescope\rate2.wav
      //        SND_WAV_RATE1 = C:\Program Files\Common Files\ASCOM\Telescope\rate1.wav
      //          SND_WAV_DECREVERSEON = C:\Program Files\Common Files\ASCOM\Telescope\decreverseon.wav
      //            SND_WAV_DECREVERSEOFF = C:\Program Files\Common Files\ASCOM\Telescope\decreverseoff.wav
      //              SND_WAV_RAREVERSEON = C:\Program Files\Common Files\ASCOM\Telescope\rareverseon.wav
      //                SND_WAV_RAREVERSEOFF = C:\Program Files\Common Files\ASCOM\Telescope\rareverseoff.wav
      //                  SND_WAV_MONITOROFF = C:\Program Files\Common Files\ASCOM\Telescope\monitoroff.wav
      //                    SND_WAV_MONITORON = C:\Program Files\Common Files\ASCOM\Telescope\monitoron.wav
      //                      SND_WAV_GPLOFF = C:\Program Files\Common Files\ASCOM\Telescope\GamepadUnlocked.wav
      //                        SND_WAV_GPLON = C:\Program Files\Common Files\ASCOM\Telescope\GamepadLocked.wav
      //                          SND_WAV_DMS2 = C:\Program Files\Common Files\ASCOM\Telescope\DMSDisarmed.wav
      //                            SND_WAV_DMS = C:\Program Files\Common Files\ASCOM\Telescope\DMSArmed.wav
      //                              SND_WAV_PALIGNED = C:\Program Files\Common Files\ASCOM\Telescope\polarscopealigned.wav
      //                                SND_WAV_PALIGN = C:\Program Files\Common Files\ASCOM\Telescope\aligningpolarscope.wav
      //                                  SND_WAV_PHOME = C:\Program Files\Common Files\ASCOM\Telescope\polarhome.wav
      //                                    SND_WAV_END = C:\Program Files\Common Files\ASCOM\Telescope\end.wav
      //                                      SND_WAV_CANCEL = C:\Program Files\Common Files\ASCOM\Telescope\cancel.wav
      //                                        SND_WAV_ACCEPT = C:\Program Files\Common Files\ASCOM\Telescope\accept.wav
      //                                          SND_WAV_CUSTOM = C:\Program Files\Common Files\ASCOM\Telescope\custom.wav
      //                                            SND_WAV_SOLAR = C:\Program Files\Common Files\ASCOM\Telescope\solar.wav
      //                                              SND_WAV_LUNAR = C:\Program Files\Common Files\ASCOM\Telescope\lunar.wav
      //                                                SND_WAV_SIDEREAL = C:\Program Files\Common Files\ASCOM\Telescope\sidereal.wav
      //                                                  SND_WAV_STOP = C:\Program Files\Common Files\ASCOM\Telescope\stop.wav
      //                                                    SND_WAV_GOTOSTART = C:\Program Files\Common Files\ASCOM\Telescope\slewstart.wav
      //                                                      SND_WAV_GOTO = C:\Program Files\Common Files\ASCOM\Telescope\slewcomplete.wav
      //                                                        SND_WAV_PARKED = C:\Program Files\Common Files\ASCOM\Telescope\parked.wav
      //                                                          SND_WAV_UNPARK = C:\Program Files\Common Files\ASCOM\Telescope\unparked.wav
      //                                                            SND_WAV_PARK = C:\Program Files\Common Files\ASCOM\Telescope\parking.wav
      //                                                              SND_WAV_SYNC = C:\Program Files\Common Files\ASCOM\Telescope\sync.wav
      //                                                                SND_WAV_BEEP = EQMOD_beep.wav
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

      public AscomCompliance AscomCompliance { get; set; }

      public Settings()
      {
         this.AscomCompliance = new AscomCompliance();
         this.PolarAlignment = new PolarAlignment();
         this.CustomMount = new CustomMount();
         this.ParkPositions = new ObservableCollection<ParkPosition>();
         this.UNParkPositions = new ObservableCollection<ParkPosition>();
         SetDefaults();
      }

      private void SetDefaults()
      {
      }
   }
}
