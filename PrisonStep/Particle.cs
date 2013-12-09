using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace PrisonStep
{
    public class Particle
    {
        private Vector3 position;
        private Vector3 velocity;
        private Vector3 acceleration;
        private float lifetime;
        private float age;
        private float scale;
        private float orientation;
        private float angularVelocity;

        /// <summary>
        /// Position of the particle in space
        /// </summary>
        public Vector3 Position { get { return position; } set { position = value; } }

        /// <summary>
        /// 3D particle velocity
        /// </summary>
        public Vector3 Velocity { get { return velocity; } set { velocity = value; } }

        /// <summary>
        /// 3D particle acceleration
        /// </summary>
        public Vector3 Acceleration { get { return acceleration; } set { acceleration = value; } }

        /// <summary>
        /// How long this particle will live
        /// </summary>
        public float Lifetime { get { return lifetime; } set { lifetime = value; } }

        /// <summary>
        /// How long as this particle been in existence?
        /// </summary>
        public float Age { get { return age; } set { age = value; } }

        /// <summary>
        /// The scale of this particle
        /// </summary>
        public float Scale { get { return scale; } set { scale = value; } }

        /// <summary>
        /// Orientation of the particle in radians
        /// </summary>
        public float Orientation { get { return orientation; } set { orientation = value; } }

        /// <summary>
        /// How fast does it rotate?
        /// </summary>
        public float AngularVelocity { get { return angularVelocity; } set { angularVelocity = value; } }

        /// <summary>
        /// Is this particle still alive?  It's no longer alive once it is older than 
        /// it's lifetime.
        /// </summary>
        public bool Active { get { return Age < Lifetime; } }

        /// <summary>
        /// Initialize is called by the particle when to set up a particle and prepare 
        /// it for use.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <param name="acceleration"></param>
        /// <param name="lifetime"></param>
        /// <param name="scale"></param>
        /// <param name="rotationSpeed"></param>
        public void Initialize(Vector3 position, Vector3 velocity, Vector3 acceleration,
                               float lifetime, float scale, float rotationSpeed, float orientation)
        {
            // set the values to the requested values
            this.Position = position;
            this.Velocity = velocity;
            this.Acceleration = acceleration;
            this.Lifetime = lifetime;
            this.Scale = scale;
            this.AngularVelocity = rotationSpeed;
            this.Age = 0.0f;
            this.Orientation = orientation;
        }

        /// <summary>
        /// Update for the particle.  Does an Euler step.
        /// </summary>
        /// <param name="delta">Time step</param>
        public void Update(float delta)
        {
            // Update velocity
            Velocity += Acceleration * delta;

            // Update position
            Position += Velocity * delta;

            // Update orientation
            Orientation += AngularVelocity * delta;

            // Update age
            Age += delta;
        }
    }
}
