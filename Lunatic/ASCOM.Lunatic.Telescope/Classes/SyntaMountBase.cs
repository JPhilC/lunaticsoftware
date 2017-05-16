using GalaSoft.MvvmLight;
using Lunatic.Core;
using Lunatic.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// Imported from https://github.com/skywatcher-pacific/skywatcher_open

/// Notes:
/// 1. Use exception instead of ErrCode because there is no dll import issue we need to handle and 
/// the exception code stlye will much easier to maintain.
/// 2. Need to confirm the mapping between SerialPort class in C# and DCB class in C++, such as CTS.
/// 3. Rename UpdateAxisPosition and UpdateAxesStatus to GetAxisPosition and GetAxesStatus to hide the details
/// 4. LastSlewingIsPositive has been merge with AxesStatus.SLEWING_FORWARD
///
/// 5. While bluetooth connection fail, user should apply Connect_COM and try to connect again
/// RTSEnable may not be accepcted 
/// http://blog.csdn.net/solond/archive/2008/03/04/2146446.aspx
/// 6. It looks like Skywatcher mounts response time is 1.5x longer than Celestron's mount

namespace ASCOM.Lunatic.Telescope
{
   /// <summary>
   /// Define the abstract interface of a Mount, includes:
   /// 1) Connection via Serial Port    
   /// 2) Protocol 
   /// 3) Basic Mount control interface 
   /// LV0. 
   /// TalkWithAxis
   /// LV1. 
   /// DetectMount // Not implement yet
   /// MCOpenTelescopeConnection
   /// MCCloseTelescopeConnection
   /// MCAxisSlew
   /// MCAxisSlewTo
   /// MCAxisStop
   /// MCSetAxisPosition
   /// MCGetAxisPosition
   /// MCGetAxisStatus
   /// </summary>
   /// Checked 2/7/2011
   public abstract class SyntaMountBase : ReferenceCountedObjectBase
   {



      public bool IsEQMount = false;      // the physical meaning of mount (Az or EQ)



      #region Properties ....
      /// <summary>
      /// Returns true if there is a valid connection to the driver hardware
      /// </summary>
      protected bool IsConnected { get; set; }

      #endregion


      public SyntaMountBase() : base()
      {
         IsEQMount = false;
      }

      ~SyntaMountBase()
      {
         base.Dispose(false);

      }

   }
}
