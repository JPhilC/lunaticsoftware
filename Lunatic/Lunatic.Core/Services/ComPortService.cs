using System;
using System.Management;
using System.Collections.Generic;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

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
         return ReferenceEquals(comPort1, comPort2)
           || (!ReferenceEquals(comPort1, null) && comPort1.Equals(comPort2));
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
      public ItemCollection GetValues()
      {
         ItemCollection ports = new ItemCollection();
         foreach (COMPortInfo comPort in COMPortService.GetCOMPortsInfo()) {
            ports.Add(comPort, comPort.Name);
         }
         return ports;
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
   }
}
