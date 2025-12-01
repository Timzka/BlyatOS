using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using BlyatOS.Library.Helpers;
using Cosmos.System.Audio.IO;
using Cosmos.System.Graphics;
using IL2CPU.API.Attribs;

namespace BlyatOS.Library.Ressources;

public static class Ressourceloader
{
    //Audio
    private const string defaultPathAudio = "BlyatOS.Library.Ressources.FilesAudio.";
    [ManifestResourceStream(ResourceName = defaultPathAudio + "Title.wav")]
    public static byte[] TitleWavBytes;
    [ManifestResourceStream(ResourceName = defaultPathAudio + "MainMusic.wav")]
    public static byte[] MainMusicWavBytes;
    [ManifestResourceStream(ResourceName = defaultPathAudio + "HighScore.wav")]
    public static byte[] HighScoreWavBytes;
    [ManifestResourceStream(ResourceName = defaultPathAudio + "GameOver.wav")]
    public static byte[] GameOverWavBytes;
    [ManifestResourceStream(ResourceName = defaultPathAudio + "blyattraktor.wav")]
    public static byte[] BlyatTraktorBytes;
    [ManifestResourceStream(ResourceName = defaultPathAudio + "narkotikkal.wav")]
    public static byte[] NarkotikKalBytes;

    //BMP
    private const string defaultPathBMP = "BlyatOS.Library.Ressources.FilesBMP.";
    [ManifestResourceStream(ResourceName = defaultPathBMP + "TetrisLogo.bmp")]
    public static byte[] TetrisLogoBmpBytes;
    [ManifestResourceStream(ResourceName = defaultPathBMP + "BlyatLogo.bmp")]
    public static byte[] BlyatLogoBmpBytes;
    [ManifestResourceStream(ResourceName = defaultPathBMP + "blyattraktor.bmp")]
    public static byte[] TraktorBytes;
    public static void InitRessources()
    {
        try
        {
            Console.WriteLine("Loading Title audio...");
            Audio.Title = MemoryAudioStream.FromWave(TitleWavBytes);

            Console.WriteLine("Loading MainMusic audio...");
            Audio.MainMusic = MemoryAudioStream.FromWave(MainMusicWavBytes);

            Console.WriteLine("Loading HighScore audio...");
            Audio.HighScore = MemoryAudioStream.FromWave(HighScoreWavBytes);

            Console.WriteLine("Loading GameOver audio...");
            Audio.GameOver = MemoryAudioStream.FromWave(GameOverWavBytes); 
            
            Console.WriteLine("Loading BlyatTraktor audio...");
            Audio.BlyatTraktor = MemoryAudioStream.FromWave(BlyatTraktorBytes);

            Console.WriteLine("Loading NarkotikKal audio...");
            Audio.NarkotikKal = MemoryAudioStream.FromWave(NarkotikKalBytes);

            Console.WriteLine("Loading BMP resources...");
            BMP.TetrisLogo = new Bitmap(TetrisLogoBmpBytes);
            BMP.BlyatLogo = ImageHelpers.ConvertBMP(new Bitmap(BlyatLogoBmpBytes));
            BMP.Traktor = ImageHelpers.ConvertBMP(new Bitmap(TraktorBytes));

            Console.WriteLine("All resources loaded!");
        }
        catch (Exception ex)
        {
            ConsoleHelpers.WriteLine($"Error loading resources: {ex.Message}");
        }
    }
}
