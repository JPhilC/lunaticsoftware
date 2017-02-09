using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace ASCOM.Lunatic
{
   [ComVisible(false)]
   /// <summary>
   /// Interaction logic for SetupWindow.xaml
   /// </summary>
   public partial class SetupWindow : Window
   {
      SetupViewModel _ViewModel;
      public SetupWindow(SetupViewModel viewModel)
      {
         InitializeComponent();
         _ViewModel = viewModel;
         DataContext = viewModel;

         // Hook up to the viewmodels close actions
         if (_ViewModel.SaveAndCloseAction == null) {
            _ViewModel.SaveAndCloseAction = new Action(() => {
               this.DialogResult = true;
               this.Close();
            });
         }
         if (_ViewModel.CancelAndCloseAction == null) {
            _ViewModel.CancelAndCloseAction = new Action(() => {
               this.DialogResult = false;
               this.Close();
            });
         }
      }

      protected override void OnClosed(EventArgs e)
      {
         base.OnClosed(e);
         _ViewModel.SaveAndCloseAction = null;
         _ViewModel.CancelAndCloseAction = null;
      }

      private void propertyGrid_PreparePropertyItem(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyItemEventArgs e)
      {
         PropertyDescriptor theDescriptor = ((PropertyItem)e.PropertyItem).PropertyDescriptor;
         if (theDescriptor.IsBrowsable) {
            e.PropertyItem.Visibility = Visibility.Visible;
         }
         else {
            e.PropertyItem.Visibility = Visibility.Collapsed;
         }
      }
   }
}
