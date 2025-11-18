using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    //BMP
    private const string defaultPathBMP = "BlyatOS.Library.Ressources.FilesBMP.";
    [ManifestResourceStream(ResourceName = defaultPathBMP + "TetrisLogo.bmp")]
    public static byte[] TetrisLogoBmpBytes;

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

            //Console.WriteLine("Loading BMP resources...");
            //BMP.TetrisLogo = new Bitmap(TetrisLogoBmpBytes);

            Console.WriteLine("All resources loaded!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading resources: {ex.Message}");
        }
    }
}
