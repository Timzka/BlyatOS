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
        private static bool _isInputMode = false;

        private static readonly StringBuilder _sharedStringBuilder = new StringBuilder(256);
        private static readonly string[] _charCache = new string[256];

        static ConsoleHelpers()
        {
            for (int i = 0; i < 256; i++)
                _charCache[i] = ((char)i).ToString();
        }

        private const int SCROLL_THRESHOLD = 2;
        private const int BATCH_DISPLAY_THRESHOLD = 50;
        private const int GC_THRESHOLD = 500;

        private static int _charsSinceDisplay = 0;
        private static int _drawCallsSinceGC = 0;

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

                // KEIN Console.Clear() mehr - das verursacht möglicherweise Probleme
                _cursorX = 0;
                _cursorY = 0;
                _currentLine.Clear();
                _currentLine.TrimExcess();
                _charsSinceDisplay = 0;
                _drawCallsSinceGC = 0;

                int collected = Heap.Collect();
                WriteLine($"[Garbage Collector] Collected {collected} objects.", Color.Gray);
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
            // KEINE Console.SetCursorPosition mehr
        }

        // UpdateCursorPosition komplett entfernt - das war der Bottleneck!

        public static void ReadKey() => Console.ReadKey();

        private static void PauseAndClear()
        {
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;
            if (canvas == null || font == null) return;

            try
            {
                string message = "[Press any key to Continue...]";
                int messageY = MaxHeight * font.Height;
                int messageX = 0;

                canvas.DrawFilledRectangle(DisplaySettings.BackgroundColor, messageX, messageY,
                    message.Length * font.Width, font.Height);
                canvas.DrawString(message, font, Color.LimeGreen, messageX, messageY);
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
            // FIX: Nur bei ECHTER Überschreitung pauscen, nicht schon vorher
            if (_cursorY >= MaxHeight)
                PauseAndClear();
        }

        // === ESCAPE SEQUENCES ===
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
                        case 'n': _sharedStringBuilder.Append('\n'); i++; break;
                        case 't': _sharedStringBuilder.Append('\t'); i++; break;
                        case 'r': _sharedStringBuilder.Append('\r'); i++; break;
                        case '\\': _sharedStringBuilder.Append('\\'); i++; break;
                        default: _sharedStringBuilder.Append(input[i]); break;
                    }
                }
                else
                {
                    _sharedStringBuilder.Append(input[i]);
                }
            }
            return _sharedStringBuilder.ToString();
        }

        // === CODEPAGE-437 ===
        private static char MapToCodePage437(char c)
        {
            return c switch
            {
                'ä' => (char)132,
                'ö' => (char)148,
                'ü' => (char)129,
                'Ä' => (char)142,
                'Ö' => (char)153,
                'Ü' => (char)154,
                'ß' => (char)225,
                '\r' => '\0',
                _ => c,
            };
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
        private static void WriteChar(char c, Color? fg = null, Color? bg = null, bool batchMode = false)
        {
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;
            if (canvas == null || font == null) return;

            var foreground = fg ?? DisplaySettings.ForegroundColor;
            var background = bg ?? DisplaySettings.BackgroundColor;

            try
            {
                if (c == '\n')
                {
                    _currentLine.Clear();
                    _currentLine.TrimExcess();
                    _cursorX = 0;
                    _cursorY++;
                    if (batchMode)
                    {
                        canvas.Display();
                        _charsSinceDisplay = 0;
                    }
                    // KEIN UpdateCursorPosition mehr
                    return;
                }

                if (_cursorX >= MaxWidth)
                {
                    _currentLine.Clear();
                    _currentLine.TrimExcess();
                    _cursorX = 0;
                    _cursorY++;
                }

                if (_isInputMode)
                {
                    if (_cursorX < _currentLine.Count)
                    {
                        int toRemove = _currentLine.Count - _cursorX;
                        if (toRemove > 0)
                            _currentLine.RemoveRange(_cursorX, toRemove);
                    }
                    _currentLine.Add(new ColoredChar(c, foreground, background));
                }

                int px = _cursorX * font.Width;
                int py = _cursorY * font.Height;

                if (px >= 0 && py >= 0 &&
                    px + font.Width <= DisplaySettings.ScreenWidth &&
                    py + font.Height <= DisplaySettings.ScreenHeight)
                {
                    canvas.DrawFilledRectangle(background, px, py, font.Width, font.Height);

                    int code = (int)c;
                    string charStr = (code >= 0 && code < 256) ? _charCache[code] : c.ToString();
                    canvas.DrawString(charStr, font, foreground, px, py);

                    _drawCallsSinceGC++;

                    if (batchMode)
                    {
                        _charsSinceDisplay++;
                        if (_charsSinceDisplay >= BATCH_DISPLAY_THRESHOLD)
                        {
                            canvas.Display();
                            _charsSinceDisplay = 0;

                            if (_drawCallsSinceGC >= GC_THRESHOLD)
                            {
                                Heap.Collect();
                                _drawCallsSinceGC = 0;
                            }
                        }
                    }
                    else
                    {
                        canvas.Display();
                    }
                }

                _cursorX++;
                // KEIN UpdateCursorPosition mehr
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

        private static void WriteStyled(string text, Color? fg = null, Color? bg = null)
        {
            if (string.IsNullOrEmpty(text)) return;

            text = MapStringToCodePage437(text);
            var canvas = DisplaySettings.Canvas;
            if (canvas == null) return;

            try
            {
                bool batch = text.Length > BATCH_DISPLAY_THRESHOLD;
                foreach (char c in text)
                    WriteChar(c, fg, bg, batch);

                if (_charsSinceDisplay > 0)
                {
                    canvas.Display();
                    _charsSinceDisplay = 0;
                }

                EnsureCursorVisible();
            }
            catch { }
        }

        public static void Write(string text, Color? fg = null, Color? bg = null)
            => WriteStyled(text, fg, bg);

        public static void WriteLine(string text = "", Color? fg = null, Color? bg = null)
            => WriteStyled(text + "\n", fg, bg);

        // === INPUT ===
        public static string ReadLine(string prompt = "> ", Color? promptColor = null)
        {
            Write(prompt, promptColor);
            var input = new StringBuilder();
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;

            _isInputMode = true;
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
                        _cursorX = 0;
                        _cursorY++;
                        EnsureCursorVisible();
                        // KEIN UpdateCursorPosition
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

                                canvas.DrawFilledRectangle(DisplaySettings.BackgroundColor,
                                    _cursorX * font.Width,
                                    _cursorY * font.Height,
                                    font.Width, font.Height);
                                canvas.Display();
                                // KEIN UpdateCursorPosition
                            }
                        }
                        break;

                    default:
                        char inputChar = key.KeyChar;
                        if (inputChar != 0 && !char.IsControl(inputChar))
                        {
                            input.Append(inputChar);
                            WriteChar(MapToCodePage437(inputChar));
                            EnsureCursorVisible();
                        }
                        break;
                }
            }

            string result = input.ToString();
            input.Clear();
            _isInputMode = false;
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

            _isInputMode = true;
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
                        _cursorX = 0;
                        _cursorY++;
                        EnsureCursorVisible();
                        // KEIN UpdateCursorPosition
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

                                canvas.DrawFilledRectangle(DisplaySettings.BackgroundColor,
                                    _cursorX * font.Width,
                                    _cursorY * font.Height,
                                    font.Width, font.Height);
                                canvas.Display();
                                // KEIN UpdateCursorPosition
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
            password.Clear();
            _isInputMode = false;
            _currentLine.Clear();
            _currentLine.TrimExcess();
            return result;
        }
    }
}