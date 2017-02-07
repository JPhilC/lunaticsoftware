using System;
using System.Management;
using System.Collections.Generic;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.IO.Ports;
using System.Linq;

/* Thanks to:
 * Dario Santarelli
 *  https://dariosantarelli.wordpress.com/2010/10/18/c-how-to-programmatically-find-a-com-port-by-friendly-name/
*/

namespace Lunatic.Core.Services
{
   internal class ProcessConnection
   {

      public static ConnectionOptions ProcessConnectionOptions()
      {
         ConnectionOptions options = new ConnectionOptions();
         options.Impersonation = ImpersonationLevel.Impersonate;
         options.Authentication = AuthenticationLevel.Default;
         options.EnablePrivileges = true;
         return options;
      }

      public static ManagementScope ConnectionScope(string machineName, ConnectionOptions options, string path)
      {
         ManagementScope connectScope = new ManagementScope();
         connectScope.Path = new ManagementPath(@"\\" + machineName + path);
         connectScope.Options = options;
         connectScope.Connect();
         return connectScope;
      }
   }

   public class COMPortInfo : IEquatable<COMPortInfo>
   {
      public string Name { get; private set; }
      public string Description { get; private set; }

      public COMPortInfo() { }

      public COMPortInfo(string name, string description)
      {
         this.Name = name;
         this.Description = description;
      }


      public bool Equals(COMPortInfo other)
      {
         return !ReferenceEquals(other, null) &&
           Name == other.Name &&
           Description == other.Description;
      }

      public override bool Equals(object obj)
      {
         return Equals(obj as COMPortInfo);
      }
      public static bool operator !=(COMPortInfo comPort1, COMPortInfo comPort2)
      {
         return !(comPort1 == comPort2);
      }
      public static bool operator ==(COMPortInfo comPort1, COMPortInfo comPort2)
      {
         if (object.ReferenceEquals(comPort1, comPort2)) {
            return true;
         }
         if (((object)comPort1 == null) || ((object)comPort2 == null)) {
            return false;
         }
         return (comPort1.Name == comPort2.Name &&
            comPort1.Description == comPort2.Description);
      }

      public override int GetHashCode()
      {
         int hash = 13;
         hash = (hash * 7) + Name.GetHashCode();
         hash = (hash * 7) + Description.GetHashCode();
         return hash;
      }
   }

   public class COMPortService : IItemsSource
   {
      private static readonly Destructor Finalise = new Destructor();

      private sealed class Destructor
      {
         ~Destructor()
         {
            COMPortService.CleanUp();
         }
      }


      private static ManagementEventWatcher _Watcher;

      private static int _ListenerCount = 0;

      public ItemCollection GetValues()
      {
         ItemCollection ports = new ItemCollection();
         foreach (COMPortInfo comPort in COMPortService.GetCOMPortsInfo()) {
            ports.Add(comPort, comPort.Name);
         }
         return ports;
      }

      static COMPortService()
      {
         MonitorDeviceChanges();
      }

      public static void AddListener()
      {
         _ListenerCount++;
         if (_ListenerCount >=1) {
            _Watcher.Start();
         }
      }

      public static void RemoveListener()
      {
         _ListenerCount--;
         if (_ListenerCount <= 0) {
            _Watcher.Stop();
         }
      }

      public static void CleanUp()
      {
         _Watcher.Stop();
      }


      public static List<COMPortInfo> GetCOMPortsInfo()
      {
         List<COMPortInfo> comPortInfoList = new List<COMPortInfo>();

         ConnectionOptions options = ProcessConnection.ProcessConnectionOptions();
         ManagementScope connectionScope = ProcessConnection.ConnectionScope(Environment.MachineName, options, @"\root\CIMV2");

         ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0");
         ManagementObjectSearcher comPortSearcher = new ManagementObjectSearcher(connectionScope, objectQuery);

         using (comPortSearcher) {
            string caption = null;
            foreach (ManagementObject obj in comPortSearcher.Get()) {
               if (obj != null) {
                  object captionObj = obj["Caption"];
                  if (captionObj != null) {
                     caption = captionObj.ToString();
                     if (caption.Contains("(COM")) {
                        COMPortInfo comPortInfo = new COMPortInfo(
                           caption.Substring(caption.LastIndexOf("(COM")).Replace("(", string.Empty).Replace(")",
                                                             string.Empty),
                           caption);
                        comPortInfoList.Add(comPortInfo);
                     }
                  }
               }
            }
         }
         return comPortInfoList;
      }

      public static event EventHandler PortRemoved;

      private static void MonitorDeviceChanges()
      {
         try {
            var deviceRemovalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");
            _Watcher = new ManagementEventWatcher(deviceRemovalQuery);
            _Watcher.EventArrived += (sender, eventArgs) => RaisePortRemoved();
         }
         catch (ManagementException err) {

         }
      }

      private static void RaisePortRemoved()
      {
         var handler = PortRemoved;
         if (handler != null) {
            handler(null, new EventArgs());
         }
      }

   }

}
