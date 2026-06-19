using Raylib_cs;
using System.Globalization;
using System.Numerics;
using Vector.Core.Animation;
using Vector.Core.Location;
using Vector.Core.Models;

namespace Vector
{
    class Button
    {
        public Raylib_cs.Rectangle Rectangle;

        Color ButtonColor;

        Color HoverColor;

        Color PressedColor;

        Color TextColor;

        string Text;

        public Action OnClick;

        bool IsHovered;

        bool IsPressed;

        float FontSize;

        Vector2 TextPosition;

        Font Font;

        public Button(float x, float y, float width, float height, string text, Action onClick)
        {
            Rectangle = new Raylib_cs.Rectangle(x, y, width, height);
            Text = text;
            OnClick = onClick;
            ButtonColor = Color.LightGray;
            HoverColor = Color.Gray;
            PressedColor = Color.DarkGray;
            TextColor = Color.Black;

            Font = Raylib.GetFontDefault();

            FontSize = GetFittingFontSize(
                Font,
                Text,
                Rectangle.Width - 12,
                Rectangle.Height - 12,
                1);

            var textSize = Raylib.MeasureTextEx(Font, Text, FontSize, 1);

            TextPosition = new Vector2(
                Rectangle.X + (Rectangle.Width - textSize.X) / 2,
                Rectangle.Y + (Rectangle.Height - textSize.Y) / 2);
        }

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
                    if (IsPressed)
                    {
                        OnClick?.Invoke();
                    }
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
            Raylib.DrawRectangleRec(
                Rectangle,
                IsPressed ? PressedColor : IsHovered ? HoverColor : ButtonColor
            );

            Raylib.DrawTextEx(
                Font,
                Text,
                TextPosition,
                FontSize,
                1,
                TextColor
            );
        }

