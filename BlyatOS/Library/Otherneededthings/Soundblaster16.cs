using BlyatOS.Library.Helpers;
using Cosmos.Core;
using Cosmos.Core.Memory;
using Cosmos.HAL;
using Cosmos.HAL.Audio;
using Cosmos.HAL.Drivers.Audio;
using System;

namespace CosmosAudioInfrastructure.HAL.Drivers.PCI.Audio
{
    /// <summary>
    /// Sound Blaster 16 Treiber - Low-Memory DMA Fix
    /// </summary>
    public unsafe sealed class SoundBlaster16 : AudioDriver
    {
        private AudioBuffer transferBuffer;

        // DMA Buffer im Low-Memory (unter 16 MB)
        private const uint DMABufferAddress = 0x00100000; // 1 MB
        private const uint DMABufferMaxSize = 65536;      // 64 KB
        private byte* dmaPtr => (byte*)DMABufferAddress;

        private ushort bufferSizeSamples;
        private int bufferSizeBytes;
        private SampleFormat currentFormat;

        // Ports
        private readonly ushort pMixer;
        private readonly ushort pMixerData;
        private readonly ushort pReset;
        private readonly ushort pRead;
        private readonly ushort pWrite;
        private readonly ushort pReadStatus;
        private readonly ushort pWriteStatus;
        private readonly ushort p8BitIRQAck;
        private readonly ushort p16BitIRQAck;

        private const ushort pPICInterruptAcknowledge = 0x20;
        private const ushort pPICExtendedInterruptAcknowledge = 0xA0;

        // DSP Commands
        private const byte DSP_SET_OUTPUT_SAMPLE_RATE = 0x41;
        private const byte DSP_TURN_SPEAKER_ON = 0xD1;
        private const byte DSP_TURN_SPEAKER_OFF = 0xD3;
        private const byte DSP_STOP_8BIT = 0xD0;
        private const byte DSP_STOP_16BIT = 0xD5;
        private const byte DSP_PAUSE_8BIT = 0xD0;
        private const byte DSP_PAUSE_16BIT = 0xD5;
        private const byte DSP_GET_VERSION = 0xE1;

        // Mixer
        private const byte MIXER_SET_MASTER_VOLUME = 0x22;
        private const byte MIXER_SET_IRQ = 0x80;
        private const byte MIXER_IRQ_5 = 0x02;

        public override IAudioBufferProvider BufferProvider { get; set; }
        public ushort SampleRate { get; set; } = 44100;
        public Version DSPVersion { get; private set; }

        private bool enabled = false;
        public override bool Enabled => enabled;

        public static SoundBlaster16 Instance { get; private set; }

        private int irqCount = 0;

        private SoundBlaster16(ushort bufferSize, SampleFormat format, ushort baseAddress = 0x220)
        {
            // Ports initialisieren
            pMixer = (ushort)(baseAddress + 0x04);
            pMixerData = (ushort)(baseAddress + 0x05);
            pReset = (ushort)(baseAddress + 0x06);
            pRead = (ushort)(baseAddress + 0x0A);
            pWrite = (ushort)(baseAddress + 0x0C);
            pReadStatus = (ushort)(baseAddress + 0x0E);
            pWriteStatus = (ushort)(baseAddress + 0x0C);
            p8BitIRQAck = (ushort)(baseAddress + 0x0E);
            p16BitIRQAck = (ushort)(baseAddress + 0x0F);

            ConsoleHelpers.WriteLine($"[SB16] Base: 0x{baseAddress:X3}");

            // DSP Reset
            if (!ResetDSP())
                throw new InvalidOperationException("SB16: DSP Reset fehlgeschlagen!");

            // DSP Version auslesen
            WriteDSP(DSP_GET_VERSION);
            byte vMajor = ReadDSP();
            byte vMinor = ReadDSP();
            DSPVersion = new Version(vMajor, vMinor);
            ConsoleHelpers.WriteLine($"[SB16] DSP Version: {DSPVersion}");

            if (DSPVersion.Major < 4)
                ConsoleHelpers.WriteLine($"[SB16] WARNUNG: DSP Version < 4.0");

            // IRQ 5 konfigurieren und aktivieren
            IOPort.Write8(pMixer, MIXER_SET_IRQ);
            IOPort.Write8(pMixerData, MIXER_IRQ_5);

            // Interrupt Handler registrieren (VOR Enable!)
            INTs.SetIrqHandler(5, HandleInterrupt);

            // PIC unmask IRQ5
            byte mask = IOPort.Read8(0x21);
            mask &= 0xDF; // Clear bit 5 (IRQ5)
            IOPort.Write8(0x21, mask);

            ConsoleHelpers.WriteLine("[SB16] IRQ 5 konfiguriert & unmaskiert");

            // Buffer erstellen
            this.currentFormat = format;
            CreateBuffers(bufferSize);

            ConsoleHelpers.WriteLine("[SB16] Init OK!");
        }

