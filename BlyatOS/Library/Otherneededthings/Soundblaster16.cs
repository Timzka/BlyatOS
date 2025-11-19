using Cosmos.Core;
using Cosmos.Core.IOGroup;
using Cosmos.HAL;
using Cosmos.HAL.Audio;
using Cosmos.HAL.Drivers.Audio;
using System;

namespace CosmosAudioInfrastructure.HAL.Drivers.PCI.Audio
{
    /// <summary>
    /// Handles Sound Blaster 16 sound cards at a low-level.
    /// </summary>
    public unsafe sealed class SoundBlaster16 : AudioDriver
    {
        // Sicherere Speicheradresse (1MB Marke)
        private const int AUDIO_BUFFER_ADDRESS = 0x100000;

        AudioBuffer backBuffer;
        AudioBuffer buffer;
        ushort bufferSize;

        // Port-Adressen (keine IOPort-Instanzen mehr!)
        readonly ushort pMixer;
        readonly ushort pMixerData;
        readonly ushort pReset;
        readonly ushort pRead;
        readonly ushort pWrite;
        readonly ushort p8BitIRQAck;
        readonly ushort p16BitIRQAck;

        const ushort pPICInterruptAcknowledge = 0x20;
        const ushort pPICExtendedInterruptAcknowledge = 0xA0;

        const int DSP_SET_TIME_CONSTANT = 0x40;
        const int DSP_SET_OUTPUT_SAMPLE_RATE = 0x41;
        const int DSP_TURN_SPEAKER_ON = 0xD1;
        const int DSP_TURN_SPEAKER_OFF = 0xD3;
        const int DSP_STOP_8BIT_CHANNEL = 0xD0;
        const int DSP_RESUME_8BIT_CHANNEL = 0xD4;
        const int DSP_STOP_16BIT_CHANNEL = 0xD5;
        const int DSP_RESUME_16BIT_CHANNEL = 0xD6;
        const int DSP_GET_VERSION = 0xE1;

        const int MIXER_SET_MASTER_VOLUME = 0x22;
        const int MIXER_SET_IRQ = 0x80;

        const byte MIXER_IRQ_2 = 0x01;
        const byte MIXER_IRQ_5 = 0x02;
        const byte MIXER_IRQ_7 = 0x04;
        const byte MIXER_IRQ_10 = 0x08;

        public override IAudioBufferProvider BufferProvider { get; set; }

        /// <summary>
        /// The sampling rate of the driver.
        /// </summary>
        public ushort SampleRate { get; set; } = 44100;

        /// <summary>
        /// The version of the Digital Signal Processor of the Sound Blaster 16. 
        /// </summary>
        public Version DSPVersion { get; private set; }

        private bool enabled;
        public override bool Enabled => enabled;

        private SoundBlaster16(ushort bufferSize, SampleFormat format, ushort baseAddress = 0x220)
        {
            this.bufferSize = bufferSize;

            // Ports mit Base-Adresse initialisieren
            pMixer = (ushort)(baseAddress + 0x04);
            pMixerData = (ushort)(baseAddress + 0x05);
            pReset = (ushort)(baseAddress + 0x06);
            pRead = (ushort)(baseAddress + 0x0A);
            pWrite = (ushort)(baseAddress + 0x0C);
            p8BitIRQAck = (ushort)(baseAddress + 0x0E);
            p16BitIRQAck = (ushort)(baseAddress + 0x0F);

            Console.WriteLine($"[SB16] Base-Adresse: 0x{baseAddress:X3}");

            if (!ResetDSP())
                throw new InvalidOperationException("No Sound Blaster 16 device could be found - the DSP reset check has failed.");

            Console.WriteLine("[SB16] DSP wurde zurückgesetzt.");

            // Get DSP Version
            IOPort.Write8(pWrite, DSP_GET_VERSION);
            byte versionMajor = IOPort.Read8(pRead);
            byte versionMinor = IOPort.Read8(pRead);
            DSPVersion = new Version(versionMajor, versionMinor);
            Console.WriteLine($"[SB16] DSP Version: {DSPVersion}");

            // Set the IRQ the SB16 will trigger
            IOPort.Write8(pMixer, MIXER_SET_IRQ);
            IOPort.Write8(pMixerData, MIXER_IRQ_10);

            // Set the IRQ handler itself
            INTs.SetIrqHandler(10, HandleInterrupt);

            // Create the buffer
            SetSampleFormat(format);
        }

        /// <summary>
        /// The global instance of the Sound Blaster 16 driver.
        /// </summary>
        public static SoundBlaster16 Instance { get; private set; } = null;

        /// <summary>
        /// Initializes the Sound Blaster 16 driver.
        /// </summary>
        public static SoundBlaster16 Initialize(ushort bufferSize, SampleFormat format, ushort baseAddress = 0x220)
        {
            if (Instance != null)
            {
                if (Instance.bufferSize != bufferSize)
                {
                    Instance.ChangeBufferSize(bufferSize);
                }
                return Instance;
            }

            Instance = new SoundBlaster16(bufferSize, format, baseAddress);
            return Instance;
        }

