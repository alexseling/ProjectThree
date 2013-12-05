using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using XnaAux;

namespace PrisonStep
{
    public class AnimatedModel
    {
        /// <summary>
        /// Reference to the game that uses this class
        /// </summary>
        private PrisonGame game;

        /// <summary>
        /// The XNA model we will be animating
        /// </summary>
        private Model model;
        public Model Model { get { return model; } }

        private Random random = new Random();
        private double aimFactor = 1;
        private double fixAimTime = 0;
        public double FixAimTime { set { fixAimTime = value; } }
        private double fixPerSecond = 0;
        private bool allowAim = false;
        public bool AllowAim { set { allowAim = value; } }
        private int currRegion = 1;
        public int CurrRegion { get { return currRegion; } }
        private int currDoor = 1;
        private int regionOppositeDoor = 2;
        private float percentOpen = 0;

        private Matrix[] bindTransforms;
        private Matrix[] boneTransforms;
        private Matrix[] absoTransforms;
        private Matrix[] skinTransforms = null;
        private List<int> skelToBone = null;
        private Matrix[] inverseBindTransforms = null;
        private Matrix rootMatrixRaw = Matrix.Identity;
        private Matrix deltaMatrix = Matrix.Identity;

        public Matrix DeltaMatrix { get { return deltaMatrix; } }
        public Vector3 DeltaPosition;
        public Matrix RootMatrix { get { return inverseBindTransforms[skelToBone[0]] * rootMatrixRaw; } }

        /// <summary>
        /// Name of the asset we are going to load
        /// </summary>
        private string asset;

        /// <summary>
        /// The number of skinning matrices in SkinnedEffect.fx. This must
        /// match the number in SkinnedEffect.fx.
        /// </summary>
        /// 
        public const int NumSkinBones = 57;

        /// <summary>
        /// This class describes a single animation clip we load from
        /// an asset.
        /// </summary>
        private class AssetClip
        {
            public AssetClip(string name, string asset)
            {
                Name = name;
                Asset = asset;
                TheClip = null;
            }

            public string Name { get; set; }
            public string Asset { get; set; }
            public AnimationClips.Clip TheClip { get; set; }
        }

        /// <summary>
        /// A dictionary that allows us to look up animation clips
        /// by name. 
        /// </summary>
        private Dictionary<string, AssetClip> assetClips = new Dictionary<string, AssetClip>();

        /// <summary>
        /// Access the current animation player
        /// </summary>
        public AnimationPlayer Player { get { return player; } }


        public AnimatedModel(PrisonGame game, string asset)
        {
            this.game = game;
            this.asset = asset;
            skinTransforms = new Matrix[NumSkinBones];
            for (int i = 0; i < skinTransforms.Length; i++)
            {
                skinTransforms[i] = Matrix.Identity;
            }
        }


