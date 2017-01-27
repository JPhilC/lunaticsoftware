using ASCOM.Lunatic.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Lunatic.Core;
using Lunatic.Core.Services;
using System.Collections.Generic;
using System.Linq;
using static Lunatic.Core.Services.COMPortService;

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

      private COMPortInfo _SelectedCOMPort;

      public COMPortInfo SelectedCOMPort
      {

         get
         {
            return _SelectedCOMPort;
         }
         set
         {
            _SelectedCOMPort = value;
            if (_SelectedCOMPort != null) {
               Settings.COMPort = _SelectedCOMPort.Name;
            }
            else {
               Settings.COMPort = string.Empty;
            }

            RaisePropertyChanged();
            RaisePropertyChanged("COMPortName");
         }
      }


      private List<COMPortInfo> _AvailableCOMPorts = new List<COMPortInfo>();
      public List<COMPortInfo> AvailableCOMPorts
      {
         get
         {
            return _AvailableCOMPorts;
         }
      }


      #endregion

      /// <summary>
      /// Initializes a new instance of the MainViewModel class.
      /// </summary>
      public SetupViewModel(ISettingsProvider settingsProvider)
      {
         _Settings = settingsProvider.CurrentSettings;
      }

      public void RefreshCOMPorts()
      {
         _AvailableCOMPorts.Clear();
         foreach (COMPortInfo comPort in COMPortService.GetCOMPortsInfo()) {
            _AvailableCOMPorts.Add(comPort);
         }
         _SelectedCOMPort = AvailableCOMPorts.Where(port => port.Name == Settings.COMPort).FirstOrDefault();
      }


   }
}