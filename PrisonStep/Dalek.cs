using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace PrisonStep
{
    /// <summary>
    /// This class implements one section of our prison ship
    /// </summary>
    public class Dalek
    {
        #region Fields

        /// <summary>
        /// The name of the asset (FBX file) for this section
        /// </summary>
        private string asset;

        private Spit spit;
        public Spit FiredSpit { get { return spit; } }
        private Random random = new Random();
        public Random Random { get { return random; } }

        private float orientation = 0;
        private Vector3 location = new Vector3(787, 0, -680);

        private int currRegion = 1;
        private int currDoor = 1;
        private int oppositeRegion = 2;
        private float percentOpen = 0;

        private float turningRate = 0;
        private float walkSpeed = 100;

        private int targetDoor = 4;
        private float targetRotation = (float)Math.PI;
        private double targetX = 787;
        private double targetZ = -1000;
        private string lastRegion = "";

        /// <summary>
        /// The game we are associated with
        /// </summary>
        private PrisonGame game;

        /// <summary>
        /// The XNA model for Dalek
        /// </summary>
        private AnimatedModel model;

        private Matrix transform = Matrix.Identity;
        public Matrix Transform { get { return transform; } }

        /// <summary>
        /// To make animation possible and easy, we save off the initial (bind) 
        /// transformation for all of the model bones. 
        /// </summary>
        private Matrix[] bindTransforms;

        /// <summary>
        /// The is the transformations for all model bones, potentially after we
        /// have made some change in the tranformation.
        /// </summary>
        private Matrix[] boneTransforms;

        private BoundingCylinder bc;
        public BoundingCylinder BC { get { return bc; } }

        #endregion

        #region Construction and Loading

        /// <summary>
        /// Constructor. Creates an object for a section.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="section"></param>
        public Dalek(PrisonGame game)
        {
            this.game = game;
            bc = new BoundingCylinder();
            bc.Radius = 33;
            bc.Height = 135;
            model = new AnimatedModel(game, "Dalek");
            spit = new Spit(game, transform, 100);
        }

        /// <summary>
        /// This function is called to load content into this component
        /// of our game.
        /// </summary>
        /// <param name="content">The content manager to load from.</param>
        public void LoadContent(ContentManager content)
        {
            // Load the second model
            model.LoadContent(content);
            spit.LoadContent(content);

            // Save off all of hte bone information
            int boneCnt = model.Model.Bones.Count;
            bindTransforms = new Matrix[boneCnt];
            boneTransforms = new Matrix[boneCnt];

            model.Model.CopyBoneTransformsTo(bindTransforms);
            model.Model.CopyBoneTransformsTo(boneTransforms);
        }

        #endregion


        #region Update and Draw

        public void Spit()
        {
            if (spit.BeenFired == true) return;
            spit.Fire(transform);
        }

        private void SetTransform()
        {
            transform = Matrix.CreateRotationY(orientation);
            transform.Translation = location;
        }

        public void SetMovements(float delta)
        {
            bool isOpen = false;

            string region = game.Player.TestRegion(location);
            //System.Diagnostics.Trace.WriteLine("Player at " + region);

            foreach (PrisonModel model in game.PhibesModels)
            {
                if (model.DoorHeight(targetDoor) >= bc.Height)
                {
                    isOpen = true;
                    break;
                }
            }

            if (region == "" && lastRegion != "" || region.StartsWith("R_Door") && lastRegion != region && !isOpen)
            {
                // If not in a region, we have stepped out of bounds
                targetRotation += (float)Math.PI;
                targetRotation %= (float)Math.PI * 2;
                turningRate = 4;
                walkSpeed = 0;
                //state = States.StanceStart;
            }
            if (Math.Abs((orientation % ((float)Math.PI * 2)) - targetRotation) < 0.1f)
            {
                orientation = targetRotation;
                turningRate = 0;
                walkSpeed = 100;
            }
            else
            {
                walkSpeed = 0;
            }
            lastRegion = region;

            double x = (double)location.X;
            double z = (double)location.Z;
            double distFromTarget = Math.Sqrt((x - targetX) * (x - targetX) + (z - targetZ) * (z - targetZ));
            if (distFromTarget < 20)
            {
                // Hit target - trying to get to next stop
                game.UpdateScore("Dalek escaped to a new room", -1000);
                int prevTargetDoor = targetDoor;
                if (prevTargetDoor == 4)
                {
                    targetDoor = 3;
                    targetX = -11;
                    targetRotation = (float)Math.PI * 3 / 2;
                    walkSpeed = 0;
                    turningRate = 4;
                }
                else if (prevTargetDoor == 3)
                {
                    targetDoor = 2;
                    targetZ = 1023;
                    targetRotation = 0;
                    walkSpeed = 0;
                    turningRate = 4;
                }
                else if (prevTargetDoor == 2)
                {
                    targetDoor = 1;
                    targetX = 587;
                    targetRotation = (float)Math.PI / 2;
                    walkSpeed = 0;
                    turningRate = 4;
                }
                else if (prevTargetDoor == 1)
                {
                    targetDoor = 0;
                    targetRotation = 0;
                    walkSpeed = 0;
                    turningRate = 4;
                    targetX = 70000;
                }
            }
        }

        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime, Vector3 playerLoc)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if ((float)random.NextDouble() * 8 < delta) Spit();

            SetMovements(delta);

            Vector3 addedTranslation = new Vector3(0,0,walkSpeed*delta);
            float addedRotation = turningRate * delta;
            orientation += addedRotation;
            Matrix deltaMatrix = Matrix.CreateRotationY(addedRotation) * Matrix.CreateTranslation(addedTranslation);
            Matrix deltaMatrix2 = Matrix.CreateTranslation(addedTranslation) * Matrix.CreateRotationY(addedRotation);

            location += (Matrix.CreateTranslation(addedTranslation) * Matrix.CreateRotationY(orientation)).Translation;
            SetTransform();
            bc.Position = transform.Translation;

            Matrix[] absoTransforms = new Matrix[model.Model.Bones.Count];
            model.Model.CopyBoneTransformsFrom(boneTransforms);
            model.Model.CopyAbsoluteBoneTransformsTo(absoTransforms);

            int eyeBoneNum = model.Model.Bones["Eye"].Index;
            int destructorBoneNum = model.Model.Bones["PlungerArm"].Index;

            Matrix eyeBoneRegularMatrix = boneTransforms[eyeBoneNum];

            Vector3 eyePos = (Matrix.CreateTranslation(absoTransforms[eyeBoneNum].Translation) * transform).Translation;
            Vector3 direction = (playerLoc - eyePos);
            float angle = (float)Math.Atan(direction.X / direction.Z);
            if (direction.Z < 0) angle -= (float)Math.PI;
            angle -= orientation;
            //System.Diagnostics.Trace.WriteLine("directionEye: (" + direction.X + "," + direction.Y + "," + direction.Z + "), angle: " + angle);
            direction.Normalize();
            boneTransforms[eyeBoneNum] = Matrix.CreateFromAxisAngle(new Vector3(0,0,1),angle) * bindTransforms[eyeBoneNum];

            Vector3 destrPos = (Matrix.CreateTranslation(absoTransforms[destructorBoneNum].Translation) * transform).Translation;
            direction = (playerLoc - destrPos);
            angle = (float)Math.Atan(direction.X / direction.Z);
            if (direction.Z < 0) angle -= (float)Math.PI;
            angle -= orientation;
            //System.Diagnostics.Trace.WriteLine("directionDestr: (" + direction.X + "," + direction.Y + "," + direction.Z + "), angle: " + angle);
            direction.Normalize();
            boneTransforms[destructorBoneNum] = Matrix.CreateFromAxisAngle(new Vector3(0, 0, 1), angle) * bindTransforms[destructorBoneNum];
            
            spit.Update(gameTime, transform);
            currDoor = game.Player.Bazooka.CurrDoor;
            percentOpen = game.Player.Bazooka.PercentOpen;
        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime)
        {
            DrawModel(graphics, model.Model, transform, gameTime);
            if (spit.BeenFired == true)
                spit.Draw(graphics, gameTime, transform);
        }

        private void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world, GameTime gameTime)
        {
            // Apply the bone transforms
            Matrix[] absoTransforms = new Matrix[model.Bones.Count];
            model.CopyBoneTransformsFrom(boneTransforms);
            model.CopyAbsoluteBoneTransformsTo(absoTransforms);

            string regionIn = game.Player.TestRegion(transform.Translation);
            if (regionIn.StartsWith("R_Section"))
                currRegion = int.Parse(regionIn.Substring(9));
            if (currDoor == 1 && currRegion < 3) oppositeRegion = 3 - currRegion;
            else if (currDoor == 2 && (currRegion == 2 || currRegion == 3)) oppositeRegion = 5 - currRegion;
            else if (currDoor == 3 && (currRegion == 4 || currRegion == 3)) oppositeRegion = 7 - currRegion;
            else if (currDoor == 4 && (currRegion == 4 || currRegion == 5)) oppositeRegion = 9 - currRegion;
            else if (currDoor == 5 && (currRegion == 4 || currRegion == 6)) oppositeRegion = 10 - currRegion;
            else oppositeRegion = 0;

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue(absoTransforms[mesh.ParentBone.Index] * world);
                    effect.Parameters["View"].SetValue(game.Camera.View);
                    effect.Parameters["Projection"].SetValue(game.Camera.Projection);
                    effect.Parameters["Slime"].SetValue(game.SlimeLevel);

                    for (int lightNum = 1; lightNum <= 3; lightNum++)
                    {
                        effect.Parameters["Light" + lightNum + "Location"].SetValue(game.Player.Victoria.LightInfo(currRegion, lightNum * 2 - 2));
                        effect.Parameters["Light" + lightNum + "Color"].SetValue(game.Player.Victoria.LightInfo(currRegion, lightNum * 2 - 1));
                    }
                    if (oppositeRegion > 0) {
                        effect.Parameters["Light4Location"].SetValue(game.Player.Victoria.LightInfo(oppositeRegion, 0));
                        effect.Parameters["Light4Color"].SetValue(percentOpen * game.Player.Victoria.LightInfo(oppositeRegion, 1));
                    }
                    else {
                        effect.Parameters["Light4Location"].SetValue(new Vector3(0,0,0));
                        effect.Parameters["Light4Color"].SetValue(new Vector3(0,0,0));
                    }
                }
                mesh.Draw();
            }
        }

        #endregion

    }
}
