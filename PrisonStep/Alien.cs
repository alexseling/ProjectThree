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
    public class Alien
    {
        #region Fields

        /// <summary>
        /// This is a range from the door center that is considered
        /// to be under the door.  This much either side.
        /// </summary>
        private const float DoorUnderRange = 40;
        private float turningRate = 1;
        /// <summary>
        /// Game that uses this player
        /// </summary>
        private PrisonGame game;

        private Spit spit;
        public Spit FiredSpit { get { return spit; } }

        private float spitFrequency = 10; // number of seconds until they spit, on average;

        private int targetDoor = 5;
        private float targetRotation = 0;
        private double targetX = 1187;
        private double targetZ = -1000;
        private string lastRegion = "";

        //
        // Player location information.  We keep a x/z location (y stays zero)
        // and an orientation (which way we are looking).
        //

        private float walkSpeed = 1;
        private double pauseTime = 0;

        /// <summary>
        /// Player location in the prison. Only x/z are important. y still stay zero
        /// unless we add some flying or jumping behavior later on.
        /// </summary>
        private Vector3 location = new Vector3(1187, 0, -1280);
        public Vector3 Location { get { return location; } set { location = value; } }

        /// <summary>
        /// The player orientation as a simple angle
        /// </summary>
        private float orientation = (float)Math.PI;
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
        private AnimatedModel animModel;
        public AnimatedModel AnimModel { get { return animModel; } }

        private KeyboardState lastKeyboardState = Keyboard.GetState();

        private enum States { Start, StanceStart, Stance, WalkStart, WalkLoopStart, WalkLoop, TurnLeft, TurnRight, Tantrum, Tantruming, EatPie, EatingPie, ReadyToCatch, WaitingToCatch }
        private States state = States.Start;

        private BoundingCylinder bc;
        public BoundingCylinder BC { get { return bc; } }

        private Pie pie = null;
        public Pie Pie { get { return pie; } set { pie = value; } }

        #endregion


        public Alien(PrisonGame game)
        {
            this.game = game;
            animModel = new AnimatedModel(game, "Alien");
            animModel.AddAssetClip("pie", "Alien-catcheat");
            animModel.AddAssetClip("stance", "Alien-stance");
            animModel.AddAssetClip("ob", "Alien-ob");
            animModel.AddAssetClip("tantrum", "Alien-tantrum");
            animModel.AddAssetClip("walkstart", "Alien-walkstart");
            animModel.AddAssetClip("walkloop", "Alien-walkloop");

            bc = new BoundingCylinder();
            bc.Radius = 21;
            bc.Height = 174;
            spit = new Spit(game, transform, 160);

            SetAlienTransform();
        }

        public void Initialize()
        {
            lastGPS = GamePad.GetState(PlayerIndex.One);
        }

        public void ReachForPie(int roomFiredFrom)
        {
            if (Math.Abs(roomFiredFrom - animModel.CurrRegion) > 2) return;
            if (pie == null && state != States.WaitingToCatch && state != States.Tantruming)
                state = States.ReadyToCatch;
        }

        public void MissedPie()
        {
            state = States.Tantrum;
            spitFrequency *= 0.95f;
        }

        /// <summary>
        /// Set the value of transform to match the current location
        /// and orientation.
        /// </summary>
        private void SetAlienTransform()
        {
            transform = Matrix.CreateRotationY(orientation);
            transform.Translation = location;
        }

        public bool CanGetPie()
        {
            return (state != States.Tantruming && state != States.EatingPie);
        }

        public void LoadContent(ContentManager content)
        {
            Model model = content.Load<Model>("AntonPhibesCollision");
            animModel.LoadContent(content);
            spit.LoadContent(content);
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
            AnimationPlayer player = animModel.PlayClip("stance");
        }

        public void Spit()
        {
            if (spit.BeenFired == true) return;
            spit.Fire(transform);
        }

        public void AttachPie(Pie pieToAttach)
        {
            int distAway = (int)Vector3.Distance(transform.Translation, game.Player.Transform.Translation);
            int multiplier = 1;
            if (game.Player.Bazooka.PiesLeft == 0) multiplier = 2;
            string kind = "pie: ";
            if (multiplier == 2) kind = "final pie: " + distAway + " * 2 = ";
            game.UpdateScore("Hit Alien with " + kind, distAway * multiplier);
            pie = pieToAttach;
            state = States.EatPie;
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
            if ((float)game.Dalek.Random.NextDouble() * spitFrequency < deltaTotal) Spit();
            float speed = 0;

            do
            {
                double delta = deltaTotal;

                switch (state)
                {
                    case States.Start:
                        state = States.Tantrum;
                        delta = 0;
                        break;

                    case States.StanceStart:
                        animModel.PlayClip("stance");
                        animModel.Player.Speed = 0;

                        state = States.Stance;
                        location.Y = 0;
                        break;

                    case States.ReadyToCatch:
                        animModel.PlayClip("pie");
                        animModel.Player.Speed = 1;
                        state = States.WaitingToCatch;
                        break;

                    case States.WaitingToCatch:
                        if (animModel.Player.Time > 1)
                        {
                            pauseTime = gameTime.TotalGameTime.TotalMilliseconds;
                            animModel.Player.Speed = 0;
                            animModel.Player.Time = 1;
                        }
                        if (animModel.Player.Time == 1 && (gameTime.TotalGameTime.TotalMilliseconds - pauseTime) > 2000)
                            MissedPie();
                        if (pie != null)
                        state = States.EatPie;
                        break;

                    case States.EatPie:
                        double prevTime = animModel.Player.Time;
                        animModel.PlayClip("pie");
                        animModel.Player.Speed = 1;
                        animModel.Player.Time = prevTime;
                        state = States.EatingPie;
                        break;

                    case States.EatingPie:
                        if (animModel.Player.Time > 2.5)
                            pie = null;
                        if (delta > animModel.Player.Clip.Duration - animModel.Player.Time)
                        {
                            delta = animModel.Player.Clip.Duration - animModel.Player.Time;
                            // The clip is done after this update
                            state = States.StanceStart;
                        }
                        location.Y = 0;
                        break;

                    case States.Tantrum:
                        animModel.PlayClip("tantrum");
                        animModel.Player.Speed = 1;
                        state = States.Tantruming;
                        
                        break;

                    case States.Tantruming:
                        if (delta > animModel.Player.Clip.Duration - animModel.Player.Time)
                        {
                            delta = animModel.Player.Clip.Duration - animModel.Player.Time;
                            // The clip is done after this update
                            state = States.StanceStart;
                        }
                        location.Y = 0;
                        break;

                    case States.Stance:
                        speed = walkSpeed;
                        float turnRate = 0;
                        if (speed > 0)
                        {
                            // We need to leave the stance state and start walking
                            animModel.PlayClip("walkstart");
                            animModel.Player.Speed = speed;
                            state = States.WalkStart;
                        }
                        else if (turnRate > 0)
                        {
                            state = States.TurnLeft;
                        }
                        else if (turnRate < 0)
                        {
                            state = States.TurnRight;
                        }

                        break;

                    case States.WalkStart:
                    case States.WalkLoop:
                        if (delta > animModel.Player.Clip.Duration - animModel.Player.Time)
                        {
                            delta = animModel.Player.Clip.Duration - animModel.Player.Time;

                            // The clip is done after this update
                            state = States.WalkLoopStart;
                        }

                        speed = walkSpeed;
                        if (speed == 0)
                        {
                            delta = 0;
                            state = States.StanceStart;
                        }
                        else
                        {
                            animModel.Player.Speed = walkSpeed;
                        }
                        location.Y = 0;
                        break;

                    case States.WalkLoopStart:
                        animModel.PlayClip("walkloop").Speed = walkSpeed;
                        state = States.WalkLoop;
                        break;

                    case States.TurnLeft:
                    case States.TurnRight:
                        if (delta > animModel.Player.Clip.Duration - animModel.Player.Time)
                        {
                            delta = animModel.Player.Clip.Duration - animModel.Player.Time;

                            // The clip is done after this update
                            state = States.StanceStart;
                        }

                        speed = 1;//GetDesiredSpeed(ref keyboardState, ref gamePadState);
                        if (speed > 0)
                        {
                            animModel.PlayClip("walkstart");
                            animModel.Player.Speed = 0.2;
                            state = States.WalkStart;
                        }
                        location.Y = 0;
                        break;
                }

                if (state != States.EatingPie && state != States.Tantruming)
                    orientation += turningRate * (float)delta;// GetDesiredTurnRate(ref keyboardState, ref gamePadState) * (float)delta;
                walkSpeed = 1;

                // 
                // State update
                //


                animModel.Update(delta);
                bc.Position = this.transform.Translation;
                Matrix[] absBones = new Matrix[animModel.Model.Bones.Count];
                animModel.Model.CopyAbsoluteBoneTransformsTo(absBones);
                bc.Height = 174;

                //
                // Part 1:  Compute a new orientation
                //

                Matrix deltaMatrix = animModel.DeltaMatrix;
                float deltaAngle = (float)Math.Atan2(deltaMatrix.Backward.X, deltaMatrix.Backward.Z);
                float newOrientation = orientation + deltaAngle;
                
                //
                // Part 2:  Compute a new location
                //

                // We are likely rotated from the angle the model expects to be in
                // Determine that angle.
                Matrix rootMatrix = animModel.RootMatrix;
                float actualAngle = (float)Math.Atan2(rootMatrix.Backward.X, rootMatrix.Backward.Z);
                Vector3 newLocation = location + 2 * Vector3.TransformNormal(animModel.DeltaPosition,
                               Matrix.CreateRotationY(newOrientation - actualAngle));

                //
                // I'm just taking these here.  You'll likely want to add something 
                // for collision detection instead.
                //

                bool collision = false;     // Until we know otherwise
                bool isOpen = false;

                string region = TestRegion(newLocation);
                //System.Diagnostics.Trace.WriteLine("Player at " + region);

                foreach (PrisonModel model in game.PhibesModels)
                {
                    if (model.DoorIsOpen(targetDoor))
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
                    collision = true;
                    //state = States.StanceStart;
                }
                if (Math.Abs((orientation % ((float)Math.PI * 2)) - targetRotation) < 0.1f)
                {
                    orientation = targetRotation;
                    turningRate = 0;
                    walkSpeed = 1;
                }
                else
                {
                    walkSpeed = 0;
                }
                lastRegion = region;

                double x = (double)transform.Translation.X;
                double z = (double)transform.Translation.Z;
                double distFromTarget = Math.Sqrt((x - targetX) * (x - targetX) + (z - targetZ) * (z - targetZ));
                if (distFromTarget < 20)
                {
                    game.UpdateScore("Alien escaped to a new room", -700);
                    // Hit target - trying to get to next stop
                    int prevTargetDoor = targetDoor;
                    if (prevTargetDoor == 5)
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

                if (!collision)
                    location = newLocation;
                SetAlienTransform();
                spit.Update(gameTime, transform);


                deltaTotal -= delta;
            } while (deltaTotal > 0);
        }


        /// <summary>
        /// This function is called to draw the Alien.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime)
        {
            Matrix transform = Matrix.CreateRotationY(orientation);
            transform.Translation = location;

            animModel.Draw(graphics, gameTime, transform);
            if (spit.BeenFired == true)
                spit.Draw(graphics, gameTime, transform);
            if (pie != null) {
                Matrix[] absBones = new Matrix[animModel.Model.Bones.Count];
                animModel.Model.CopyAbsoluteBoneTransformsTo(absBones);
                int handBoneIndex = animModel.Model.Bones["Bip01 L Finger0"].Index;
                pie.Draw(graphics, gameTime, absBones[handBoneIndex] * transform);
            }
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
