﻿using ASCOM.Lunatic.TelescopeDriver;
using System.ComponentModel;

namespace ASCOM.Lunatic.Interfaces
{
   public interface ISettingsProvider 
   {
      Settings CurrentSettings { get; }
      void SaveSettings();

   }
}