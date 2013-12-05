using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace PrisonStep
{
    public class Spit
    {
        public Spit(PrisonGame game, Matrix alienTransform, float spitHeight)
        {
            this.game = game;

            bs = new BoundingSphere();
            bs.Radius = 8;
            transform = alienTransform;
            height = spitHeight;
            transform.M42 = height;
        }

        /// <summary>
        /// This function is called to load content into this component
        /// of our game.
        /// </summary>
        /// <param name="content">The content manager to load from.</param>
        public void LoadContent(ContentManager content)
        {
            model = content.Load<Model>("Spit");
        }

        public void Fire(Matrix alienTransform)
        {
            speed = 300;
            beenFired = true;
            transform = alienTransform;
            transform.M42 = height;
//            game.AddFlyingPie(this);
        }

        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime, Matrix alienT)
        {
            if (beenFired == false)
                return;
            float addedDistance = speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

            alienTransform = alienT;

            distance += addedDistance;
            bs.Position = (Matrix.CreateTranslation(0, 0, distance) * transform).Translation;
            if (bs.testCollision(game.Player.BC))
            {
                game.Slimed = true;
                game.UpdateScore("Got slimed", -200);
                Reset();
            }
            string region = game.TestRegion(bs.Position);
            if (region == "" && speed > 0)
            {
                Reset();
            }
            else if (region.StartsWith("R_Door") && speed != 0)
            {
                Reset();
            }
        }

        public void Reset()
        {
            beenFired = false;
            speed = 0;
            distance = 0;
            transform = alienTransform;
        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime, Matrix alienTransform)
        {
            if (beenFired == false)
            {
                this.transform = alienTransform;
                transform.M42 = height;
            }
            if (beenFired == true)
                DrawModel(graphics, model, this.transform);
        }

        private void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            world = Matrix.CreateScale(1.5f) * Matrix.CreateTranslation(0, 0, distance) * world;
            world.M42 = height;
                
            for (int i = 0; i < model.Meshes.Count; i++)
            {
                ModelMesh mesh = model.Meshes[i];
                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue(transforms[mesh.ParentBone.Index] * world);
                    effect.Parameters["View"].SetValue(game.Camera.View);
                    effect.Parameters["Projection"].SetValue(game.Camera.Projection);
                    /* effect.EnableDefaultLighting();
                     effect.World = transforms[mesh.ParentBone.Index] * world;
                     effect.View = game.Camera.View;
                     effect.Projection = game.Camera.Projection;*/
                }
                mesh.Draw();
            }
        }

        private Matrix alienTransform;
        private PrisonGame game;
        private Model model;
        public Model Model { get { return model; } set { model = value; } }
        private float distance = 0;
        private Matrix transform;
        private float speed = 0;
        private int pieNum = 1;
        private bool beenFired = false;
        public bool BeenFired { get { return beenFired; } }
        private BoundingSphere bs;
        public BoundingSphere BS { get { return bs; } }
        private float doorUp = 0;
        private float height;
    }
}