        public static SoundBlaster16 Initialize(ushort bufferSize, SampleFormat format, ushort baseAddress = 0x220)
        {
            if (Instance != null)
            {
                if (Instance.bufferSizeSamples != bufferSize)
                    Instance.ChangeBufferSize(bufferSize);
                return Instance;
            }

            Instance = new SoundBlaster16(bufferSize, format, baseAddress);
            return Instance;
        }

        private void CreateBuffers(ushort bufferSize)
        {
            transferBuffer = new AudioBuffer(bufferSize, currentFormat);
            bufferSizeSamples = bufferSize;
            bufferSizeBytes = bufferSize * currentFormat.Size;

            if (bufferSizeBytes > DMABufferMaxSize)
                throw new ArgumentException($"Buffer zu groß! Max: {DMABufferMaxSize}");

            // DMA-Buffer löschen
            MemoryOperations.Fill(dmaPtr, 0x00, bufferSizeBytes);
            ConsoleHelpers.WriteLine($"[SB16] Buffer: {bufferSizeSamples} samples = {bufferSizeBytes} bytes");
        }

        public void ChangeBufferSize(ushort newSize)
        {
            bool wasEnabled = enabled;
            if (wasEnabled) Disable();
            CreateBuffers(newSize);
            if (wasEnabled) Enable();
        }

        private bool ResetDSP()
        {
            // Reset senden
            IOPort.Write8(pReset, 1);
            for (int i = 0; i < 10000; i++) { }
            IOPort.Write8(pReset, 0);

            // Warte auf 0xAA
            for (int i = 0; i < 100000; i++)
            {
                if ((IOPort.Read8(pReadStatus) & 0x80) != 0)
                {
                    if (IOPort.Read8(pRead) == 0xAA)
                        return true;
                }
            }
            return false;
        }

        private void WriteDSP(byte value)
        {
            for (int i = 0; i < 65536; i++)
            {
                if ((IOPort.Read8(pWriteStatus) & 0x80) == 0)
                {
                    IOPort.Write8(pWrite, value);
                    return;
                }
            }
            ConsoleHelpers.WriteLine($"[SB16] WriteDSP timeout: 0x{value:X2}");
        }

        private byte ReadDSP()
        {
            for (int i = 0; i < 65536; i++)
            {
                if ((IOPort.Read8(pReadStatus) & 0x80) != 0)
                    return IOPort.Read8(pRead);
            }
            ConsoleHelpers.WriteLine("[SB16] ReadDSP timeout!");
            return 0xFF;
        }

