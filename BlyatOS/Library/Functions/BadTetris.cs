using System;
using System.Collections.Generic;
using System.Drawing;
using Cosmos.HAL;
using Cosmos.System.Graphics;
using BlyatOS.Library.Configs;
using Sys = Cosmos.System;
using Cosmos.System.Graphics.Fonts;
using BlyatOS.Library.Helpers;

namespace BadTetrisCS;

public enum TetrominoType { I, O, T, S, Z, J, L }

public class Tetromino
{
    public TetrominoType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool[] Shape { get; set; }
    public Color BlockColor { get; set; }

    public Tetromino(TetrominoType type, int startX, int startY)
    {
        Type = type;
        X = startX;
        Y = startY;
        Shape = new bool[16];
        BlockColor = GetColorForType(type);
        InitializeShape();
    }

    private Color GetColorForType(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.I: return Color.Cyan;
            case TetrominoType.O: return Color.Yellow;
            case TetrominoType.T: return Color.Purple;
            case TetrominoType.S: return Color.Green;
            case TetrominoType.Z: return Color.Red;
            case TetrominoType.J: return Color.Blue;
            case TetrominoType.L: return Color.Orange;
            default: return Color.White;
        }
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

    public bool CanPlace(int fieldWidth, int fieldHeight, Color[] field, int xOffset = 0, int yOffset = 0)
    {
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                if (GetShape(x, y))
                {
                    int newX = X + x + xOffset;
                    int newY = Y + y + yOffset;
                    if (newX < 0 || newX >= fieldWidth || newY < 0 || newY >= fieldHeight)
                        return false;
                    if (field[newY * fieldWidth + newX].ToArgb() != Color.Black.ToArgb())
                        return false;
                }
        return true;
    }

    public void RotateWithKick(int fieldWidth, int fieldHeight, Color[] field)
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
    private const int DAS = 8;   // Delayed Auto Shift
    private const int ARR = 1;   // Auto Repeat Rate

    private const ulong TARGET_FRAME_TIME_NS = 16666666; // 60 FPS = ~16.67ms in nanoseconds

    private Color[] field;
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

    private int boardX;
    private int boardY;

