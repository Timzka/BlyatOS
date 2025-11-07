using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BlyatOS.Library.Helpers;
using Cosmos.HAL;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using Sys = Cosmos.System;

namespace BlyatOS.Library.Configs;

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
                Console.ReadKey();

                Cosmos.System.Power.Shutdown();
            }
        }
    }

    //Image Resizer
    public static int[] ResizeBitmap(Bitmap bmp, uint nX, uint nY, bool check = false)
    {
        if (check && bmp.Width == nX && bmp.Height == nY) { return bmp.rawData; }
        int[] result = new int[nX * nY];
        if (bmp.Width == nX && bmp.Height == nY) { result = bmp.rawData; return result; }

        for (int i = 0; i < nX; i++)
        {
            for (int j = 0; j < nY; j++)
            {
                result[i + j * nX] = bmp.rawData[(i * bmp.Width / nX) + (j * bmp.Height / nY) * bmp.Width];
            }
        }
        return result;
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

    public static void ChangeColorSet()
    {
        ConsoleHelpers.WriteLine($"Choose a Color --> Background / Foreground", Color.Yellow);
        for (int i = 0; i < ColorSets.Count; i++)
        {
            var set = ColorSets[i];
            ConsoleHelpers.WriteLine($"{i}. {set.BackgroundColor} / {set.ForegroundColor}", set.ForegroundColor, set.BackgroundColor);
        }
        string colorIndexStr = ConsoleHelpers.ReadLine();
        int colorIndex = int.TryParse(colorIndexStr, out int result) ? result : throw new GenericException("Invalid integer");

        BackgroundColor = ColorSets[colorIndex].BackgroundColor;
        ForegroundColor = ColorSets[colorIndex].ForegroundColor;

        ConsoleHelpers.ClearConsole();
    }

    private static readonly List<Colorset> ColorSets = new()
    {
        new Colorset(Color.Black, Color.White),          // 0
        new Colorset(Color.Black, Color.Orange),         // 1
        new Colorset(Color.Black, Color.Yellow),         // 2
        new Colorset(Color.DarkBlue, Color.White),       // 3
        new Colorset(Color.DarkBlue, Color.Orange),      // 4
        new Colorset(Color.DarkMagenta, Color.Yellow),   // 5
        new Colorset(Color.DarkMagenta, Color.Orange),   // 6
        new Colorset(Color.White, Color.Black),          // 7
        new Colorset(Color.White, Color.Orange),         // 8
        new Colorset(Color.Blue, Color.Orange),          // 9
        new Colorset(Color.Magenta, Color.White),        // 10
        new Colorset(Color.Orange, Color.Black),         // 11
        new Colorset(Color.Orange, Color.White),         // 12
        new Colorset(Color.Pink, Color.Black),           // 13
        new Colorset(Color.Pink, Color.White)            // 14
    };

    private static readonly List<ReUsePen> _penCache = new List<ReUsePen>
    {
        new ReUsePen(Color.Black, new Pen(Color.Black)),
        new ReUsePen(Color.White, new Pen(Color.White)),
        new ReUsePen(Color.Red, new Pen(Color.Red)),
        new ReUsePen(Color.Green, new Pen(Color.Green)),
        new ReUsePen(Color.Blue, new Pen(Color.Blue)),
        new ReUsePen(Color.Yellow, new Pen(Color.Yellow)),
        new ReUsePen(Color.Cyan, new Pen(Color.Cyan)),
        new ReUsePen(Color.Magenta, new Pen(Color.Magenta)),
        new ReUsePen(Color.Gray, new Pen(Color.Gray)),
        new ReUsePen(Color.DarkGray, new Pen(Color.DarkGray)),
        new ReUsePen(Color.LightGray, new Pen(Color.LightGray)),
        new ReUsePen(Color.Orange, new Pen(Color.Orange)),
        new ReUsePen(Color.Pink, new Pen(Color.Pink)),
        new ReUsePen(Color.Brown, new Pen(Color.Brown)),
        new ReUsePen(Color.Purple, new Pen(Color.Purple)),
        new ReUsePen(Color.DarkBlue, new Pen(Color.DarkBlue)),
        new ReUsePen(Color.DarkGreen, new Pen(Color.DarkGreen)),
        new ReUsePen(Color.DarkRed, new Pen(Color.DarkRed)),
        new ReUsePen(Color.DarkMagenta, new Pen(Color.DarkMagenta))
    };

    // Gibt einen Pen aus dem Cache zurück, oder erstellt bei Bedarf einen neuen und speichert ihn
    public static Pen GetPen(Color color)
    {
        if (color == default)
            color = Color.White;
        var wantedpen = _penCache.Find(p => p.Color == color);
        if (wantedpen != null)
            return wantedpen.Pen;

        try
        {
            var newPen = new Pen(color);
            _penCache.Add(new ReUsePen(color, newPen));
            return newPen;
        }
        catch
        {
            // Fallback auf Weiß
            var fallback = _penCache.Find(p => p.Color == Color.White);
            return fallback != null ? fallback.Pen : new Pen(Color.White);
        }
    }

    // Angepasst: nutzt nun den Cache statt jedes Mal new Pen()
    public static Pen GetForegroundPen()
    {
        return GetPen(_foregroundColor);
    }
    public static Pen GetBackgroundPen()
    {
        return GetPen(_backgroundColor);
    }
}

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

public class ReUsePen
{
    public Color Color { get; set; }
    public Pen Pen { get; set; }

    public ReUsePen(Color color, Pen pen)
    {
        Color = color;
        Pen = pen;
    }
}

