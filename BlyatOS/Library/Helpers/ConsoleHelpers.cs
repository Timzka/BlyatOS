using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Cosmos.HAL;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using BlyatOS.Library.Configs;
using Sys = Cosmos.System;
using System.Linq;

namespace BlyatOS.Library.Helpers
{
    public static class ConsoleHelpers
    {
        // === INTERNAL STRUCTURES ===
        private class ColoredChar
        {
            public char Character { get; set; }
            public Color Foreground { get; set; }
            public Color Background { get; set; }

            public ColoredChar(char c, Color fg, Color bg)
            {
                Character = c;
                Foreground = fg;
                Background = bg;
            }
        }

        private static Color DefaultForeground => DisplaySettings.ForegroundColor;
        private static Color DefaultBackground => DisplaySettings.BackgroundColor;

        private static int _cursorX;
        private static int _cursorY;
        private static int _scrollOffset;

        private const int SCROLL_THRESHOLD = 2;
        private const int BATCH_DISPLAY_THRESHOLD = 50;
        private static readonly List<List<ColoredChar>> _screenBuffer = new();
        private static List<ColoredChar> _currentLine = new();

        private static bool _isRedrawing = false;
        private static int _charsSinceDisplay = 0;

        private static int MaxWidth => (int)(DisplaySettings.ScreenWidth / DisplaySettings.Font.Width) - 1;
        private static int MaxHeight => (int)(DisplaySettings.ScreenHeight / DisplaySettings.Font.Height) - 1;

        // === CLEAR / CURSOR ===
        public static void ClearConsole()
        {
            try
            {
                var canvas = DisplaySettings.Canvas;
                if (canvas == null) return;

                canvas.Clear(DefaultBackground);
                canvas.Display();

                _cursorX = 0;
                _cursorY = 0;
                _scrollOffset = 0;
                _screenBuffer.Clear();
                _currentLine = new List<ColoredChar>();
                _isRedrawing = false;
                _charsSinceDisplay = 0;
            }
            catch
            {
                _cursorX = 0;
                _cursorY = 0;
                _scrollOffset = 0;
                _screenBuffer.Clear();
                _currentLine = new List<ColoredChar>();
                _isRedrawing = false;
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
                int visibleY = _cursorY - _scrollOffset;
                visibleY = Math.Max(0, Math.Min(visibleY, MaxHeight));
                Console.SetCursorPosition(Math.Max(0, Math.Min(_cursorX, MaxWidth)), visibleY);
            }
            catch { }
        }

        public static void ReadKey() => Console.ReadKey();

        // === SCROLLING (KEPT FOR FUTURE USE) ===
        private static void ScrollUp()
        {
            if (_isRedrawing) return;
            try
            {
                _scrollOffset++;
                if (_scrollOffset > _screenBuffer.Count)
                    _scrollOffset = Math.Max(0, _screenBuffer.Count - 1);
                RedrawScreen();
            }
            catch
            {
                _isRedrawing = false;
            }
        }

        private static void DrawOptimizedLine(List<ColoredChar> line, int screenY, Canvas canvas, Font font)
        {
            if (line.Count == 0) return;

            int x = 0;
            int i = 0;

            while (i < line.Count && x < MaxWidth)
            {
                var currentChar = line[i];
                var sb = new StringBuilder();
                sb.Append(currentChar.Character);

                int j = i + 1;
                while (j < line.Count &&
                       x + (j - i) < MaxWidth &&
                       line[j].Foreground.ToArgb() == currentChar.Foreground.ToArgb() &&
                       line[j].Background.ToArgb() == currentChar.Background.ToArgb())
                {
                    sb.Append(line[j].Character);
                    j++;
                }

                try
                {
                    string text = sb.ToString();
                    int px = x * font.Width;
                    int py = screenY * font.Height;

                    if (px >= 0 && py >= 0 &&
                        px + (text.Length * font.Width) <= DisplaySettings.ScreenWidth &&
                        py + font.Height <= DisplaySettings.ScreenHeight)
                    {
                        var pen = new Pen(currentChar.Foreground);
                        var bgPen = new Pen(currentChar.Background);

                        canvas.DrawFilledRectangle(bgPen, px, py, text.Length * font.Width, font.Height);
                        canvas.DrawString(text, font, pen, px, py);
                    }
                }
                catch { }

                x += (j - i);
                i = j;
            }
        }

        private static void RedrawScreen()
        {
            if (_isRedrawing) return;
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;
            if (canvas == null || font == null) return;

            try
            {
                _isRedrawing = true;
                canvas.Clear(DefaultBackground);

                int screenY = 0;
                int startLine = _scrollOffset;
                int endLine = Math.Min(_scrollOffset + MaxHeight + 1, _screenBuffer.Count);

                for (int i = startLine; i < endLine && screenY <= MaxHeight; i++)
                {
                    if (i >= 0 && i < _screenBuffer.Count)
                    {
                        DrawOptimizedLine(_screenBuffer[i], screenY, canvas, font);
                        screenY++;
                    }
                }

                int currentLineAbsoluteY = _screenBuffer.Count;
                if (currentLineAbsoluteY >= _scrollOffset &&
                    currentLineAbsoluteY <= _scrollOffset + MaxHeight &&
                    _currentLine.Count > 0)
                {
                    int currentLineScreenY = currentLineAbsoluteY - _scrollOffset;
                    DrawOptimizedLine(_currentLine, currentLineScreenY, canvas, font);
                }

                canvas.Display();
                UpdateCursorPosition();
            }
            catch { }
            finally { _isRedrawing = false; }
        }

