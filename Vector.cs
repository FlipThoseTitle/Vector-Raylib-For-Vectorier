using Raylib_cs;
using System.Globalization;
using System.Numerics;
using Vector.Core.Animation;
using Vector.Core.Location;
using Vector.Core.Models;
using Vector.Core.Runners;

namespace Vector
{
    /// <summary>
    /// A small debug panel button: "[Key] Name". Dark and translucent so it stays out of the way; a lit marker shows
    /// whether a toggle is on, and a disabled button is dimmed.
    /// </summary>
    class Button
    {
        public const float Height = 24;
        const float FontSize = 15;
        const float Pad = 8;

        public Raylib_cs.Rectangle Rectangle;

        readonly string Key;
        readonly string Text;

        /// <summary>Optional live text (e.g. "Anti-alias: High"); replaces Text when set.</summary>
        public Func<string>? DynamicText;

        public Action OnClick;

        /// <summary>Toggle state (null: a plain action button).</summary>
        public Func<bool>? IsOn;

        /// <summary>When it returns false the button is dimmed and does nothing.</summary>
        public Func<bool>? IsEnabled;

        bool IsHovered;
        bool IsPressed;

        static readonly Color Background = new(20, 20, 24, 150);
        static readonly Color BackgroundHover = new(45, 45, 55, 190);
        static readonly Color BackgroundPressed = new(70, 70, 85, 210);
        static readonly Color KeyColor = new(255, 200, 80, 255);
        static readonly Color OnColor = new(90, 200, 120, 255);
        static readonly Color OffColor = new(110, 110, 120, 255);

        public Button(float x, float y, float width, string key, string text, Action onClick)
        {
            Rectangle = new Raylib_cs.Rectangle(x, y, width, Height);
            Key = key;
            Text = text;
            OnClick = onClick;
        }

        bool Enabled => IsEnabled?.Invoke() ?? true;

        /// <summary>True while the pointer is over it (clicks there are not meant for the game).</summary>
        public bool IsHot => IsHovered;

