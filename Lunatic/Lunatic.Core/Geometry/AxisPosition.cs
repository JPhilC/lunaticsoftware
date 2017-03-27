using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core.Geometry
{

   /// <summary>
   /// A structure to represent telecope mount axis positions
   /// </summary>
   public struct AxisPosition
   {
      private Angle _RAAxis;
      private Angle _DecAxis;

      public Angle RAAxis
      {
         get
         {
            return _RAAxis;
         }
      }
      public Angle DecAxis
      {
         get
         {
            return _DecAxis;
         }
      }

      public int AxisCount
      {
         get
         {
            return 2;
         }
      }
      /// <summary>
      /// Initialise the Axis positions
      /// </summary>
      /// <param name="raPosition">RA Axis position in degrees</param>
      /// <param name="decPosition">Dec Axis position in degrees</param>
      public AxisPosition(string raPosition, string decPosition)
      {
         _RAAxis = new Angle(raPosition);
         _DecAxis = new Angle(decPosition);
      }
      public AxisPosition(double raRadians, double decRadians)
      {
         if (raRadians < 0 || raRadians >= Constants.TWO_PI) { throw new ArgumentOutOfRangeException("RaAxis position must be between 0 and 2*PI"); }
         if (decRadians < 0 || decRadians > Constants.TWO_PI) { throw new ArgumentOutOfRangeException("DecPosition must be between 0 and 2*PI."); }
         _RAAxis = new Angle() { Radians = raRadians };
         _DecAxis = new Angle() { Radians = decRadians };
      }

      public double this[int index]
      {
         get
         {
            if (index < 0 || index > 1) {
               throw new ArgumentOutOfRangeException();
            }
            return (index == 0 ? _RAAxis.Radians : _DecAxis.Radians);
         }
         set
         {
            if (index < 0 || index > 1) {
               throw new ArgumentOutOfRangeException();
            }
            if (index == 0) {
               _RAAxis.Radians = value;
            }
            else {
               _DecAxis.Radians = value;
            }
         }
      }

      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(AxisPosition pos1, AxisPosition pos2)
      {
         return (pos1.RAAxis.Value == pos2.RAAxis.Value && pos1.DecAxis.Value == pos2.DecAxis.Value);
      }

      public static bool operator !=(AxisPosition pos1, AxisPosition pos2)
      {
         return !(pos1 == pos2);
      }

      public override int GetHashCode()
      {
         unchecked // Overflow is fine, just wrap
         {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + _RAAxis.GetHashCode();
            hash = hash * 23 + _DecAxis.GetHashCode();
            return hash;
         }
      }

      public override bool Equals(object obj)
      {
         return (obj is AxisPosition
                 && this == (AxisPosition)obj);
      }


      public override string ToString()
      {
         return string.Format("RAAxis = {0} Radians, DecAxis = {1} Radians", _RAAxis.Radians, _DecAxis.Radians);
      }

   }

}
