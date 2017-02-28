using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   [Flags]
   public enum AxisState
   {
      Stopped = 0x0001,             // The axis is in a fully stopped state
      Slewing = 0x0002,             // The axis is in constant speed operation
      Slewing_To = 0x0004,          // The axis is in the process of running to the specified target position
      Slewing_Forward = 0x0008,     // The axis runs forward
      Slewing_Highspeed = 0x0010,   // The axis is in high-speed operation
      Not_Initialised = 0x0020      // MC controller has not been initialized, axis is not initialized.
   }

   // Two-axis telescope code
   public enum AxisId { Axis1_RA = 0, Axis2_DEC = 1 }; 

}
