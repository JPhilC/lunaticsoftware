using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   public struct Coord
   {
      public double X;
      public double Y;
      public double Z;
   }

   public struct Coordt
   {
      public double X;
      public double Y;
      public double Z;
      public int F;
   }

   public struct CartesCoord
   {
      /// <summary>
      /// X - Coordinate
      /// </summary>
      public double X;
      /// <summary>
      /// Y - coordinate;
      /// </summary>
      public double Y;
      /// <summary>
      /// Radius sign
      /// </summary>
      public double R;
      /// <summary>
      /// Radius alpha
      /// </summary>
      public double Ra;
   }

   public struct SphereCoord
   {
      /// <summary>
      /// X Coordinate
      /// </summary>
      public int X;           // 
      /// <summary>
      /// Y Coordinate
      /// </summary>
      public int Y;           // 
      /// <summary>
      /// RA Range Flag
      /// </summary>
      public double Ra;        // 
   }


   public struct TriangleCoord
   {
      public int I; //         Offset 1
      public int J; //         Offset 2
      public int K; //         Offset 3
   }

   public struct TdatHolder
   {
      public double Dat;
      public double Idx;
      public Coord cc;
   }

   public struct THolder
   {
      public double A;
      public double B;
      public double C;
   }
}
