using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lunatic.Core.Services;

namespace TestConsole
{
   class Program
   {
      [STAThread]
      static void Main(string[] args)
      {
         try {
            //using (JoyStickService js = new JoyStickService()) {
            //   Console.WriteLine("Testing Joystick Service");
            //   Console.WriteLine("Press a key to exit.");
            //   Console.ReadKey();
            //}
            Console.WriteLine("Press <Enter> to choose a driver.");
            Console.ReadLine();

            //ASCOM.Lunatic.Telescope driver = new ASCOM.Lunatic.Telescope();
            // driver.SetupDialog();
            //driver.Connected = true;

            //Type ProgIdType = Type.GetTypeFromProgID("ASCOM.Lunatic.TelescopeDriver.Telescope");
            //Object oDrv = Activator.CreateInstance(ProgIdType);

            string driverId = ASCOM.DriverAccess.Telescope.Choose("");
            if (!string.IsNullOrWhiteSpace(driverId)) {
               ASCOM.DriverAccess.Telescope driver = new ASCOM.DriverAccess.Telescope(driverId);

               Console.WriteLine("Press <Enter> to Connect");
               Console.ReadLine();
               driver.Connected = true;

               Console.WriteLine("Press <Enter> to Dispose");
               Console.ReadLine();

               driver.Dispose();
            }


            Console.WriteLine("Press <Enter> to Exit");
            Console.ReadLine();
         }
         catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine(ex.Message);
         }


      }
   }
}