        private void HandleInterrupt(ref INTs.IRQContext aContext)
        {
            irqCount++;

            // Debug IMMER ausgeben für erste 10 IRQs
            if (irqCount <= 10)
                ConsoleHelpers.WriteLine($"[SB16] !!! IRQ #{irqCount} !!!");


            // DSP Interrupt bestätigen (WICHTIG: Vor PIC!)
            if (currentFormat.BitDepth == AudioBitDepth.Bits8)
                IOPort.Read8(p8BitIRQAck);
            else
                IOPort.Read8(p16BitIRQAck);

            // PIC bestätigen
            IOPort.Write8(pPICInterruptAcknowledge, 0x20);
            IOPort.Write8(pPICExtendedInterruptAcknowledge, 0x20);

            if (!enabled || BufferProvider == null)
            {
                if (irqCount <= 3)
                    ConsoleHelpers.WriteLine($"[SB16] IRQ aber nicht enabled oder kein BufferProvider");
                return;
            }

            try
            {
                // Neuen Buffer vom Provider holen
                BufferProvider.RequestBuffer(transferBuffer);

                // In DMA-Buffer kopieren
                fixed (byte* srcPtr = transferBuffer.RawData)
                {
                    MemoryOperations.Copy(dmaPtr, srcPtr, bufferSizeBytes);
                }

                // Debug: Erste paar Buffers
                if (irqCount <= 3)
                {
                    ConsoleHelpers.WriteLine($"[SB16] Buffer copied, first bytes: {transferBuffer.RawData[0]}, {transferBuffer.RawData[1]}, {transferBuffer.RawData[100]}");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelpers.WriteLine($"[SB16] IRQ Error: {ex.Message}");
            }
        }

        public override void Enable()
        {
            if (enabled)
                return;

            ConsoleHelpers.WriteLine("[SB16] === ENABLE START ===");

            // Master Volume
            IOPort.Write8(pMixer, MIXER_SET_MASTER_VOLUME);
            IOPort.Write8(pMixerData, 0xFF);
            ConsoleHelpers.WriteLine("[SB16] Volume: MAX");

            // Speaker ON
            WriteDSP(DSP_TURN_SPEAKER_ON);
            ConsoleHelpers.WriteLine("[SB16] Speaker: ON");

            // Sample Rate ZUERST setzen
            WriteDSP(DSP_SET_OUTPUT_SAMPLE_RATE);
            WriteDSP((byte)(SampleRate >> 8));
            WriteDSP((byte)(SampleRate & 0xFF));
            ConsoleHelpers.WriteLine($"[SB16] Sample Rate: {SampleRate} Hz");

            // DANN DMA programmieren
            ProgramDMA();

            // DSP Command für Auto-Init DMA
            bool is16bit = currentFormat.BitDepth == AudioBitDepth.Bits16;
            byte command = is16bit ? (byte)0xB6 : (byte)0xC6;
            WriteDSP(command);
            ConsoleHelpers.WriteLine($"[SB16] DSP Command: 0x{command:X2}");

            // Mode: Stereo/Mono + Signed
            byte mode = (byte)((currentFormat.Channels == 2 ? 0x20 : 0x00) | 0x10);
            WriteDSP(mode);
            ConsoleHelpers.WriteLine($"[SB16] Mode: 0x{mode:X2} ({(currentFormat.Channels == 2 ? "Stereo" : "Mono")}, Signed)");

            // Buffer Length in Samples - 1
            ushort samples = (ushort)(bufferSizeSamples - 1);
            WriteDSP((byte)(samples & 0xFF));
            WriteDSP((byte)(samples >> 8));
            ConsoleHelpers.WriteLine($"[SB16] Samples: {bufferSizeSamples}");

            enabled = true;
            irqCount = 0;
            ConsoleHelpers.WriteLine("[SB16] === ENABLE DONE ===");
            ConsoleHelpers.WriteLine("[SB16] Warte auf IRQs...");
        }

        public override void Disable()
        {
            if (!enabled)
                return;

            enabled = false;

            // Stoppe DMA
            if (currentFormat.BitDepth == AudioBitDepth.Bits8)
                WriteDSP(DSP_STOP_8BIT);
            else
                WriteDSP(DSP_STOP_16BIT);

            // Speaker OFF
            WriteDSP(DSP_TURN_SPEAKER_OFF);

            ConsoleHelpers.WriteLine("[SB16] Disabled");
        }

        private void ProgramDMA()
        {
            bool is16bit = currentFormat.BitDepth == AudioBitDepth.Bits16;
            uint physAddr = DMABufferAddress;

            ConsoleHelpers.WriteLine($"[SB16] DMA Buffer @ 0x{physAddr:X8}");

            if (is16bit)
            {
                // 16-Bit: DMA Channel 5
                uint dmaAddr = physAddr >> 1;  // Word-Adresse
                uint count = ((uint)bufferSizeBytes >> 1) - 1; // Words - 1

                ConsoleHelpers.WriteLine($"[SB16] DMA16: Ch5, Addr=0x{dmaAddr:X}, Count={count} words");

                IOPort.Write8(0xD4, 0x05);    // Mask Ch5
                IOPort.Write8(0xD8, 0xFF);    // Flip-Flop
                IOPort.Write8(0xD6, 0x59);    // Mode: Auto, Read, Ch5

                IOPort.Write8(0x8B, (byte)(physAddr >> 16)); // Page (physical!)
                IOPort.Write8(0xC4, (byte)(dmaAddr & 0xFF));
                IOPort.Write8(0xC4, (byte)(dmaAddr >> 8));
                IOPort.Write8(0xC6, (byte)(count & 0xFF));
                IOPort.Write8(0xC6, (byte)(count >> 8));

                IOPort.Write8(0xD4, 0x01);    // Unmask Ch5
            }
            else
            {
                // 8-Bit: DMA Channel 1
                uint count = (uint)bufferSizeBytes - 1;

                ConsoleHelpers.WriteLine($"[SB16] DMA8: Ch1, Addr=0x{physAddr:X}, Count={count} bytes");

                IOPort.Write8(0x0A, 0x05);    // Mask Ch1
                IOPort.Write8(0x0C, 0xFF);    // Flip-Flop
                IOPort.Write8(0x0B, 0x49);    // Mode: Auto, Read, Ch1

                IOPort.Write8(0x83, (byte)(physAddr >> 16)); // Page
                IOPort.Write8(0x02, (byte)(physAddr & 0xFF));
                IOPort.Write8(0x02, (byte)(physAddr >> 8));
                IOPort.Write8(0x03, (byte)(count & 0xFF));
                IOPort.Write8(0x03, (byte)(count >> 8));

                IOPort.Write8(0x0A, 0x01);    // Unmask Ch1
            }

            ConsoleHelpers.WriteLine("[SB16] DMA programmed");
        }

        public override SampleFormat[] GetSupportedSampleFormats()
        {
            return new SampleFormat[]
            {
                new SampleFormat(AudioBitDepth.Bits16, 2, true),
                new SampleFormat(AudioBitDepth.Bits16, 1, true),
                new SampleFormat(AudioBitDepth.Bits8, 2, true),
                new SampleFormat(AudioBitDepth.Bits8, 1, true)
            };
        }

        public override void SetSampleFormat(SampleFormat sampleFormat)
        {
            if (enabled)
                throw new InvalidOperationException("Kann Format nicht ändern während aktiv!");
            currentFormat = sampleFormat;
            CreateBuffers(bufferSizeSamples);
        }
    }
}