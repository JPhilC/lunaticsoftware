using System;
using System.Text;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using Lunatic.Core;
using Core = Lunatic.Core;
using System.Threading;
using ASCOM.Lunatic.Telescope;
using ASCOM.Lunatic.Telescope.Classes;
using Lunatic.Core.Geometry;
using Lunatic.SyntaController;

/// <summary>
/// The ASCOM ITelescopeV3 implimentation for the driver.
/// </summary>
namespace ASCOM.Lunatic.Telescope
{
   partial class Telescope
   {
      #region PEC related member variables ...

      private bool PECEnabled = false;
      private double LastPECRate = 0.0;

      #endregion

      private void PECLoPassScrollChange()
      {
         throw new NotImplementedException("PECLoPassScrollChange");
      }

      private void PEC_MagScroll_Change()
      {
         throw new NotImplementedException("PEC_MagScroll_Change()");
      }
      private void PEC_PhaseScroll_Change()
      {
         throw new NotImplementedException("PEC_PhaseScroll_Change");
      }
      private void PEC_Initialise()
      {
         throw new NotImplementedException("PEC_Initialise");
      }
      private void PEC_Timestamp()
      {
         throw new NotImplementedException("PEC_Timestamp");
      }

      [Obsolete("This method needs writing.")]
      private void PECStartTracking()
      {
         throw new NotImplementedException("PEC_StartTracking");
      }

      [Obsolete("This method needs finishing off.")]
      private void PECStopTracking()
      {
         PECEnabled = false;
         // PlaybackTimer.strPlayback = oLangDll.GetLangString(6117)
         LastPECRate = 0;
         // Close TraceFileNum
      }

      private void PEC_Unload()
      {
         throw new NotImplementedException("PEC_Unload");
      }
      private void PEC_GainScroll_Change()
      {
         throw new NotImplementedException("PEC_GainScroll_Change");
      }
      private void PEC_Clear()
      {
         throw new NotImplementedException("PEC_Clear");
      }
      private void PEC_OnUse()
      {
         throw new NotImplementedException("PEC_OnUse");
      }
      private void PEC_SetGain()
      {
         throw new NotImplementedException("PEC_SetGain");
      }
      private void PEC_SetPhase()
      {
         throw new NotImplementedException("PEC_SetPhase");
      }
      private void PEC_Load()
      {
         throw new NotImplementedException("PEC_Load");
      }
      private bool PEC_LoadFile(string fileName)
      {
         throw new NotImplementedException("PEC_LoadFile");
      }
      private void PEC_Save()
      {
         throw new NotImplementedException("PEC_Save");
      }
   }
}