        /// <summary>
        /// This function is called to load content into this component
        /// of our game.
        /// </summary>
        /// <param name="content">The content manager to load from.</param>
        public void LoadContent(ContentManager content)
        {
            model = content.Load<Model>(asset);

            int boneCnt = model.Bones.Count;
            bindTransforms = new Matrix[boneCnt];
            boneTransforms = new Matrix[boneCnt];
            absoTransforms = new Matrix[boneCnt];

            model.CopyBoneTransformsTo(bindTransforms);
            model.CopyBoneTransformsTo(boneTransforms);
            model.CopyAbsoluteBoneTransformsTo(absoTransforms);

            AnimationClips clips = model.Tag as AnimationClips;
            if (clips != null && clips.SkelToBone.Count > 0)
            {
                skelToBone = clips.SkelToBone;

                inverseBindTransforms = new Matrix[boneCnt];
                skinTransforms = new Matrix[NumSkinBones];

                model.CopyAbsoluteBoneTransformsTo(inverseBindTransforms);

                for (int b = 0; b < inverseBindTransforms.Length; b++)
                    inverseBindTransforms[b] = Matrix.Invert(inverseBindTransforms[b]);

                for (int i = 0; i < skinTransforms.Length; i++)
                    skinTransforms[i] = Matrix.Identity;
            }

            foreach (AssetClip clip in assetClips.Values)
            {
                Model clipmodel = content.Load<Model>(clip.Asset);
                AnimationClips modelclips = clipmodel.Tag as AnimationClips;
                clip.TheClip = modelclips.Clips["Take 001"];
            }
        }

        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(double delta)
        {
            if (fixAimTime != 0)
            {
                fixPerSecond = (1 - aimFactor) / fixAimTime;
                fixAimTime = 0;
            }

            if (player != null)
            {
                // Update the clip
                player.Update(delta);

                KeyboardState keyboardState = Keyboard.GetState();

                if (fixPerSecond == 0)
                {
                    if (allowAim == true)
                    {
                        if (keyboardState.IsKeyDown(Keys.T)) aimFactor += 3 * delta;
                        if (keyboardState.IsKeyDown(Keys.G)) aimFactor -= 3 * delta;
                        if (aimFactor < -3) aimFactor = -3;
                        if (aimFactor > 5) aimFactor = 5;
                    }
                }
                else
                {
                    double prevAim = aimFactor;
                    aimFactor += fixPerSecond * delta;
                    if (prevAim <= 1 && aimFactor >= 1 || prevAim >= 1 && aimFactor <= 1)
                    {
                        aimFactor = 1;
                        fixPerSecond = 0;
                    }
                }

                float randNum = (float)random.NextDouble();
                for (int b = 0; b < player.BoneCount; b++)
                {
                    AnimationPlayer.Bone bone = player.GetBone(b);
                    if (!bone.Valid)
                        continue;

                    Vector3 scale = new Vector3(bindTransforms[b].Right.Length(),
                        bindTransforms[b].Up.Length(),
                        bindTransforms[b].Backward.Length());

                    Quaternion boneRotation = bone.Rotation;

                    if (this.asset == "Victoria")
                    {
                        if (b == model.Bones.IndexOf(model.Bones["Bip01 Spine1"]))
                        {
                            boneRotation.Z *= (float)aimFactor;
                        }
                    }

                    boneTransforms[b] = Matrix.CreateScale(scale) *
                        Matrix.CreateFromQuaternion(boneRotation) *
                        Matrix.CreateTranslation(bone.Translation);

                }

                if (skelToBone != null)
                {
                    int rootBone = skelToBone[0];

                    deltaMatrix = Matrix.Invert(rootMatrixRaw) * boneTransforms[rootBone];
                    DeltaPosition = boneTransforms[rootBone].Translation - rootMatrixRaw.Translation;

                    rootMatrixRaw = boneTransforms[rootBone];

                    boneTransforms[rootBone] = bindTransforms[rootBone];
                }
                model.CopyBoneTransformsFrom(boneTransforms);
            }


            model.CopyBoneTransformsFrom(boneTransforms);
            model.CopyAbsoluteBoneTransformsTo(absoTransforms);

            Vector3 playerLoc = game.Player.Transform.Translation;
            if (this.asset != "Victoria")
                playerLoc = game.Alien.Transform.Translation;
            string regionIn = game.Player.TestRegion(playerLoc);
            if (regionIn.StartsWith("R_Door"))
                currDoor = int.Parse(regionIn.Substring(6));

            if (currDoor == 1)
            {
                if (playerLoc.X < 218) currRegion = 2;
                else currRegion = 1;
            }
            else if (currDoor == 2)
            {
                if (playerLoc.Z < -769) currRegion = 3;
                else currRegion = 2;
            }
            else if (currDoor == 3)
            {
                if (playerLoc.X < 587) currRegion = 3;
                else currRegion = 4;
            }
            else if (currDoor == 4)
            {
                if (playerLoc.Z < -763) currRegion = 4;
                else currRegion = 5;
            }
            else if (currDoor == 5)
            {
                if (playerLoc.Z < -1218) currRegion = 6;
                else currRegion = 4;
            }

            percentOpen = 0;
            for (int i = 0; i < game.PhibesModels.Count; i++)
            {
                float currPercent = game.PhibesModels[i].DoorPercentOpen(currDoor);
                if (currPercent > 0)
                {
                    percentOpen = currPercent;
                    break;
                }
            }

            if (currDoor == 1) regionOppositeDoor = 3 - currRegion;
            if (currDoor == 2) regionOppositeDoor = 5 - currRegion;
            if (currDoor == 3) regionOppositeDoor = 7 - currRegion;
            if (currDoor == 4) regionOppositeDoor = 9 - currRegion;
            if (currDoor == 5) regionOppositeDoor = 10 - currRegion;

            if (this.asset == "Victoria")
            {
                game.Player.Bazooka.CurrRegion = currRegion;
                game.Player.Bazooka.CurrDoor = currDoor;
                game.Player.Bazooka.OppositeRegion = regionOppositeDoor;
                game.Player.Bazooka.PercentOpen = percentOpen;

                game.CurrentPlayerDoor = currDoor;
                game.CurrentDoorPercent = percentOpen;
            }
        }


