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

namespace Lunatic.TelescopeControl.Controls
{
   /// <summary>
   /// Interaction logic for SlewButtons.xaml
   /// </summary>
   public partial class SlewButtons : UserControl
   {
      public SlewButtons()
      {
         InitializeComponent();
      }

      private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
      {
         Button button = sender as Button;
         System.Diagnostics.Debug.WriteLine(string.Format("Button {0} down.", button.Name));
         switch (button.Name) {
            case "North":     // DEC +
               break;
            case "South":     // DEC -
               break;
            case "East":      // RA +
               break;
            case "West":      // RA -
               break;
         }
      }

      private void Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
      {
         Button button = sender as Button;
         System.Diagnostics.Debug.WriteLine(string.Format("Button {0} up.", button.Name));
         switch (button.Name) {
            case "North":     // DEC +
               break;
            case "South":     // DEC -
               break;
            case "East":      // RA +
               break;
            case "West":      // RA -
               break;
         }
      }
   }
}
