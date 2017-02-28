using Lunatic.SyntaController;
using Lunatic.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ASCOM.Lunatic
{
   public static class SharedResources
   {

      #region Shared Serial Port Connection
      public static MountController Controller
      {
         get
         {
            return MountController.Instance; ;
         }
      }

      public static bool IsConnected
      {
         get
         {
            return Controller.IsConnected;
         }
      }




      //private static void WatchCOMPorts(bool AddWatch)
      //{
      //   if (AddWatch) {
      //      COMPortService.AddListener();
      //      COMPortService.PortRemoved += COMPortService_PortRemoved;
      //   }
      //   else {
      //      COMPortService.PortRemoved -= COMPortService_PortRemoved;
      //      COMPortService.RemoveListener();
      //   }
      //}

      //private static void COMPortService_PortRemoved(object sender, EventArgs e)
      //{
      //   // Check that the current com port is still in the list of available ports
      //   if (_Connection != null) {
      //      if (!_Connection.IsOpen) {
      //         _Connection.Close();
      //      }
      //   }
      //}

      //public static event EventHandler COMPortsChanged;

      #endregion
   }
}
