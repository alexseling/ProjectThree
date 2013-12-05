using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace PrisonStep
{
    public class Bazooka
    {
        public Bazooka(PrisonGame game, Player player)
        {
            piesLoaded = new LinkedList<Pie>();
            this.game = game;
            this.player = player;
            pieNum = 0;
        }

        /// <summary>
        /// This function is called to load content into this component
        /// of our game.
        /// </summary>
        /// <param name="content">The content manager to load from.</param>
        public void LoadContent(ContentManager content)
        {
            model = content.Load<Model>("PieBazooka");
            pieModel = content.Load<Model>("pies");
            Reload();
        }

        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            KeyboardState keyb = Keyboard.GetState();
            if (keyb.IsKeyDown(Keys.Space) && !prevKeyb.IsKeyDown(Keys.Space) && piesLoaded.Count > 0 && player.BazookaRaised == true) {
                Pie toRemove = piesLoaded.First.Value;
                toRemove.Fire();
                piesLoaded.Remove(toRemove);
            }
            prevKeyb = keyb;
            foreach (Pie pie in piesLoaded)
            {
                pie.Update(gameTime);
                pie.SetEffectParams(currRegion, currDoor, oppositeRegion, percentOpen);
            }
        }

        public void Reload()
        {
            int numPiesLoaded = piesLoaded.Count;
            if (numPiesLoaded < 10)
            {
                for (int i = numPiesLoaded; i < 10; i++)
                {
                    Pie newPie = new Pie(this, game, ++pieNum);
                    newPie.Model = pieModel;
                    piesLoaded.AddLast(newPie);
                    pieNum %= 3;
                }
            }
        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime, Matrix transform)
        {
            this.transform = transform;
            DrawModel(graphics, model, transform);
            int i = 0;
            foreach(Pie pie in piesLoaded)
            {
                Matrix pieTransform = Matrix.CreateTranslation(0, 36 - 4*i++, 0) * Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), (float)Math.PI / 2) * transform;
                pie.Draw(graphics, gameTime, pieTransform);
            }
        }

        private void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue(transforms[mesh.ParentBone.Index] * world);
                    effect.Parameters["View"].SetValue(game.Camera.View);
                    effect.Parameters["Projection"].SetValue(game.Camera.Projection);
                    effect.Parameters["Slime"].SetValue(game.SlimeLevel);

                    for (int lightNum = 1; lightNum <= 3; lightNum++)
                    {
                        effect.Parameters["Light" + lightNum + "Location"].SetValue(game.Player.Victoria.LightInfo(currRegion, lightNum * 2 - 2));
                        effect.Parameters["Light" + lightNum + "Color"].SetValue(game.Player.Victoria.LightInfo(currRegion, lightNum * 2 - 1));
                    }
                    effect.Parameters["Light4Location"].SetValue(game.Player.Victoria.LightInfo(oppositeRegion, 0));
                    effect.Parameters["Light4Color"].SetValue(percentOpen * game.Player.Victoria.LightInfo(oppositeRegion, 1));

                    effect.Parameters["Slime"].SetValue(game.SlimeLevel);
                }
                mesh.Draw();
            }
        }
        
        private PrisonGame game;
        private Model model;
        private Player player;
        private Matrix transform;
        public Model Model { get { return model; } }
        private LinkedList<Pie> piesLoaded;
        private Model pieModel;
        KeyboardState prevKeyb = Keyboard.GetState();
        private int currRegion = 0;
        public int CurrRegion { get { return currRegion; } set { currRegion = value; } }
        private int currDoor = 0;
        public int CurrDoor { get { return currDoor; } set { currDoor = value; } }
        private int oppositeRegion = 0;
        public int OppositeRegion { get { return oppositeRegion; } set { oppositeRegion = value; } }
        private float percentOpen = 0;
        public float PercentOpen { get { return percentOpen; } set { percentOpen = value; } }
        public int PiesLeft { get { return piesLoaded.Count; } }
        private int pieNum = 0;
    }
}
