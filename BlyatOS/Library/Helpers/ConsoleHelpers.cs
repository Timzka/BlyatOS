using System;
using System.Drawing;
using System.Text;
using Cosmos.HAL;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using BlyatOS.Library.Configs;
using Sys = Cosmos.System;
using Cosmos.Core.Memory;

namespace BlyatOS.Library.Helpers
{
    public static class ConsoleHelpers
    {
        // === INTERNAL STRUCTURES ===
        private readonly struct ColoredChar
        {
            public char Character { get; }
            public Color Foreground { get; }
            public Color Background { get; }

            public ColoredChar(char c, Color fg, Color bg)
            {
                Character = c;
                Foreground = fg;
                Background = bg;
            }
        }

        private static int _cursorX;
        private static int _cursorY;
        private static System.Collections.Generic.List<ColoredChar> _currentLine = new();
        private static bool _isInputMode = false; // Track if we're in input mode

        // Reusable StringBuilder to reduce allocations
        private static readonly StringBuilder _sharedStringBuilder = new StringBuilder(256);

        // Character string cache to avoid allocating strings for every character
        private static readonly string[] _charCache = new string[256];

        // Cache last used pens to avoid repeated GetPen calls
        private static Color _lastFgColor = Color.White;
        private static Color _lastBgColor = Color.Black;
        private static Pen _lastFgPen = null;
        private static Pen _lastBgPen = null;

        static ConsoleHelpers()
        {
            // Pre-allocate common ASCII characters
            for (int i = 0; i < 256; i++)
            {
                _charCache[i] = ((char)i).ToString();
            }
        }

        private const int SCROLL_THRESHOLD = 2;
        private const int BATCH_DISPLAY_THRESHOLD = 50; // Balance between performance and responsiveness
        private static int _charsSinceDisplay = 0;
        private static int _drawCallsSinceGC = 0;
        private const int GC_THRESHOLD = 500; // Collect after N draw calls

        private static int MaxWidth => (int)(DisplaySettings.ScreenWidth / DisplaySettings.Font.Width) - 1;
        private static int MaxHeight => (int)(DisplaySettings.ScreenHeight / DisplaySettings.Font.Height) - 1;

        // === CLEAR / CURSOR ===
        public static void ClearConsole()
        {
            try
            {
                var canvas = DisplaySettings.Canvas;
                if (canvas == null) return;

                canvas.Clear(DisplaySettings.BackgroundColor);
                canvas.Display();
                Console.Clear();
                _cursorX = 0;
                _cursorY = 0;
                _currentLine.Clear();
                _currentLine.TrimExcess(); // Release memory
                _charsSinceDisplay = 0;
                _drawCallsSinceGC = 0; // Reset GC counter

                int collected = Heap.Collect();
                ConsoleHelpers.WriteLine($"[Garbage Collector] Collected "+collected+" objects.", Color.Gray);//for debug
            }
            catch
            {
                _cursorX = 0;
                _cursorY = 0;
                _currentLine.Clear();
                _charsSinceDisplay = 0;
            }
        }

        public static void SetCursorPosition(int left, int top)
        {
            _cursorX = Math.Max(0, Math.Min(left, MaxWidth));
            _cursorY = Math.Max(0, Math.Min(top, MaxHeight));
            UpdateCursorPosition();
        }

        private static void UpdateCursorPosition()
        {
            try
            {
                int clampedY = Math.Max(0, Math.Min(_cursorY, MaxHeight));
                Console.SetCursorPosition(Math.Max(0, Math.Min(_cursorX, MaxWidth)), clampedY);
            }
            catch { }
        }

        public static void ReadKey() => Console.ReadKey();

        // === PAUSE AND CLEAR ===
        private static void PauseAndClear()
        {
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;
            if (canvas == null || font == null) return;

            try
            {
                string message = "[Press any key to Continue...]";
                var greenPen = DisplaySettings.GetPen(Color.Green);
                var bgPen = DisplaySettings.GetBackgroundPen();

                int messageY = MaxHeight * font.Height;
                int messageX = 0;

                canvas.DrawFilledRectangle(bgPen, messageX, messageY,
                    message.Length * font.Width, font.Height);
                canvas.DrawString(message, font, greenPen, messageX, messageY);
                canvas.Display();

                while (!Sys.KeyboardManager.KeyAvailable)
                    Global.PIT.Wait(10);
                Sys.KeyboardManager.ReadKey();

                ClearConsole();
            }
            catch
            {
                ClearConsole();
            }
        }

        private static void EnsureCursorVisible()
        {
            if (_cursorY > MaxHeight - SCROLL_THRESHOLD)
            {
                PauseAndClear();
            }
        }