        public void Update()
        {
            if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), Rectangle))
            {
                IsHovered = true;
                if (Raylib.IsMouseButtonDown(MouseButton.Left))
                {
                    IsPressed = true;
                }
                else
                {
                    if (IsPressed && Enabled)
                        OnClick?.Invoke();
                    IsPressed = false;
                }
            }
            else
            {
                IsHovered = false;
                IsPressed = false;
            }
        }

        public void Draw()
        {
            bool enabled = Enabled;
            var bg = !enabled ? Background : IsPressed ? BackgroundPressed : IsHovered ? BackgroundHover : Background;
            Raylib.DrawRectangleRounded(Rectangle, 0.25f, 4, bg);

            float x = Rectangle.X + Pad;
            float textY = Rectangle.Y + (Height - FontSize) / 2;
            byte alpha = (byte)(enabled ? 255 : 110);

            // Toggle marker: a small lit / unlit dot.
            if (IsOn != null)
            {
                bool on = IsOn();
                var dot = on ? OnColor : OffColor;
                Raylib.DrawCircleV(new Vector2(x + 3, Rectangle.Y + Height / 2), 3.5f, new Color(dot.R, dot.G, dot.B, alpha));
            }
            x += 13; // action buttons keep the same text column as the toggles

            string key = Key;
            GameFont.Draw(key, x, textY, FontSize, new Color(KeyColor.R, KeyColor.G, KeyColor.B, alpha));
            x += Raylib.MeasureTextEx(GameFont.Font, key, FontSize, FontSize / 10f).X + 7;
            GameFont.Draw(DynamicText?.Invoke() ?? Text, x, textY, FontSize, new Color((byte)235, (byte)235, (byte)235, alpha));
        }
    }

    public class Vector
    {
        int ScreenWidth;
        int ScreenHeight;

        public Camera2D Camera;

        public static readonly float OrtographicSize = 214;

        public static Vector Instance;

        public bool ImagesVisible = true;

        public Rectangle CameraRectangle;

        bool pause = false;
        bool stepOneFrame = false;

        public static bool RenderPlatforms;
        public static bool RenderTriggers;
        public static bool RenderAreas;

        /// <summary>N cycles: no nodes, every node, only COM / DetectorH / DetectorV / Camera.</summary>
        public enum NodeDisplay { None, All, Important }

        public static NodeDisplay NodeMode;

        static readonly string[] NodeModeNames = { "Off", "All", "Important" };

        /// <summary>G: a cross line through the screen center.</summary>
        public static bool ShowGrid;

        /// <summary>I: the conditions of the moves of the current interval (see MoveConditionsOverlay).</summary>
        public static bool ShowMoveConditions;
        public static bool RenderEdges;
        public static bool RenderDetectors;
        public static bool RenderBoxes;

        public static string DebugSettingsFile = Path.Combine(VectorPaths.AppFolder, "debugsettings.txt");

        public static string Settings;

        /// <summary>H: hides every debug overlay on the screen (buttons, animation name, FPS).</summary>
        bool hideDebugUi = false;

        readonly List<Button> Buttons = new();
        readonly Dictionary<KeyboardKey, Button> ButtonKeys = new();

        public Vector(int width, int height, string name)
        {
            ScreenWidth = width;
            ScreenHeight = height;

            // raylib logs several INFO lines per texture / sound loaded: printing them to the console slows loading
            // down (on Windows especially). Warnings and errors are still shown.
            Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
            Raylib.InitWindow(width, height, name);
            Raylib.InitAudioDevice();

            Raylib.SetExitKey(KeyboardKey.Null);

            Raylib.SetTargetFPS(60);
            Camera = new Camera2D(Raylib.GetScreenCenter(), Vector2.Zero, 0, Raylib.GetScreenHeight() / (OrtographicSize * 2));

            CameraRectangle = GetCameraWorldRect(Camera);

            LoadSettings();
            SetButtons();

            ResourceManager.LoadResources();
        }

        public static Rectangle GetCameraWorldRect(Camera2D camera)
        {
            Vector2 topLeft = Raylib.GetScreenToWorld2D(
                new Vector2(0, 0),
                camera
            );

            Vector2 bottomRight = Raylib.GetScreenToWorld2D(
                new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight()),
                camera
            );

            float x = MathF.Min(topLeft.X, bottomRight.X);
            float y = MathF.Min(topLeft.Y, bottomRight.Y);
            float width = MathF.Abs(bottomRight.X - topLeft.X);
            float height = MathF.Abs(bottomRight.Y - topLeft.Y);

            return new Rectangle(x, y, width, height);
        }

        [STAThread]
        public void Run()
        {
            AnimationLoader.Current.Init();

            // Every move's .bin is independent: parse them on all cores.
            var animations = Animations.ToList();
            Parallel.ForEach(animations, animation => animation.LoadBin());

            string level = "DOWNTOWN_STORY_02";

            bool hunterMode = false;

            string[] args = Environment.GetCommandLineArgs();
            int levelArg = Array.IndexOf(args, "-level");
            if (levelArg >= 0 && levelArg < args.Length - 1)
            {
                level = args[levelArg + 1];
            }

            int hunterModeArg = Array.IndexOf(args, "-huntermode");
            if (hunterModeArg >= 0)
            {
                hunterMode = true;
            }

            LevelMainController.Init(level, hunterMode);

            GlobalObjects.Init();

            GlobalObjects.InitAfterAdding = true;

            while (!Raylib.WindowShouldClose())
            {
                Raylib.UpdateMusicStream(SoundsManager.CurrentMusic);

                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.Escape))
                {
                    pause = !pause;
                }

                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.F))
                {
                    stepOneFrame = true;
                }

                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.R))
                {
                    LevelMainController.current.ReloadButton();
                }

                // Debug toggles and actions: same as clicking their button.
                foreach (var (key, button) in ButtonKeys.ToList()) // a button may rebuild the panel (Full Screen)
                {
                    if (key is KeyboardKey.F or KeyboardKey.S or KeyboardKey.Escape or KeyboardKey.R)
                        continue; // handled above / below as before
                    if (InputManager.WasKeyPressedThisFrame(key))
                        button.OnClick?.Invoke();
                }

                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.Up))
                {
                    LevelMainController.current.HandleNewInput(new Core.Controllers.KeyVariables(KeyboardKey.Up));
                }
                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.Down))
                {
                    LevelMainController.current.HandleNewInput(new Core.Controllers.KeyVariables(KeyboardKey.Down));
                }
                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.Left))
                {
                    LevelMainController.current.HandleNewInput(new Core.Controllers.KeyVariables(KeyboardKey.Left));
                }
                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.Right))
                {
                    LevelMainController.current.HandleNewInput(new Core.Controllers.KeyVariables(KeyboardKey.Right));
                }

                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.S))
                {
                    ToggleSlowMode();
                }

                if (!hideDebugUi)
                    UpdateButtons();

                // The game logic advances in fixed 1/60 s ticks. When a frame takes longer than that (a slow PC, a
                // heavy scene), the missed ticks are caught up so the game keeps its real speed instead of slowing
                // down with the frame rate. Paused: nothing runs, except one tick for Next Frame (F).
                int ticks;
                if (pause)
                {
                    tickAccumulator = 0;
                    ticks = stepOneFrame ? 1 : 0;
                }
                else
                {
                    ticks = TicksThisFrame();
                }

                for (int tick = 0; tick < ticks; tick++)
                {
                    LevelMainController.current.Render();

                    for (int i = 0; i < GlobalObjects.Updatables.Count; i++)
                    {
                        GlobalObjects.Updatables[i].Update();
                    }

                    // The delay before the automatic restart (Win / Loss / Death) only runs with the game: Esc
                    // freezes it, unpausing resumes it.
                    LevelMainController.current.TickReloadDelay(TickSeconds);
                }
                stepOneFrame = false;

                // Anti-aliasing: the level is first drawn into a bigger texture (outside BeginDrawing).
                RenderWorldSupersampled();

                Raylib.BeginDrawing();
                {
                    Raylib.ClearBackground(Color.Gray);

                    DrawWorldAntiAliased();

                    ModelHuman player = LevelMainController.current.Location.GetUserModel();
                    string animation = "null";
                    if (player != null && player.ControllerAnimations != null && player.ControllerAnimations.Animation != null)
                    {
                        animation = player.ControllerAnimations.CurrentFrame + " - " + player.ControllerAnimations.Animation.Name;
                    }

                    if (ShowGrid)
                        DrawGrid();

                    // Names and outlines of the debug shapes shown (screen space, like the editor's labels).
                    DrawDebugLabels();

                    if (ShowMoveConditions)
                        MoveConditionsOverlay.Draw(Camera);

                    if (!hideDebugUi)
                    {
                        GameFont.Draw(animation, 0, Raylib.GetScreenHeight() - 30, 30, Color.White);

                        DrawButtons();

                        // Same place and size as raylib's FPS counter, in Arial and always white.
                        GameFont.Draw($"{Raylib.GetFPS()} FPS", 0, 0, 20, Color.White);
                    }
                }
                Raylib.EndDrawing();
            }
            SaveSettings();
            Raylib.CloseWindow();
        }

        void SetButtons()
        {
            Buttons.Clear();
            ButtonKeys.Clear();

            const float width = 170, gap = 4, margin = 10, sectionGap = 12;
            float x = ScreenWidth - width - margin;
            float y = margin;

            void Add(KeyboardKey key, string label, string text, Action onClick, Func<bool>? isOn = null, Func<bool>? isEnabled = null)
            {
                var button = new Button(x, y, width, label, text, onClick) { IsOn = isOn, IsEnabled = isEnabled };
                Buttons.Add(button);
                ButtonKeys[key] = button;
                y += Button.Height + gap;
            }

            // What is drawn over the level.
            Add(KeyboardKey.T, "T", "Triggers", () => RenderTriggers = !RenderTriggers, () => RenderTriggers);
            Add(KeyboardKey.A, "A", "Areas", () => RenderAreas = !RenderAreas, () => RenderAreas);
            Add(KeyboardKey.P, "P", "Platforms", () => RenderPlatforms = !RenderPlatforms, () => RenderPlatforms);
            Add(KeyboardKey.N, "N", "Nodes", () => NodeMode = (NodeDisplay)(((int)NodeMode + 1) % NodeModeNames.Length), () => NodeMode != NodeDisplay.None);
            Buttons[^1].DynamicText = () => "Nodes: " + NodeModeNames[(int)NodeMode];
            Add(KeyboardKey.E, "E", "Edges", () => RenderEdges = !RenderEdges, () => RenderEdges);
            Add(KeyboardKey.D, "D", "Detectors", () => RenderDetectors = !RenderDetectors, () => RenderDetectors);
            Add(KeyboardKey.B, "B", "Bounding", () => RenderBoxes = !RenderBoxes, () => RenderBoxes);
            Add(KeyboardKey.I, "I", "Move Conditions", () => ShowMoveConditions = !ShowMoveConditions, () => ShowMoveConditions);
            Add(KeyboardKey.G, "G", "Center Line", () => ShowGrid = !ShowGrid, () => ShowGrid);

            // Playback.
            y += sectionGap - gap;
            Add(KeyboardKey.Escape, "Esc", "Pause", () => pause = !pause, () => pause);
            Add(KeyboardKey.F, "F", "Next Frame", () => stepOneFrame = true, isEnabled: () => pause);
            Add(KeyboardKey.S, "S", "Slow Mode", ToggleSlowMode, () => LevelMainController.current?.slowModeFrames == 10);
            Add(KeyboardKey.R, "R", "Restart", () => LevelMainController.current.ReloadButton());
            Add(KeyboardKey.H, "H", "Hide UI", () => hideDebugUi = !hideDebugUi);

            // Display.
            y += sectionGap - gap;
            Add(KeyboardKey.Q, "Q", "Anti-alias", ToggleAntiAlias);
            Buttons[^1].DynamicText = () => "Anti-alias: " + AntiAliasNames[AntiAlias];
            Add(KeyboardKey.F11, "F11", "Full Screen", ToggleFullScreen, () => Raylib.IsWindowState(ConfigFlags.BorderlessWindowMode));
            Add(KeyboardKey.M, "M", "Mute Music", SoundsManager.ToggleMusicMute, () => SoundsManager.MusicMuted);
        }

        void ToggleSlowMode()
        {
            LevelMainController.current.slowModeFrames = LevelMainController.current.slowModeFrames == 10 ? 1 : 10;
        }

        // ================= FIXED TIME STEP ================= //

        /// <summary>Length of one game logic tick: the game was made for 60 updates per second.</summary>
        public const float TickSeconds = 1f / 60f;

        /// <summary>Most ticks run in one frame: below 15 FPS the game slows down rather than freezing to catch up.</summary>
        const int MaxTicksPerFrame = 4;

        float tickAccumulator;

        /// <summary>
        /// How many logic ticks this frame needs to keep the game at real speed. At a steady 60 FPS it is always 1
        /// (frame times are rounded to the nearest tick, so the small jitter of a 60 Hz frame does not alternate 0
        /// and 2); at 30 FPS it is 2. A single very long frame (loading, a dragged window) counts as one tick.
        /// </summary>
        int TicksThisFrame()
        {
            float frame = Raylib.GetFrameTime();
            if (frame <= 0 || frame > TickSeconds * MaxTicksPerFrame * 2)
            {
                tickAccumulator = 0;
                return 1;
            }
            tickAccumulator += frame;
            int ticks = (int)MathF.Round(tickAccumulator / TickSeconds);
            if (ticks > MaxTicksPerFrame)
            {
                tickAccumulator = 0;
                return MaxTicksPerFrame;
            }
            tickAccumulator -= ticks * TickSeconds;
            return ticks;
        }

        // ================= CENTER LINE ================= //

        static readonly Color GridCenter = new(255, 255, 255, 140);

        /// <summary>G: a white translucent cross line through the exact screen center.</summary>
        void DrawGrid()
        {
            float cx = ScreenWidth / 2f, cy = ScreenHeight / 2f;
            Raylib.DrawLineEx(new Vector2(cx, 0), new Vector2(cx, ScreenHeight), 1.5f, GridCenter);
            Raylib.DrawLineEx(new Vector2(0, cy), new Vector2(ScreenWidth, cy), 1.5f, GridCenter);
        }

        // ================= ANTI-ALIASING ================= //

        /// <summary>0 = Off, 1 = 2× supersampling, 2 = 4× supersampling (Q).</summary>
        public static int AntiAlias;

        static readonly string[] AntiAliasNames = { "Off", "Medium", "High" };

        RenderTexture2D sceneTarget;
        bool sceneTargetReady;
        int sceneScale;

        /// <summary>4×: the scene is halved twice; this holds the 2× step.</summary>
        RenderTexture2D halfTarget;
        bool halfTargetReady;

        /// <summary>Supersampling factor for the current setting and screen (capped to what a GPU texture allows).</summary>
        int SupersampleFactor()
        {
            int factor = AntiAlias switch { 1 => 2, 2 => 4, _ => 1 };
            int largest = Math.Max(ScreenWidth, ScreenHeight);
            while (factor > 1 && largest * factor > 8192)
                factor--;
            return factor;
        }

        void ToggleAntiAlias()
        {
            AntiAlias = (AntiAlias + 1) % AntiAliasNames.Length;
        }

        /// <summary>
        /// Draws a render texture as an exact copy (blending off: source color and alpha replace the destination).
        /// The scene texture's color is already the final, blended color, but its alpha is not 1 wherever something
        /// translucent was drawn (with normal blending the alpha channel is blended too: a·a + 1·(1−a) &lt; 1). Drawn
        /// with normal blending, those pixels let the gray background show through again, and textures, triggers,
        /// areas and every other translucent thing looked paler with anti-aliasing on.
        /// </summary>
        static void DrawTextureCopy(Texture2D texture, int width, int height)
        {
            Rlgl.SetBlendFactors(1 /* GL_ONE */, 0 /* GL_ZERO */, 0x8006 /* GL_FUNC_ADD */);
            Raylib.BeginBlendMode(BlendMode.Custom);
            // Render textures are stored upside down: a negative source height flips them back.
            Raylib.DrawTexturePro(texture,
                new Raylib_cs.Rectangle(0, 0, texture.Width, -texture.Height),
                new Raylib_cs.Rectangle(0, 0, width, height),
                Vector2.Zero, 0, Color.White);
            Raylib.EndBlendMode();
        }

        static bool EnsureTarget(ref RenderTexture2D target, ref bool ready, int width, int height)
        {
            if (ready && target.Texture.Width == width && target.Texture.Height == height)
                return true;
            if (ready)
                Raylib.UnloadRenderTexture(target);
            target = Raylib.LoadRenderTexture(width, height);
            ready = target.Id != 0;
            if (ready)
                Raylib.SetTextureFilter(target.Texture, TextureFilter.Bilinear);
            return ready;
        }

        /// <summary>
        /// Models are drawn with plain triangles, circles and thick lines, which raylib does not smooth. With
        /// anti-aliasing on, the level is drawn at 2× or 4× the screen size and scaled down with filtering: the
        /// jagged edges of the models (and of rotated images) become smooth. The UI is drawn on top at screen size.
        /// Each halving is one bilinear draw (bilinear at exactly half the size averages each 2×2 block), so 4× is
        /// two cheap passes instead of rebuilding a full mipmap chain of the big texture every frame.
        /// </summary>
        void RenderWorldSupersampled()
        {
            int factor = SupersampleFactor();
            if (factor <= 1)
            {
                ReleaseSceneTarget();
                return;
            }
            int width = ScreenWidth * factor, height = ScreenHeight * factor;
            if (!EnsureTarget(ref sceneTarget, ref sceneTargetReady, width, height))
            {
                AntiAlias = 0; // the GPU refused: back to direct drawing
                return;
            }
            sceneScale = factor;

            var camera = Camera;
            camera.Offset *= factor;
            camera.Zoom *= factor;

            Raylib.BeginTextureMode(sceneTarget);
            Raylib.ClearBackground(Color.Gray);
            Raylib.BeginMode2D(camera);
            DrawWorld();
            Raylib.EndMode2D();
            Raylib.EndTextureMode();

            if (factor == 4 && EnsureTarget(ref halfTarget, ref halfTargetReady, ScreenWidth * 2, ScreenHeight * 2))
            {
                Raylib.BeginTextureMode(halfTarget);
                DrawTextureCopy(sceneTarget.Texture, ScreenWidth * 2, ScreenHeight * 2);
                Raylib.EndTextureMode();
                return;
            }
            ReleaseHalfTarget();

            // 3× (a screen too big for 4×): mipmaps average the blocks (trilinear).
            if (factor > 2)
            {
                Raylib.GenTextureMipmaps(ref sceneTarget.Texture);
                Raylib.SetTextureFilter(sceneTarget.Texture, TextureFilter.Trilinear);
            }
        }

        void ReleaseSceneTarget()
        {
            ReleaseHalfTarget();
            if (!sceneTargetReady)
                return;
            Raylib.UnloadRenderTexture(sceneTarget);
            sceneTargetReady = false;
        }

        void ReleaseHalfTarget()
        {
            if (!halfTargetReady)
                return;
            Raylib.UnloadRenderTexture(halfTarget);
            halfTargetReady = false;
        }

        /// <summary>The level: the supersampled texture when anti-aliasing is on, drawn directly otherwise.</summary>
        void DrawWorldAntiAliased()
        {
            if (sceneTargetReady && SupersampleFactor() > 1)
            {
                var texture = sceneScale == 4 && halfTargetReady ? halfTarget.Texture : sceneTarget.Texture;
                DrawTextureCopy(texture, ScreenWidth, ScreenHeight);
                return;
            }
            Raylib.BeginMode2D(Camera);
            DrawWorld();
            Raylib.EndMode2D();
        }

        void DrawWorld()
        {

            for (int i = 0; i < GlobalObjects.Drawables.Count; i++)
            {
                GlobalObjects.Drawables[i].Draw();
            }

            // Debug shapes: only those inside the camera's view are drawn.
            if (RenderPlatforms)
            {
                for (int i = 0; i < Sets.Current.Quads.Count; i++)
                {
                    if (IsOnCamera(Sets.Current.Quads[i]))
                        Sets.Current.Quads[i].Draw();
                }
            }
            if (RenderAreas)
            {
                for (int i = 0; i < Sets.Current.Areas.Count; i++)
                {
                    if (IsOnCamera(Sets.Current.Areas[i]))
                        Sets.Current.Areas[i].Draw();
                }
            }
            if (RenderTriggers)
            {
                for (int i = 0; i < Sets.Current.Triggers.Count; i++)
                {
                    if (IsOnCamera(Sets.Current.Triggers[i]))
                        Sets.Current.Triggers[i].DrawRectangle();
                }
            }

            for (int i = 0; i < Core.Models.Model.ModelsAll.Count; i++)
            {
                Core.Models.Model.ModelsAll[i].Draw();
            }

        }

        /// <summary>True when the runner's rectangle (as drawn) overlaps the camera's view.</summary>
        public static bool IsOnCamera(QuadRunner runner)
        {
            if (Sets.Current == null)
                return true;
            var vs = Sets.Current.Containers[1];
            var origin = vs.TransformPoint(runner.rectangle.Origin);
            var size = runner.rectangle.Size * runner.WorldScale;
            var view = Instance.CameraRectangle;
            float minX = MathF.Min(origin.X, origin.X + size.X), maxX = MathF.Max(origin.X, origin.X + size.X);
            float minY = MathF.Min(origin.Y, origin.Y + size.Y), maxY = MathF.Max(origin.Y, origin.Y + size.Y);
            const float margin = 4;
            return maxX >= view.MinX - margin && minX <= view.MaxX + margin && maxY >= view.MinY - margin && minY <= view.MaxY + margin;
        }

        // ================= FULL SCREEN ================= //

        /// <summary>F11: borderless full screen at the monitor's resolution, and back to the window.</summary>
        void ToggleFullScreen()
        {
            Raylib.ToggleBorderlessWindowed();

            ScreenWidth = Raylib.GetScreenWidth();
            ScreenHeight = Raylib.GetScreenHeight();

            Camera.Offset = Raylib.GetScreenCenter();
            Camera.Zoom = ScreenHeight / (OrtographicSize * 2);

            CameraRectangle = GetCameraWorldRect(Camera);

            SetButtons();
        }

        // ================= DEBUG LABELS ================= //

        // Same colors as the Vectorier editor.
        static readonly Color TriggerOutline = new(0xCE, 0x7D, 0x14, 0xFF);
        static readonly Color AreaOutline = new(0x80, 0x00, 0x00, 0xFF);
        static readonly Color CollisionOutline = new(0x00, 0x00, 0xFF, 0xFF);
        static readonly Color LabelColor = new(0x20, 0x20, 0x20, 0xFF);
        const float LabelSize = 13;
        const float LabelPad = 3;

        /// <summary>
        /// Outline and name of each Trigger, Area, Platform and Trapezoid whose debug view is on, drawn in screen
        /// pixels so they stay sharp and readable at any zoom (the editor's look: 1 px outline, dark label in the
        /// top-left corner, clipped to the shape).
        /// </summary>
        void DrawDebugLabels()
        {
            if (Sets.Current == null)
                return;
            var vs = Sets.Current.Containers[1];

            if (RenderPlatforms)
            {
                for (int i = 0; i < Sets.Current.Quads.Count; i++)
                {
                    var quad = Sets.Current.Quads[i];
                    if (!quad.IsEnabled || quad is AreaRunner || quad is TriggerRunner || !IsOnCamera(quad))
                        continue;
                    if (quad is TrapezoidRunner trapezoid)
                    {
                        var pts = new[] { trapezoid.Point1, trapezoid.Point2, trapezoid.Point3, trapezoid.Point4 }
                            .Select(p => Raylib.GetWorldToScreen2D(vs.TransformPoint(p), Camera)).ToArray();
                        DrawOutline(pts, CollisionOutline);
                        DrawLabel(quad.Name, Bounds(pts));
                        continue;
                    }
                    DrawRunner(quad, vs, CollisionOutline, quad.Name);
                }
            }
            if (RenderAreas)
            {
                for (int i = 0; i < Sets.Current.Areas.Count; i++)
                {
                    var area = Sets.Current.Areas[i];
                    if (!area.IsEnabled || !IsOnCamera(area))
                        continue;
                    string label = area.Name;
                    DrawRunner(area, vs, AreaOutline, label);
                }
            }
            if (RenderTriggers)
            {
                for (int i = 0; i < Sets.Current.Triggers.Count; i++)
                {
                    var trigger = Sets.Current.Triggers[i];
                    if (!trigger.IsEnabled || !IsOnCamera(trigger))
                        continue;
                    DrawRunner(trigger, vs, TriggerOutline, trigger.Name);
                }
            }
        }

        void DrawRunner(QuadRunner runner, Core.Runners.VisualContainer vs, Color outline, string? label)
        {
            var origin = vs.TransformPoint(runner.rectangle.Origin);
            var size = runner.rectangle.Size * runner.WorldScale;
            var a = Raylib.GetWorldToScreen2D(origin, Camera);
            var b = Raylib.GetWorldToScreen2D(origin + size, Camera);
            var r = new Raylib_cs.Rectangle(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Abs(b.X - a.X), MathF.Abs(b.Y - a.Y));
            if (r.Width >= 1 && r.Height >= 1)
                Raylib.DrawRectangleLinesEx(new Raylib_cs.Rectangle(MathF.Round(r.X), MathF.Round(r.Y), MathF.Round(r.Width), MathF.Round(r.Height)), 1, outline);
            DrawLabel(label, r);
        }

        static void DrawOutline(Vector2[] pts, Color color)
        {
            for (int i = 0; i < pts.Length; i++)
                Raylib.DrawLineEx(pts[i], pts[(i + 1) % pts.Length], 1, color);
        }

        static Raylib_cs.Rectangle Bounds(Vector2[] pts)
        {
            float minX = pts.Min(p => p.X), maxX = pts.Max(p => p.X), minY = pts.Min(p => p.Y), maxY = pts.Max(p => p.Y);
            return new Raylib_cs.Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        static float TextWidth(string text) => Raylib.MeasureTextEx(GameFont.Font, text, LabelSize, LabelSize / 10f).X;

        static List<string> WrapText(string text, float maxWidth)
        {
            var lines = new List<string>();
            string current = string.Empty;
            foreach (var word in text.Split(' '))
            {
                string candidate = current.Length == 0 ? word : current + " " + word;
                if (TextWidth(candidate) <= maxWidth)
                {
                    current = candidate;
                    continue;
                }
                if (current.Length > 0)
                    lines.Add(current);
                // A single word wider than the shape: split it by characters.
                current = string.Empty;
                foreach (char c in word)
                {
                    if (current.Length > 0 && TextWidth(current + c) > maxWidth)
                    {
                        lines.Add(current);
                        current = string.Empty;
                    }
                    current += c;
                }
            }
            if (current.Length > 0)
                lines.Add(current);
            return lines;
        }

        static void DrawLabel(string? text, Raylib_cs.Rectangle r)
        {
            if (string.IsNullOrEmpty(text) || r.Width < 14 || r.Height < 10)
                return;
            float x = r.X + LabelPad, y = r.Y + LabelPad;
            float w = r.Width - LabelPad * 2, h = r.Height - LabelPad * 2;
            if (w <= 0 || h <= 0)
                return;
            // Off screen: nothing to draw (the scissor would clamp it to the screen edge).
            if (x > Raylib.GetScreenWidth() || y > Raylib.GetScreenHeight() || x + w < 0 || y + h < 0)
                return;
            Raylib.BeginScissorMode((int)MathF.Floor(x), (int)MathF.Floor(y), (int)MathF.Ceiling(w), (int)MathF.Ceiling(h));
            // Wrapped to the shape's width (like the editor): words move to the next line, a word longer than the
            // width is split; only lines below the shape's bottom are cut.
            float lineY = MathF.Round(y);
            foreach (var line in WrapText(text, w))
            {
                if (lineY > y + h)
                    break;
                GameFont.Draw(line, MathF.Round(x), lineY, LabelSize, LabelColor);
                lineY += LabelSize + 2;
            }
            Raylib.EndScissorMode();
        }

        void SaveSettings()
        {
            Settings = $"RenderPlatforms={RenderPlatforms}\n" +
                       $"RenderTriggers={RenderTriggers}\n" +
                       $"RenderAreas={RenderAreas}\n" +
                       $"NodeMode={(int)NodeMode}\n" +
                       $"ShowGrid={ShowGrid}\n" +
                       $"ShowMoveConditions={ShowMoveConditions}\n" +
                       $"MusicMuted={SoundsManager.MusicMuted}\n" +
                       $"RenderEdges={RenderEdges}\n" +
                       $"RenderDetectors={RenderDetectors}\n" +
                       $"RenderBoxes={RenderBoxes}\n" +
                       $"AntiAlias={AntiAlias}";
            File.WriteAllText(DebugSettingsFile, Settings);
        }

        void LoadSettings()
        {
            if (File.Exists(DebugSettingsFile))
            {
                string[] lines = File.ReadAllLines(DebugSettingsFile);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        bool boolValue = value.ToLower() == "true";
                        switch (key)
                        {
                            case "RenderPlatforms":
                                RenderPlatforms = boolValue;
                                break;
                            case "RenderTriggers":
                                RenderTriggers = boolValue;
                                break;
                            case "RenderAreas":
                                RenderAreas = boolValue;
                                break;
                            case "RenderNodes": // older settings files: on = all nodes
                                NodeMode = boolValue ? NodeDisplay.All : NodeDisplay.None;
                                break;
                            case "NodeMode":
                                if (int.TryParse(value, out int nodeMode))
                                    NodeMode = (NodeDisplay)Math.Clamp(nodeMode, 0, NodeModeNames.Length - 1);
                                break;
                            case "ShowGrid":
                                ShowGrid = boolValue;
                                break;
                            case "ShowMoveConditions":
                                ShowMoveConditions = boolValue;
                                break;
                            case "MusicMuted":
                                SoundsManager.MusicMuted = boolValue;
                                break;
                            case "RenderEdges":
                                RenderEdges = boolValue;
                                break;
                            case "RenderDetectors":
                                RenderDetectors = boolValue;
                                break;
                            case "RenderBoxes":
                                RenderBoxes = boolValue;
                                break;
                            case "AntiAlias":
                                if (int.TryParse(value, out int aa))
                                    AntiAlias = Math.Clamp(aa, 0, 2);
                                break;
                        }
                    }
                }
            }
        }

        void UpdateButtons()
        {
            foreach (var button in Buttons.ToList()) // Full Screen rebuilds the panel while it is clicked
                button.Update();
        }

        void DrawButtons()
        {
            foreach (var button in Buttons)
                button.Draw();
        }

        public static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "Vector";

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            // -project NAME: play the Vectorier project Data/Projects/NAME (before anything is loaded).
            int projectArg = Array.IndexOf(args, "-project");
            if (projectArg >= 0 && projectArg < args.Length - 1)
            {
                string project = args[projectArg + 1];
                if (!VectorPaths.UseProject(project))
                    Console.WriteLine($"Project \"{project}\" was not found (looked for Projects/{project} above {VectorPaths.AppFolder}). Using {VectorPaths.Resources}.");
            }
            Console.WriteLine("Content: " + VectorPaths.Resources);

            Vector game = new Vector(1280, 720, "Vector");

            Instance = game;

            game.Run();
        }

    }
}