        private static void CheckAndScroll()
        {
            if (_isRedrawing) return;
            try
            {
                int absoluteLine = _screenBuffer.Count;
                int visibleLine = absoluteLine - _scrollOffset;
                int scrollCount = 0;
                while (visibleLine > MaxHeight - SCROLL_THRESHOLD && scrollCount < 10)
                {
                    ScrollUp();
                    visibleLine = absoluteLine - _scrollOffset;
                    scrollCount++;
                }
            }
            catch { _scrollOffset = Math.Max(0, _screenBuffer.Count - MaxHeight); }
        }

        // === TEMPORARY SCROLL REPLACEMENT ===
        private static void PauseAndClear()
        {
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;
            if (canvas == null || font == null) return;

            try
            {
                string message = "[Press any key to Continue...]";
                var greenPen = new Pen(Color.Green);
                var bgPen = new Pen(DefaultBackground);

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
            int absoluteLine = _screenBuffer.Count;
            int visibleLine = absoluteLine - _scrollOffset;

            if (visibleLine > MaxHeight - SCROLL_THRESHOLD)
            {
                PauseAndClear();
            }
        }

        // === ESCAPE SEQUENCE PROCESSING ===
        public static string ProcessEscapeSequences(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var result = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\\' && i + 1 < input.Length)
                {
                    switch (input[i + 1])
                    {
                        case 'n':
                            result.Append('\n');
                            i++;
                            break;
                        case 't':
                            result.Append('\t');
                            i++;
                            break;
                        case 'r':
                            result.Append('\r');
                            i++;
                            break;
                        case '\\':
                            result.Append('\\');
                            i++;
                            break;
                        default:
                            result.Append(input[i]);
                            break;
                    }
                }
                else
                {
                    result.Append(input[i]);
                }
            }
            return result.ToString();
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
            var sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                char mapped = MapToCodePage437(c);
                if (mapped != '\0')
                    sb.Append(mapped);
            }
            return sb.ToString();
        }

        // === TEXT OUTPUT ===
        private static void WriteChar(char c, Color? foreground = null, Color? background = null, bool batchMode = false)
        {
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;
            if (canvas == null || font == null) return;

            var fg = foreground ?? DefaultForeground;
            var bg = background ?? DefaultBackground;

            try
            {
                if (c == '\n')
                {
                    _screenBuffer.Add(new List<ColoredChar>(_currentLine));
                    _currentLine = new List<ColoredChar>();
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
                    _screenBuffer.Add(new List<ColoredChar>(_currentLine));
                    _currentLine = new List<ColoredChar>();
                    _cursorX = 0;
                    _cursorY++;
                }

                _currentLine.Add(new ColoredChar(c, fg, bg));

                int absoluteY = _screenBuffer.Count;
                int visibleY = absoluteY - _scrollOffset;

                if (visibleY >= 0 && visibleY <= MaxHeight && !_isRedrawing)
                {
                    int px = _cursorX * font.Width;
                    int py = visibleY * font.Height;
                    if (px >= 0 && py >= 0 &&
                        px + font.Width <= DisplaySettings.ScreenWidth &&
                        py + font.Height <= DisplaySettings.ScreenHeight)
                    {
                        var pen = new Pen(fg);
                        var bgPen = new Pen(bg);
                        canvas.DrawFilledRectangle(bgPen, px, py, font.Width, font.Height);
                        canvas.DrawString(c.ToString(), font, pen, px, py);
                        if (batchMode)
                        {
                            _charsSinceDisplay++;
                            if (_charsSinceDisplay >= BATCH_DISPLAY_THRESHOLD)
                            {
                                canvas.Display();
                                _charsSinceDisplay = 0;
                            }
                        }
                        else canvas.Display();
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
                bool useBatchMode = text.Length > BATCH_DISPLAY_THRESHOLD;
                foreach (char c in text)
                    WriteChar(c, foreground, background, useBatchMode);

                if (useBatchMode && _charsSinceDisplay > 0)
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
            var pen = new Pen(DefaultForeground);
            var bgPen = new Pen(DefaultBackground);

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
                        _screenBuffer.Add(new List<ColoredChar>(_currentLine));
                        _currentLine = new List<ColoredChar>();
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
                                    int visibleY = _screenBuffer.Count - _scrollOffset;
                                    if (visibleY >= 0 && visibleY <= MaxHeight)
                                    {
                                        canvas.DrawFilledRectangle(bgPen,
                                            _cursorX * font.Width,
                                            visibleY * font.Height,
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
            return input.ToString();
        }

        public static string ReadPassword(string prompt = "Password: ", Color? promptColor = null, char maskChar = '*')
        {
            Write(prompt, promptColor);
            var password = new StringBuilder();
            var canvas = DisplaySettings.Canvas;
            var font = DisplaySettings.Font;
            var pen = new Pen(DefaultForeground);
            var bgPen = new Pen(DefaultBackground);

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
                        _screenBuffer.Add(new List<ColoredChar>(_currentLine));
                        _currentLine = new List<ColoredChar>();
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
                                    int visibleY = _screenBuffer.Count - _scrollOffset;
                                    if (visibleY >= 0 && visibleY <= MaxHeight)
                                    {
                                        canvas.DrawFilledRectangle(bgPen,
                                            _cursorX * font.Width,
                                            visibleY * font.Height,
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
            return password.ToString();
        }
    }
}