using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Lunatic.Core.Classes;
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
            RaisePropertyChanged();
            RaisePropertyChanged("COMPortName");
         }
      }

      public string COMPortName
      {
         get
         {
            return (SelectedCOMPort != null ? SelectedCOMPort.Name : string.Empty);
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


      private bool _TraceState;

      public bool TraceState
      {
         get
         {
            return _TraceState;
         }
         set
         {
            if (value == _TraceState) {
               return;
            }
            _TraceState = value;
            RaisePropertyChanged();
         }
      }
      #endregion

      /// <summary>
      /// Initializes a new instance of the MainViewModel class.
      /// </summary>
      public SetupViewModel()
      {
         Initialise();
      }

      public void RefreshCOMPorts()
      {
         _AvailableCOMPorts.Clear();
         foreach (COMPortInfo comPort in COMPortService.GetCOMPortsInfo()) {
            _AvailableCOMPorts.Add(comPort);
         }
         SelectedCOMPort = AvailableCOMPorts.Where(port => port.Name == SyntaTelescope.COM_PORT).FirstOrDefault();
      }

      private void Initialise()
      {
         TraceState = SyntaTelescope.TRACE_STATE;
      }

      protected override bool OnSaveCommand()
      {
         SyntaTelescope.COM_PORT = this.COMPortName;
         SyntaTelescope.TRACE_STATE = this.TraceState;
         return base.OnSaveCommand();
      }

   }
}