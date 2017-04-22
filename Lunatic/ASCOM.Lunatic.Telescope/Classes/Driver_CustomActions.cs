using System;
using System.Text;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using Lunatic.Core;
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
      /// <summary>
      /// Processes customer actions
      /// </summary>
      /// <param name="actionName"></param>
      /// <param name="actionParameters">List of parameters delimited with '|' or tabs</param>
      /// <returns>Return value for Gets or OK.</returns>
      private string ProcessCustomAction(string actionName, string actionParameters)
      {
         string result = "OK";
         char[] delimiters = new char[] { '|', '\t' };
         string[] values = actionParameters.Split(delimiters);
         switch (actionName) {
            case "Lunatic:SetUnparkPositions":
               Settings.RAEncoderUnparkPosition = Convert.ToInt32(values[0]);
               Settings.DECEncoderUnparkPosition = Convert.ToInt32(values[1]);
               break;

            case "Lunatic:SetParkPositions":
               Settings.RAEncoderParkPosition = Convert.ToInt32(values[0]);
               Settings.DECEncoderParkPosition = Convert.ToInt32(values[1]);
               break;

            case "Lunatic:SetTrackUsingPEC":
               Settings.TrackUsingPEC = Convert.ToBoolean(actionParameters);
               break;

            case "Lunatic:SetAutoGuiderPortRates":
               Settings.RAAutoGuiderPortRate = (AutoguiderPortRate)Convert.ToInt32(values[0]);
               Settings.DECAutoGuiderPortRate = (AutoguiderPortRate)Convert.ToInt32(values[1]);
               _Mount.EQ_SetAutoguiderPortRate(AxisId.Axis1_RA, Settings.RAAutoGuiderPortRate);
               _Mount.EQ_SetAutoguiderPortRate(AxisId.Axis2_DEC, Settings.DECAutoGuiderPortRate);
               break;

            default:
               throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
         }
         return result;
      }

      private bool ProcessCommandBool(string command, bool raw)
      {
         bool result = false;
         switch (command) {
            case "Lunatic:IsInitialised":
               result = (_Mount.EQ_GetMountStatus() == 1);
               break;
            default:
               throw new ASCOM.DriverException(string.Format("CommandBool command is not recognised '{0}'.", command));

         }
         return result;
      }
   }
}
