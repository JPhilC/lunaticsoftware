using ASCOM.Lunatic.Telescope.Classes;
using Lunatic.Core.Geometry;
using Lunatic.SyntaController;
using ASCOM.Astrometry.Transform;
using Lunatic.Core;

/// <summary>
/// The ASCOM ITelescopeV3 implimentation for the driver.
/// </summary>
namespace ASCOM.Lunatic.Telescope
{
   partial class Telescope
   {
      private double LimitWest;
      private double LimitEast;

      private bool _CheckLimitsActive;
      public bool CheckLimitsActive
      {
         get
         {
            return _CheckLimitsActive;
         }
         private set
         {
            _CheckLimitsActive = value;
         }
      }

      public bool LimitsActive
      {
         get
         {
            if (!CheckLimitsActive) {
               return false;
            }
            else {
               if (LimitEast == 0 || LimitWest == 0) {
                  return false;
               }
               else {
                  return true;
               }
            }
         }
      }
   }
}