        // === ESCAPE SEQUENCE PROCESSING ===
        public static string ProcessEscapeSequences(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            _sharedStringBuilder.Clear();
            _sharedStringBuilder.EnsureCapacity(input.Length);

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\\' && i + 1 < input.Length)
                {
                    switch (input[i + 1])
                    {
                        case 'n':
                            _sharedStringBuilder.Append('\n');
                            i++;
                            break;
                        case 't':
                            _sharedStringBuilder.Append('\t');
                            i++;
                            break;
                        case 'r':
                            _sharedStringBuilder.Append('\r');
                            i++;
                            break;
                        case '\\':
                            _sharedStringBuilder.Append('\\');
                            i++;
                            break;
                        default:
                            _sharedStringBuilder.Append(input[i]);
                            break;
                    }
                }
                else
                {
                    _sharedStringBuilder.Append(input[i]);
                }
            }
            return _sharedStringBuilder.ToString();
        }

        // === CODEPAGE-437 MAPPING ===
        private static char MapToCodePage437(char c)
        {
            switch (c)
            {
                case 'ä': return (char)132;
                case 'ö': return (char)148;
                case 'ü': return (char)129;
                case 'Ä': return (char)142;
                case 'Ö': return (char)153;
                case 'Ü': return (char)154;
                case 'ß': return (char)225;
                case '\r': return '\0';
                default: return c;
            }
        }

        private static string MapStringToCodePage437(string input)
        {
            _sharedStringBuilder.Clear();
            _sharedStringBuilder.EnsureCapacity(input.Length);

            foreach (char c in input)
            {
                char mapped = MapToCodePage437(c);
                if (mapped != '\0')
                    _sharedStringBuilder.Append(mapped);
            }
            return _sharedStringBuilder.ToString();
        }

        // === TEXT OUTPUT ===
        private static void WriteChar(char c, Color? foreground = null, Color? background = null, bool batchMode = false)
        {
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;
            if (canvas == null || font == null) return;

            var fg = foreground ?? DisplaySettings.ForegroundColor;
            var bg = background ?? DisplaySettings.BackgroundColor;

            try
            {
                if (c == '\n')
                {
                    // Aggressively clear the line buffer to prevent memory buildup
                    if (_currentLine.Count > 0)
                    {
                        _currentLine.Clear();
                        _currentLine.TrimExcess(); // Release excess capacity
                    }
                    _cursorX = 0;
                    _cursorY++;
                    if (batchMode)
                    {
                        canvas.Display();
                        _charsSinceDisplay = 0;
                    }
                    UpdateCursorPosition();
                    return;
                }

                if (_cursorX >= MaxWidth)
                {
                    // Clear old line and release memory
                    _currentLine.Clear();
                    _currentLine.TrimExcess();
                    _cursorX = 0;
                    _cursorY++;
                }

                // Only track characters in input mode (for backspace support)
                if (_isInputMode)
                {
                    // Only keep current line position in buffer
                    if (_cursorX < _currentLine.Count)
                    {
                        // We're overwriting - remove everything after cursor
                        int toRemove = _currentLine.Count - _cursorX;
                        if (toRemove > 0)
                        {
                            _currentLine.RemoveRange(_cursorX, toRemove);
                        }
                    }

                    _currentLine.Add(new ColoredChar(c, fg, bg));
                }

                int px = _cursorX * font.Width;
                int py = _cursorY * font.Height;

                if (px >= 0 && py >= 0 &&
                    px + font.Width <= DisplaySettings.ScreenWidth &&
                    py + font.Height <= DisplaySettings.ScreenHeight)
                {
                    // Cache pens to avoid repeated GetPen lookups
                    if (_lastFgPen == null || _lastFgColor.ToArgb() != fg.ToArgb())
                    {
                        _lastFgColor = fg;
                        _lastFgPen = DisplaySettings.GetPen(fg);
                    }
                    if (_lastBgPen == null || _lastBgColor.ToArgb() != bg.ToArgb())
                    {
                        _lastBgColor = bg;
                        _lastBgPen = DisplaySettings.GetPen(bg);
                    }

                    canvas.DrawFilledRectangle(_lastBgPen, px, py, font.Width, font.Height);

                    // Use cached string to avoid allocation
                    int charCode = (int)c;
                    string charStr = (charCode >= 0 && charCode < 256) ? _charCache[charCode] : c.ToString();
                    canvas.DrawString(charStr, font, _lastFgPen, px, py);

                    // Track draw calls for GC
                    _drawCallsSinceGC++;

                    // Batch mode: accumulate draws, display periodically
                    if (batchMode)
                    {
                        _charsSinceDisplay++;
                        if (_charsSinceDisplay >= BATCH_DISPLAY_THRESHOLD)
                        {
                            canvas.Display();
                            _charsSinceDisplay = 0;

                            // Periodic GC to prevent buildup 
                            if (_drawCallsSinceGC >= GC_THRESHOLD)
                            {
                                Heap.Collect();
                                _drawCallsSinceGC = 0;
                            }
                        }
                    }
                    else
                    {
                        // Non-batch mode: display immediately (for input)
                        canvas.Display();
                    }
                }

                _cursorX++;
                UpdateCursorPosition();
            }
            catch
            {
                _cursorX++;
                if (_cursorX >= MaxWidth)
                {
                    _cursorX = 0;
                    _cursorY++;
                }
            }
        }

