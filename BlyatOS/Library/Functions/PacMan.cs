using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using BlyatOS.Library.Configs;
using BlyatOS.Library.Ressources;
using Cosmos.HAL;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using Cosmos.System.Audio.IO;
using Sys = Cosmos.System;

namespace BadTetrisCS;

public enum GhostType { Blinky, Pinky, Inky, Clyde }

public class Ghost
{
    public int X { get; set; }
    public int Y { get; set; }
    public GhostType Type { get; set; }
  public Color GhostColor { get; set; }
    public int StartX { get; set; }
    public int StartY { get; set; }
    public bool IsVulnerable { get; set; }
    private Random random = new Random();
    private int moveCounter = 0;
    private int lastDirX = 0;
 private int lastDirY = 0;

    public Ghost(GhostType type, int x, int y)
    {
        Type = type;
        X = x;
        Y = y;
        StartX = x;
        StartY = y;
        IsVulnerable = false;

        GhostColor = type switch
        {
            GhostType.Blinky => Color.Red,
       GhostType.Pinky => Color.Magenta,
   GhostType.Inky => Color.Cyan,
   GhostType.Clyde => Color.Orange,
       _ => Color.White
    };
    }

    public void Reset()
    {
        X = StartX;
        Y = StartY;
        IsVulnerable = false;
        moveCounter = 0;
      lastDirX = 0;
 lastDirY = 0;
    }

    public void Update(int pacX, int pacY, bool[][] maze, int width, int height, bool isClosest)
    {
        moveCounter++;
        
    // Geister bewegen sich alle 2 Frames
        if (moveCounter < 2) return;
        moveCounter = 0;

        // Alle Geister bewegen sich zufällig intelligent
        MoveRandomlyIntelligent(maze, width, height);
    }

private void MoveRandomlyIntelligent(bool[][] maze, int width, int height)
    {
        int[][] directions = { new[] { 0, -1 }, new[] { 0, 1 }, new[] { -1, 0 }, new[] { 1, 0 } };
     List<(int dx, int dy)> validMoves = new List<(int, int)>();
        List<(int dx, int dy)> preferredMoves = new List<(int, int)>();

        // Sammle alle möglichen Züge
     foreach (var dir in directions)
        {
            int nx = X + dir[0];
            int ny = Y + dir[1];
            if (CanMoveTo(nx, ny, maze, width, height))
            {
           validMoves.Add((dir[0], dir[1]));
         
 // Bevorzuge Züge, die nicht zurück in die gleiche Richtung gehen
        if (dir[0] != -lastDirX || dir[1] != -lastDirY)
         {
    preferredMoves.Add((dir[0], dir[1]));
       }
    }
  }

        // Wähle einen Zug
        (int moveX, int moveY) moveToMake = (0, 0);
      
        if (preferredMoves.Count > 0)
        {
  // Bevorzuge Züge, die nicht zurückgehen
 moveToMake = preferredMoves[random.Next(preferredMoves.Count)];
        }
        else if (validMoves.Count > 0)
        {
        // Wenn nur Rückwärts möglich, gehe zurück
  moveToMake = validMoves[random.Next(validMoves.Count)];
  }

        // Führe den Zug aus
     if (moveToMake.moveX != 0 || moveToMake.moveY != 0)
        {
  X += moveToMake.moveX;
 Y += moveToMake.moveY;
  lastDirX = moveToMake.moveX;
          lastDirY = moveToMake.moveY;
        }
    }

    public bool CanMoveTo(int x, int y, bool[][] maze, int width, int height)
    {
        return x >= 0 && x < width && y >= 0 && y < height && !maze[y][x];
    }
}

public class PacMan
{
    private const int TILE_SIZE = 12;
    private int boardOffsetX;  // Offset für zentriertes Rendering
    private int boardOffsetY;

    private int pacX;
 private int pacY;
    private int pacDirX = 0;
    private int pacDirY = 0;
    private int nextDirX = 0;
    private int nextDirY = 0;
    
