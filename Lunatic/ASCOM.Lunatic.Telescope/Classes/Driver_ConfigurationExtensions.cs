using Lunatic.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Lunatic
{
   public partial class Telescope
   {
      private ISettingsProvider<Settings> _SettingsManager = null;

      public ISettingsProvider<Settings> SettingsManager
      {
         get
         {
            if (_SettingsManager == null) {
               _SettingsManager = new SettingsProvider();
            }
            return _SettingsManager;
         }
      }

      private Settings Settings
      {
         get
         {
            return SettingsManager.CurrentSettings;
         }
      }
   }
}
