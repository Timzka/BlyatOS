// Paste in eine Hilfsklasse in deinem Kernel (Cosmos)
using System;
using Cosmos.Core;

namespace BlyatOS.Library;

public static class VGACursorFix
{
    private const ushort INDEX_PORT = 0x3D4;
    private const ushort DATA_PORT = 0x3D5;
    private static byte _savedStart = 0;
    private static byte _savedEnd = 0;
    private static bool _saved = false;

    // Debug / tracing additions
    private static bool _debug = false;
    private static int _opCount = 0;
    private static bool _inLog = false;

    public static void EnableDebug(bool enabled = true)
    {
        _debug = enabled;
        SafeLog("Debug " + (enabled ? "ENABLED" : "DISABLED"));
    }

    private static void SafeLog(string msg)
    {
        if (!_debug || _inLog)
            return;
        try
        {
            _inLog = true;
            Console.WriteLine("[VGACursorFix] " + msg);
        }
        catch
        {
            // Ignore logging failures (e.g., early boot before Console ready)
        }
        finally
        {
            _inLog = false;
        }
    }

    // Helper class to access protected static methods
    private class IOPortAccess : IOPortBase
    {
        public IOPortAccess(ushort port) : base(port) { }

        // Add 'new' to explicitly hide the inherited protected static members (fixes CS0108 warnings)
        public new static void Write8(ushort port, byte data)
        {
            IOPortBase.Write8(port, data);
        }

        public new static byte Read8(ushort port)
        {
            return IOPortBase.Read8(port);
        }
    }

    private static byte ReadRegister(byte index)
    {
        IOPortAccess.Write8(INDEX_PORT, index);
        byte value = IOPortAccess.Read8(DATA_PORT);
        _opCount++;
        SafeLog("READ idx=0x" + index.ToString("X2") + " -> 0x" + value.ToString("X2") + " (op " + _opCount + ")");
        return value;
    }

    private static void WriteRegister(byte index, byte value)
    {
        IOPortAccess.Write8(INDEX_PORT, index);
        IOPortAccess.Write8(DATA_PORT, value);
        _opCount++;
        SafeLog("WRITE idx=0x" + index.ToString("X2") + " val=0x" + value.ToString("X2") + " (op " + _opCount + ")");
    }

    public static void HideCursor()
    {
        try
        {
            // save current start/end if not yet saved
            if (!_saved)
            {
                _savedStart = ReadRegister(0x0A);
                _savedEnd = ReadRegister(0x0B);
                _saved = true;
                SafeLog("Saved start=0x" + _savedStart.ToString("X2") + " end=0x" + _savedEnd.ToString("X2"));
            }
            // set bit 5 (cursor disable)
            var start = (byte)(ReadRegister(0x0A) | 0x20);
            WriteRegister(0x0A, start);
            SafeLog("Cursor hidden (start reg now 0x" + start.ToString("X2") + ")");
        }
        catch (Exception ex)
        {
            SafeLog("Exception in HideCursor: " + ex.Message);
            SafeLog(ex.GetType().FullName);
        }
    }

    public static void ShowCursor()
    {
        try
        {
            // clear bit 5 (enable cursor)
            var start = (byte)(ReadRegister(0x0A) & ~0x20);
            WriteRegister(0x0A, start);

            // restore saved end scanline (if we have it)
            if (_saved)
            {
                WriteRegister(0x0B, _savedEnd);
            }

            // Guard: Console properties may not be valid early in boot
            int x = 0, y = 0, width = 80;
            try
            {
                x = Console.CursorLeft;
                y = Console.CursorTop;
                width = Console.BufferWidth;
            }
            catch
            {
                // Fallback defaults
            }

            int pos = y * width + x;
            WriteRegister(0x0E, (byte)((pos >> 8) & 0xFF));
            WriteRegister(0x0F, (byte)(pos & 0xFF));
            SafeLog("Cursor shown at x=" + x + " y=" + y + " pos=" + pos);
        }
        catch (Exception ex)
        {
            SafeLog("Exception in ShowCursor: " + ex.Message);
            SafeLog(ex.GetType().FullName);
        }
    }
}