using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace PrisonStep
{
    public class BoundingSphere
    {
        public BoundingSphere() { }
        private float radius;
        public float Radius { get { return radius; } set { radius = value; } }
        private Vector3 position; // bottom
        public Vector3 Position { get { return position; } set { position = value; } }
        public bool testCollision(BoundingCylinder bc) {
            float x1 = position.X;
            float y1 = position.Y;
            float z1 = position.Z;
            float x2 = bc.Position.X;
            float y2 = bc.Position.Y;
            float z2 = bc.Position.Z;
            // One completely above the other - FALSE
            if (y1 + radius < y2 || y2 + bc.Height < y1 - radius) return false;
            // One too far away horizontally from the other - FALSE
            float distAway = (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (z1 - z2) * (z1 - z2));
            if (distAway > (radius + bc.Radius)) return false;
          //  return true;
            // From a bird's eye view (above), they look like they intersect. But, might
            // have sphere's center point below or above the cylinder, so the farthest
            // horizontal width doesn't cross the cylinder. If it's between, however,
            // definitely intersects.
            if (y1 >= y2 && y1 <= y2 + bc.Height) return true;

            // Find closest xz point from cylinder to exact top of sphere
            float xClosest = 0, zClosest = 0;
            // Avoid dividing by zero
            if (x2 == x1) x2 += 0.00001f;
            float slope = ((z2 - z1) / (x2 - x1));
            float xDistFromCenter = bc.Radius / (float)Math.Sqrt(slope*slope + 1); // Positive number
            float zDistFromCenter = xDistFromCenter * slope; // Positive number
            float yClosest = 0;
            xClosest = x2 + xDistFromCenter;
            if (x2 > x1) xClosest = x2 - xDistFromCenter;
            zClosest = z2 + zDistFromCenter;
            if (z2 > z1) zClosest = z2 - zDistFromCenter;
            if (y1 < y2) yClosest = y2;
            else yClosest = y2 + bc.Height;
            float distClosest = (float)Math.Sqrt((xClosest - x1) * (xClosest - x1) + (yClosest - y1) * (yClosest - y1) + (zClosest - z1) * (zClosest - z1));

            if (radius < distClosest) return false;
            else return true;
        }
        public bool testCollision(BoundingSphere bs)
        {
            float x1 = position.X;
            float y1 = position.Y;
            float z1 = position.Z;
            float x2 = bs.Position.X;
            float y2 = bs.Position.Y;
            float z2 = bs.Position.Z;
            float distAway = (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2) + (z1 - z2) * (z1 - z2));
            if (distAway > (radius + bs.Radius)) return false;
            else return true;
        }

        public bool testRayCollision(Ray ray)
        {
            //Microsoft.Xna.Framework.BoundingSphere bs = new Microsoft.Xna.Framework.BoundingSphere(position, radius);

            float a = Vector3.Dot(ray.Direction, ray.Direction);
            float b = 2 * Vector3.Dot(ray.Direction, ray.Position - position);
            float c = Vector3.Dot(ray.Position - position, ray.Position - position) - radius * radius;

            float disc = b * b - 4 * a * c;

            if (disc < 0 || a == 0) return false;

            disc = (float)Math.Sqrt(disc);

            float t0 = (-b + disc) / (2 * a);
            float t1 = (-b - disc) / (2 * a);

            if (t1 < 0) return false;
            return true;

            //return ray.Intersects(bs) != null;
        }
    }
}
