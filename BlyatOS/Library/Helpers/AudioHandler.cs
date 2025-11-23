using System;
using System.IO;
using BlyatOS.Library.Helpers;
using Cosmos.HAL.Audio;
using Cosmos.HAL.Drivers.Audio;
using Cosmos.System.Audio;
using Cosmos.System.Audio.IO;
using CosmosAudioInfrastructure.HAL.Drivers.PCI.Audio;

namespace BlyatOS.Library.Ressources
{
    public enum AudioDriverType
    {
        AC97,
        //SoundBlaster16, //doesnt work as of now
        Auto
    }

    public static class AudioHandler
    {
        private static AudioMixer? mixer;
        private static Cosmos.System.Audio.AudioManager? manager;
        private static AC97? ac97Driver;
        private static SoundBlaster16? sb16Driver;
        private static MemoryAudioStream? currentStream; 
        private static bool isInitialized = false;
        private static bool _debug = false;
        private static AudioDriverType currentDriver;

        /// <summary>
        /// Initialisiert den AudioManager mit automatischer Treibererkennung oder manuellem Treiber
        /// </summary>
        public static bool Initialize(AudioDriverType driverType = AudioDriverType.Auto, bool debug = false)
        {
            try
            {
                if (isInitialized)
                    return true;

                _debug = debug;

                // AudioMixer erstellen
                mixer = new AudioMixer();

                // Treiber initialisieren basierend auf Typ
                bool driverInitialized = false;

                if (driverType == AudioDriverType.Auto || driverType == AudioDriverType.AC97)
                {
                    try
                    {
                        if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Versuche AC97 zu initialisieren...");
                        ac97Driver = AC97.Initialize(bufferSize: 4096);

                        manager = new Cosmos.System.Audio.AudioManager()
                        {
                            Stream = mixer,
                            Output = ac97Driver
                        };

                        // BufferProvider wird vom AudioManager gesetzt
                        currentDriver = AudioDriverType.AC97;
                        driverInitialized = true;
                        if (_debug) ConsoleHelpers.WriteLine("[AudioManager] AC97 erfolgreich initialisiert");
                    }
                    catch (Exception ex)
                    {
                        if (_debug) ConsoleHelpers.WriteLine($"[AudioManager] AC97 nicht verfügbar: {ex.Message}");
                    }
                }

                // Falls AC97 fehlschlägt oder SB16 explizit gewünscht
                //if (!driverInitialized && (driverType == AudioDriverType.Auto || driverType == AudioDriverType.SoundBlaster16))
                //{
                //    try
                //    {
                //        if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Versuche Sound Blaster 16 zu initialisieren...");

                //        // Versuche zuerst Auto-Detection
                //        ushort? baseAddr = 0x220;//SoundBlaster16.DetectBaseAddress();
                //        if (baseAddr == null && driverType == AudioDriverType.SoundBlaster16)
                //        {
                //            // Wenn explizit gewünscht, versuche Standardadresse
                //            if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Auto-Detection fehlgeschlagen, versuche 0x220...");
                //            baseAddr = 0x220;
                //        }

                //        if (baseAddr != null)
                //        {
                //            // SB16 mit 16-bit Stereo signed initialisieren (ähnlich wie AC97)
                //            sb16Driver = SoundBlaster16.Initialize(
                //                bufferSize: 4096,
                //                format: new SampleFormat(AudioBitDepth.Bits16, 2, true),
                //                baseAddress: baseAddr.Value
                //            );

                //            manager = new Cosmos.System.Audio.AudioManager()
                //            {
                //                Stream = mixer,
                //                Output = sb16Driver
                //            };

                //            // BufferProvider wird vom AudioManager gesetzt
                //            currentDriver = AudioDriverType.SoundBlaster16;
                //            driverInitialized = true;
                //            if (_debug) ConsoleHelpers.WriteLine($"[AudioManager] Sound Blaster 16 erfolgreich initialisiert (DSP Version: {sb16Driver.DSPVersion})");
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        if (_debug) ConsoleHelpers.WriteLine($"[AudioManager] Sound Blaster 16 nicht verfügbar: {ex.Message}");
                //    }
                //}

                if (!driverInitialized)
                {
                    if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Kein Audio-Treiber verfügbar!");
                    return false;
                }

                // AudioManager aktivieren - dies setzt automatisch den BufferProvider
                if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Aktiviere AudioManager...");

                try
                {
                    manager.Enable();
                    if (_debug) ConsoleHelpers.WriteLine("[AudioManager] AudioManager erfolgreich aktiviert!");
                }
                catch (Exception ex)
                {
                    if (_debug) ConsoleHelpers.WriteLine($"[AudioManager] Fehler beim Aktivieren: {ex.Message}");
                    return false;
                }

                isInitialized = true;
                if (debug)
                {
                    //ConsoleHelpers.WriteLine($"[AudioManager] ✓ Erfolgreich initialisiert mit {currentDriver}");
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
        /// Gibt den aktuell verwendeten Treiber zurück
        /// </summary>
        public static AudioDriverType CurrentDriver => currentDriver;

        /// <summary>
        /// Spielt einen Audio-Stream ab
        /// </summary>
        public static void Play(MemoryAudioStream stream)
        {
            if (!isInitialized || mixer == null)
            {
                if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Nicht initialisiert!");
                return;
            }

            try
            {
                Stop(); // Stoppt alle Musik

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
                if (_debug) ConsoleHelpers.WriteLine("[AudioManager] Stream hinzugefügt");
            }
            catch (Exception ex)
            {
                if (_debug) ConsoleHelpers.WriteLine($"[AudioManager] Fehler beim Hinzufügen: {ex.Message}");
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

        /// <summary>
        /// Gibt Informationen über den aktuellen Treiber aus
        /// </summary>
        public static string GetDriverInfo()
        {
            if (!isInitialized)
                return "Nicht initialisiert";

            switch (currentDriver)
            {
                case AudioDriverType.AC97:
                    return "AC97 Audio Controller";
                //case AudioDriverType.SoundBlaster16:
                //    return $"Sound Blaster 16 (DSP v{sb16Driver?.DSPVersion})";
                default:
                    return "Unbekannt";
            }
        }
    }
}