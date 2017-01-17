/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:ASCOMTester"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace ASCOM.Lunatic.TelescopeDriver
{

   /// <summary>
   /// This class contains static references to all the view models in the
   /// application and provides an entry point for the bindings.
   /// </summary>
   public class ViewModelLocator
   {
      #region Singleton implimentation ...
      private static ViewModelLocator _Current = null;
      public static ViewModelLocator Current
      {
         get
         {
            if (_Current == null) {
               _Current = new ViewModelLocator();
            }
            return _Current;
         }
      }
      #endregion

      /// <summary>
      /// Initializes a new instance of the ViewModelLocator class.
      /// </summary>
      internal ViewModelLocator()
      {
         ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

         ////if (ViewModelBase.IsInDesignModeStatic)
         ////{
         ////    // Create design time view services and models
         ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
         ////}
         ////else
         ////{
         ////    // Create run time view services and models
         ////    SimpleIoc.Default.Register<IDataService, DataService>();
         ////}

         SimpleIoc.Default.Register<SetupViewModel>();
      }

      public SetupViewModel Setup
      {
         get
         {
            return ServiceLocator.Current.GetInstance<SetupViewModel>();
         }
      }

      public static void Cleanup()
      {
         // TODO Clear the ViewModels
      }
   }
}