    private bool[][] maze;
    private bool[][] pellets;
    private bool[][] powerPellets;
    
    private int MAZE_WIDTH;
    private int MAZE_HEIGHT;
    private int pelletsRemaining;
    private int score;
    private int highScore;
    private int lives;
    private Ghost[] ghosts;
    private Random random;
    private Canvas canvas;
    private Font font;
    private int gameState;
    private int powerUpCounter = 0;
    private const int POWER_UP_DURATION = 300;
    private int moveCounter = 0;
    
    private StringBuilder sb = new StringBuilder(64);
    private MemoryAudioStream currentMusic = null!;
    private long lastMusicCheck = 0;

    public PacMan()
    {
        canvas = DisplaySettings.Canvas;
      font = DisplaySettings.Font;
     random = new Random();
      score = 0;
        highScore = 0;
lives = 3;
        gameState = 0;

        InitializeMaze();
     InitializePellets();
   
    // Berechne Board-Offsets für zentriertes Rendering
 int boardWidth = MAZE_WIDTH * TILE_SIZE;
        int boardHeight = MAZE_HEIGHT * TILE_SIZE;
        boardOffsetX = ((int)DisplaySettings.ScreenWidth - boardWidth) / 2;
        boardOffsetY = ((int)DisplaySettings.ScreenHeight - boardHeight - 30) / 2;
   
  pacX = 12;
        pacY = 15;
        pacDirX = 0;
      pacDirY = 0;

        ghosts = new Ghost[4];
        ghosts[0] = new Ghost(GhostType.Blinky, 12, 8);
        ghosts[1] = new Ghost(GhostType.Pinky, 11, 9);
    ghosts[2] = new Ghost(GhostType.Inky, 12, 9);
  ghosts[3] = new Ghost(GhostType.Clyde, 13, 9);

  AudioHandler.Initialize();
    }

    private void InitializeMaze()
    {
        // Klassisches Pac-Man Labyrinth - perfekt an den Screen angepasst
  string[] mazeLayout = new string[]
        {
          "##########################",
     "#............#............#",
            "#.##.#####.#####.#####.##.#",
            "#.#...#.....#.....#...#..#",
    "#.###.#####.#.#####.#.####.#",
         "#.....#.....#.....#.....#.#",
            "#.#########.#.#########.#.#",
    "#.#.....#.....#.....#.#.#.#",
      "#.#.###.#####.#####.#.#.#.#",
     "#...#...#.....#.....#.....#",
"###.#.###.#####.#####.###.#",
 "#...#.....#.....#.......#.#",
   "#.#########.#.#########.#.#",
  "#.#...#.....#.....#...#...#",
"#.###.#####.#.#####.#.###.#",
        "#.....#.....#.....#.....#.#",
         "#.#########.#.#########.#.#",
            "#.................#.....#.#",
      "##########################"
      };

   MAZE_HEIGHT = mazeLayout.Length;
        MAZE_WIDTH = mazeLayout[0].Length;

        maze = new bool[MAZE_HEIGHT][];
        for (int y = 0; y < MAZE_HEIGHT; y++)
      {
      maze[y] = new bool[MAZE_WIDTH];
            for (int x = 0; x < MAZE_WIDTH; x++)
            {
    maze[y][x] = mazeLayout[y][x] == '#';
            }
        }
    }

    private void InitializePellets()
{
        pellets = new bool[MAZE_HEIGHT][];
        powerPellets = new bool[MAZE_HEIGHT][];
        pelletsRemaining = 0;

        for (int y = 0; y < MAZE_HEIGHT; y++)
        {
         pellets[y] = new bool[MAZE_WIDTH];
          powerPellets[y] = new bool[MAZE_WIDTH];
     for (int x = 0; x < MAZE_WIDTH; x++)
         {
  if (!maze[y][x])
   {
         // Power-Pellets in den Ecken
  if ((x == 1 && y == 1) || (x == MAZE_WIDTH - 2 && y == 1) ||
           (x == 1 && y == MAZE_HEIGHT - 2) || (x == MAZE_WIDTH - 2 && y == MAZE_HEIGHT - 2))
  {
       powerPellets[y][x] = true;
        pelletsRemaining++;
         }
    else
           {
     pellets[y][x] = true;
        pelletsRemaining++;
       }
           }
}
        }
    }

