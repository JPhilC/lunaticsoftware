namespace Lunatic.Core
{
   public interface ISettingsProvider <T> 
   {
      T Settings { get; }
      void SaveSettings();

   }
}