        private float GetFittingFontSize(Font font, string text, float maxWidth, float maxHeight, float spacing)
        {
            float fontSize = maxHeight;

            while (fontSize > 1)
            {
                Vector2 size = Raylib.MeasureTextEx(font, text, fontSize, spacing);

                if (size.X <= maxWidth && size.Y <= maxHeight)
                    return fontSize;

                fontSize -= 1;
            }

            return 1;
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

        public static bool RenderNodes;
        public static bool RenderEdges;
        public static bool RenderDetectors;
        public static bool RenderBoxes;

        public static string DebugSettingsFile = Directory.GetCurrentDirectory() + "/debugsettings.txt";

        public static string Settings;

        Button PlatformButton;
        Button TriggerButton;
        Button AreaButton;

        Button NodeButton;
        Button EdgeButton;
        Button DetectorButton;
        Button BoxesButton;

        public Vector(int width, int height, string name)
        {
            ScreenWidth = width;
            ScreenHeight = height;

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

            var animations = Animations.ToList();
            int count = animations.Count;
            for (int i = 0; i < count; i++)
            {
                animations[i].LoadBin();
            }

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

                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.F11))
                {
                    Raylib.ToggleFullscreen();

                    ScreenWidth = Raylib.GetScreenWidth();
                    ScreenHeight = Raylib.GetScreenHeight();

                    Camera.Offset = Raylib.GetScreenCenter();
                    Camera.Zoom = ScreenHeight / (OrtographicSize * 2);

                    CameraRectangle = GetCameraWorldRect(Camera);

                    SetButtons();
                }

                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.P))
                {
                    PlatformButton.OnClick?.Invoke();
                }
                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.T))
                {
                    TriggerButton.OnClick?.Invoke();
                }
                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.A))
                {
                    AreaButton.OnClick?.Invoke();
                }

                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.N))
                {
                    NodeButton.OnClick?.Invoke();
                }
                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.E))
                {
                    EdgeButton.OnClick?.Invoke();
                }
                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.D))
                {
                    DetectorButton.OnClick?.Invoke();
                }
                if (InputManager.WasKeyPressedThisFrame(KeyboardKey.B))
                {
                    BoxesButton.OnClick?.Invoke();
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
                    LevelMainController.current.slowModeFrames = LevelMainController.current.slowModeFrames == 10 ? 1 : 10;
                }

                UpdateButtons();

                bool shouldUpdateGame = !pause || stepOneFrame;

                if (shouldUpdateGame)
                {
                    LevelMainController.current.Render();

                    for (int i = 0; i < GlobalObjects.Updatables.Count; i++)
                    {
                        GlobalObjects.Updatables[i].Update();
                    }

                    stepOneFrame = false;
                }

                Raylib.BeginDrawing();
                {
                    Raylib.ClearBackground(Color.Gray);

                    Raylib.BeginMode2D(Camera);

                    for (int i = 0; i < GlobalObjects.Drawables.Count; i++)
                    {
                        GlobalObjects.Drawables[i].Draw();
                    }

                    if (RenderPlatforms)
                    {
                        for (int i = 0; i < Sets.Current.Platforms.Count; i++)
                        {
                            Sets.Current.Platforms[i].Draw();
                        }
                    }
                    if (RenderAreas)
                    {
                        for (int i = 0; i < Sets.Current.Areas.Count; i++)
                        {
                            Sets.Current.Areas[i].Draw();
                        }
                    }
                    if (RenderTriggers)
                    {
                        for (int i = 0; i < Sets.Current.Triggers.Count; i++)
                        {
                            Sets.Current.Triggers[i].DrawRectangle();
                        }
                    }

                    for (int i = 0; i < Core.Models.Model.ModelsAll.Count; i++)
                    {
                        Core.Models.Model.ModelsAll[i].Draw();
                    }

                    Raylib.EndMode2D();

                    ModelHuman player = LevelMainController.current.Location.GetUserModel();
                    string animation = "null";
                    if (player != null && player.ControllerAnimations != null && player.ControllerAnimations.Animation != null)
                    {
                        animation = player.ControllerAnimations.CurrentFrame + " - " + player.ControllerAnimations.Animation.Name;
                    }

                    Raylib.DrawText(animation, 0, Raylib.GetScreenHeight() - 30, 30, Color.White);

                    DrawButtons();

                    Raylib.DrawFPS(0, 0);
                }
                Raylib.EndDrawing();
            }
            SaveSettings();
            Raylib.CloseWindow();
        }

        void SetButtons()
        {
            PlatformButton = new Button(ScreenWidth - 100, 20, 50, 50, "P", () => { RenderPlatforms = !RenderPlatforms;  });
            TriggerButton = new Button(PlatformButton.Rectangle.X - 70, 20, 50, 50, "T", () => { RenderTriggers = !RenderTriggers;  });
            AreaButton = new Button(TriggerButton.Rectangle.X - 70, 20, 50, 50, "A", () => { RenderAreas = !RenderAreas;  });

            NodeButton = new Button(ScreenWidth - 100, 100, 50, 50, "N", () => { RenderNodes = !RenderNodes;  });
            EdgeButton = new Button(ScreenWidth - 100, NodeButton.Rectangle.Y + 70, 50, 50, "E", () => { RenderEdges = !RenderEdges;  });
            DetectorButton = new Button(ScreenWidth - 100, EdgeButton.Rectangle.Y + 70, 50, 50, "D", () => { RenderDetectors = !RenderDetectors;  });
            BoxesButton = new Button(ScreenWidth - 100, DetectorButton.Rectangle.Y + 70, 50, 50, "B", () => { RenderBoxes = !RenderBoxes;  });
        }

        void SaveSettings()
        {
            Settings = $"RenderPlatforms={RenderPlatforms}\n" +
                       $"RenderTriggers={RenderTriggers}\n" +
                       $"RenderAreas={RenderAreas}\n" +
                       $"RenderNodes={RenderNodes}\n" +
                       $"RenderEdges={RenderEdges}\n" +
                       $"RenderDetectors={RenderDetectors}\n" +
                       $"RenderBoxes={RenderBoxes}";
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
                            case "RenderNodes":
                                RenderNodes = boolValue;
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
                        }
                    }
                }
            }
        }

        void UpdateButtons()
        {
            PlatformButton.Update();
            TriggerButton.Update();
            AreaButton.Update();

            NodeButton.Update();
            EdgeButton.Update();
            DetectorButton.Update();
            BoxesButton.Update();
        }

        void DrawButtons()
        {
            PlatformButton.Draw();
            TriggerButton.Draw();
            AreaButton.Draw();

            NodeButton.Draw();
            EdgeButton.Draw();
            DetectorButton.Draw();
            BoxesButton.Draw();
        }

        public static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "Vector";

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            Vector game = new Vector(1280, 720, "Vector");

            Instance = game;

            game.Run();
        }

    }
}
