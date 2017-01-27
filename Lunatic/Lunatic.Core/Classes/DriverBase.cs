using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   public abstract class DriverBase : ObservableObject
   {

      protected object _Lock = new object();
   }
}
