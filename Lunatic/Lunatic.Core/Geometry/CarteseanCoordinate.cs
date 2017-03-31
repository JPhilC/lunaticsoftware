using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core.Geometry
{
   /// <summary>
   /// A structure to represent an EquatorialCoordinate
   /// </summary>
   public struct CarteseanCoordinate
   {
      private double _X;
      private double _Y;
      private double _R;        // Radius Sign
      private double _RA;       // Radius Alpha
      private bool _Flag;        // Was .F in VB6 seems to be a flag used to indicate whether Taki transform worked         

      public double X
      {
         get
         {
            return _X;
         }
         set
         {
            _X = value;
         }
      }
      public double Y
      {
         get
         {
            return _Y;
         }
         set
         {
            _Y = value;
         }
      }
      public double R
      {
         get
         {
            return _R;
         }
         set
         {
            _R = value;
         }
      }
      public double RA
      {
         get
         {
            return _RA;
         }
         set
         {
            _RA = value;
         }
      }
      public bool Flag
      {
         get
         {
            return _Flag;
         }
         set
         {
            _Flag = value;
         }
      }

      public CarteseanCoordinate(double x, double y) 
      {
         _X = x;
         _Y = y;
         //_Longitude = new Angle(longitude);
         //_ObservedWhen = observedTime;
         _R = 0.0;
         _RA = 0.0;
         _Flag = false;
      }

      public double this[int index]
      {
         get
         {
            if (index < 0 || index > 1) {
               throw new ArgumentOutOfRangeException();
            }
            return (index == 0 ? _X: _Y);
         }
         set
         {
            if (index < 0 || index > 1) {
               throw new ArgumentOutOfRangeException();
            }
            if (index == 0) {
               _X = value;
            }
            else {
               _Y = value;
            }
         }
      }


      #region Operator overloads ...
      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
      {
         return (pos1.X == pos2.X && pos1.Y == pos2.Y);
      }

      public static bool operator !=(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
      {
         return !(pos1 == pos2);
      }

      public override int GetHashCode()
      {
         unchecked // Overflow is fine, just wrap
         {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + _X.GetHashCode();
            hash = hash * 23 + _Y.GetHashCode();
            return hash;
         }
      }

      public override bool Equals(object obj)
      {
         return (obj is CarteseanCoordinate
                 && this == (CarteseanCoordinate)obj);
      }

      public static CarteseanCoordinate operator -(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
      {
         return new CarteseanCoordinate(pos1.X - pos2.X, pos1.Y - pos2.Y);
      }

      public static CarteseanCoordinate operator +(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
      {
         return new CarteseanCoordinate(pos1.X + pos2.X, pos1.Y + pos2.Y);
      }

      public override string ToString()
      {
         return string.Format("({0},{1})", _X, _Y);
      }
      #endregion
   }

}
