namespace Lunatic.Core
{
   public interface ISettingsProvider <T> where T : DataObjectBase
   {
      T Settings { get; }
      void SaveSettings();

   }
}
