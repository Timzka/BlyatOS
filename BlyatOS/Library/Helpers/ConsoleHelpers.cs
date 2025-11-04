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
        // Character with color information
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
        private static int _cursorY; // Absolute line number
        private static int _scrollOffset;

        private const int SCROLL_THRESHOLD = 2;
        private const int BATCH_DISPLAY_THRESHOLD = 50; // Display after this many characters
        private static readonly List<List<ColoredChar>> _screenBuffer = new();
        private static List<ColoredChar> _currentLine = new();

        private static bool _isRedrawing = false;
        private static int _charsSinceDisplay = 0; // Track characters for batch display

        private static int MaxWidth => (int)(DisplaySettings.ScreenWidth / DisplaySettings.Font.Width) - 1;
        private static int MaxHeight => (int)(DisplaySettings.ScreenHeight / DisplaySettings.Font.Height) - 1;

        // === BASIC CONSOLE CONTROL ===

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
            catch (Exception ex)
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
            catch
            {
                // Ignore cursor position errors
            }
        }

        public static void ReadKey()
        {
            Console.ReadKey();
        }

        // === SCROLLING ===

        private static void ScrollUp()
        {
            if (_isRedrawing) return;

            try
            {
                _scrollOffset++;

                if (_scrollOffset > _screenBuffer.Count)
                {
                    _scrollOffset = Math.Max(0, _screenBuffer.Count - 1);
                }

                RedrawScreen();
            }
            catch (Exception ex)
            {
                _isRedrawing = false;
                try
                {
                    var canvas = DisplaySettings.Canvas;
                    canvas?.Clear(DefaultBackground);
                    canvas?.Display();
                }
                catch { }
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

                // Draw buffered lines in batch
                for (int i = startLine; i < endLine && screenY <= MaxHeight; i++)
                {
                    if (i >= 0 && i < _screenBuffer.Count)
                    {
                        var line = _screenBuffer[i];
                        int x = 0;

                        foreach (var coloredChar in line)
                        {
                            if (x >= MaxWidth) break;

                            try
                            {
                                var pen = new Pen(coloredChar.Foreground);
                                var bgPen = new Pen(coloredChar.Background);

                                int px = x * font.Width;
                                int py = screenY * font.Height;

                                if (px >= 0 && py >= 0 &&
                                    px + font.Width <= DisplaySettings.ScreenWidth &&
                                    py + font.Height <= DisplaySettings.ScreenHeight)
                                {
                                    canvas.DrawFilledRectangle(bgPen, px, py, font.Width, font.Height);
                                    canvas.DrawString(coloredChar.Character.ToString(), font, pen, px, py);
                                }
                            }
                            catch { }

                            x++;
                        }

                        screenY++;
                    }
                }

                // Draw current line
                int currentLineAbsoluteY = _screenBuffer.Count;
                if (currentLineAbsoluteY >= _scrollOffset &&
                    currentLineAbsoluteY <= _scrollOffset + MaxHeight &&
                    _currentLine.Count > 0)
                {
                    int currentLineScreenY = currentLineAbsoluteY - _scrollOffset;
                    int x = 0;

                    foreach (var coloredChar in _currentLine)
                    {
                        if (x >= MaxWidth) break;

                        try
                        {
                            var pen = new Pen(coloredChar.Foreground);
                            var bgPen = new Pen(coloredChar.Background);

                            int px = x * font.Width;
                            int py = currentLineScreenY * font.Height;

                            if (px >= 0 && py >= 0 &&
                                px + font.Width <= DisplaySettings.ScreenWidth &&
                                py + font.Height <= DisplaySettings.ScreenHeight)
                            {
                                canvas.DrawFilledRectangle(bgPen, px, py, font.Width, font.Height);
                                canvas.DrawString(coloredChar.Character.ToString(), font, pen, px, py);
                            }
                        }
                        catch { }

                        x++;
                    }
                }

                canvas.Display();
                UpdateCursorPosition();
            }
            catch (Exception ex)
            {
                try
                {
                    canvas.Clear(DefaultBackground);
                    canvas.Display();
                }
                catch { }
            }
            finally
            {
                _isRedrawing = false;
            }
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
            catch
            {
                _scrollOffset = Math.Max(0, _screenBuffer.Count - MaxHeight);
            }
        }

        // Ensure cursor is visible after write operations
        private static void EnsureCursorVisible()
        {
            int absoluteLine = _screenBuffer.Count;
            int visibleLine = absoluteLine - _scrollOffset;

            // If cursor is not visible, scroll to make it visible
            while (visibleLine > MaxHeight && _scrollOffset < _screenBuffer.Count)
            {
                _scrollOffset++;
                visibleLine = absoluteLine - _scrollOffset;
            }

            if (visibleLine > MaxHeight)
            {
                RedrawScreen();
            }
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
                // Handle newline
                if (c == '\n')
                {
                    _screenBuffer.Add(new List<ColoredChar>(_currentLine));
                    _currentLine = new List<ColoredChar>();
                    _cursorX = 0;
                    _cursorY++;

                    // Force display on newline
                    if (batchMode)
                    {
                        canvas.Display();
                        _charsSinceDisplay = 0;
                    }

                    CheckAndScroll();
                    UpdateCursorPosition();
                    return;
                }

                // Handle line wrap
                if (_cursorX >= MaxWidth)
                {
                    _screenBuffer.Add(new List<ColoredChar>(_currentLine));
                    _currentLine = new List<ColoredChar>();
                    _cursorX = 0;
                    _cursorY++;
                    CheckAndScroll();
                }

                // Add to current line
                _currentLine.Add(new ColoredChar(c, fg, bg));

                // Draw if visible
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

                        // Batch display: only update screen every N characters
                        if (batchMode)
                        {
                            _charsSinceDisplay++;
                            if (_charsSinceDisplay >= BATCH_DISPLAY_THRESHOLD)
                            {
                                canvas.Display();
                                _charsSinceDisplay = 0;
                            }
                        }
                        else
                        {
                            canvas.Display();
                        }
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

            var canvas = DisplaySettings.Canvas;
            if (canvas == null) return;

            try
            {
                // Use batch mode for longer texts
                bool useBatchMode = text.Length > BATCH_DISPLAY_THRESHOLD;

                foreach (char c in text)
                {
                    WriteChar(c, foreground, background, useBatchMode);
                }

                // Final display if in batch mode
                if (useBatchMode && _charsSinceDisplay > 0)
                {
                    canvas.Display();
                    _charsSinceDisplay = 0;
                }

                // Ensure cursor/input prompt is visible after write
                EnsureCursorVisible();
            }
            catch
            {
                // Continue even if some characters fail
            }
        }

        public static void Write(string text, Color? foreground = null, Color? background = null) =>
            WriteStyled(text, foreground, background);

        public static void WriteLine(string text = "", Color? foreground = null, Color? background = null)
        {
            WriteStyled(text + "\n", foreground, background);
        }

        // === INPUT WITH GERMAN CHARACTERS ===

        // German keyboard layout mapping
        private static char GetGermanChar(Sys.KeyEvent key)
        {
            // Handle German umlauts and special characters
            if (key.Key == Sys.ConsoleKeyEx.NoName)
            {
                // Check for German specific keys by scancode or KeyChar
                switch (key.KeyChar)
                {
                    case 'ä': return 'ä';
                    case 'Ä': return 'Ä';
                    case 'ö': return 'ö';
                    case 'Ö': return 'Ö';
                    case 'ü': return 'ü';
                    case 'Ü': return 'Ü';
                    case 'ß': return 'ß';
                }
            }

            // Standard character
            return key.KeyChar;
        }

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
                        CheckAndScroll();
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
                                {
                                    _currentLine.RemoveAt(_currentLine.Count - 1);
                                }

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
                        char inputChar = GetGermanChar(key);

                        if (inputChar != 0 && !char.IsControl(inputChar))
                        {
                            WriteChar(inputChar);
                            input.Append(inputChar);
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
                        CheckAndScroll();
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
                                {
                                    _currentLine.RemoveAt(_currentLine.Count - 1);
                                }

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
                        char inputChar = GetGermanChar(key);

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