        private AnimationPlayer player = null;

        /// <summary>
        /// Play an animation clip on this model.
        /// </summary>
        /// <param name="name"></param>
        public AnimationPlayer PlayClip(string name)
        {
            if (name != "Take 001")
            {
                player = new AnimationPlayer(this, assetClips[name].TheClip);
                Update(0); 
                return player;
            }

            player = null;

            AnimationClips clips = model.Tag as AnimationClips;

            if (clips != null)
            {
                player = new AnimationPlayer(this, clips.Clips[name]);
                Update(0);
            }

            return player;
        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics">Device to draw the model on.</param>
        /// <param name="gameTime">Current game time.</param>
        /// <param name="transform">Transform that puts the model where we want it.</param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime, Matrix transform)
        {
            DrawModel(graphics, model, transform);
        }

        private void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {
            if (skelToBone != null)
            {
                for (int b = 0; b < skelToBone.Count; b++)
                {
                    int n = skelToBone[b];
                    skinTransforms[b] = inverseBindTransforms[n] * absoTransforms[n];
                }
            }

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue(absoTransforms[mesh.ParentBone.Index] * world);
                    effect.Parameters["View"].SetValue(game.Camera.View);
                    effect.Parameters["Projection"].SetValue(game.Camera.Projection);

                    for (int lightNum = 1; lightNum <= 3; lightNum++)
                    {
                        effect.Parameters["Light" + lightNum + "Location"].SetValue(LightInfo(currRegion, lightNum*2 - 2));
                        effect.Parameters["Light" + lightNum + "Color"].SetValue(LightInfo(currRegion, lightNum * 2 - 1));
                    }
                    effect.Parameters["Light4Location"].SetValue(LightInfo(regionOppositeDoor, 0));
                    effect.Parameters["Light4Color"].SetValue(percentOpen * LightInfo(regionOppositeDoor, 1));
                    
                    effect.Parameters["Bones"].SetValue(skinTransforms);
                }
                mesh.Draw();
            }
        }

        /// <summary>
        /// Add an asset clip to the dictionary.
        /// </summary>
        /// <param name="name">Name we will use for the clip</param>
        /// <param name="asset">The FBX asset to load</param>
        public void AddAssetClip(string name, string asset)
        {
            assetClips[name] = new AssetClip(name, asset);
        }

        public Vector3 LightInfo(int section, int item)
        {
            int offset = (section - 1) * 19 + 1 + (item * 3);
            return new Vector3((float)lightData[offset],
                               (float)lightData[offset + 1],
                               (float)lightData[offset + 2]);
        }

        private double[] lightData =
{   1,      568,      246,    1036,   0.53,   0.53,   0.53,     821,     224, 
  941,  14.2941,       45, 43.9412,    814,    224,   1275,    82.5,       0,  0,
    2,       -5,      169,     428, 0.3964,  0.503, 0.4044,    -5.4,     169,
 1020, 129.4902, 107.5686, 41.8039,   -5.4,    169,   -138, 37.8275,      91, 91,
    3,      113,      217,    -933,    0.5,      0,      0,    -129,     185,
-1085,	     50,        0,       0,    501,    185,  -1087,      48,       0,  0,
    4,      781,      209,    -998,    0.2, 0.1678, 0.1341,    1183,     209,
 -998,	     50,  41.9608, 33.5294,    984,    113,   -932,       0,      80,  0,
    5,      782,      177,    -463,   0.65, 0.5455, 0.4359,     563,     195,
 -197,	     50,        0,       0,   1018,    181,   -188,      80,       0,  0,
    6,     1182,      177,   -1577,   0.65, 0.5455, 0.4359,     971,     181,
-1801,        0,  13.1765,      80,   1406,    181,  -1801,       0, 13.1765,  80};

    }
}
