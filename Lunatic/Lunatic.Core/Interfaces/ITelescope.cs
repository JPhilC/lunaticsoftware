using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   /// <summary>
   /// Provides additional interface methods that the Lunatic Telescope Control program
   /// can make use of
   /// </summary>
   public interface ITelescope
   {
      TrackingStatus TrackingState {get; set; }
      HemisphereOption Hemisphere { get; set; }

      SyncModeOption SyncMode { get; set; }
   }
}
