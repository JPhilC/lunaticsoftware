using Lunatic.Core.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Lunatic.Core.Classes
{
   public enum PointFilterOption
   {
      [Description("All points")]
      AllPoints,
      [Description("This side of meridian")]
      MeridianSide,
      [Description("Local Quadrant")]
      LocalQuadrant
   }

   public class AlignmentPoint
   {
      public EquatorialCoordinate Target { get; set; }
      public AltAzCoordinate TargetAltAz { get; set; }
      public AltAzCoordinate AlignedAltAz { get; set; }
      public CarteseanCoordinate TargetCartesean { get; set; }
      public CarteseanCoordinate AlignedCartesean { set; get; }
      public DateTime AlignTime { get; set; }
      public AxisPosition TargetAxisPosition { get; set; }
      public AxisPosition AlignedAxisPosition { get; set; }


      public AlignmentPoint(EquatorialCoordinate target, CarteseanCoordinate targetCartesean, DateTime time, AxisPosition targetAxisPosition, AxisPosition alignedAxisPosition)
      {
         Target = target;
         TargetCartesean = targetCartesean;
         AlignTime = time;
         TargetAxisPosition = targetAxisPosition;
         AlignedAxisPosition = alignedAxisPosition;
      }
   }

   public struct AlignmentTriangle
   {
      public AlignmentPoint[] Points;
      public int PointCount
      {
         get
         {
            return Points.Length;
         }
      }
   }

   public class AlignmentPointDistance
   {
      public AlignmentPoint AlignmentPoint { get; set; }
      public double Distance { get; set; }

      public AlignmentPointDistance(AlignmentPoint point, double distance)
      {
         AlignmentPoint = point;
         Distance = distance;
      }
   }

   public class AlignmentPointCollection : ObservableCollection<AlignmentPoint>
   {
      #region  Contructors ...
      public AlignmentPointCollection()
      {
      }

      public AlignmentPointCollection(IEnumerable<AlignmentPoint> range) :
            base(range)
      {
      }

      public AlignmentPointCollection(IList<AlignmentPoint> list) :
            base(list)
      {
      }

      #endregion

      #region Adding items ...
      public void AddRange(AlignmentPoint[] range)
      {
         foreach (AlignmentPoint item in range) {
            Add(item);
         }
      }

      public void AddRange(IEnumerable range)
      {
         foreach (AlignmentPoint item in range) {
            Add(item);
         }
      }

      public void AddRange(ICollection<AlignmentPoint> range)
      {
         foreach (AlignmentPoint item in range) {
            Add(item);
         }
      }
      #endregion

      #region Removing items ...

      public void RemoveRange(AlignmentPoint[] range)
      {
         foreach (AlignmentPoint item in range) {
            Remove(item);
         }
      }

      public void RemoveRange(IEnumerable range)
      {
         foreach (AlignmentPoint item in range) {
            Remove(item);
         }
      }

      public void RemoveRange(ObservableCollection<AlignmentPoint> range)
      {
         foreach (AlignmentPoint item in range) {
            Remove(item);
         }
      }

      public void RemoveRangeAt(int index, int count)
      {
         for (int i = 0; i < count; ++i) {
            RemoveAt(index);
         }
      }

      public void RemoveRange(ICollection<AlignmentPoint> range)
      {
         foreach (AlignmentPoint item in range) {
            Remove(item);
         }
      }

      public void RemoveRange(ICollection range)
      {
         foreach (AlignmentPoint item in range) {
            Remove(item);
         }
      }
      #endregion

      #region Queries ...
      public AlignmentTriangle GetNearest3Points(AltAzCoordinate targetAltAz, Angle latitude, PointFilterOption filterOption, bool localToPier, bool affineTaki)
      {
         AlignmentTriangle triangle = new AlignmentTriangle();
         if (base.Items.Count == 0) {
            return triangle;
         }
         if (base.Items.Count < 4) {
            triangle.Points = new AlignmentPoint[base.Items.Count];
            Array.Copy(base.Items.ToArray(), triangle.Points, base.Items.Count);
         }
         else {
            // Convert RADec to Carteasean
            CarteseanCoordinate tmpCoord = targetAltAz.ToCartesean();
            Quadrant tmpQuadrant = tmpCoord.Quadrant;
            List<AlignmentPointDistance> pointsToConsider = new List<AlignmentPointDistance>();
            // first find out the distances to the alignment points
            foreach (AlignmentPoint alignPt in Items) {
               switch (filterOption) {
                  case PointFilterOption.LocalQuadrant:
                     // Only consider the points in the same quadrant
                     if (alignPt.TargetCartesean.Quadrant != tmpQuadrant) {
                        continue;
                     }
                     break;
                  case PointFilterOption.MeridianSide:
                     // Only consider points on the same side of the meridian
                     if (alignPt.TargetCartesean.Y * tmpCoord.Y < 0) {
                        continue;
                     }
                     break;
                  default:    // All points considered
                     break;
               }

               double distance;
               if (localToPier) {
                  distance = targetAltAz.OrderingDistanceTo(alignPt.TargetAltAz);
               }
               else {
                  distance = Math.Pow(alignPt.TargetCartesean.X - tmpCoord.X, 2) + Math.Pow(alignPt.TargetCartesean.Y - tmpCoord.Y, 2);
               }
               pointsToConsider.Add(new AlignmentPointDistance(alignPt, distance));
            }  // foreach
            if (pointsToConsider.Count == 3) {
               triangle.Points = pointsToConsider.Take(3).Select(apd => apd.AlignmentPoint).ToArray<AlignmentPoint>();

            }
            else if (pointsToConsider.Count > 3) {
               // iterate through all the triangles posible using the nearest alignment points
               AlignmentPoint[] nearest50 = pointsToConsider.OrderBy(apd => apd.Distance)
                  .Take(50)
                  .Select(apd => apd.AlignmentPoint)
                  .ToArray<AlignmentPoint>();
               int pointCount = nearest50.Length;
               int l = 1;
               int m = 2;
               int n = 3;
               bool allDone = false;
               for (int i = 0; i < pointCount - 2; i++) {
                  AlignmentPoint p1 = nearest50[i];
                  for (int j = i + 1; j < pointCount - 1; j++) {
                     AlignmentPoint p2 = nearest50[j];
                     for (int k = (j + 1); k < pointCount; k++) {
                        AlignmentPoint p3 = nearest50[k];
                        if (CheckPointInTargetTriangle(tmpCoord.X, tmpCoord.Y, p1, p2, p3)) {
                           l = i;
                           m = j;
                           n = k;
                           allDone = true;
                        }
                        if (allDone) {
                           break;
                        }
                     }  // Next k
                     if (allDone) {
                        break;
                     }
                  } //  Next j
                  if (allDone) {
                     break;
                  }
               } // next i

               if (allDone) {
                  triangle.Points = new AlignmentPoint[] {
                     nearest50[l],
                     nearest50[m],
                     nearest50[n]
                  };
               }
            }

         }
         return triangle;
      }


      private bool CheckPointInTargetTriangle(double targetX, double targetY, AlignmentPoint p1, AlignmentPoint p2, AlignmentPoint p3)
      {

         double ta = TriangleArea(p1.TargetCartesean.X, p1.TargetCartesean.Y, p2.TargetCartesean.X, p2.TargetCartesean.Y, p3.TargetCartesean.X, p3.TargetCartesean.Y);
         double t1 = TriangleArea(targetX, targetY, p2.TargetCartesean.X, p2.TargetCartesean.Y, p3.TargetCartesean.X, p3.TargetCartesean.Y);
         double t2 = TriangleArea(p1.TargetCartesean.X, p1.TargetCartesean.Y, targetX, targetY, p3.TargetCartesean.X, p3.TargetCartesean.Y);
         double t3 = TriangleArea(p1.TargetCartesean.X, p1.TargetCartesean.Y, p2.TargetCartesean.X, p2.TargetCartesean.Y, targetX, targetY);

         return (Math.Abs(ta - t1 - t2 - t3) < 2);
      }

      private double TriangleArea(double p1x, double p1y, double p2x, double p2y, double p3x, double p3y)
      {
         double area = Math.Abs(((p2x * p1y) - (p1x * p2y)) + ((p3x * p2y) - (p2x * p3y)) + ((p1x * p3y) - (p3x * p1y))) / 2.0;
         return area;
      }

      #endregion


   }
}
