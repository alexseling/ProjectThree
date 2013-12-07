using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace PrisonStep
{
    public class ParticleSystem
    {
        private Random rand = new Random();
        private List<Particle> particles;
        private PrisonGame game;
        private Vector3 location;
        public Vector3 Location { get { return location; } set { location = value; } }
        public ParticleSystem(PrisonGame game, Vector3 pos)
        {
            this.game = game;
            location = pos;
            particles = new List<Particle>();
            for(int i = 0; i < 30; i++) {
                Vector3 offset = new Vector3(50 - 100 * (float)rand.NextDouble(), 50 - 100 * (float)rand.NextDouble(), 50 - 100 * (float)rand.NextDouble());
                Vector3 veloc = new Vector3(200 - 400 * (float)rand.NextDouble(), 200 - 250 * (float)rand.NextDouble(), 200 - 400 * (float)rand.NextDouble());
                Particle p = new Particle(game, pos + offset);
                p.Initialize(offset, veloc, new Vector3(0, -980, 0), 2.5f, 1 + (float)rand.NextDouble() * 5, 5 - (float)rand.NextDouble() * 10, (float)rand.NextDouble() * 2 * (float)Math.PI);
                particles.Add(p);
            }
        }

        public void Update(GameTime gameTime, Vector3 pos)
        {
            location = pos;
            foreach (Particle p in particles)
                p.Update(gameTime, location);
        }
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime)
        {
            foreach (Particle p in particles)
                p.Draw(graphics, gameTime);
        }
    }
}
