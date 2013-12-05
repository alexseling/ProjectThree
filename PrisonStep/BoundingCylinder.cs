using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace PrisonStep
{
    public class BoundingCylinder
    {
        public BoundingCylinder() { }
        private float radius;
        public float Radius { get { return radius; } set { radius = value; } }
        private float height;
        public float Height { get { return height; } set { height = value; } }
        private Vector3 position;
        public Vector3 Position { get { return position; } set { position = value; } }
        public bool testCollision(BoundingCylinder bc) {
            float x1 = position.X;
            float y1 = position.Y;
            float z1 = position.Z;
            float x2 = bc.Position.X;
            float y2 = bc.Position.Y;
            float z2 = bc.Position.Z;
            if (y1 + height < y2 || y2 + bc.Height < y1) return false;
            float distAway = (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (z1 - z2) * (z1 - z2));
            if (distAway > (radius + bc.Radius)) return false;
            else return true;
        }
    }
}