    public BadTetris()
    {
        canvas = DisplaySettings.Canvas;
        font = DisplaySettings.Font;
        field = new Color[FIELD_HEIGHT * FIELD_WIDTH];
        random = new Random();
        score = 0;

        boardX = ((int)DisplaySettings.ScreenWidth - (FIELD_WIDTH * BLOCK_SIZE)) / 2;
        boardY = 50;

        for (int i = 0; i < field.Length; i++)
            field[i] = Color.Black;
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
                        field[ny * FIELD_WIDTH + nx] = block.BlockColor;
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

    private void DrawBlock(int x, int y, Color color)
    {
        int px = boardX + x * BLOCK_SIZE;
        int py = boardY + y * BLOCK_SIZE;

        canvas.DrawFilledRectangle(new Pen(color), px, py, BLOCK_SIZE - 1, BLOCK_SIZE - 1);
        canvas.DrawRectangle(new Pen(Color.White), px, py, BLOCK_SIZE - 1, BLOCK_SIZE - 1);
    }

    // CHANGED: Separated drawing logic - draw without current piece
    private void DrawBoardBase()
    {
        canvas.Clear(Color.Black);

        string title = "TETRIS";
        canvas.DrawString(title, font, new Pen(Color.White),
            boardX + (FIELD_WIDTH * BLOCK_SIZE / 2) - (title.Length * font.Width / 2), 10);

        string scoreText = $"Score: {score}";
        canvas.DrawString(scoreText, font, new Pen(Color.Yellow), boardX, boardY - 25);

        string nextText = $"Next: {TetrominoTypeToString(nextType)}";
        canvas.DrawString(nextText, font, new Pen(Color.Cyan),
            boardX + FIELD_WIDTH * BLOCK_SIZE + 20, boardY);

        int borderX = boardX - 2;
        int borderY = boardY - 2;
        int borderW = FIELD_WIDTH * BLOCK_SIZE + 3;
        int borderH = FIELD_HEIGHT * BLOCK_SIZE + 3;
        canvas.DrawRectangle(new Pen(Color.White), borderX, borderY, borderW, borderH);

        // Draw locked pieces only
        for (int y = 0; y < FIELD_HEIGHT; y++)
        {
            for (int x = 0; x < FIELD_WIDTH; x++)
            {
                Color blockColor = field[y * FIELD_WIDTH + x];
                if (blockColor.ToArgb() != Color.Black.ToArgb())
                {
                    DrawBlock(x, y, blockColor);
                }
            }
        }

        string controls = "WASD/Arrows | Space=Drop | Q=Quit";
        canvas.DrawString(controls, font, new Pen(Color.Gray),
            10, (int)DisplaySettings.ScreenHeight - 20);
    }

    // CHANGED: Draw complete board with current piece
    private void DrawBoard()
    {
        DrawBoardBase();

        // Draw current piece on top
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
                        DrawBlock(x, y, currentBlock.BlockColor);
                    }
                }
            }
        }

        canvas.Display();
    }

    // CHANGED: Flash animation now uses DrawBoardBase instead of full board
    private void FlashRows(List<int> rows)
    {
        if (rows.Count == 0) return;

        for (int flash = 0; flash < 3; flash++)
        {
            // Draw base board without current piece
            DrawBoardBase();

            // Flash rows
            foreach (int row in rows)
            {
                for (int x = 0; x < FIELD_WIDTH; x++)
                {
                    DrawBlock(x, row, Color.DeepPink);
                }
            }
            canvas.Display();
            Global.PIT.Wait(5);

            // Draw base board without current piece
            DrawBoardBase();

            // Flash back to original
            foreach (int row in rows)
            {
                for (int x = 0; x < FIELD_WIDTH; x++)
                {
                    DrawBlock(x, row, field[row * FIELD_WIDTH + x]);
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
                if (field[y * FIELD_WIDTH + x].ToArgb() == Color.Black.ToArgb())
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
                if (field[y * FIELD_WIDTH + x].ToArgb() == Color.Black.ToArgb())
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
                    field[x] = Color.Black;

                y++;
                linesCleared++;
            }
        }

        return linesCleared;
    }

    // CHANGED: Improved input handling with better key detection
    private bool HandleInput(out bool locked, out bool quit)
    {
        bool moved = false;
        locked = false;
        quit = false;
        bool anyKeyPressed = false;

        // Clear all keys from buffer for responsive input
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

        // CHANGED: Smoother DAS/ARR handling
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

        // Release keys immediately when not pressed
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
        string scoreText = $"Score: {score}";
        string pressKey = "Press any key...";

        int centerX = (int)DisplaySettings.ScreenWidth / 2;
        int centerY = (int)DisplaySettings.ScreenHeight / 2;

        canvas.DrawString(gameOver, font, new Pen(Color.Red),
            centerX - (gameOver.Length * font.Width / 2), centerY - 30);
        canvas.DrawString(scoreText, font, new Pen(Color.Yellow),
            centerX - (scoreText.Length * font.Width / 2), centerY);
        canvas.DrawString(pressKey, font, new Pen(Color.White),
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
            field[i] = Color.Black;

        currentBlock = SpawnTetromino(GetRandomTetrominoType());
        nextType = GetRandomTetrominoType();

        int gravityCounter = 0;
        const int GRAVITY_DELAY = 25;
        bool running = true;
        bool needsRedraw = true;

        while (running)
        {
            // Frame timing start
            ulong frameStartTime = (ulong)DateTime.Now.Ticks;

            bool locked = false;
            bool quit = false;
            bool moved = HandleInput(out locked, out quit);
            if (quit) running = false;
            if (moved) needsRedraw = true;

            gravityCounter++;
            int gravitySpeed = GRAVITY_DELAY - (score / 5000);
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
                // Check and clear lines BEFORE spawning new piece
                int linesCleared = CheckBoard();
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

            if (needsRedraw)
            {
                DrawBoard();
                needsRedraw = false;
            }

            // Frame timing end - dynamic wait for 60 FPS max
            ulong frameEndTime = (ulong)DateTime.Now.Ticks;
            ulong frameTime = (frameEndTime - frameStartTime) * 100; // Convert to nanoseconds (1 tick = 100ns)
            ulong waitTime = 0;
            if (frameTime > TARGET_FRAME_TIME_NS)
            {
                waitTime = 1; // Prevent negative wait time --> ulong makes it very large
            }
            else
            {
                waitTime = TARGET_FRAME_TIME_NS - frameTime;
            }

            if (waitTime > 0)
            {
                // Wait for remaining time to hit target FPS
                Global.PIT.WaitNS(waitTime);
            }
        }

        ShowGameOver();
    }
}