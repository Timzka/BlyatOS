using System;
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
        private static int _screenWidth = 80 * 8;  // 80 columns * 8px font width
        private static int _screenHeight = 25 * 16; // 25 rows * 16px font height
        private static ColorDepth _colorDepth = ColorDepth.ColorDepth32;
        private static Color _backgroundColor = Color.Black;
        private static Color _foregroundColor = Color.White;
        private static Font _font = null;
        private static Canvas _canvas = null;

        // Screen properties
        public static int ScreenWidth
        {
            get => _screenWidth;
            set
            {
                if (_screenWidth != value)
                {
                    _screenWidth = value;
                    // TODO: Handle screen size change if needed
                }
            }
        }

        public static void ChangeColorSet()
        {
            ConsoleHelpers.WriteLine($"Choose a Color", Color.Yellow);
            ConsoleHelpers.WriteLine("0. Black / White", Color.White);
            ConsoleHelpers.WriteLine("1. Blue / Orange", Color.Orange);
            string colorIndexStr = ConsoleHelpers.ReadLine();
            int colorIndex = int.TryParse(colorIndexStr, out int result) ? result : throw new GenericException("Invalid integer");
            switch (colorIndex)
            {
                case 0:
                    BackgroundColor = Color.Black;
                    ForegroundColor = Color.White;
                    break;
                case 1:
                    BackgroundColor = Color.Blue;
                    ForegroundColor = Color.Orange;
                    break;
                default:
                    throw new GenericException("Invalid Color");
            }

            ConsoleHelpers.ClearConsole();
        }
        public static int ScreenHeight
        {
            get => _screenHeight;
            set
            {
                if (_screenHeight != value)
                {
                    _screenHeight = value;
                    // TODO: Handle screen size change if needed
                }
            }
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
                        _canvas.Clear(_backgroundColor);
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
                // Initialize default font if not set
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
                    throw new System.InvalidOperationException("Canvas has not been initialized. Call InitializeGraphics() first.");
                return _canvas;
            }
            internal set => _canvas = value;
        }

        // Graphics initialization
        public static void InitializeGraphics()
        {
            if (_canvas == null)
            {
                try
                {
                    // Set up the graphics mode
                    var mode = new Mode(_screenWidth, _screenHeight, _colorDepth);
                    _canvas = FullScreenCanvas.GetFullScreenCanvas(mode);
                    _canvas.Clear(_backgroundColor);
                    _canvas.Display();
                    
                    // Initialize default font if not already set
                    if (_font == null)
                        _font = PCScreenFont.Default;
                }
                catch (Exception ex)
                {
                    // Fallback to text mode if graphics initialization fails
                    Console.Clear();
                    Console.WriteLine($"Graphics initialization failed: {ex.Message}");
                    Console.WriteLine("Falling back to text mode.");
                }
            }
        }
        
        
        // Apply new display settings
        public static void ApplyDisplaySettings(int width, int height, ColorDepth depth)
        {
            if (_screenWidth != width || _screenHeight != height || _colorDepth != depth)
            {
                _screenWidth = width;
                _screenHeight = height;
                _colorDepth = depth;
                
                // Reinitialize graphics with new settings
                if (_canvas != null)
                {
                    _canvas.Disable();
                    _canvas = null;
                }
                
                InitializeGraphics();
            }
        }

        // Helper property to check if graphics are initialized
        public static bool IsGraphicsInitialized => _canvas != null;

        // Helper method to get a pen with the current foreground color
        public static Pen GetForegroundPen()
        {
            return new Pen(_foregroundColor);
        }
    }
}
