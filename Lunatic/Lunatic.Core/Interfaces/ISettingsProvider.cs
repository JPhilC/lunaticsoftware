namespace Lunatic.Core
{
   public interface ISettingsProvider <T> where T : DataObjectBase
   {
      T CurrentSettings { get; }
      void SaveSettings();

   }
}
