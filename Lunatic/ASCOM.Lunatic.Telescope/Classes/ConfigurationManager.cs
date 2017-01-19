using Lunatic.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Lunatic.Classes
{
   public class MountSettings
   {
      public bool PolarAlignment { get; set; }
      public bool ThreePointAlignement { get; set; }


      public MountSettings()
      {

      }
   }
   public class ConfigurationManager
   {
      private const string AppSettingsFilename = "SyntaDriver.config";

      private static MountSettings _Settings = null;

      private static object _Lock = new object();

      public static MountSettings Settings
      {
         get
         {
            lock (_Lock) {
               if (_Settings == null) {
                  _Settings = ConfigurationManager.LoadSettings();

               }
               return _Settings;
            }
         }
      }

      /// <summary>
      /// Loads any previously saved settings
      /// </summary>
      public static MountSettings LoadSettings()
      {
         lock (_Lock) {
            MountSettings settings = new MountSettings();
            string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.OrganisationName, AppSettingsFilename);
            if (File.Exists(settingsFile)) {
               using (StreamReader sr = new StreamReader(settingsFile)) {
                  JsonSerializer serializer = new JsonSerializer();
                  settings = (MountSettings)serializer.Deserialize(sr, typeof(MountSettings));
               }
            }
            return settings;
         }
      }

      /// <summary>
      /// Saves the current settings to user storage
      /// </summary>
      public static void SaveSettings(MountSettings settings)
      {
         lock (_Lock) {
            string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.OrganisationName, AppSettingsFilename);
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            using (StreamWriter sw = new StreamWriter(settingsFile))
            using (JsonWriter writer = new JsonTextWriter(sw)) {
               {
                  writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                  serializer.Serialize(writer, settings);
               }
            }
         }
      }
   }
}
