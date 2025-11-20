using System;
using System.Collections.Generic;
using System.Drawing;
using BlyatOS.Library.Helpers;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using Sys = Cosmos.System;

namespace BlyatOS.Library.Configs
{
    public static class DisplaySettings
    {
        // Default values for Cosmos
        private static uint _screenWidth = 1024;
        private static uint _screenHeight = 768;
        private static ColorDepth _colorDepth = ColorDepth.ColorDepth32;
        private static Color _backgroundColor = Color.Black;
        private static Color _foregroundColor = Color.White;
        private static Font _font = null;
        private static Canvas _canvas = null;

        // Screen properties
        public static uint ScreenWidth
        {
            get => _screenWidth;
            set => _screenWidth = value;
        }

        public static uint ScreenHeight
        {
            get => _screenHeight;
            set => _screenHeight = value;
        }

        // Color properties
        public static Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    if (_canvas != null)
                    {
                        _canvas.Clear(_backgroundColor);
                        _canvas.Display();
                    }
                }
            }
        }

        public static Color ForegroundColor
        {
            get => _foregroundColor;
            set => _foregroundColor = value;
        }

        // Font property
        public static Font Font
        {
            get
            {
                if (_font == null)
                    _font = PCScreenFont.Default;
                return _font;
            }
            set
            {
                if (value != null && _font != value)
                    _font = value;
            }
        }

        // Canvas property
        public static Canvas Canvas
        {
            get
            {
                if (_canvas == null)
                    throw new InvalidOperationException("Canvas has not been initialized. Call InitializeGraphics() first.");
                return _canvas;
            }
            internal set => _canvas = value;
        }

        // === GRAPHICS INITIALIZATION ===
        public static void InitializeGraphics()
        {
            if (_canvas != null) return;

            try
            {
                var mode = new Mode(_screenWidth, _screenHeight, _colorDepth);
                _canvas = FullScreenCanvas.GetFullScreenCanvas(mode);
                _canvas.Clear(_backgroundColor);
                _canvas.Display();

                if (_font == null)
                    _font = PCScreenFont.Default;
            }
            catch (Exception ex)
            {
                Console.Clear();
                Console.WriteLine($"Graphics initialization failed: {ex.Message}");
                Console.WriteLine("Falling back to text mode.");
                Console.ReadKey();
                Sys.Power.Shutdown();
            }
        }

        // === IMAGE RESIZER ===
        public static int[] ResizeBitmap(Bitmap bmp, uint nX, uint nY, bool check = false)
        {
            if (check && bmp.Width == nX && bmp.Height == nY)
                return bmp.RawData;

            int[] result = new int[nX * nY];

            if (bmp.Width == nX && bmp.Height == nY)
                return bmp.RawData;

            for (int i = 0; i < nX; i++)
            {
                for (int j = 0; j < nY; j++)
                {
                    result[i + j * (int)nX] =
                        bmp.RawData[(i * bmp.Width / (int)nX) + (j * bmp.Height / (int)nY) * bmp.Width];
                }
            }

            return result;
        }

        public static void ReDoCanvas()
        {
            if (_canvas != null)
            {
                _canvas.Disable();
                _canvas = null;
            }
            InitializeGraphics();
        }

        public static void ApplyDisplaySettings(uint width, uint height, ColorDepth depth)
        {
            if (_screenWidth == width && _screenHeight == height && _colorDepth == depth)
                return;

            _screenWidth = width;
            _screenHeight = height;
            _colorDepth = depth;

            if (_canvas != null)
            {
                _canvas.Disable();
                _canvas = null;
            }

            InitializeGraphics();
        }

        public static bool IsGraphicsInitialized => _canvas != null;

        // === COLOR SET SYSTEM ===
        public static void ChangeColorSet()
        {
            ConsoleHelpers.WriteLine($"Choose a Color --> Background / Foreground", Color.Yellow);

            for (int i = 0; i < ColorSets.Count; i++)
            {
                var set = ColorSets[i];
                ConsoleHelpers.WriteLine($"{i}. {set.BackgroundColor} / {set.ForegroundColor}",
                    set.ForegroundColor, set.BackgroundColor);
            }

            string colorIndexStr = ConsoleHelpers.ReadLine();
            if (!int.TryParse(colorIndexStr, out int colorIndex) || colorIndex < 0 || colorIndex >= ColorSets.Count)
                throw new Exception("Invalid color index.");

            BackgroundColor = ColorSets[colorIndex].BackgroundColor;
            ForegroundColor = ColorSets[colorIndex].ForegroundColor;

            ConsoleHelpers.ClearConsole();
        }

        // === PRESET COLOR COMBINATIONS ===
        private static readonly List<Colorset> ColorSets = new()
        {
            new Colorset(Color.Black, Color.White),
            new Colorset(Color.Black, Color.Orange),
            new Colorset(Color.Black, Color.Yellow),
            new Colorset(Color.DarkBlue, Color.White),
            new Colorset(Color.DarkBlue, Color.Orange),
            new Colorset(Color.DarkMagenta, Color.Yellow),
            new Colorset(Color.DarkMagenta, Color.Orange),
            new Colorset(Color.White, Color.Black),
            new Colorset(Color.White, Color.Orange),
            new Colorset(Color.Blue, Color.Orange),
            new Colorset(Color.Magenta, Color.White),
            new Colorset(Color.Orange, Color.Black),
            new Colorset(Color.Orange, Color.White),
            new Colorset(Color.Pink, Color.Black),
            new Colorset(Color.Pink, Color.White)
        };
    }

    // === SIMPLE DATA CLASSES ===
    public class Colorset
    {
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }

        public Colorset(Color background, Color foreground)
        {
            BackgroundColor = background;
            ForegroundColor = foreground;
        }
    }
}
