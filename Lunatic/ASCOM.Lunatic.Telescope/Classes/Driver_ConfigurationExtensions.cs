using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Lunatic.TelescopeDriver
{
   public partial class SyntaTelescope
   {
      private Settings _Settings = null;
      public Settings Settings
      {
         get
         {
            return _Settings;
         }
      }


      protected override string ConfigurationSettingsFilename
      {
         get
         {
            return "SyntaMount.config";
         }
      }

      protected override void LoadSettings()
      {
         _Settings = (Settings)LoadSettings<Settings>();
      }

      protected override void SaveSettings()
      {
         if (Settings != null) {
            SaveSettings<Settings>(Settings);
         }
      }

   }
}
