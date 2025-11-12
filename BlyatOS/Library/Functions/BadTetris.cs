using System;
using System.Collections.Generic;
using System.Drawing;
using Cosmos.HAL;
using Cosmos.System.Graphics;
using BlyatOS.Library.Configs;
using Sys = Cosmos.System;
using Cosmos.System.Graphics.Fonts;
using BlyatOS.Library.Helpers;
using System.Text;
using Cosmos.Core.Memory;

namespace BadTetrisCS;

public enum TetrominoType { I, O, T, S, Z, J, L }

public class Tetromino
{
    public TetrominoType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool[] Shape { get; set; }

    public Tetromino(TetrominoType type, int startX, int startY)
    {
        Type = type;
        X = startX;
        Y = startY;
        Shape = new bool[16];
        InitializeShape();
    }

    private int GetShapeIndex(int x, int y) => y * 4 + x;
    public bool GetShape(int x, int y) => Shape[GetShapeIndex(x, y)];
    public void SetShape(int x, int y, bool value) => Shape[GetShapeIndex(x, y)] = value;

    private void InitializeShape()
    {
        switch (Type)
        {
            case TetrominoType.I:
                SetShape(0, 1, true); SetShape(1, 1, true); SetShape(2, 1, true); SetShape(3, 1, true);
                break;
            case TetrominoType.O:
                SetShape(1, 1, true); SetShape(2, 1, true);
                SetShape(1, 2, true); SetShape(2, 2, true);
                break;
            case TetrominoType.T:
                SetShape(0, 1, true); SetShape(1, 1, true); SetShape(2, 1, true);
                SetShape(1, 2, true);
                break;
            case TetrominoType.S:
                SetShape(1, 1, true); SetShape(2, 1, true);
                SetShape(0, 2, true); SetShape(1, 2, true);
                break;
            case TetrominoType.Z:
                SetShape(0, 1, true); SetShape(1, 1, true);
                SetShape(1, 2, true); SetShape(2, 2, true);
                break;
            case TetrominoType.J:
                SetShape(0, 1, true); SetShape(1, 1, true); SetShape(2, 1, true);
                SetShape(2, 2, true);
                break;
            case TetrominoType.L:
                SetShape(0, 1, true); SetShape(1, 1, true); SetShape(2, 1, true);
                SetShape(0, 2, true);
                break;
        }
    }

    public void RotateClockwise()
    {
        bool[] newShape = new bool[16];
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                newShape[x * 4 + (3 - y)] = Shape[y * 4 + x];
        Shape = newShape;
    }

    public int GetLeftmost()
    {
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                if (GetShape(x, y)) return x;
        return 0;
    }

    public int GetRightmost()
    {
        for (int x = 3; x >= 0; x--)
            for (int y = 0; y < 4; y++)
                if (GetShape(x, y)) return x;
        return 3;
    }

    public bool CanPlace(int fieldWidth, int fieldHeight, bool[] field, int xOffset = 0, int yOffset = 0)
    {
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                if (GetShape(x, y))
                {
                    int newX = X + x + xOffset;
                    int newY = Y + y + yOffset;
                    if (newX < 0 || newX >= fieldWidth || newY < 0 || newY >= fieldHeight)
                        return false;
                    if (field[newY * fieldWidth + newX])
                        return false;
                }
        return true;
    }

    public void RotateWithKick(int fieldWidth, int fieldHeight, bool[] field)
    {
        bool[] oldShape = new bool[16];
        Array.Copy(Shape, oldShape, 16);
        RotateClockwise();

        int[] offsets = { 0, -1, 1, -2, 2 };
        bool success = false;
        foreach (int off in offsets)
        {
            int oldX = X;
            X += off;

            if (X + GetLeftmost() < 0) X = -GetLeftmost();
            if (X + GetRightmost() >= fieldWidth) X = fieldWidth - 1 - GetRightmost();

            if (CanPlace(fieldWidth, fieldHeight, field))
            {
                success = true;
                break;
            }
            X = oldX;
        }

        if (!success)
            Shape = oldShape;
    }
}

