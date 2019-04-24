// PlayState.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using BloomPostprocess;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Tutorial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using System.Text;

namespace DwarfCorp
{
    public partial class WorldManager
    {
        public enum LoadingStatus
        {
            Loading,
            Success,
            Failure
        }

        public LoadingStatus LoadStatus = LoadingStatus.Loading;

        public Exception LoadingException = null;

        public void Setup()
        {
            Screenshots = new List<Screenshot>();
            Game.Graphics.PreferMultiSampling = GameSettings.Default.AntiAliasing > 1;
          
            try
            {
                Game.Graphics.ApplyChanges();
            }
            catch (NoSuitableGraphicsDeviceException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
            // This can occur when the user is plugging in a secondary monitor just as we enter this state.
            // ugh.
            catch (ArgumentException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }

            LoadingThread = new Thread(LoadThreaded) { IsBackground = true };
            LoadingThread.Name = "Load";
            LoadingThread.Start();
        }

        private void LoadThreaded()
        {
            DwarfGame.ExitGame = false;
            // Ensure we're using the invariant culture.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            LoadStatus = LoadingStatus.Loading;
            SetLoadingMessage("Initializing ...");

            while (GraphicsDevice == null)
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(1000);

#if !DEBUG
            try
            {
#endif
            bool fileExists = !string.IsNullOrEmpty(ExistingFile);

            SetLoadingMessage("Creating Sky...");

            Sky = new SkyRenderer();

            #region Reading game file

            if (fileExists)
            {
                SetLoadingMessage("Loading " + ExistingFile);

                gameFile = SaveGame.CreateFromDirectory(ExistingFile);
                if (gameFile == null) throw new InvalidOperationException("Game File does not exist.");

                if (gameFile.Metadata.Version != Program.Version && !Program.CompatibleVersions.Contains(gameFile.Metadata.Version))
                {
                    throw new InvalidOperationException(String.Format("Game file is from version {0}. Compatible versions are {1}.", gameFile.Metadata.Version,
                        TextGenerator.GetListString(Program.CompatibleVersions)));
                }

                Sky.TimeOfDay = gameFile.Metadata.TimeOfDay;
                Time = gameFile.Metadata.Time;
                WorldOrigin = gameFile.Metadata.WorldOrigin;
                WorldScale = gameFile.Metadata.WorldScale;
                WorldSizeInChunks = gameFile.Metadata.NumChunks;
                GameID = gameFile.Metadata.GameID;

                if (gameFile.Metadata.OverworldFile != null && gameFile.Metadata.OverworldFile != "flat")
                {
                    SetLoadingMessage("Loading world " + gameFile.Metadata.OverworldFile);
                    Overworld.Name = gameFile.Metadata.OverworldFile;
                    DirectoryInfo worldDirectory =
                        Directory.CreateDirectory(DwarfGame.GetWorldDirectory() +
                                                  Path.DirectorySeparatorChar + Overworld.Name);
                    var overWorldFile = new NewOverworldFile(worldDirectory.FullName);
                    Overworld.Map = overWorldFile.Data.Data;
                    Overworld.Name = overWorldFile.Data.Name;
                }
                else
                {
                    SetLoadingMessage("Generating flat world..");
                    DebugOverworlds.CreateUniformLand();
                }
            }

            #endregion

            #region Initialize static data

            bool actionComplete = false;

            Game.DoLazyAction(new Action(() =>
           {
               Vector3 origin = new Vector3(0, 0, 0);
               Vector3 extents = new Vector3(1500, 1500, 1500);
               //OctTree = new OctTreeNode<GameComponent>(origin - extents, origin + extents);

               PrimitiveLibrary.Initialize(GraphicsDevice, Content);

               InstanceRenderer = new InstanceRenderer(GraphicsDevice, Content);

               Color[] white = new Color[1];
               white[0] = Color.White;
               pixel = new Texture2D(GraphicsDevice, 1, 1);
               pixel.SetData(white);

               Tilesheet = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_tiles);
               AspectRatio = GraphicsDevice.Viewport.AspectRatio;
               DefaultShader = new Shader(Content.Load<Effect>(ContentPaths.Shaders.TexturedShaders), true);
               DefaultShader.ScreenWidth = GraphicsDevice.Viewport.Width;
               DefaultShader.ScreenHeight = GraphicsDevice.Viewport.Height;
               CraftLibrary.InitializeDefaultLibrary();
               PotionLibrary.Initialize();
               VoxelLibrary.InitializeDefaultLibrary(GraphicsDevice);
               GrassLibrary.InitializeDefaultLibrary();
               DecalLibrary.InitializeDefaultLibrary();

               bloom = new BloomComponent(Game)
               {
                   Settings = BloomSettings.PresetSettings[5]
               };
               bloom.Initialize();

               SoundManager.Content = Content;
               if (PlanService != null)
                   PlanService.Restart();

               JobLibrary.Initialize();
               MonsterSpawner = new MonsterSpawner(this);
               EntityFactory.Initialize(this);
           }), () => { actionComplete = true; return true; });

            while (!actionComplete)
            {
                Thread.Sleep(10);
            }

            #endregion


            SetLoadingMessage("Creating Planner ...");
            PlanService = new PlanService();

            SetLoadingMessage("Creating Shadows...");
            Shadows = new ShadowRenderer(GraphicsDevice, 1024, 1024);

            SetLoadingMessage("Creating Liquids ...");

            #region liquids

            WaterRenderer = new WaterRenderer(GraphicsDevice);

            #endregion

            SetLoadingMessage("Generating Initial Terrain Chunks ...");

            if (!fileExists)
                GameID = MathFunctions.Random.Next(0, 1024);

            #region Load Components

            // Create updateable systems.
            foreach (var updateSystemFactory in AssetManager.EnumerateModHooks(typeof(UpdateSystemFactoryAttribute), typeof(EngineModule), new Type[] { typeof(WorldManager) }))
                UpdateSystems.Add(updateSystemFactory.Invoke(null, new Object[] { this }) as EngineModule);

            if (fileExists)
            {
                ChunkManager = new ChunkManager(Content, this, WorldSizeInChunks);
                Splasher = new Splasher(ChunkManager);

                ChunkRenderer = new ChunkRenderer(ChunkManager.ChunkData);

                SetLoadingMessage("Loading Terrain...");
                gameFile.ReadChunks(ExistingFile);
                ChunkManager.ChunkData.LoadFromFile(ChunkManager, gameFile, SetLoadingMessage);

                SetLoadingMessage("Loading Entities...");
                gameFile.LoadPlayData(ExistingFile, this);
                Camera = gameFile.PlayData.Camera;
                DesignationDrawer = gameFile.PlayData.Designations;

                Vector3 origin = new Vector3(WorldOrigin.X, 0, WorldOrigin.Y);
                Vector3 extents = new Vector3(1500, 1500, 1500);
                if (gameFile.PlayData.Stats != null)
                {
                    Stats = gameFile.PlayData.Stats;
                }
                if (gameFile.PlayData.Resources != null)
                {
                    foreach (var resource in gameFile.PlayData.Resources)
                    {
                        if (!ResourceLibrary.Resources.ContainsKey(resource.Key))
                        {
                            ResourceLibrary.Add(resource.Value);
                        }
                    }
                }
                ComponentManager = new ComponentManager(gameFile.PlayData.Components, this);

                foreach (var component in gameFile.PlayData.Components.SaveableComponents)
                {
                    if (!ComponentManager.HasComponent(component.GlobalID) &&
                        ComponentManager.HasComponent(component.Parent.GlobalID))
                    {
                        // Logically impossible.
                        throw new InvalidOperationException("Component exists in save data but not in manager.");
                    }
                }

                ConversationMemory = gameFile.PlayData.ConversationMemory;

                Factions = gameFile.PlayData.Factions;
                ComponentManager.World = this;

                Sky.TimeOfDay = gameFile.Metadata.TimeOfDay;
                Time = gameFile.Metadata.Time;
                WorldOrigin = gameFile.Metadata.WorldOrigin;
                WorldScale = gameFile.Metadata.WorldScale;

                // Restore native factions from deserialized data.
                Natives = new List<Faction>();

                foreach (Faction faction in Factions.Factions.Values)
                {
                    if (faction.Race.IsNative && faction.Race.IsIntelligent && !faction.IsRaceFaction)
                    {
                        Natives.Add(faction);
                    }
                }

                Diplomacy = gameFile.PlayData.Diplomacy;

                GoalManager = new Goals.GoalManager();
                GoalManager.Initialize(new List<Goals.Goal>());// gameFile.PlayData.Goals);

                TutorialManager = new Tutorial.TutorialManager();
                TutorialManager.SetFromSaveData(gameFile.PlayData.TutorialSaveData);

            }
            else
            {
                Time = new WorldTime();

                Camera = new OrbitCamera(this, // Todo: Is setting the camera position and target redundant here?
                    new Vector3(VoxelConstants.ChunkSizeX,
                        WorldSizeInVoxels.Y - 1.0f,
                        VoxelConstants.ChunkSizeZ),
                    new Vector3(VoxelConstants.ChunkSizeX, WorldSizeInVoxels.Y - 1.0f,
                        VoxelConstants.ChunkSizeZ) +
                    Vector3.Up * 10.0f + Vector3.Backward * 10,
                    MathHelper.PiOver4, AspectRatio, 0.1f,
                    GameSettings.Default.VertexCullDistance);

                ChunkManager = new ChunkManager(Content, this, WorldSizeInChunks);
                Splasher = new Splasher(ChunkManager);


                ChunkRenderer = new ChunkRenderer(ChunkManager.ChunkData);

                Camera.Position = new Vector3(0, 10, 0) + new Vector3(WorldSizeInChunks.X * VoxelConstants.ChunkSizeX, 0, WorldSizeInChunks.Z * VoxelConstants.ChunkSizeZ) * 0.5f;
                Camera.Target = new Vector3(0, 10, 1) + new Vector3(WorldSizeInChunks.X * VoxelConstants.ChunkSizeX, 0, WorldSizeInChunks.Z * VoxelConstants.ChunkSizeZ) * 0.5f;
             

                ComponentManager = new ComponentManager(this);
                ComponentManager.SetRootComponent(new GameComponent(ComponentManager, "root", Matrix.Identity, Vector3.Zero, Vector3.Zero));

                if (Natives == null) // Todo: Always true??
                {
                    FactionLibrary library = new FactionLibrary();
                    library.Initialize(this, CompanyMakerState.CompanyInformation);
                    Natives = new List<Faction>();
                    for (int i = 0; i < 10; i++)
                    {
                        Natives.Add(library.GenerateFaction(this, i, 10));
                    }

                }

                #region Prepare Factions

                foreach (Faction faction in Natives)
                {
                    faction.World = this;

                    if (faction.RoomBuilder == null)
                        faction.RoomBuilder = new RoomBuilder(faction, this);
                }

                Factions = new FactionLibrary();
                if (Natives != null && Natives.Count > 0)
                {
                    Factions.AddFactions(this, Natives);
                }

                Factions.Initialize(this, CompanyMakerState.CompanyInformation);
                Point playerOrigin = new Point((int)(WorldOrigin.X), (int)(WorldOrigin.Y));

                Factions.Factions["Player"].Center = playerOrigin;
                Factions.Factions["The Motherland"].Center = new Point(playerOrigin.X + 50, playerOrigin.Y + 50);

                #endregion

                Diplomacy = new Diplomacy(this);
                Diplomacy.Initialize(Time.CurrentDate);

                // Initialize goal manager here.
                GoalManager = new Goals.GoalManager();
                GoalManager.Initialize(new List<Goals.Goal>());

                TutorialManager = new Tutorial.TutorialManager();
                TutorialManager.TutorialEnabled = !GameSettings.Default.TutorialDisabledGlobally;
                Tutorial("new game start");

                foreach(var item in CraftLibrary.EnumerateCraftables())
                {
                    if (!String.IsNullOrEmpty(item.Tutorial))
                    {
                        TutorialManager.AddTutorial(item.Name, item.Tutorial, item.Icon);
                    }
                }
            }

            Camera.World = this;
            //Drawer3D.Camera = Camera;


            #endregion

            SetLoadingMessage("Creating Particles ...");
            Game.DoLazyAction(new Action(() => ParticleManager = new ParticleManager(ComponentManager)));

            SetLoadingMessage("Creating GameMaster ...");
            Master = new GameMaster(Factions.Factions["Player"], Game, ComponentManager, ChunkManager,
                Camera, GraphicsDevice);

            if (gameFile == null)
            {
                Game.LogSentryBreadcrumb("Loading", "Started new game without an existing file.");
                if (Overworld.Map == null)
                    throw new InvalidProgramException("Tried to start game with an empty overworld. This should not happen.");

                var generatorSettings = new Generation.GeneratorSettings(Seed, 0.02f, GenerationSettings)
                {
                    SeaLevel = SeaLevel,
                    WorldSizeInChunks = WorldSizeInChunks,
                    SetLoadingMessage = SetLoadingMessage,
                    World = this
                };

                SetLoadingMessage("Generating Chunks...");
                Generation.Generator.Generate(SpawnRect, ChunkManager.ChunkData, this, generatorSettings, SetLoadingMessage);
                CreateInitialEmbarkment(generatorSettings);
                ChunkManager.NeedsMinimapUpdate = true;
                ChunkManager.RecalculateBounds();
            }

            if (gameFile != null)
            {
                    Game.LogSentryBreadcrumb("Loading", "Started new game with an existing file.");
                if (gameFile.PlayData.Tasks != null)
                {
                    Master.NewArrivals = gameFile.PlayData.NewArrivals ?? new List<GameMaster.ApplicantArrival>();
                    Master.TaskManager = gameFile.PlayData.Tasks;
                    Master.TaskManager.Faction = Master.Faction;
                }
                if (gameFile.PlayData.InitialEmbark != null)
                {
                    InitialEmbark = gameFile.PlayData.InitialEmbark;
                }
                ChunkManager.World.Master.SetMaxViewingLevel(gameFile.Metadata.Slice > 0 ? gameFile.Metadata.Slice : ChunkManager.World.Master.MaxViewingLevel);
            }

            if (Master.Faction.Economy.Company.Information == null)
                Master.Faction.Economy.Company.Information = new CompanyInformation();

            

            SetLoadingMessage("Creating Geometry...");
            //ChunkManager.GenerateAllGeometry();

            if (MathFunctions.RandEvent(0.01f))
                SetLoadingMessage("Reticulating Splines...");

            ChunkManager.StartThreads();
            SetLoadingMessage("Presimulating ...");
            ShowingWorld = false;
            OnLoadedEvent();
            Thread.Sleep(1000);

            ShowingWorld = true;

            SetLoadingMessage("Complete.");

            // GameFile is no longer needed.
            gameFile = null;
            LoadStatus = LoadingStatus.Success;
#if !DEBUG
        }
            catch (Exception exception)
            {
                Game.CaptureException(exception);
                LoadingException = exception;
                LoadStatus = LoadingStatus.Failure;
                ProgramData.WriteExceptionLog(exception);
            }
#endif
        }

    }
}