using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace PrisonStep
{
    /// <summary>
    /// This class describes our player in the game. 
    /// </summary>
    public class Player
    {
        #region Fields

        /// <summary>
        /// This is a range from the door center that is considered
        /// to be under the door.  This much either side.
        /// </summary>
        private const float DoorUnderRange = 40;

        /// <summary>
        /// Game that uses this player
        /// </summary>
        private PrisonGame game;

        //
        // Player location information.  We keep a x/z location (y stays zero)
        // and an orientation (which way we are looking).
        //

        /// <summary>
        /// Player location in the prison. Only x/z are important. y still stay zero
        /// unless we add some flying or jumping behavior later on.
        /// </summary>
        private Vector3 location = new Vector3(275, 0, 1053);
        public Vector3 Location { get { return location; } set { location = value; } }

        /// <summary>
        /// The player orientation as a simple angle
        /// </summary>
        private float orientation = (float)Math.PI * 3 / 2;
        public float Orientation { get { return orientation; } set { orientation = value; } }

        /// <summary>
        /// The player transformation matrix. Places the player where they need to be.
        /// </summary>
        private Matrix transform;
        public Matrix Transform { get { return transform; } }

        /// <summary>
        /// The rotation rate in radians per second when player is rotating
        /// </summary>
        private float panRate = 2;

        /// <summary>
        /// The player move rate in centimeters per second
        /// </summary>
        private float moveRate = 500;

        /// <summary>
        /// Id for a door we are opening or 0 if none.
        /// </summary>
        private int openDoor = 0;

        /// <summary>
        /// Keeps track of the last game pad state
        /// </summary>
        GamePadState lastGPS;

        private Dictionary<string, List<Vector2>> regions = new Dictionary<string, List<Vector2>>();

        /// <summary>
        /// Our animated model
        /// </summary>
        private AnimatedModel victoria;
        public AnimatedModel Victoria { get { return victoria; } }

        public bool BazookaRaised { get { return (bazookaState == 2); } }
        private int bazookaState = -2;

        private KeyboardState lastKeyboardState = Keyboard.GetState();

        private enum States { Start, StanceStart, Stance, WalkStart, WalkLoopStart, WalkLoop, TurnLeft, TurnRight, Crouch, Crouching, RaiseBazooka, RaisingBazooka, LowerBazooka, LoweringBazooka }
        private States state = States.Start;

        private Bazooka bazooka;
        public Bazooka Bazooka { get { return bazooka; } set { bazooka = value; } }

        private BoundingCylinder bc;
        public BoundingCylinder BC { get { return bc; } }

        #endregion


        public Player(PrisonGame game)
        {
            this.game = game;
            victoria = new AnimatedModel(game, "Victoria");
           // bazooka = new AnimatedModel(game, "PieBazooka"); 
            victoria.AddAssetClip("dance", "Victoria-dance");
            victoria.AddAssetClip("stance", "Victoria-stance");
            victoria.AddAssetClip("crouch_bazooka", "Victoria-crouchbazooka");
            victoria.AddAssetClip("walk", "Victoria-walk");
            victoria.AddAssetClip("walkstart", "Victoria-walkstart");
            victoria.AddAssetClip("walkloop", "Victoria-walkloop");
            victoria.AddAssetClip("lowerBazooka", "Victoria-lowerbazooka");
            victoria.AddAssetClip("raiseBazooka", "Victoria-raisebazooka");
            victoria.AddAssetClip("walkstart_bazooka", "Victoria-walkstartbazooka");
            victoria.AddAssetClip("walkloop_bazooka", "Victoria-walkloopbazooka");
            victoria.AddAssetClip("leftturn", "Victoria-leftturn");
            victoria.AddAssetClip("rightturn", "Victoria-rightturn");

            bc = new BoundingCylinder();
            bc.Radius = 21;
            bc.Height = 174;

            SetPlayerTransform();
        }

        public void Initialize()
        {
            lastGPS = GamePad.GetState(PlayerIndex.One);
        }

        /// <summary>
        /// Set the value of transform to match the current location
        /// and orientation.
        /// </summary>
        private void SetPlayerTransform()
        {
            transform = Matrix.CreateRotationY(orientation);
            transform.Translation = location;
        }


        public void LoadContent(ContentManager content)
        {
            Model model = content.Load<Model>("AntonPhibesCollision");
            victoria.LoadContent(content);
            //bazooka.LoadContent(content);
            Matrix[] M = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(M);

            foreach (ModelMesh mesh in model.Meshes)
            {
                // For accumulating the triangles for this mesh
                List<Vector2> triangles = new List<Vector2>();

                // Loop over the mesh parts
                foreach(ModelMeshPart meshPart in mesh.MeshParts)
                {
                    // 
                    // Obtain the vertices for the mesh part
                    //

                    int numVertices = meshPart.VertexBuffer.VertexCount;
                    VertexPositionColorTexture[] verticesRaw = new VertexPositionColorTexture[numVertices];
                    meshPart.VertexBuffer.GetData<VertexPositionColorTexture>(verticesRaw);

                    //
                    // Obtain the indices for the mesh part
                    //

                    int numIndices = meshPart.IndexBuffer.IndexCount;
                    short [] indices = new short[numIndices];
                    meshPart.IndexBuffer.GetData<short>(indices);

                    //
                    // Build the list of triangles
                    //

                    for (int i = 0; i < meshPart.PrimitiveCount * 3; i++)
                    {
                        // The actual index is relative to a supplied start position
                        int index = i + meshPart.StartIndex;

                        // Transform the vertex into world coordinates
                        Vector3 v = Vector3.Transform(verticesRaw[indices[index] + meshPart.VertexOffset].Position, M[mesh.ParentBone.Index]);
                        triangles.Add(new Vector2(v.X, v.Z));
                    }

                }

                regions[mesh.Name] = triangles;
            }
            AnimationPlayer player = victoria.PlayClip("stance");
        }

        public string TestRegion(Vector3 v3)
        {
            // Convert to a 2D Point
            float x = v3.X;
            float y = v3.Z;

            foreach (KeyValuePair<string, List<Vector2>> region in regions)
            {
                // For now we ignore the walls
                if(region.Key.StartsWith("W"))
                    continue;

                for (int i = 0; i < region.Value.Count; i += 3)
                {
                    float x1 = region.Value[i].X;
                    float x2 = region.Value[i + 1].X;
                    float x3 = region.Value[i + 2].X;
                    float y1 = region.Value[i].Y;
                    float y2 = region.Value[i + 1].Y;
                    float y3 = region.Value[i + 2].Y;

                    float d = 1.0f / ((x1 - x3) * (y2 - y3) - (x2 - x3) * (y1 - y3));
                    float l1 = ((y2 - y3) * (x - x3) + (x3 - x2) * (y - y3)) * d;
                    if (l1 < 0)
                        continue;

                    float l2 = ((y3 - y1) * (x - x3) + (x1 - x3) * (y - y3)) * d;
                    if (l2 < 0)
                        continue;

                    float l3 = 1 - l1 - l2;
                    if (l3 < 0)
                        continue;

                    return region.Key;
                }
            }

            return "";
        }


        public void Update(GameTime gameTime)
        {
            double deltaTotal = gameTime.ElapsedGameTime.TotalSeconds;

            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            float speed = 0;

            do
            {
                double delta = deltaTotal;
                if (keyboardState.IsKeyDown(Keys.B) && !lastKeyboardState.IsKeyDown(Keys.B) && state != States.Crouching && state != States.Crouch)
                {
                    if (bazookaState < 0)
                        state = States.RaiseBazooka;
                    else
                        state = States.LowerBazooka;
                }
                if (bazookaState == -2 && keyboardState.IsKeyDown(Keys.Down) && !lastKeyboardState.IsKeyDown(Keys.Down))
                {
                    state = States.Crouch;
                }
                lastKeyboardState = keyboardState;

                switch (state)
                {
                    case States.Start:
                        state = States.StanceStart;
                        delta = 0;
                        break;

                    case States.StanceStart:
                        if (BazookaRaised == false)
                            victoria.PlayClip("raiseBazooka");
                        else
                            victoria.PlayClip("lowerBazooka");
                        victoria.Player.Speed = 0;

                        state = States.Stance;
                        location.Y = 0;
                        break;

                    case States.Crouch:
                        victoria.PlayClip("crouch_bazooka");
                        victoria.Player.Speed = 1;
                        state = States.Crouching;
                        break;

                    case States.Crouching:
                        if (delta > victoria.Player.Clip.Duration - victoria.Player.Time)
                        {
                            delta = victoria.Player.Clip.Duration - victoria.Player.Time;
                            // The clip is done after this update
                            state = States.StanceStart;
                        }
                        break;

                    case States.RaiseBazooka:
                        double timeElapsed = 0;
                        if (bazookaState == -1)
                            timeElapsed = victoria.Player.Time;
                        victoria.PlayClip("raiseBazooka");
                        victoria.Player.Speed = 1;
                        if (bazookaState == -1)
                            victoria.Player.Time = victoria.Player.Clip.Duration - timeElapsed;
                        bazookaState = 1;
                        state = States.RaisingBazooka;
                        break;

                    case States.RaisingBazooka:
                        if (delta > victoria.Player.Clip.Duration - victoria.Player.Time)
                        {
                            delta = victoria.Player.Clip.Duration - victoria.Player.Time;
                            bazookaState = 2;
                            victoria.AllowAim = true;
                            // The clip is done after this update
                            state = States.StanceStart;
                        }
                        break;

                    case States.LowerBazooka:
                        double timeElapsed2 = 0;
                        if (bazookaState == 1)
                            timeElapsed2 = victoria.Player.Time;
                        victoria.PlayClip("lowerBazooka");
                        victoria.Player.Speed = 1;
                        if (bazookaState == 1)
                            victoria.Player.Time = victoria.Player.Clip.Duration - timeElapsed2;
                        victoria.FixAimTime = victoria.Player.Clip.Duration - victoria.Player.Time;
                        victoria.AllowAim = false;
                        bazookaState = -1;
                        state = States.LoweringBazooka;
                        break;

                    case States.LoweringBazooka:
                        if (delta > victoria.Player.Clip.Duration - victoria.Player.Time)
                        {
                            delta = victoria.Player.Clip.Duration - victoria.Player.Time;
                            bazookaState = -2;
                            // The clip is done after this update
                            state = States.StanceStart;
                        }
                        break;

                    case States.Stance:
                        speed = GetDesiredSpeed(ref keyboardState, ref gamePadState);
                        float turnRate = GetDesiredTurnRate(ref keyboardState, ref gamePadState);
                        if (speed > 0 && BazookaRaised == false)
                        {
                            // We need to leave the stance state and start walking
                            victoria.PlayClip("walkstart_bazooka");
                            victoria.Player.Speed = speed;
                            state = States.WalkStart;
                        }
                        else if (turnRate > 0)
                        {
                         //   victoria.PlayClip("leftturn");
                         //   victoria.Player.Speed = 1;
                            state = States.TurnLeft;
                        }
                        else if (turnRate < 0)
                        {
                         //   victoria.PlayClip("rightturn");
                         //   victoria.Player.Speed = 1;
                            state = States.TurnRight;
                        }

                        break;

                    case States.WalkStart:
                    case States.WalkLoop:
                        if (delta > victoria.Player.Clip.Duration - victoria.Player.Time)
                        {
                            delta = victoria.Player.Clip.Duration - victoria.Player.Time;

                            // The clip is done after this update
                            state = States.WalkLoopStart;
                        }

                        speed = GetDesiredSpeed(ref keyboardState, ref gamePadState);
                        if (speed == 0)
                        {
                            delta = 0;
                            state = States.StanceStart;
                        }
                        else
                        {
                            victoria.Player.Speed = speed;
                        }
                        location.Y = 0;
                        break;

                    case States.WalkLoopStart:
                        victoria.PlayClip("walkloop_bazooka").Speed = GetDesiredSpeed(ref keyboardState, ref gamePadState);
                        state = States.WalkLoop;
                        break;

                    case States.TurnLeft:
                    case States.TurnRight:
                        if (delta > victoria.Player.Clip.Duration - victoria.Player.Time)
                        {
                            delta = victoria.Player.Clip.Duration - victoria.Player.Time;

                            // The clip is done after this update
                            state = States.StanceStart;
                        }

                        speed = GetDesiredSpeed(ref keyboardState, ref gamePadState);
                        if (speed > 0 && BazookaRaised == false)
                        {
                            victoria.PlayClip("walkstart_bazooka");
                            victoria.Player.Speed = speed;
                            state = States.WalkStart;
                        }
                        location.Y = 0;
                        break;
                }

                orientation += GetDesiredTurnRate(ref keyboardState, ref gamePadState) * (float)delta;

                // 
                // State update
                //


                victoria.Update(delta);
                bc.Position = this.transform.Translation;
                Matrix[] absBones = new Matrix[victoria.Model.Bones.Count];
                victoria.Model.CopyAbsoluteBoneTransformsTo(absBones);
                int headBoneIndex = victoria.Model.Bones["Bip01 Head"].Index;
                bc.Height = absBones[headBoneIndex].M42 + 23;

                //
                // Part 1:  Compute a new orientation
                //

                Matrix deltaMatrix = victoria.DeltaMatrix;
                float deltaAngle = (float)Math.Atan2(deltaMatrix.Backward.X, deltaMatrix.Backward.Z);
                float newOrientation = orientation + deltaAngle;

                //
                // Part 2:  Compute a new location
                //

                // We are likely rotated from the angle the model expects to be in
                // Determine that angle.
                Matrix rootMatrix = victoria.RootMatrix;
                float actualAngle = (float)Math.Atan2(rootMatrix.Backward.X, rootMatrix.Backward.Z);
                Vector3 newLocation = location + 2*Vector3.TransformNormal(victoria.DeltaPosition,
                               Matrix.CreateRotationY(newOrientation - actualAngle));

                //
                // I'm just taking these here.  You'll likely want to add something 
                // for collision detection instead.
                //

                bool collision = false;     // Until we know otherwise

                string region = TestRegion(newLocation);
                if (region.StartsWith("R_Section"))
                {
                    int regionNum = int.Parse(region.Substring(9));
                }
                //System.Diagnostics.Trace.WriteLine("Player at " + region);

                // Slimed support
                /*if (!game.Slimed && region == "R_Section6")
                {
                    game.Slimed = true;
                }
                else */if (game.Slimed && region == "R_Section1")
                {
                    game.Slimed = false;
                }
                if (region == "R_Section1")
                    bazooka.Reload();

                if (region == "")
                {
                    // If not in a region, we have stepped out of bounds
                    collision = true;
                    state = States.StanceStart;
                }
                else if (region.StartsWith("R_Door"))   // Are we in a door region
                {
                    // What is the door number for the region we are in?
                    int dnum = int.Parse(region.Substring(6));

                    // Are we currently facing the door or walking through a 
                    // door?

                    bool underDoor;
                    if (DoorShouldBeOpen(dnum, location, transform.Backward, out underDoor))
                    {
                        SetOpenDoor(dnum);
                    }
                    else
                    {
                        SetOpenDoor(0);
                    }

                    if (underDoor)
                    {
                        // is the door actually open right now?
                        bool isOpen = false;
                        foreach (PrisonModel model in game.PhibesModels)
                        {
                            if (model.DoorIsOpen(dnum))
                            {
                                isOpen = true;
                                break;
                            }
                        }

                        if (!isOpen)
                            collision = true;
                    }
                }
                else if (openDoor > 0)
                {
                    // Indicate none are open
                    SetOpenDoor(0);
                }

                if (!collision)
                {
                    location = newLocation;
                    orientation = newOrientation;
                }



                SetPlayerTransform();


                deltaTotal -= delta;
            } while (deltaTotal > 0);
        }


        /// <summary>
        /// This function is called to draw the player.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime)
        {
            Matrix transform = Matrix.CreateRotationY(orientation);
            transform.Translation = location;

            victoria.Draw(graphics, gameTime, transform);
           // bazooka.Draw(graphics, gameTime, transform); 

            //bazooka.Draw(graphics, gameTime, bazookaMatrix);
        }

        private float GetDesiredSpeed(ref KeyboardState keyboardState, ref GamePadState gamePadState)
        {
            if (keyboardState.IsKeyDown(Keys.Up))
                return 1.5f;

            float speed = gamePadState.ThumbSticks.Right.Y;

            // I'm not allowing you to walk backwards
            if (speed < 0)
                speed = 0;

            return speed;
        }

        private float GetDesiredTurnRate(ref KeyboardState keyboardState, ref GamePadState gamePadState)
        {
            if (keyboardState.IsKeyDown(Keys.Left))
            {
                return panRate;
            }

            if (keyboardState.IsKeyDown(Keys.Right))
            {
                return -panRate;
            }

            return -gamePadState.ThumbSticks.Right.X * panRate;
        }

        /// <summary>
        /// This is the logic that determines if a door should be open.  This is 
        /// based on a position and a direction we are traveling.  
        /// </summary>
        /// <param name="dnum">Door number we are interested in (1-5)</param>
        /// <param name="loc">A location near the door</param>
        /// <param name="dir">Direction we are currently facing as a vector.</param>
        /// <param name="doorVector">A vector pointing throught the door.</param>
        /// <param name="doorCenter">The center of the door.</param>
        /// <param name="under">Return value - indicates we are under the door</param>
        /// <returns>True if we are under the door</returns>
        private bool DoorShouldBeOpen(int dnum, Vector3 loc, Vector3 dir, out bool under)
        {
            Vector3 doorCenter;
            Vector3 doorVector;

            // I need to know information about the doors.  This 
            // is the location and a vector through the door for each door.
            switch (dnum)
            {
                case 1:
                    doorCenter = new Vector3(218, 0, 1023);
                    doorVector = new Vector3(1, 0, 0);
                    break;

                case 2:
                    doorCenter = new Vector3(-11, 0, -769);
                    doorVector = new Vector3(0, 0, 1);
                    break;

                case 3:
                    doorCenter = new Vector3(587, 0, -999);
                    doorVector = new Vector3(1, 0, 0);
                    break;

                case 4:
                    doorCenter = new Vector3(787, 0, -763);
                    doorVector = new Vector3(0, 0, 1);
                    break;

                case 5:
                default:
                    doorCenter = new Vector3(1187, 0, -1218);
                    doorVector = new Vector3(0, 0, 1);
                    break;
            }

            // I want the door vector to indicate the direction we are doing through the
            // door.  This depends on the side of the center we are on.
            Vector3 toDoor = doorCenter - loc;
            if (Vector3.Dot(toDoor, doorVector) < 0)
            {
                doorVector = -doorVector;
            }


            // Determine if we are under the door
            // Determine points after the center where we are 
            // considered to be under the door
            Vector3 doorBefore = doorCenter - doorVector * DoorUnderRange;
            Vector3 doorAfter = doorCenter + doorVector * DoorUnderRange;
            under = false;

            // If we have passed the point before the door, a vector 
            // to our position from that point will be pointing within 
            // 90 degrees of the door vector.  
            if (Vector3.Dot(loc - doorAfter, doorVector) <= 0 &&
                Vector3.Dot(loc - doorBefore, doorVector) >= 0)
            {
                under = true;
                return true;
            }

            // Are we facing the door?
            if (Vector3.Dot(dir, doorVector) >= 0)
            {
                // We are, so the door should be open
                return true;
            }

            return false;
        }


        /// <summary>
        /// Set the current open/opening door
        /// </summary>
        /// <param name="dnum">Door to set open or 0 if none</param>
        private void SetOpenDoor(int dnum)
        {
            // Is this already indicated?
            if (openDoor == dnum)
                return;

            // Is a door other than this already open?
            // If so, make it close
            if (openDoor > 0 && openDoor != dnum)
            {
                foreach (PrisonModel model in game.PhibesModels)
                {
                    model.SetDoor(openDoor, false);
                }
            }

            // Make this the open door and flag it as open
            openDoor = dnum;
            if (openDoor > 0)
            {
                foreach (PrisonModel model in game.PhibesModels)
                {
                    model.SetDoor(openDoor, true);
                }
            }
        }




    }
}
