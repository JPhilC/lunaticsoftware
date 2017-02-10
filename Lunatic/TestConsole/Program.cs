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
            //Console.WriteLine("Press any key");
            //Console.ReadKey();
            ASCOM.Lunatic.SyntaTelescope driver = new ASCOM.Lunatic.SyntaTelescope();
            driver.SetupDialog();

            //Type ProgIdType = Type.GetTypeFromProgID("ASCOM.Lunatic.TelescopeDriver.SyntaTelescope");
            //Object oDrv = Activator.CreateInstance(ProgIdType);
         }
         catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine(ex.Message);
         }


      }
   }
}
