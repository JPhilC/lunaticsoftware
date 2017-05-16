using Lunatic.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections;
using System.Runtime.InteropServices;

namespace Lunatic.SyntaController
{

   [ComVisible(false)]
   public class ControllerSettings : DataObjectBase
   {

      public ParkStatus ParkStatus { get; set; }

      /// <summary>
      /// RA ParkPosition in Radians
      /// </summary>
      public double RAParkPosition { get; set; }

      /// <summary>
      /// DEC ParkPosition in Radians
      /// </summary>
      public double DECParkPosition { get; set; }

      public ControllerSettings()
      {
         ParkStatus = ParkStatus.Parked;

      }

   }
}