    private void PlayMusic(MemoryAudioStream music, bool loop = true)
    {
     if (music == null) return;
        AudioHandler.Stop();
      currentMusic = music;
        currentMusic.Position = 0;
        AudioHandler.Play(currentMusic);
    }

    private void UpdateMusicLoop()
    {
        long now = DateTime.Now.Ticks;
        if (now - lastMusicCheck > 1000000)
        {
  lastMusicCheck = now;
 if (currentMusic != null && !AudioHandler.IsPlaying)
      {
         currentMusic.Position = 0;
         AudioHandler.Play(currentMusic);
            }
        }
    }

    private bool CanMove(int x, int y)
    {
 return x >= 0 && x < MAZE_WIDTH && y >= 0 && y < MAZE_HEIGHT && !maze[y][x];
    }

private void HandleInput()
    {
        while (Sys.KeyboardManager.KeyAvailable)
        {
   var key = Sys.KeyboardManager.ReadKey();

     switch (key.Key)
            {
     case Sys.ConsoleKeyEx.UpArrow:
  case Sys.ConsoleKeyEx.W:
         nextDirX = 0; nextDirY = -1;
         break;
     case Sys.ConsoleKeyEx.DownArrow:
   case Sys.ConsoleKeyEx.S:
              nextDirX = 0; nextDirY = 1;
          break;
      case Sys.ConsoleKeyEx.LeftArrow:
      case Sys.ConsoleKeyEx.A:
           nextDirX = -1; nextDirY = 0;
   break;
  case Sys.ConsoleKeyEx.RightArrow:
          case Sys.ConsoleKeyEx.D:
      nextDirX = 1; nextDirY = 0;
          break;
        case Sys.ConsoleKeyEx.Q:
                case Sys.ConsoleKeyEx.Escape:
           gameState = 2;
      break;
        }
        }
    }

    private void UpdatePacManPosition()
    {
if (CanMove(pacX + nextDirX, pacY + nextDirY))
        {
          pacDirX = nextDirX;
  pacDirY = nextDirY;
        }

    if (CanMove(pacX + pacDirX, pacY + pacDirY))
  {
      pacX += pacDirX;
       pacY += pacDirY;

          if (pellets[pacY][pacX])
      {
                pellets[pacY][pacX] = false;
          pelletsRemaining--;
    score += 10;
        }

            if (powerPellets[pacY][pacX])
  {
powerPellets[pacY][pacX] = false;
      pelletsRemaining--;
        score += 50;
            powerUpCounter = POWER_UP_DURATION;

              foreach (var ghost in ghosts)
       ghost.IsVulnerable = true;
            }

            if (pelletsRemaining == 0)
       gameState = 1;
        }
    }

    private int GetClosestGhostIndex()
    {
        int closestIndex = 0;
        int closestDist = int.MaxValue;

   for (int i = 0; i < ghosts.Length; i++)
        {
            int dist = Math.Abs(ghosts[i].X - pacX) + Math.Abs(ghosts[i].Y - pacY);
if (dist < closestDist)
       {
                closestDist = dist;
   closestIndex = i;
   }
        }

        return closestIndex;
    }

    private void UpdateGhosts()
    {
        powerUpCounter--;
        if (powerUpCounter <= 0)
        {
  foreach (var ghost in ghosts)
                ghost.IsVulnerable = false;
        }

        int closestGhostIndex = GetClosestGhostIndex();

        for (int i = 0; i < ghosts.Length; i++)
        {
          Ghost ghost = ghosts[i];
            bool isClosest = (i == closestGhostIndex);

     ghost.Update(pacX, pacY, maze, MAZE_WIDTH, MAZE_HEIGHT, isClosest);

      if (ghost.X == pacX && ghost.Y == pacY)
            {
       if (ghost.IsVulnerable)
                {
             ghost.Reset();
       score += 200;
         }
     else
       {
          lives--;
    if (lives <= 0)
     {
     gameState = 2;
         }
      else
              {
               ResetPlayerPosition();
               }
 }
            }
        }
    }