        /// <summary>
        /// Testet verschiedene Base-Adressen
        /// </summary>
        public static ushort? DetectBaseAddress()
        {
            ushort[] possibleAddresses = { 0x220, 0x240, 0x260, 0x280 };

            Console.WriteLine("[SB16] Suche nach Sound Blaster 16...");

            foreach (var baseAddr in possibleAddresses)
            {
                Console.WriteLine($"[SB16] Teste Base-Adresse: 0x{baseAddr:X3}");

                try
                {
                    ushort resetPort = (ushort)(baseAddr + 0x06);
                    ushort readPort = (ushort)(baseAddr + 0x0A);

                    // Reset versuchen
                    IOPort.Write8(resetPort, 1);
                    Cosmos.HAL.Global.PIT.Wait(3);
                    IOPort.Write8(resetPort, 0);

                    // Warte auf Ready-Signal
                    for (int i = 0; i < 100; i++)
                    {
                        if (IOPort.Read8(readPort) == 0xAA)
                        {
                            Console.WriteLine($"[SB16] ✓ Gefunden bei 0x{baseAddr:X3}!");
                            return baseAddr;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SB16] ✗ Fehler bei 0x{baseAddr:X3}: {ex.Message}");
                }
            }

            Console.WriteLine("[SB16] Kein Sound Blaster 16 gefunden!");
            return null;
        }

        private void HandleInterrupt(ref INTs.IRQContext aContext)
        {
            if (!enabled)
            {
                IOPort.Write8(pWrite, 0xDA); // stop any 8-bit output
                IOPort.Write8(pWrite, 0xD9); // stop any 16-bit output
                return;
            }

            // Acknowledge IRQ
            if (buffer.Format.BitDepth == AudioBitDepth.Bits8)
            {
                IOPort.Read8(p8BitIRQAck);
            }
            else
            {
                IOPort.Read8(p16BitIRQAck);
            }

            // Copy back buffer to main buffer
            fixed (byte* backBufferPtr = backBuffer.RawData)
            {
                fixed (byte* bufferPtr = buffer.RawData)
                {
                    MemoryOperations.Copy(
                        bufferPtr,
                        backBufferPtr,
                        buffer.RawData.Length
                    );
                }
            }

            // Request new buffer from provider
            BufferProvider?.RequestBuffer(backBuffer);
        }

        public void ChangeBufferSize(ushort newSize)
        {
            Disable();

            bufferSize = newSize;
            buffer = new AudioBuffer(newSize, buffer.Format);
            backBuffer = new AudioBuffer(newSize, buffer.Format);

            Enable();
        }

        public override void Enable()
        {
            // Set max volume
            IOPort.Write8(pMixer, MIXER_SET_MASTER_VOLUME);
            IOPort.Write8(pMixerData, 0xFF);

            enabled = true;
            Start();
            Console.WriteLine("[SB16] Aktiviert.");
        }

        public override void Disable()
        {
            IOPort.Write8(pWrite, DSP_STOP_16BIT_CHANNEL);
            IOPort.Write8(pWrite, DSP_STOP_8BIT_CHANNEL);
            enabled = false;
            Console.WriteLine("[SB16] Deaktiviert.");
        }

        /// <summary>
        /// Resets the DSP of the Sound Blaster 16.
        /// </summary>
        private bool ResetDSP()
        {
            IOPort.Write8(pReset, 1);
            Cosmos.HAL.Global.PIT.Wait(3);
            IOPort.Write8(pReset, 0);

            return IOPort.Read8(pRead) == 0xAA;
        }

        /// <summary>
        /// Starts the data transfer operation.
        /// </summary>
        private void Start()
        {
            Console.WriteLine("[SB16] Starte Sound Blaster 16...");
            IOPort.Write8(pWrite, DSP_TURN_SPEAKER_ON);
            ProgramDMATransfer(buffer.Format.BitDepth);

            // Set the sample rate
            IOPort.Write8(pWrite, 0x41);
            IOPort.Write8(pWrite, Hi(SampleRate));
            IOPort.Write8(pWrite, Lo(SampleRate));

            // Set the bit-depth
            IOPort.Write8(pWrite, buffer.Format.BitDepth == AudioBitDepth.Bits8 ? (byte)0xC6 : (byte)0xB6);

            // Set the channel number (stereo or mono) and if it's signed.
            switch (buffer.Format.Channels)
            {
                case 1:
                    if (buffer.Format.Signed)
                        IOPort.Write8(pWrite, 0b00_01_0000);
                    else
                        IOPort.Write8(pWrite, 0b00_00_0000);
                    break;
                case 2:
                    if (buffer.Format.Signed)
                        IOPort.Write8(pWrite, 0b00_11_0000);
                    else
                        IOPort.Write8(pWrite, 0b00_10_0000);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported amount of channels ({buffer.Format.Channels}).");
            }

            // Set data size
            IOPort.Write8(pWrite, Lo((uint)bufferSize - 1));
            IOPort.Write8(pWrite, Hi((uint)bufferSize - 1));
            Console.WriteLine("[SB16] Start-Routine abgeschlossen.");
        }

        /// <summary>
        /// Programs the DMA channel for audio buffer transfer.
        /// </summary>
        private void ProgramDMATransfer(AudioBitDepth bitDepth)
        {
            switch (bitDepth)
            {
                case AudioBitDepth.Bits8:
                    ProgramDMATransfer(
                        enableDisablePort: 0x0A,
                        flipFlopPort: 0x0C,
                        transferModePort: 0x0B,
                        pagePort: 0x83,
                        addressPort: 0x02,
                        lengthPort: 0x03
                    );
                    break;
                case AudioBitDepth.Bits16:
                    ProgramDMATransfer(
                        enableDisablePort: 0xD4,
                        flipFlopPort: 0xD8,
                        transferModePort: 0xD6,
                        pagePort: 0x8B,
                        addressPort: 0xC4,
                        lengthPort: 0xC6
                    );
                    break;
                default:
                    throw new NotSupportedException("The Sound Blaster 16 only supports 8-bit and 16-bit DMA transfers.");
            }
        }

        private unsafe void ProgramDMATransfer(
            ushort enableDisablePort,
            ushort flipFlopPort,
            ushort transferModePort,
            ushort pagePort,
            ushort addressPort,
            ushort lengthPort
        )
        {
            // Fill both buffers
            BufferProvider?.RequestBuffer(buffer);
            BufferProvider?.RequestBuffer(backBuffer);

            Console.WriteLine("[SB16] Programmiere DMA-Transfer.");

            // Turn the channel off
            IOPort.Write8(enableDisablePort, 0x05);

            // Flip-flop
            IOPort.Write8(flipFlopPort, 1);

            // Set transfer-mode (auto mode, keeps playing and triggers interrupt)
            IOPort.Write8(transferModePort, (byte)(0x58 + 1));

            fixed (byte* bufferPtr = buffer.RawData)
            {
                uint address = (uint)bufferPtr;

                if (address > 0xFFFFFF)
                {
                    throw new InvalidOperationException($"The Sound Blaster 16 cannot access memory addresses beyond 0xFFFFFF (given address 0x{address:X8})");
                }

                Console.WriteLine($"[SB16] Buffer liegt bei 0x{address:X8}.");

                // Set page, address low and high
                IOPort.Write8(pagePort, (byte)(address >> 4 >> 12));
                IOPort.Write8(addressPort, Lo(address));
                IOPort.Write8(addressPort, Hi(address));
            }

            // Set length
            IOPort.Write8(lengthPort, Lo((uint)buffer.RawData.Length));
            IOPort.Write8(lengthPort, Hi((uint)buffer.RawData.Length));

            // Enable channel
            IOPort.Write8(enableDisablePort, 1);
            Console.WriteLine("[SB16] DMA wurde programmiert.");
        }

        static byte Hi(ushort val) => (byte)(val >> 8);
        static byte Hi(uint val) => (byte)((val >> 8) & 0xff);
        static byte Lo(uint val) => (byte)(val & 0xff);
        static byte Lo(ushort val) => (byte)(val & 0xff);

        public override SampleFormat[] GetSupportedSampleFormats()
            => new SampleFormat[]
            {
                new SampleFormat(AudioBitDepth.Bits8, 1, true),
                new SampleFormat(AudioBitDepth.Bits8, 2, true),
                new SampleFormat(AudioBitDepth.Bits8, 1, false),
                new SampleFormat(AudioBitDepth.Bits8, 2, false),
                new SampleFormat(AudioBitDepth.Bits16, 1, true),
                new SampleFormat(AudioBitDepth.Bits16, 2, true),
                new SampleFormat(AudioBitDepth.Bits16, 1, false),
                new SampleFormat(AudioBitDepth.Bits16, 2, false),
            };

        public override void SetSampleFormat(SampleFormat sampleFormat)
        {
            if (sampleFormat.BitDepth != AudioBitDepth.Bits8 && sampleFormat.BitDepth != AudioBitDepth.Bits16)
                throw new NotSupportedException("The Sound Blaster 16 only supports 8-bit and 16-bit audio.");

            if (sampleFormat.Channels == 0)
                throw new NotSupportedException("The Sound Blaster 16 does not support null audio output (0 channels).");

            if (sampleFormat.Channels > 2)
                throw new NotSupportedException("The Sound Blaster 16 supports up to 2 channels.");

            buffer = new AudioBuffer(bufferSize, sampleFormat);
            backBuffer = new AudioBuffer(bufferSize, sampleFormat);

            // Clear buffer
            fixed (byte* bufPtr = buffer.RawData)
            {
                MemoryOperations.Fill(bufPtr, 0x00, buffer.RawData.Length);
            }
        }
    }
}