public class BadTetris
{
    private const int FIELD_WIDTH = 10;
    private const int FIELD_HEIGHT = 20;
    private const int BLOCK_SIZE = 20;
    private const int BLOCK_INNER_SIZE = 18; // Block ist 2px kleiner (1px Rand)
    private const int DAS = 8;
    private const int ARR = 1;

    private const ulong TARGET_FRAME_TIME_NS = 16666666; // 60 FPS

    // MEMORY OPTIMIZATION: bool array statt Color array (1 byte statt 4 bytes pro Block)
    private bool[] field;
    private Tetromino currentBlock = null!;
    private TetrominoType nextType;
    private int score;
    private Random random;

    private bool leftHeld = false;
    private bool rightHeld = false;
    private bool downHeld = false;
    private bool rotateHeld = false;
    private int leftRepeatTime = 0;
    private int rightRepeatTime = 0;
    private int downRepeatTime = 0;

    private Canvas canvas;
    private Font font;

    // CACHED PENS - einmal holen, immer wiederverwenden
    private Pen blackPen;
    private Pen whitePen;
    private Pen lightGrayPen;
    private Pen redPen;
    private Pen yellowPen;
    private Pen cyanPen;
    private Pen grayPen;

    private int boardX;
    private int boardY;

    // Reusable StringBuilder für Text
    private StringBuilder sb = new StringBuilder(64);

    public BadTetris()
    {
        canvas = DisplaySettings.Canvas;
        font = DisplaySettings.Font;
        field = new bool[FIELD_HEIGHT * FIELD_WIDTH];
        random = new Random();
        score = 0;

        boardX = ((int)DisplaySettings.ScreenWidth - (FIELD_WIDTH * BLOCK_SIZE)) / 2;
        boardY = 50;

        // Cache all pens once
        blackPen = DisplaySettings.GetPen(Color.Black);
        whitePen = DisplaySettings.GetPen(Color.White);
        lightGrayPen = DisplaySettings.GetPen(Color.LightGray);
        redPen = DisplaySettings.GetPen(Color.Red);
        yellowPen = DisplaySettings.GetPen(Color.Yellow);
        cyanPen = DisplaySettings.GetPen(Color.Cyan);
        grayPen = DisplaySettings.GetPen(Color.Gray);

        for (int i = 0; i < field.Length; i++)
            field[i] = false;
    }

