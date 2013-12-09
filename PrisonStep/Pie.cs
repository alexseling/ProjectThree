using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace PrisonStep
{
    public class Pie
    {
        private bool isEffectStarted = false;     // <------ Added
        private bool isTrailing = false;     // <------ Added

        public Pie(Bazooka bazooka, PrisonGame game, int pieNum)
        {
            this.bazooka = bazooka;
            this.game = game;
            this.pieNum = pieNum;

            if (pieNum == 1)
                distance = 0;
            if (pieNum == 2)
                distance = -20;
            if (pieNum == 3)
                distance = -10;

            bs = new BoundingSphere();
            bs.Radius = 8;

            partSys = new ParticleSystem(2);      // <---------- Added
            partSys.LoadContent(game.Content);
            smokeTrail = new ParticleSystem(1);      // <---------- Added
            smokeTrail.LoadContent(game.Content);
        }

        /// <summary>
        /// This function is called to load content into this component
        /// of our game.
        /// </summary>
        /// <param name="content">The content manager to load from.</param>
        public void LoadContent(ContentManager content)
        {
        }

        public void Fire()
        {
            speed = 800;
            beenFired = true;
            bazooka = null;
            game.AddFlyingPie(this);
            if (game.Alien.Pie == null) game.Alien.ReachForPie(currRegion);
        }

        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            float prevDistance = distance; // Used in case of collision later
            float prevY = bs.Position.Y;
            float addedDistance = speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (beenFired == false)
                distance += addedDistance;
            else
            {
                float offset = 0;
                if (pieNum == 3) offset = 10;
                if (pieNum == 2) offset = 20;
                distance += addedDistance;
                Vector3 spherePos = (Matrix.CreateTranslation(0, distance + offset, 0) * transform).Translation;
                spherePos.Y += doorUp;
                Vector3 prevPos = bs.Position;
                bs.Position = spherePos;
                deltaMovement = bs.Position - prevPos;
                partSys.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
                smokeTrail.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

                if (bs.Position.Y <= 0)
                {
                    float deltaY = prevY - bs.Position.Y;
                    float percentToFloor = prevY / deltaY;
                    distance = prevDistance + addedDistance * percentToFloor;
                    bs.Position = (Matrix.CreateTranslation(0, distance + offset, 0) * transform).Translation;
                    speed = 0;
                }
                if(bs.testCollision(game.Dalek.BC)) {
                    hitDalek = true;
                }
                else if (bs.testCollision(game.Alien.BC) && game.Alien.CanGetPie() == true) {
                    hitAlien = true;
                }
                string region = game.TestRegion(bs.Position);
                if (region.StartsWith("R_Section")) currRegion = int.Parse(region.Substring(9));
                if (region == "" && speed > 0)
                {
                    //distance += 50;
                    speed = 0;
                }
                else if (region.StartsWith("R_Door") && speed != 0) {
                    if (game.getDoorHeight(int.Parse(region.Substring(6))) < bs.Position.Y + bs.Radius) {
                        game.AttachPieToDoor(this,region);
                        distance += 40;
                        if (region.EndsWith("1"))
                            distance += 40;
                        else if (region.EndsWith("4"))
                            distance += 60;
                        else if (region.EndsWith("5"))
                            distance += 80;
                        speed = 0;
                    }
                }
            }

            if (beenFired == true && speed == 0 && !isEffectStarted)            // <---------- Added
            {
                partSys.AddParticles(bs.Position);
                isEffectStarted = true;
            }
            else if (beenFired == true && !isTrailing)
            {
                smokeTrail.AddParticles(bs.Position);
                isTrailing = true;
            }
            else if (beenFired == true && speed != 0)
                smokeTrail.MoveParticles(deltaMovement);

        }
        

        public void AddDoorMovement(float deltaY)
        {
            doorUp += deltaY/2.05f;
        }

        public void SetEffectParams(int region, int door, int opposite, float percent)
        {
            currDoor = door;
            percentOpen = percent;
            if (region != -1)
            {
                currRegion = region;
                oppositeRegion = opposite;
            }
            else
            {
                if (currDoor == 1 && currRegion < 3) oppositeRegion = 3 - currRegion;
                else if (currDoor == 2 && (currRegion == 2 || currRegion == 3)) oppositeRegion = 5 - currRegion;
                else if (currDoor == 3 && (currRegion == 4 || currRegion == 3)) oppositeRegion = 7 - currRegion;
                else if (currDoor == 4 && (currRegion == 4 || currRegion == 5)) oppositeRegion = 9 - currRegion;
                else if (currDoor == 5 && (currRegion == 4 || currRegion == 6)) oppositeRegion = 10 - currRegion;
                else oppositeRegion = 0;
            }
        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime, Matrix transform)
        {
            if (beenFired == false || hitAlien == true)
                this.transform = transform;
            if (hitAlien == true)
                distance = 0;
            DrawModel(graphics, model, this.transform);

            partSys.Draw(graphics.GraphicsDevice, game.Camera);
            if (beenFired == true)
            smokeTrail.Draw(graphics.GraphicsDevice, game.Camera);
        }

        private void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            float offset = 0;
            if (pieNum == 2) offset = -20;
            if (pieNum == 3) offset = -10;
            if (hitAlien == true) distance = offset;

            world = Matrix.CreateTranslation(0, distance, 0) * world * Matrix.CreateTranslation(0, doorUp, 0);

            string regionIn = game.Player.TestRegion((Matrix.CreateTranslation(0, distance + offset, 0) * transform).Translation);
            if (regionIn.StartsWith("R_Section"))
                currRegion = int.Parse(regionIn.Substring(9));

            for (int i = 0; i < model.Meshes.Count; i++)
            {
                // PIE 1 = mesh 0
                // PIE 2 = meshes 1 and 2
                // PIE 3 = meshes 3 and 4
                if (!(pieNum == 1 && i == 0 || pieNum == 2 && (i == 1 || i == 2) || pieNum == 3 && (i == 3 || i == 4))) continue;
                ModelMesh mesh = model.Meshes[i];
                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue(transforms[mesh.ParentBone.Index] * world);
                    effect.Parameters["View"].SetValue(game.Camera.View);
                    effect.Parameters["Projection"].SetValue(game.Camera.Projection);

                    for (int lightNum = 1; lightNum <= 3; lightNum++)
                    {
                        effect.Parameters["Light" + lightNum + "Location"].SetValue(game.Player.Victoria.LightInfo(currRegion, lightNum * 2 - 2));
                        effect.Parameters["Light" + lightNum + "Color"].SetValue(game.Player.Victoria.LightInfo(currRegion, lightNum * 2 - 1));
                    }
                    if (oppositeRegion > 0)
                    {
                        effect.Parameters["Light4Location"].SetValue(game.Player.Victoria.LightInfo(oppositeRegion, 0));
                        effect.Parameters["Light4Color"].SetValue(percentOpen * game.Player.Victoria.LightInfo(oppositeRegion, 1));
                    }
                    else
                    {
                        effect.Parameters["Light4Location"].SetValue(new Vector3(0, 0, 0));
                        effect.Parameters["Light4Color"].SetValue(new Vector3(0, 0, 0));
                    }

                    effect.Parameters["Slime"].SetValue(game.SlimeLevel);
                }
                mesh.Draw();
            }
        }
        
        private PrisonGame game;
        private Model model;
        public Model Model { get { return model; } set { model = value; } }
        private Bazooka bazooka;
        private float distance = 0;
        private Matrix transform;
        private float speed = 0;
        private int pieNum = 1;
        private int currRegion = 1;
        private int currDoor = 1;
        private int oppositeRegion = 2;
        private float percentOpen = 0;
        private bool beenFired = false;
        private ParticleSystem partSys;
        private ParticleSystem smokeTrail;
        private Vector3 deltaMovement = new Vector3(0, 0, 0);
        private BoundingSphere bs;
        public BoundingSphere BS { get { return bs; } }
        private float doorUp = 0;
        private bool hitDalek = false;
        public bool HitDalek { get { return hitDalek; } }
        private bool hitAlien = false;
        public bool HitAlien { get { return hitAlien; } }
    }
}
