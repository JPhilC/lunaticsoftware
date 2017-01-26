using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   public abstract class DriverBase : ObservableObject
   {

      protected object _Lock = new object();

      #region Version info ...
      private string _CompanyName = null;
      protected string CompanyName {
         get {
            if (_CompanyName == null) {
               LoadAssemblyInfo();
            }
            return _CompanyName;
         }
      }

      private string _Copyright = null;
      protected string Copyright
      {
         get
         {
            if (_Copyright == null) {
               LoadAssemblyInfo();
            }
            return _Copyright;
         }
      }
      private string _Comments = null;
      protected string Comments
      {
         get
         {
            if (_Comments == null) {
               LoadAssemblyInfo();
            }
            return _Comments;
         }
      }
      private int? _MajorVersion = null;
      protected int MajorVersion
      {
         get
         {
            if (_MajorVersion == null) {
               LoadAssemblyInfo();
            }
            return _MajorVersion.Value;
         }
      }
      private int? _MinorVersion = null;
      protected int MinorVersion
      {
         get
         {
            if (_MinorVersion == null) {
               LoadAssemblyInfo();
            }
            return _MinorVersion.Value;
         }
      }


      private void LoadAssemblyInfo()
      {
         FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
         _CompanyName = versionInfo.CompanyName;
         _Copyright = versionInfo.LegalCopyright;
         _Comments = versionInfo.Comments;
         _MajorVersion = versionInfo.ProductMajorPart;
         _MinorVersion = versionInfo.ProductMinorPart;
      }
      #endregion

      #region Configuration  ...

      protected abstract string ConfigurationSettingsFilename { get; }

      protected abstract void LoadSettings();
      protected abstract void SaveSettings();


      /// <summary>
      /// Loads any previously saved settings
      /// </summary>
      protected T LoadSettings<T>()
      {
         lock (_Lock) {
            object settings = new object();
            string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyName, ConfigurationSettingsFilename);
            if (File.Exists(settingsFile)) {
               using (StreamReader sr = new StreamReader(settingsFile)) {
                  JsonSerializer serializer = new JsonSerializer();
                  settings = (T)serializer.Deserialize(sr, typeof(T));
               }
            }
            return (T)settings;
         }
      }

      /// <summary>
      /// Saves the current settings to user storage
      /// </summary>
      protected void SaveSettings<T>(T settings)
      {
         lock (_Lock) {
            string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyName, ConfigurationSettingsFilename);
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

      #endregion
   }
}
