using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace PrisonStep
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PrisonGame : Microsoft.Xna.Framework.Game
    {

        #region Fields

        /// <summary>
        /// This graphics device we are drawing on in this assignment
        /// </summary>
        GraphicsDeviceManager graphics;

        /// <summary>
        /// The camera we use
        /// </summary>
        private Camera camera;
        private float cameraZ = -450;

        private int score = 0;
        private double totalElapsed = 0;

        /// <summary>
        /// The player in your game is modeled with this class
        /// </summary>
        private Player player;
        public Player Player { get { return player; } }

        private Dalek dalek;
        public Dalek Dalek { get { return dalek; } }

        private Alien alien;
        public Alien Alien { get { return alien; } }

        private int currentPlayerDoor;
        public int CurrentPlayerDoor { set { currentPlayerDoor = value; } }
        private float currentDoorPercent;
        public float CurrentDoorPercent { set { currentDoorPercent = value; } }

        private SpriteBatch spriteBatch;
        private SpriteFont scoreFont;
        private SpriteFont messageFont; 
        
        private Bazooka bazooka;
        private Matrix bazookaMatrixRelative =
                Matrix.CreateRotationX(MathHelper.ToRadians(109.5f)) *
                Matrix.CreateRotationY(MathHelper.ToRadians(9.7f)) *
                Matrix.CreateRotationZ(MathHelper.ToRadians(72.9f)) *
                Matrix.CreateTranslation(-9.6f, 11.85f, 21.1f);

        private List<Pie> flyingPies = new List<Pie>();


        /// <summary>
        /// This is the actual model we are using for the prison
        /// </summary>
        private List<PrisonModel> phibesModels = new List<PrisonModel>();

        private List<String> updateStrings = new List<String>();
        private List<double> updateTimes = new List<double>();




        #endregion

        #region Properties

        /// <summary>
        /// The game camera
        /// </summary>
        public Camera Camera { get { return camera; } }

        public List<PrisonModel> PhibesModels
        {
            get { return phibesModels; }
        }
        #endregion

        private bool slimed = false;
        public bool Slimed { get { return slimed; } set { slimed = value; } }

        private float slimeLevel = 1.0f;
        public float SlimeLevel { get { return slimeLevel; } }

        private PSLineDraw lineDraw;
        public PSLineDraw LineDraw { get { return lineDraw; } }


        /// <summary>
        /// Constructor
        /// </summary>
        public PrisonGame()
        {
            // XNA startup
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            this.IsMouseVisible = true;

            // Create a player object
            player = new Player(this);
            bazooka = new Bazooka(this, player);
            player.Bazooka = bazooka;

            // Some basic setup for the display window
            this.IsMouseVisible = true;
			this.Window.AllowUserResizing = true;
			this.graphics.PreferredBackBufferWidth = 1024;
			this.graphics.PreferredBackBufferHeight = 768;

            // Basic camera settings
            camera = new Camera(graphics);
            camera.FieldOfView = MathHelper.ToRadians(30);
            camera.ZNear = 315;
            camera.Eye = new Vector3(800, 680, 1853);
            camera.Center = new Vector3(275, 90, 1053);

            lineDraw = new PSLineDraw(this, Camera);
            this.Components.Add(lineDraw);

            // Create objects for the parts of the ship
            for (int i = 1; i <= 6; i++)
            {
                phibesModels.Add(new PrisonModel(this, i));
            }
            dalek = new Dalek(this);
            alien = new Alien(this);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            camera.Initialize();
            player.Initialize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            player.LoadContent(Content);
            alien.LoadContent(Content);
            bazooka.LoadContent(Content);
            dalek.LoadContent(Content);
            scoreFont = Content.Load<SpriteFont>("totalScore");
            messageFont = Content.Load<SpriteFont>("messages");
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            foreach (PrisonModel model in phibesModels)
            {
                model.LoadContent(Content);
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            totalElapsed = gameTime.TotalGameTime.TotalMilliseconds;
            List<double> timesToRemove = new List<double>();
            List<String> messagesToRemove = new List<String>();
            for (int i = 0; i < updateTimes.Count; i++)
            {
                if (updateTimes[i] + 3000 < totalElapsed)
                {
                    timesToRemove.Add(updateTimes[i]);
                    messagesToRemove.Add(updateStrings[i]);
                }
            }
            foreach (double timeToRemove in timesToRemove)
                updateTimes.Remove(timeToRemove);
            foreach (String messageToRemove in messagesToRemove)
                updateStrings.Remove(messageToRemove);

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();
            
            List<Pie> piesToDelete = new List<Pie>();
            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                Vector3 mouseV = new Vector3(Mouse.GetState().X, Mouse.GetState().Y, 1);
                Vector3 spaceV = graphics.GraphicsDevice.Viewport.Unproject(mouseV, 
                    camera.Projection, camera.View, Matrix.Identity);

                Vector3 rayOrigin = camera.Eye;
                Vector3 rayDirection = spaceV - rayOrigin;
                rayDirection.Normalize();

                Ray r = new Ray(rayOrigin, rayDirection);

                if (alien.FiredSpit.BS.testRayCollision(r) && dalek.FiredSpit.BeenFired)
                {
                    alien.FiredSpit.Reset();
                    UpdateScore("Cleared a spit", 10);
                }
                if (dalek.FiredSpit.BS.testRayCollision(r) && dalek.FiredSpit.BeenFired)
                {
                    dalek.FiredSpit.Reset();
                    UpdateScore("Cleared a spit", 10);
                }

                foreach (Pie p in flyingPies)
                {
                    if (p.BS.testRayCollision(r))
                    {
                        piesToDelete.Add(p);
                        UpdateScore("Cleaning up after yourself", 2);
                    }
                }
            }

            /*lineDraw.Clear();
            lineDraw.Crosshair(player.BC.Position + new Vector3(0, player.BC.Height, 0), player.BC.Radius, Color.White);

            lineDraw.Begin();
            lineDraw.Vertex(player.BC.Position, Color.White);
            lineDraw.Vertex(player.BC.Position + new Vector3(0, player.BC.Height, 0), Color.Red);
            lineDraw.End();

            lineDraw.Crosshair(dalek.BC.Position + new Vector3(0, dalek.BC.Height, 0), dalek.BC.Radius, Color.White);

            lineDraw.Begin();
            lineDraw.Vertex(dalek.BC.Position, Color.White);
            lineDraw.Vertex(dalek.BC.Position + new Vector3(0, dalek.BC.Height, 0), Color.Red);
            lineDraw.End();*/
            //
            // Update game components
            //


            player.Update(gameTime);
            bazooka.Update(gameTime);
            alien.Update(gameTime);
            dalek.Update(gameTime, player.Transform.Translation);

            foreach (Pie pie in flyingPies)
            {
                pie.Update(gameTime);
                if (pie.HitDalek)
                {
                    piesToDelete.Add(pie);
                    int distAway = (int)Vector3.Distance(dalek.Transform.Translation, player.Transform.Translation);
                    int multiplier = 1;
                    if (player.Bazooka.PiesLeft == 0) multiplier = 2;
                    string kind = "pie: ";
                    if (multiplier == 2) kind = "final pie: " + distAway + " * 2 = ";
                    UpdateScore("Hit Dalek with " + kind, distAway * multiplier);
                }
                else if (pie.HitAlien && alien.Pie == null && alien.CanGetPie())
                {
                    alien.AttachPie(pie);
                    piesToDelete.Add(pie);
                }
                else
                {
                   // lineDraw.Crosshair(pie.BS.Position, pie.BS.Radius, Color.Orange);
                    pie.SetEffectParams(-1, currentPlayerDoor, -1, currentDoorPercent);
                }
            }
            foreach (Pie pieToDelete in piesToDelete)
            {
                flyingPies.Remove(pieToDelete);
            }

            foreach (PrisonModel model in phibesModels)
            {
                model.Update(gameTime);
            }

            camera.Update(gameTime);
            camera.Center = Vector3.Transform(new Vector3(0, 0, 400), Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), player.Orientation) * Matrix.CreateTranslation(player.Location));
            Matrix cameraPos = Matrix.CreateFromAxisAngle(new Vector3(1, 0, 0), player.Orientation) * Matrix.CreateTranslation(player.Location - new Vector3(-100, 200, 0));
            float checkZ = -450;
            for (checkZ = -450; checkZ <= 0; checkZ+=5)
            {
                Vector3 possibleCameraEye = Vector3.Transform(new Vector3(0, 250, checkZ), Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), player.Orientation) * Matrix.CreateTranslation(player.Location));
                if (player.TestRegion(possibleCameraEye) != "") break;
            }
            camera.Eye = Vector3.Transform(new Vector3(0, 240 - (checkZ + 450) / 7, checkZ), Matrix.CreateFromAxisAngle(new Vector3(0, 1, 0), player.Orientation) * Matrix.CreateTranslation(player.Location));
            float potentialZNear = -checkZ - 30;
            if (potentialZNear < 1) potentialZNear = 1;
            if (potentialZNear > 315) potentialZNear = 315;
            camera.ZNear = potentialZNear;

            // Amount to change slimeLevel in one second
            float slimeRate = 2.5f;

            if (slimed && slimeLevel >= -1.5)
            {
                slimeLevel -= (float)gameTime.ElapsedGameTime.TotalSeconds * slimeRate;
            }
            else if (!slimed && slimeLevel < 1)
            {
                slimeLevel += (float)gameTime.ElapsedGameTime.TotalSeconds * slimeRate;
            }

            base.Update(gameTime);
        }

        public void UpdateScore(string message, int change) {
            score += change;
            message += ": " + change;
            updateStrings.Add(message);
            updateTimes.Add(totalElapsed);
        }
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.BlendState = BlendState.Opaque;
            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default; 
            graphics.GraphicsDevice.Clear(Color.Black);

            foreach (PrisonModel model in phibesModels)
            {
                model.Draw(graphics, gameTime);
            }

            dalek.Draw(graphics, gameTime);
            alien.Draw(graphics, gameTime);
            player.Draw(graphics, gameTime);
            foreach (Pie pie in flyingPies)
                pie.Draw(graphics, gameTime, Matrix.Identity);

            ModelBone handBone = player.Victoria.Model.Bones["Bip01 R Hand"];
            Matrix[] victoriaAbsTransform = new Matrix[player.Victoria.Model.Bones.Count];
            player.Victoria.Model.CopyAbsoluteBoneTransformsTo(victoriaAbsTransform);            
            Matrix handBoneAbsTransform = victoriaAbsTransform[handBone.Index];

            bazooka.Draw(graphics, gameTime, bazookaMatrixRelative * handBoneAbsTransform * player.Transform);

            spriteBatch.Begin();
            spriteBatch.DrawString(scoreFont, "SCORE: " + score, new Vector2(10, 10), Color.Yellow);
            string start = "PIES LEFT:";
            if (player.Bazooka.PiesLeft < 10) start += " ";
            spriteBatch.DrawString(scoreFont, start + player.Bazooka.PiesLeft, new Vector2(765, 10), Color.White);
            int numMessages = 0;
            foreach(String message in updateStrings)
                spriteBatch.DrawString(messageFont, message, new Vector2(10, 50+15*numMessages++), Color.Red);
            spriteBatch.End();
            GraphicsDevice.DepthStencilState = DepthStencilState.Default; 
            
            base.Draw(gameTime);
        }

        public string TestRegion(Vector3 pos)
        {
            return player.TestRegion(pos);
        }

        public void AddFlyingPie(Pie pie)
        {
            flyingPies.Add(pie);
        }

        public void AttachPieToDoor(Pie pie, string region)
        {
            char doorNum = region[region.Length - 1];
            foreach (PrisonModel mod in phibesModels)
            {
                mod.AttachPie(pie, doorNum);
            }
        }

        public float getDoorHeight(int doorNum)
        {
            foreach (PrisonModel mod in phibesModels)
            {
                if (mod.DoorHeight(doorNum) > 0)
                    return mod.DoorHeight(doorNum);
            }
            return 0;
        }
    }
}
