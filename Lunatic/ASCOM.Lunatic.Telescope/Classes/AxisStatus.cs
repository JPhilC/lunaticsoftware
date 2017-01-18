using System;

namespace ASCOM.Lunatic.TelescopeDriver
{
   public struct AxisStatus
   {
      /// <summary>
      /// 4 different state
      /// 1. FullStop
      /// 2. Slewing
      /// 3. SlewingTo
      /// 4. Notinitialized
      /// </summary>

      public bool FullStop;
      public bool Slewing;
      public bool SlewingTo;
      public bool SlewingForward;
      public bool HighSpeed;
      public bool NotInitialized;

      public void SetFullStop()
      {
         FullStop = true;
         SlewingTo = Slewing = false;
      }
      public void SetSlewing(bool forward, bool highspeed)
      {
         FullStop = SlewingTo = false;
         Slewing = true;

         SlewingForward = forward;
         HighSpeed = highspeed;
      }
      public void SetSlewingTo(bool forward, bool highspeed)
      {
         FullStop = Slewing = false;
         SlewingTo = true;

         SlewingForward = forward;
         HighSpeed = highspeed;
      }


   //   public override int GetHashCode()
   //   {
   //      int hash = 0;
   //      if (FullStop) {
   //         hash |= AxisState.Stopped;
   //      }
   //      if (Slewing) hash |= AxisState.Slewing;
   //   if (SlewingTo) hash |=AxisState.Slewing_To;
   //   if (SlewingForward) hash |=AxisState.Slewing_Forward;
   //   if (HighSpeed) hash |= 
   //   public bool NotInitialized;
   //}

   //public override bool Equals(object obj)
   //   {
   //      return (obj is Length
   //          && this == (Length)obj);
   //   }

   //   public override string ToString()
   //   {
   //      return this.Value.ToString() + " " + this.Units.GetDescription();
   //   }

   }

}
