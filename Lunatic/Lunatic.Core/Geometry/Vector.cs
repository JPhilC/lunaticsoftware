using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core.Geometry
{
   public struct Vector
   {
      double[] _data;

      public Vector(int size)
      {
         _data = new double[size];
      }

      public Vector(double l, double m, double n)
      {
         _data = new double[] { l, m, n };
      }

      public double this[int index]
      {
         get
         {
            return _data[index];
         }
         set
         {
            _data[index] = value;
         }
      }
   }



   #region Old stuff ...
   /// <summary>
   /// Return a LMN/lmn matrix array from 3 coordinates
   /// </summary>
   /// <param name="p1"></param>
   /// <param name="p2"></param>
   /// <param name="p2"></param>
   /// <returns></returns>
   //public static Matrix GetLMN(Coord p1, Coord p2, Coord p3)
   //{
   //   Matrix temp = new Matrix(3, 3);
   //   temp.Element[0, 0] = p2.X - p1.X;
   //   temp.Element[1, 0] = p3.X - p1.X;

   //   temp.Element[0, 1] = p2.Y - p1.Y;
   //   temp.Element[1, 1] = p3.Y - p1.Y;

   //   temp.Element[0, 2] = p2.Z - p1.Z;
   //   temp.Element[1, 2] = p3.Z - p1.Z;

   //   Matrix unitVector = new Matrix(3, 3);
   //   unitVector.Element[0, 0] = (temp.Element[0, 1] * temp.Element[1, 2]) - (temp.Element[0, 2] * temp.Element[1, 1]);
   //   unitVector.Element[0, 1] = (temp.Element[0, 2] * temp.Element[1, 0]) - (temp.Element[0, 0] * temp.Element[1, 2]);
   //   unitVector.Element[0, 2] = (temp.Element[0, 0] * temp.Element[1, 1]) - (temp.Element[0, 1] * temp.Element[1, 0]);
   //   unitVector.Element[1, 0] = unitVector.Element[0, 0] * unitVector.Element[0, 0]
   //                              + unitVector.Element[0, 1] * unitVector.Element[0, 1]
   //                              + unitVector.Element[0, 2] * unitVector.Element[0, 2];
   //   unitVector.Element[1, 1] = Math.Sqrt(unitVector.Element[2, 1]);
   //   if (unitVector.Element[1, 1] != 0) {
   //      unitVector.Element[1, 2] = 1 / unitVector.Element[1, 1];
   //   }

   //   temp.Element[2, 0] = unitVector.Element[1, 2] * unitVector.Element[0, 0];
   //   temp.Element[2, 1] = unitVector.Element[1, 2] * unitVector.Element[0, 1];
   //   temp.Element[2, 2] = unitVector.Element[1, 2] * unitVector.Element[0, 2];

   //   return temp;

   //}
   #endregion

}