    private void ResetPlayerPosition()
    {
        pacX = 12;
        pacY = 15;
        pacDirX = 0;
      pacDirY = 0;
        nextDirX = 0;
    nextDirY = 0;
    powerUpCounter = 0;

  foreach (var ghost in ghosts)
 ghost.Reset();
    }

    private void DrawTile(int x, int y, Color color, char symbol)
    {
int px = boardOffsetX + (x * TILE_SIZE);
   int py = boardOffsetY + (y * TILE_SIZE);

   canvas.DrawFilledRectangle(color, px, py, TILE_SIZE, TILE_SIZE);
    }

    private void DrawGame()
    {
        canvas.Clear(Color.Black);

     for (int y = 0; y < MAZE_HEIGHT; y++)
        {
        for (int x = 0; x < MAZE_WIDTH; x++)
     {
    if (maze[y][x])
 {
DrawTile(x, y, Color.Blue, '#');
         }
 else if (powerPellets[y][x])
        {
   // Power-Pellets blinken weiß
  if ((moveCounter / 3) % 2 == 0)
  {
    int px = boardOffsetX + (x * TILE_SIZE + TILE_SIZE / 2 - 2);
   int py = boardOffsetY + (y * TILE_SIZE + TILE_SIZE / 2 - 2);
    canvas.DrawFilledRectangle(Color.White, px, py, 4, 4);
   }
    }
   else if (pellets[y][x])
  {
      // Normale Pellets
  int px = boardOffsetX + (x * TILE_SIZE + TILE_SIZE / 2 - 1);
   int py = boardOffsetY + (y * TILE_SIZE + TILE_SIZE / 2 - 1);
 canvas.DrawFilledRectangle(Color.White, px, py, 2, 2);
     }
            }
    }

 DrawTile(pacX, pacY, Color.Yellow, 'C');

   foreach (var ghost in ghosts)
  {
      Color ghostColor = ghost.GhostColor;
        
      // Wenn Powerup aktiv und Ghost verwundbar, Geister weiß blinken lassen
 if (ghost.IsVulnerable && (moveCounter / 4) % 2 == 0)
  {
         ghostColor = Color.White;
      }
   else if (ghost.IsVulnerable)
  {
        ghostColor = Color.Blue;
   }
         
   DrawTile(ghost.X, ghost.Y, ghostColor, 'G');
    }

      // HUD zentriert unter dem Spielbrett
   int hudY = boardOffsetY + (MAZE_HEIGHT * TILE_SIZE) + 10;
    
  sb.Clear();
        sb.Append("Score: ");
     sb.Append(score);
  canvas.DrawString(sb.ToString(), font, Color.Yellow, boardOffsetX + 10, hudY);

   sb.Clear();
        sb.Append("Lives: ");
        sb.Append(lives);
canvas.DrawString(sb.ToString(), font, Color.Red, boardOffsetX + 120, hudY);

 sb.Clear();
     sb.Append("Pellets: ");
    sb.Append(pelletsRemaining);
  canvas.DrawString(sb.ToString(), font, Color.White, boardOffsetX + 240, hudY);

  if (powerUpCounter > 0)
  {
      canvas.DrawString("POWER!", font, Color.Cyan, boardOffsetX + 420, hudY);
        }

      canvas.Display();
    }

