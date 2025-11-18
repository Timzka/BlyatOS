using System;
using System.IO;
using BlyatOS.Library.Helpers;
using Cosmos.HAL.Drivers.Audio;
using Cosmos.System.Audio;
using Cosmos.System.Audio.IO;

namespace BlyatOS.Library.Ressources
{
    public static class AudioHandler
    {
        private static AudioMixer? mixer;
        private static Cosmos.System.Audio.AudioManager? manager;
        private static AC97? driver;
        private static MemoryAudioStream? currentStream;
        private static bool isInitialized = false;
        private static bool _debug = false;

        /// <summary>
        /// Initialisiert den AudioManager. Muss einmal beim Start aufgerufen werden.
        /// </summary>
        public static bool Initialize(bool debug = false)
        {
            try
            {
                if (isInitialized)
                    return true;

                // AudioMixer erstellen
                mixer = new AudioMixer();

                // AC97 Driver initialisieren
                driver = AC97.Initialize(bufferSize: 4096);

                // AudioManager erstellen und konfigurieren
                manager = new Cosmos.System.Audio.AudioManager()
                {
                    Stream = mixer,
                    Output = driver
                };

                // AudioManager aktivieren
                manager.Enable();

                isInitialized = true;
                if (debug)
                {
                    _debug = true;
                    ConsoleHelpers.WriteLine("[AudioManager] Erfolgreich initialisiert");
                }
                return true;
            }
            catch (Exception ex)
            {
                ConsoleHelpers.WriteLine($"[AudioManager] Fehler bei Initialisierung: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Spielt einen Audio-Stream ab
        /// </summary>
        public static void Play(MemoryAudioStream stream)
        {
            if (!isInitialized || mixer == null)
            {
                if(_debug)ConsoleHelpers.WriteLine("[AudioManager] Nicht initialisiert!");
                return;
            }

            try
            {
                Stop(); // Stoppt alle musik

                currentStream = stream;
                stream.Position = 0;
                mixer.Streams.Add(stream);
                if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Wiedergabe gestartet");
            }
            catch (Exception ex)
            {
                if (_debug) ConsoleHelpers.WriteLine($"[AudioManager] Fehler beim Abspielen: {ex.Message}");
            }
        }

        public static void Add(MemoryAudioStream stream)
        {
            if (!isInitialized || mixer == null)
            {
                if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Nicht initialisiert!");
                return;
            }

            try
            {
                currentStream = stream;
                stream.Position = 0;
                mixer.Streams.Add(stream);
                if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Wiedergabe gestartet");
            }
            catch (Exception ex)
            {
                if (_debug) ConsoleHelpers.WriteLine($"[AudioManager] Fehler beim Abspielen: {ex.Message}");
            }
        }
        /// <summary>
        /// Stoppt die aktuelle Wiedergabe
        /// </summary>
        public static void Stop()
        {
            if (!isInitialized || mixer == null)
                return;

            try
            {
                if (currentStream != null && mixer.Streams.Count != 0)
                {
                    mixer.Streams.Clear();
                    currentStream = null;
                    if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Wiedergabe gestoppt");
                }
            }
            catch (Exception ex)
            {
                if (_debug) ConsoleHelpers.WriteLine($"[AudioManager] Fehler beim Stoppen: {ex.Message}");
            }
        }

        /// <summary>
        /// Stoppt alle Streams
        /// </summary>
        public static void StopAll()
        {
            if (!isInitialized || mixer == null)
                return;

            try
            {
                mixer.Streams.Clear();
                currentStream = null;
                if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Alle Streams gestoppt");
            }
            catch (Exception ex)
            {
                if (_debug) ConsoleHelpers.WriteLine($"[AudioManager] Fehler beim Stoppen: {ex.Message}");
            }
        }

        /// <summary>
        /// Prüft ob gerade etwas abgespielt wird
        /// </summary>
        public static bool IsPlaying => mixer != null && mixer.Streams.Count > 0;

        /// <summary>
        /// Prüft ob AudioManager initialisiert ist
        /// </summary>
        public static bool IsInitialized => isInitialized;

        /// <summary>
        /// Gibt den AudioMixer zurück (für erweiterte Nutzung)
        /// </summary>
        public static AudioMixer? GetMixer() => mixer;
    }
}