        private static void WriteStyled(string text, Color? foreground = null, Color? background = null)
        {
            if (string.IsNullOrEmpty(text)) return;

            text = MapStringToCodePage437(text);

            var canvas = DisplaySettings.Canvas;
            if (canvas == null) return;

            try
            {
                // Use batch mode for longer text to reduce Display() calls
                bool useBatchMode = text.Length > BATCH_DISPLAY_THRESHOLD;
                foreach (char c in text)
                    WriteChar(c, foreground, background, useBatchMode);

                // Always flush at end of write operation
                if (_charsSinceDisplay > 0)
                {
                    canvas.Display();
                    _charsSinceDisplay = 0;
                }

                EnsureCursorVisible();
            }
            catch { }
        }

        public static void Write(string text, Color? foreground = null, Color? background = null)
            => WriteStyled(text, foreground, background);

        public static void WriteLine(string text = "", Color? foreground = null, Color? background = null)
            => WriteStyled(text + "\n", foreground, background);

        // === INPUT ===
        public static string ReadLine(string prompt = "> ", Color? promptColor = null)
        {
            Write(prompt, promptColor);
            var input = new StringBuilder();
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;
            var bgPen = DisplaySettings.GetBackgroundPen();

            _isInputMode = true; // Enable input tracking
            bool done = false;
            while (!done)
            {
                while (!Sys.KeyboardManager.KeyAvailable)
                    Global.PIT.Wait(10);

                var key = Sys.KeyboardManager.ReadKey();
                switch (key.Key)
                {
                    case Sys.ConsoleKeyEx.Enter:
                        done = true;
                        _currentLine.Clear();
                        _currentLine.TrimExcess();
                        _cursorX = 0;
                        _cursorY++;
                        EnsureCursorVisible();
                        UpdateCursorPosition();
                        break;

                    case Sys.ConsoleKeyEx.Backspace:
                        if (input.Length > 0)
                        {
                            input.Length--;
                            if (_cursorX > 0)
                            {
                                _cursorX--;
                                if (_currentLine.Count > 0)
                                    _currentLine.RemoveAt(_currentLine.Count - 1);
                                try
                                {
                                    if (_cursorY >= 0 && _cursorY <= MaxHeight)
                                    {
                                        canvas.DrawFilledRectangle(bgPen,
                                            _cursorX * font.Width,
                                            _cursorY * font.Height,
                                            font.Width, font.Height);
                                        canvas.Display();
                                    }
                                }
                                catch { }
                                UpdateCursorPosition();
                            }
                        }
                        break;

                    default:
                        char inputChar = key.KeyChar;

                        if (inputChar != 0 && !char.IsControl(inputChar))
                        {
                            input.Append(inputChar);
                            char displayChar = MapToCodePage437(inputChar);
                            WriteChar(displayChar);
                            EnsureCursorVisible();
                        }
                        break;
                }
            }

            string result = input.ToString();
            input.Clear(); // StringBuilder aufräumen

            _isInputMode = false; // Disable input tracking
            _currentLine.Clear();
            _currentLine.TrimExcess();

            return result;
        }

        public static string ReadPassword(string prompt = "Password: ", Color? promptColor = null, char maskChar = '*')
        {
            Write(prompt, promptColor);
            var password = new StringBuilder();
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;
            var bgPen = DisplaySettings.GetBackgroundPen();

            _isInputMode = true; // Enable input tracking
            bool done = false;
            while (!done)
            {
                while (!Sys.KeyboardManager.KeyAvailable)
                    Global.PIT.Wait(10);

                var key = Sys.KeyboardManager.ReadKey();
                switch (key.Key)
                {
                    case Sys.ConsoleKeyEx.Enter:
                        done = true;
                        _currentLine.Clear();
                        _currentLine.TrimExcess();
                        _cursorX = 0;
                        _cursorY++;
                        EnsureCursorVisible();
                        UpdateCursorPosition();
                        break;

                    case Sys.ConsoleKeyEx.Backspace:
                        if (password.Length > 0)
                        {
                            password.Length--;
                            if (_cursorX > 0)
                            {
                                _cursorX--;
                                if (_currentLine.Count > 0)
                                    _currentLine.RemoveAt(_currentLine.Count - 1);
                                try
                                {
                                    if (_cursorY >= 0 && _cursorY <= MaxHeight)
                                    {
                                        canvas.DrawFilledRectangle(bgPen,
                                            _cursorX * font.Width,
                                            _cursorY * font.Height,
                                            font.Width, font.Height);
                                        canvas.Display();
                                    }
                                }
                                catch { }
                                UpdateCursorPosition();
                            }
                        }
                        break;

                    default:
                        char inputChar = key.KeyChar;

                        if (inputChar != 0 && !char.IsControl(inputChar))
                        {
                            password.Append(inputChar);
                            WriteChar(maskChar);
                            EnsureCursorVisible();
                        }
                        break;
                }
            }

            string result = password.ToString();
            password.Clear(); // StringBuilder aufräumen

            _isInputMode = false; // Disable input tracking
            _currentLine.Clear();
            _currentLine.TrimExcess();

            return result;
        }
    }
}
