using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lunatic.Core.Services;
using System.Threading.Tasks;

namespace Lunatic.Core.Tests
{
   /// <summary>
   /// Summary description for WeatherServiceTests
   /// </summary>
   [TestClass]
   public class WeatherServiceTests
   {
      public WeatherServiceTests()
      {
         //
         // TODO: Add constructor logic here
         //
      }


      [TestMethod]
      public async Task Can_Get_Temperature()
      {
         
         double temperature = await WeatherService.GetCurrentTemperature(52.66842473829653, -1.3393039268281037);
         Assert.IsFalse(double.IsNaN(temperature));

      }
   }
}
