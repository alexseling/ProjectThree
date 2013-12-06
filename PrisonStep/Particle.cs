using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace PrisonStep
{
    class Particle
    {
        private PrisonGame game;
        private Model model;
        public Model Model { get { return model; } set { model = value; } }
        private Matrix transform;

        private Vector3 velocity;
        private Vector3 position;
        private Vector3 acceleration;
        private float lifetime;
        private float age;
        private float scale;
        private float orientation;
        private float angularVelocity;

        public Particle(PrisonGame game, Vector3 positionOfSystem)
        {
            System.Diagnostics.Trace.WriteLine("Particle.Particle() called");

            this.game = game;
        }

        public void Initialize(Vector3 position, Vector3 velocity, Vector3 acceleration, float lifetime, float scale, float rotationSpeed, float orientation)
        {
            // set the values to the requested values -- This could be rewritten as some base value + a randomly generated offset
            this.position = position;
            this.velocity = velocity;
            this.acceleration = acceleration;
            this.lifetime = lifetime;
            this.scale = scale;
            this.angularVelocity = rotationSpeed;
            this.age = 0.0f;
            this.orientation = orientation;
        }

        /// <summary>
        /// This function is called to load content into this component
        /// of our game.
        /// </summary>
        /// <param name="content">The content manager to load from.</param>
        public void LoadContent(ContentManager content)
        {
            System.Diagnostics.Trace.WriteLine("Particle.LoadContent() called");

            //model = content.Load<Model>("Particle");  <------ Need a particle model
        }

        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            System.Diagnostics.Trace.WriteLine("Particle.Update() called");

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update velocity
            velocity += acceleration * delta;
            // Update position
            position += velocity * delta;
            // Update orientation
            orientation += angularVelocity * delta;
            // Update age
            age += delta;
        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime)
        {
            System.Diagnostics.Trace.WriteLine("Particle.Draw() called");

            DrawModel(graphics, model, this.transform);
        }

        private void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {
            System.Diagnostics.Trace.WriteLine("Particle.DrawModel() called");
        }
    }
}