    private void ShowStartScreen()
    {
        canvas.Clear(Color.Black);
     PlayMusic(Audio.Title, true);

        string title = "PAC-MAN";
        string subtitle = "Classic Edition";
        string pressKey = "Press any key to start";

        int centerX = (int)DisplaySettings.ScreenWidth / 2;
        int centerY = (int)DisplaySettings.ScreenHeight / 2;

        canvas.DrawString(title, font, Color.Yellow,
          centerX - (title.Length * font.Width / 2), centerY - 60);
        canvas.DrawString(subtitle, font, Color.White,
         centerX - (subtitle.Length * font.Width / 2), centerY - 30);

        if (highScore > 0)
        {
   sb.Clear();
         sb.Append("High Score: ");
     sb.Append(highScore);
            string highScoreText = sb.ToString();
            canvas.DrawString(highScoreText, font, Color.Gold,
    centerX - (highScoreText.Length * font.Width / 2), centerY + 10);
 }

        canvas.DrawString(pressKey, font, Color.Gray,
 centerX - (pressKey.Length * font.Width / 2), centerY + 50);

        canvas.Display();

        while (!Sys.KeyboardManager.KeyAvailable)
        {
UpdateMusicLoop();
     Global.PIT.Wait(10);
    }
     Sys.KeyboardManager.ReadKey();
    }

    private void ShowGameOver(bool isWon)
    {
   PlayMusic(Audio.GameOver, false);

        canvas.Clear(Color.Black);
        string gameOverText = isWon ? "YOU WIN!" : "GAME OVER!";
        Color textColor = isWon ? Color.Green : Color.Red;

        sb.Clear();
   sb.Append("Score: ");
  sb.Append(score);
        string scoreText = sb.ToString();

        int centerX = (int)DisplaySettings.ScreenWidth / 2;
        int centerY = (int)DisplaySettings.ScreenHeight / 2;

        canvas.DrawString(gameOverText, font, textColor,
      centerX - (gameOverText.Length * font.Width / 2), centerY - 40);
        canvas.DrawString(scoreText, font, Color.Yellow,
         centerX - (scoreText.Length * font.Width / 2), centerY - 10);

        bool isNewHighScore = score > highScore;
        if (isNewHighScore)
 {
      canvas.DrawString("NEW HIGH SCORE!", font, Color.Gold,
         centerX - (14 * font.Width / 2), centerY + 20);
    }

        string pressKey = "Press any key...";
        canvas.DrawString(pressKey, font, Color.White,
      centerX - (pressKey.Length * font.Width / 2), centerY + 50);

   canvas.Display();

while (AudioHandler.IsPlaying)
        {
   Global.PIT.Wait(10);
        }

        if (isNewHighScore && Audio.HighScore != null)
        {
      PlayMusic(Audio.HighScore, false);
    }

        while (!Sys.KeyboardManager.KeyAvailable)
   {
            Global.PIT.Wait(10);
    }
        Sys.KeyboardManager.ReadKey();

 if (isNewHighScore)
highScore = score;
    }

    public void Run()
    {
        ShowStartScreen();
PlayMusic(Audio.MainMusic, true);

   canvas.Clear(Color.Black);
        bool running = true;
        int pacMoveCounter = 0;
        int ghostMoveCounter = 0;
      const int PAC_MOVE_DELAY = 3;
        const int GHOST_MOVE_DELAY = 3;

      while (running)
        {
        UpdateMusicLoop();
         HandleInput();

 moveCounter++;
            
    // Reset moveCounter jeden großen Wert um Integer Overflow zu vermeiden
        if (moveCounter > 100000)
           moveCounter = 0;

   pacMoveCounter++;
   ghostMoveCounter++;

            // Pac-Man bewegt sich langsamer
 if (pacMoveCounter >= PAC_MOVE_DELAY)
   {
        UpdatePacManPosition();
       pacMoveCounter = 0;
       }

      // Geister bewegen sich noch langsamer
       if (ghostMoveCounter >= GHOST_MOVE_DELAY)
            {
   UpdateGhosts();
    ghostMoveCounter = 0;
        }

 DrawGame();

        if (gameState == 1)
      {
     running = false;
           ShowGameOver(true);
   }
            else if (gameState == 2)
        {
 running = false;
          ShowGameOver(false);
   }

      Global.PIT.Wait(25);
 }

        AudioHandler.Stop();
    }
}