    private string TetrominoTypeToString(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.I: return "I";
            case TetrominoType.O: return "O";
            case TetrominoType.T: return "T";
            case TetrominoType.S: return "S";
            case TetrominoType.Z: return "Z";
            case TetrominoType.J: return "J";
            case TetrominoType.L: return "L";
            default: return "?";
        }
    }

    private TetrominoType GetRandomTetrominoType()
    {
        TetrominoType[] types = { TetrominoType.I, TetrominoType.O, TetrominoType.T,
                                  TetrominoType.S, TetrominoType.Z, TetrominoType.J, TetrominoType.L };
        return types[random.Next(7)];
    }

    private Tetromino SpawnTetromino(TetrominoType type)
    {
        return new Tetromino(type, FIELD_WIDTH / 2 - 2, 0);
    }

    private bool CanSpawn(TetrominoType type)
    {
        Tetromino tetromino = new Tetromino(type, FIELD_WIDTH / 2 - 2, 0);
        return tetromino.CanPlace(FIELD_WIDTH, FIELD_HEIGHT, field);
    }

    private bool CanMoveDown(Tetromino block)
    {
        return block.CanPlace(FIELD_WIDTH, FIELD_HEIGHT, field, 0, 1);
    }

    private bool CanMoveLeft(Tetromino block)
    {
        return block.CanPlace(FIELD_WIDTH, FIELD_HEIGHT, field, -1, 0);
    }

    private bool CanMoveRight(Tetromino block)
    {
        return block.CanPlace(FIELD_WIDTH, FIELD_HEIGHT, field, 1, 0);
    }

    private void LockPiece(Tetromino block)
    {
        for (int by = 0; by < 4; by++)
            for (int bx = 0; bx < 4; bx++)
                if (block.GetShape(bx, by))
                {
                    int nx = block.X + bx;
                    int ny = block.Y + by;
                    if (nx >= 0 && nx < FIELD_WIDTH && ny >= 0 && ny < FIELD_HEIGHT)
                        field[ny * FIELD_WIDTH + nx] = true;
                }
    }

    private int HardDrop(Tetromino block)
    {
        int dropDistance = 0;
        while (CanMoveDown(block))
        {
            block.Y++;
            dropDistance++;
        }
        return dropDistance;
    }

    // OPTIMIZED: Zeichnet hellgrauen Block mit schwarzem Hintergrund und 1px Rand
    private void DrawBlock(int x, int y, Pen fillPen)
    {
        int px = boardX + x * BLOCK_SIZE;
        int py = boardY + y * BLOCK_SIZE;

        // Schwarzer Hintergrund (ganzer Block)
        canvas.DrawFilledRectangle(blackPen, px, py, BLOCK_SIZE, BLOCK_SIZE);

        // Hellgrauer/Roter Block (etwas kleiner, 1px Rand)
        int innerPx = px + 1;
        int innerPy = py + 1;
        canvas.DrawFilledRectangle(fillPen, innerPx, innerPy, BLOCK_INNER_SIZE, BLOCK_INNER_SIZE);
    }

    private void DrawBoardBase()
    {
        canvas.Clear(Color.Black);

        string title = "TETRIS";
        canvas.DrawString(title, font, whitePen,
            boardX + (FIELD_WIDTH * BLOCK_SIZE / 2) - (title.Length * font.Width / 2), 10);

        // OPTIMIZED: StringBuilder statt String-Interpolation
        sb.Clear();
        sb.Append("Score: ");
        sb.Append(score);
        canvas.DrawString(sb.ToString(), font, yellowPen, boardX, boardY - 25);

        sb.Clear();
        sb.Append("Next: ");
        sb.Append(TetrominoTypeToString(nextType));
        canvas.DrawString(sb.ToString(), font, cyanPen,
            boardX + FIELD_WIDTH * BLOCK_SIZE + 20, boardY);

        int borderX = boardX - 2;
        int borderY = boardY - 2;
        int borderW = FIELD_WIDTH * BLOCK_SIZE + 3;
        int borderH = FIELD_HEIGHT * BLOCK_SIZE + 3;
        canvas.DrawRectangle(whitePen, borderX, borderY, borderW, borderH);

        // Draw locked pieces (alle hellgrau)
        for (int y = 0; y < FIELD_HEIGHT; y++)
        {
            for (int x = 0; x < FIELD_WIDTH; x++)
            {
                if (field[y * FIELD_WIDTH + x])
                {
                    DrawBlock(x, y, lightGrayPen);
                }
            }
        }

        string controls = "WASD/Arrows | Space=Drop | Q=Quit";
        canvas.DrawString(controls, font, grayPen,
            10, (int)DisplaySettings.ScreenHeight - 20);
    }

    private void DrawBoard()
    {
        DrawBoardBase();

        // Draw current piece (hellgrau)
        for (int by = 0; by < 4; by++)
        {
            for (int bx = 0; bx < 4; bx++)
            {
                if (currentBlock.GetShape(bx, by))
                {
                    int x = currentBlock.X + bx;
                    int y = currentBlock.Y + by;
                    if (x >= 0 && x < FIELD_WIDTH && y >= 0 && y < FIELD_HEIGHT)
                    {
                        DrawBlock(x, y, lightGrayPen);
                    }
                }
            }
        }

        canvas.Display();
    }

    // OPTIMIZED: Rote Blink-Animation ohne Color-Objekte zu erstellen
    private void FlashRows(List<int> rows)
    {
        if (rows.Count == 0) return;

        for (int flash = 0; flash < 3; flash++)
        {
            DrawBoardBase();

            // Flash rows ROT
            foreach (int row in rows)
            {
                for (int x = 0; x < FIELD_WIDTH; x++)
                {
                    DrawBlock(x, row, redPen);
                }
            }
            canvas.Display();
            Global.PIT.Wait(5);

            DrawBoardBase();

            // Flash back to original (hellgrau)
            foreach (int row in rows)
            {
                for (int x = 0; x < FIELD_WIDTH; x++)
                {
                    DrawBlock(x, row, lightGrayPen);
                }
            }
            canvas.Display();
            Global.PIT.Wait(5);
        }
    }

    private int CheckBoard()
    {
        List<int> fullRows = new List<int>();
        for (int y = FIELD_HEIGHT - 1; y >= 0; y--)
        {
            bool full = true;
            for (int x = 0; x < FIELD_WIDTH; x++)
            {
                if (!field[y * FIELD_WIDTH + x])
                {
                    full = false;
                    break;
                }
            }
            if (full)
                fullRows.Add(y);
        }

        if (fullRows.Count > 0)
            FlashRows(fullRows);

        int linesCleared = 0;
        for (int y = FIELD_HEIGHT - 1; y >= 0; y--)
        {
            bool full = true;
            for (int x = 0; x < FIELD_WIDTH; x++)
            {
                if (!field[y * FIELD_WIDTH + x])
                {
                    full = false;
                    break;
                }
            }

            if (full)
            {
                for (int ny = y; ny > 0; ny--)
                {
                    for (int x = 0; x < FIELD_WIDTH; x++)
                    {
                        field[ny * FIELD_WIDTH + x] = field[(ny - 1) * FIELD_WIDTH + x];
                    }
                }
                for (int x = 0; x < FIELD_WIDTH; x++)
                    field[x] = false;

                y++;
                linesCleared++;
            }
        }

        fullRows.Clear(); // Liste aufräumen
        return linesCleared;
    }

    private bool HandleInput(out bool locked, out bool quit)
    {
        bool moved = false;
        locked = false;
        quit = false;
        bool anyKeyPressed = false;

        while (Sys.KeyboardManager.KeyAvailable)
        {
            anyKeyPressed = true;
            var key = Sys.KeyboardManager.ReadKey();

            switch (key.Key)
            {
                case Sys.ConsoleKeyEx.A:
                case Sys.ConsoleKeyEx.LeftArrow:
                    if (!leftHeld)
                    {
                        if (CanMoveLeft(currentBlock))
                        {
                            currentBlock.X--;
                            moved = true;
                        }
                        leftHeld = true;
                        leftRepeatTime = DAS;
                    }
                    rightHeld = false;
                    break;

                case Sys.ConsoleKeyEx.D:
                case Sys.ConsoleKeyEx.RightArrow:
                    if (!rightHeld)
                    {
                        if (CanMoveRight(currentBlock))
                        {
                            currentBlock.X++;
                            moved = true;
                        }
                        rightHeld = true;
                        rightRepeatTime = DAS;
                    }
                    leftHeld = false;
                    break;

                case Sys.ConsoleKeyEx.S:
                case Sys.ConsoleKeyEx.DownArrow:
                    if (!downHeld)
                    {
                        if (CanMoveDown(currentBlock))
                        {
                            currentBlock.Y++;
                            moved = true;
                        }
                        else
                        {
                            LockPiece(currentBlock);
                            locked = true;
                        }
                        downHeld = true;
                        downRepeatTime = DAS;
                    }
                    break;

                case Sys.ConsoleKeyEx.W:
                case Sys.ConsoleKeyEx.UpArrow:
                    if (!rotateHeld)
                    {
                        currentBlock.RotateWithKick(FIELD_WIDTH, FIELD_HEIGHT, field);
                        moved = true;
                        rotateHeld = true;
                    }
                    break;

                case Sys.ConsoleKeyEx.Spacebar:
                    HardDrop(currentBlock);
                    LockPiece(currentBlock);
                    locked = true;
                    moved = true;
                    break;

                case Sys.ConsoleKeyEx.Q:
                case Sys.ConsoleKeyEx.Escape:
                    quit = true;
                    break;
            }
        }

        if (leftHeld)
        {
            leftRepeatTime--;
            if (leftRepeatTime <= 0)
            {
                if (CanMoveLeft(currentBlock))
                {
                    currentBlock.X--;
                    moved = true;
                }
                leftRepeatTime = ARR;
            }
        }

        if (rightHeld)
        {
            rightRepeatTime--;
            if (rightRepeatTime <= 0)
            {
                if (CanMoveRight(currentBlock))
                {
                    currentBlock.X++;
                    moved = true;
                }
                rightRepeatTime = ARR;
            }
        }

        if (downHeld)
        {
            downRepeatTime--;
            if (downRepeatTime <= 0)
            {
                if (CanMoveDown(currentBlock))
                {
                    currentBlock.Y++;
                    moved = true;
                }
                else
                {
                    LockPiece(currentBlock);
                    locked = true;
                }
                downRepeatTime = ARR;
            }
        }

        if (!anyKeyPressed)
        {
            leftHeld = false;
            rightHeld = false;
            downHeld = false;
            rotateHeld = false;
        }

        return moved;
    }

    private void ShowGameOver()
    {
        canvas.Clear(Color.Black);
        string gameOver = "GAME OVER!";

        // OPTIMIZED: StringBuilder statt String-Interpolation
        sb.Clear();
        sb.Append("Score: ");
        sb.Append(score);
        string scoreText = sb.ToString();

        string pressKey = "Press any key...";

        int centerX = (int)DisplaySettings.ScreenWidth / 2;
        int centerY = (int)DisplaySettings.ScreenHeight / 2;

        canvas.DrawString(gameOver, font, redPen,
            centerX - (gameOver.Length * font.Width / 2), centerY - 30);
        canvas.DrawString(scoreText, font, yellowPen,
            centerX - (scoreText.Length * font.Width / 2), centerY);
        canvas.DrawString(pressKey, font, whitePen,
            centerX - (pressKey.Length * font.Width / 2), centerY + 30);

        canvas.Display();

        while (!Sys.KeyboardManager.KeyAvailable)
            Global.PIT.Wait(10);
        Sys.KeyboardManager.ReadKey();
    }

    public void Run()
    {
        canvas.Clear(Color.Black);
        score = 0;
        for (int i = 0; i < field.Length; i++)
            field[i] = false;

        currentBlock = SpawnTetromino(GetRandomTetrominoType());
        nextType = GetRandomTetrominoType();

        int gravityCounter = 0;
        const int GRAVITY_DELAY = 25;
        bool running = true;
        bool needsRedraw = true;
        int linesCleared = 0;
        bool locked = false;
        bool quit = false;
        bool moved = false;
        int gravitySpeed = 0;
        ulong frameStartTime = 0;
        ulong frameEndTime = 0;
        ulong frameTime = 0;
        ulong waitTime = 0;

        while (running)
        {
            frameStartTime = (ulong)DateTime.Now.Ticks;

            locked = false;
            locked = false;
            moved = HandleInput(out locked, out quit);
            if (quit) running = false;
            if (moved) needsRedraw = true;

            gravityCounter++;
            gravitySpeed = GRAVITY_DELAY - (score / 5000);
            if (gravitySpeed < 1) gravitySpeed = 1;

            if (gravityCounter >= gravitySpeed)
            {
                gravityCounter = 0;
                needsRedraw = true;
                if (CanMoveDown(currentBlock))
                {
                    currentBlock.Y++;
                }
                else
                {
                    LockPiece(currentBlock);
                    locked = true;
                }
            }

            if (locked)
            {
                linesCleared = CheckBoard();
                score += (linesCleared * linesCleared * 100);

                if (!CanSpawn(nextType))
                {
                    running = false;
                }
                else
                {
                    currentBlock = SpawnTetromino(nextType);
                    nextType = GetRandomTetrominoType();
                    needsRedraw = true;
                }
            }
            Heap.Collect();

            if (needsRedraw)
            {
                DrawBoard();
                needsRedraw = false;
            }

            frameEndTime = (ulong)DateTime.Now.Ticks;
            frameTime = (frameEndTime - frameStartTime) * 100;
            waitTime = 0;
            if (frameTime > TARGET_FRAME_TIME_NS)
            {
                waitTime = 1;
            }
            else
            {
                waitTime = TARGET_FRAME_TIME_NS - frameTime;
            }

            if (waitTime > 0)
            {
                Global.PIT.WaitNS(waitTime);
            }
        }

        ShowGameOver();

        // aufräumen vor letztem GC
        sb.Clear();
        currentBlock = null;
        random = null;
        field = null;
        sb = null;
        Heap.Collect();
    }
}