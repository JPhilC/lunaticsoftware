﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   public static class ExtensionMethods
   {
      /// <summary>
      /// Tell subscribers, if any, that this event has been raised.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="handler">The generic event handler</param>
      /// <param name="sender">this or null, usually</param>
      /// <param name="args">Whatever you want sent</param>
      public static void Raise<T>(this EventHandler<T> handler, object sender, T args) where T : EventArgs
      {
         // Copy to temp var to be thread-safe (taken from C# 3.0 Cookbook - don't know if it's true)
         EventHandler<T> copy = handler;
         if (copy != null) {
            copy(sender, args);
         }
      }

      /// <summary>
      /// Extension method to convert a DateTime to a Julian date.
      /// </summary>
      /// <param name="date"></param>
      /// <returns></returns>
      public static double ToJulianDate(this DateTime date)
      {
         return date.ToOADate() + 2415018.5;
      }


      public static bool IsInitialised(this ASCOM.Astrometry.Transform.Transform transform)
      {
         bool initialised = false;
         try {
            initialised = !(double.IsNaN(transform.SiteElevation)
                  || double.IsNaN(transform.SiteLatitude)
                  || double.IsNaN(transform.SiteLongitude)
                  || double.IsNaN(transform.SiteTemperature));
         }
         catch (ASCOM.Astrometry.Exceptions.TransformUninitialisedException ex) {
            initialised = false;
         }
         catch { throw; }
         return initialised;
      }
   }
}
