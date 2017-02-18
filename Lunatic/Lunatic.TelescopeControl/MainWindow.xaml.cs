using Lunatic.TelescopeControl.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lunatic.TelescopeControl
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      private MainViewModel _ViewModel;

      public MainWindow()
      {
         InitializeComponent();

         _ViewModel = (MainViewModel)this.DataContext;

         // Hook up to the viewmodels close actions
         if (_ViewModel.SaveAndCloseAction == null) {
            _ViewModel.SaveAndCloseAction = new Action(() => {
               this.Close();
            });
         }
         if (_ViewModel.CancelAndCloseAction == null) {
            _ViewModel.CancelAndCloseAction = new Action(() => {
               this.Close();
            });
         }
      }

      protected override void OnClosed(EventArgs e)
      {
         _ViewModel.SaveSettings();
         base.OnClosed(e);
         _ViewModel.SaveAndCloseAction = null;
         _ViewModel.CancelAndCloseAction = null;
      }
   }
}
