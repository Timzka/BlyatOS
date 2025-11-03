using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BlyatOS.Library;
using Cosmos.System;
using Console = System.Console;

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
        Shape = new bool[16]; // 4x4 = 16
        InitializeShape();
    }

    // helper function to convert 2D coordinates to 1D index
    private int GetShapeIndex(int x, int y)
    {
        return y * 4 + x;
    }

    // helper function to get shape value at 2D coordinates
    public bool GetShape(int x, int y)
    {
        return Shape[GetShapeIndex(x, y)];
    }

    // helper function to set shape value at 2D coordinates
    public void SetShape(int x, int y, bool value)
    {
        Shape[GetShapeIndex(x, y)] = value;
    }

    private void InitializeShape()
    {
        switch (Type)
        {
            case TetrominoType.I:
                // Row 1: true, true, true, true
                SetShape(0, 1, true); SetShape(1, 1, true); SetShape(2, 1, true); SetShape(3, 1, true);
                break;
            case TetrominoType.O:
                // Row 1: false, true, true, false
                // Row 2: false, true, true, false
                SetShape(1, 1, true); SetShape(2, 1, true);
                SetShape(1, 2, true); SetShape(2, 2, true);
                break;
            case TetrominoType.T:
                // Row 1: true, true, true, false
                // Row 2: false, true, false, false
                SetShape(0, 1, true); SetShape(1, 1, true); SetShape(2, 1, true);
                SetShape(1, 2, true);
                break;
            case TetrominoType.S:
                // Row 1: false, true, true, false
                // Row 2: true, true, false, false
                SetShape(1, 1, true); SetShape(2, 1, true);
                SetShape(0, 2, true); SetShape(1, 2, true);
                break;
            case TetrominoType.Z:
                // Row 1: true, true, false, false
                // Row 2: false, true, true, false
                SetShape(0, 1, true); SetShape(1, 1, true);
                SetShape(1, 2, true); SetShape(2, 2, true);
                break;
            case TetrominoType.J:
                // Row 1: true, true, true, false
                // Row 2: false, false, true, false
                SetShape(0, 1, true); SetShape(1, 1, true); SetShape(2, 1, true);
                SetShape(2, 2, true);
                break;
            case TetrominoType.L:
                // Row 1: true, true, true, false
                // Row 2: true, false, false, false
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

public class InputState
{
    public bool LeftHeld = false;
    public bool RightHeld = false;
    public bool DownHeld = false;
    public bool RotateHeld = false;
    public int LeftRepeatTime = 0;
    public int RightRepeatTime = 0;
    public int DownRepeatTime = 0;
    public int RotateRepeatTime = 0;
}

public class BadTetris
{
    private const int FIELD_WIDTH = 10;
    private const int FIELD_HEIGHT = 20;
    private const int PADDING = 20; // a try to make it align in the middle of the screen //TODO: make a config option for this instead of hardcoded
    private const int DAS = 8;   // Delayed Auto Shift 
    private const int ARR = 2;   // Auto Repeat Rate 

    private bool[] field;
    private Tetromino currentBlock = null!;
    private TetrominoType nextType;
    private int score;
    private Random random;
    private InputState inputState;
    private int lastGravityTick;
    private const int STANDARD_GRAVITY_INTERVAL = 1000;

    public BadTetris()
    {
        field = new bool[FIELD_HEIGHT * FIELD_WIDTH];
        random = new Random();
        inputState = new InputState();
        score = 0;
    }

    // helper function to convert 2D field coordinates to 1D index
    private int GetFieldIndex(int x, int y)
    {
        return y * FIELD_WIDTH + x;
    }

    // helper function to get field value at 2D coordinates
    private bool GetField(int x, int y)
    {
        return field[GetFieldIndex(x, y)];
    }

    // helper function to set field value at 2D coordinates
    private void SetField(int x, int y, bool value)
    {
        field[GetFieldIndex(x, y)] = value;
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

    private void HideCursor()
    {
    }

    private void ShowCursor()
    {
    }

    private void ClearScreen()
    {
        Console.SetCursorPosition(0, 0);
    }

    private void PrintPadding(int padding)
    {
        if (padding > 0)
            Console.Write(new string(' ', padding));
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

    private bool CanMoveLeft(Tetromino block)
    {
        for (int by = 0; by < 4; by++)
            for (int bx = 0; bx < 4; bx++)
                if (block.GetShape(bx, by))
                {
                    int nx = block.X + bx - 1;
                    int ny = block.Y + by;
                    if (nx < 0) return false;
                    if (GetField(nx, ny)) return false;
                }
        return true;
    }

    private bool CanMoveRight(Tetromino block)
    {
        for (int by = 0; by < 4; by++)
            for (int bx = 0; bx < 4; bx++)
                if (block.GetShape(bx, by))
                {
                    int nx = block.X + bx + 1;
                    int ny = block.Y + by;
                    if (nx >= FIELD_WIDTH) return false;
                    if (GetField(nx, ny)) return false;
                }
        return true;
    }

    private bool CanMoveDown(Tetromino block)
    {
        for (int by = 0; by < 4; by++)
            for (int bx = 0; bx < 4; bx++)
                if (block.GetShape(bx, by))
                {
                    int nx = block.X + bx;
                    int ny = block.Y + by + 1;
                    if (ny >= FIELD_HEIGHT) return false;
                    if (GetField(nx, ny)) return false;
                }
        return true;
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
                        SetField(nx, ny, true);
                }
    }

    private int HardDrop(Tetromino block)
    {
        int dropDistance = 0;

        // simulate "s" keypress until at the bottom from current position
        while (CanMoveDown(block))
        {
            block.Y++;
            dropDistance++;
        }
        
        return dropDistance;
    }

    private bool HandleInput(out bool locked, out bool quit, ref int inputCooldown)
    {
        bool moved = false;
        locked = false;
        quit = false;
        bool anyKeyPressed = false;

        if (KeyboardManager.KeyAvailable)
        {
            KeyEvent keyEvent;
            if (KeyboardManager.TryReadKey(out keyEvent))
            {
                anyKeyPressed = true;

                // Process the key event
                switch (keyEvent.Key)
                {
                    case ConsoleKeyEx.A:
                    case ConsoleKeyEx.LeftArrow:
                        if (!inputState.LeftHeld)
                        {
                            if (CanMoveLeft(currentBlock))
                            {
                                currentBlock.X--;
                                moved = true;
                            }
                            inputState.LeftHeld = true;
                            inputState.LeftRepeatTime = DAS;
                        }
                        // Reset other directions when pressing left
                        inputState.RightHeld = false;
                        break;

                    case ConsoleKeyEx.D:
                    case ConsoleKeyEx.RightArrow:
                        if (!inputState.RightHeld)
                        {
                            if (CanMoveRight(currentBlock))
                            {
                                currentBlock.X++;
                                moved = true;
                            }
                            inputState.RightHeld = true;
                            inputState.RightRepeatTime = DAS;
                        }
                        // Reset other directions when pressing right
                        inputState.LeftHeld = false;
                        break;

                    case ConsoleKeyEx.S:
                    case ConsoleKeyEx.DownArrow:
                        if (!inputState.DownHeld)
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
                            inputState.DownHeld = true;
                            inputState.DownRepeatTime = DAS;
                        }
                        break;

                    case ConsoleKeyEx.W:
                    case ConsoleKeyEx.UpArrow:
                        if (!inputState.RotateHeld)
                        {
                            currentBlock.RotateWithKick(FIELD_WIDTH, FIELD_HEIGHT, field);
                            moved = true;
                            inputState.RotateHeld = true;
                        }
                        break;

                    case ConsoleKeyEx.Spacebar:
                        // HARD DROP - instant drop to bottom
                        int dropDistance = HardDrop(currentBlock);
                        if (dropDistance > 0)
                        {
                            moved = true;
                            // Lock the piece immediately after hard drop
                            LockPiece(currentBlock);
                            locked = true;
                        }
                        break;

                    case ConsoleKeyEx.Q:
                    case ConsoleKeyEx.Escape:
                        quit = true;
                        break;
                }
            }
        }

        // Handle DAS/ARR for held keys
        if (inputState.LeftHeld)
        {
            inputState.LeftRepeatTime--;
            if (inputState.LeftRepeatTime <= 0)
            {
                if (CanMoveLeft(currentBlock))
                {
                    currentBlock.X--;
                    moved = true;
                }
                inputState.LeftRepeatTime = ARR;
            }
        }

        if (inputState.RightHeld)
        {
            inputState.RightRepeatTime--;
            if (inputState.RightRepeatTime <= 0)
            {
                if (CanMoveRight(currentBlock))
                {
                    currentBlock.X++;
                    moved = true;
                }
                inputState.RightRepeatTime = ARR;
            }
        }

        if (inputState.DownHeld)
        {
            inputState.DownRepeatTime--;
            if (inputState.DownRepeatTime <= 0)
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
                inputState.DownRepeatTime = ARR; // Same as other movements
            }
        }

        // Aggressive key release detection - reset after just 1 frame of no input
        if (!anyKeyPressed)
        {
            inputState.LeftHeld = false;
            inputState.RightHeld = false;
            inputState.DownHeld = false;
            inputState.RotateHeld = false;
            inputState.LeftRepeatTime = 0;
            inputState.RightRepeatTime = 0;
            inputState.DownRepeatTime = 0;
            inputState.RotateRepeatTime = 0;
        }

        return moved;
    }

    private bool HandleInputFallback(out bool locked, out bool quit, ref int inputCooldown)
    {
        bool moved = false;
        locked = false;
        quit = false;

        bool anyKeyPressed = false;

        while (Console.KeyAvailable)
        {
            anyKeyPressed = true;
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            
            switch (keyInfo.Key)
            {
                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    if (!inputState.LeftHeld)
                    {
                        if (CanMoveLeft(currentBlock))
                        {
                            currentBlock.X--;
                            moved = true;
                        }
                        inputState.LeftHeld = true;
                        inputState.LeftRepeatTime = DAS;
                    }
                    inputState.RightHeld = false; // Cancel opposite direction
                    break;
                    
                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    if (!inputState.RightHeld)
                    {
                        if (CanMoveRight(currentBlock))
                        {
                            currentBlock.X++;
                            moved = true;
                        }
                        inputState.RightHeld = true;
                        inputState.RightRepeatTime = DAS;
                    }
                    inputState.LeftHeld = false; // Cancel opposite direction
                    break;
                    
                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    if (!inputState.DownHeld)
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
                        inputState.DownHeld = true;
                        inputState.DownRepeatTime = DAS;
                    }
                    break;
                    
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    if (!inputState.RotateHeld)
                    {
                        currentBlock.RotateWithKick(FIELD_WIDTH, FIELD_HEIGHT, field);
                        moved = true;
                        inputState.RotateHeld = true;
                    }
                    break;
                    
                case ConsoleKey.Spacebar:
                    // HARD DROP - instant drop to bottom
                    int dropDistance = HardDrop(currentBlock);
                    if (dropDistance > 0)
                    {
                        moved = true;
                        LockPiece(currentBlock);
                        locked = true;
                    }
                    break;
                    
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    quit = true;
                    break;
            }
        }

        if (inputState.LeftHeld)
        {
            inputState.LeftRepeatTime--;
            if (inputState.LeftRepeatTime <= 0)
            {
                if (CanMoveLeft(currentBlock))
                {
                    currentBlock.X--;
                    moved = true;
                }
                inputState.LeftRepeatTime = ARR;
            }
        }

        if (inputState.RightHeld)
        {
            inputState.RightRepeatTime--;
            if (inputState.RightRepeatTime <= 0)
            {
                if (CanMoveRight(currentBlock))
                {
                    currentBlock.X++;
                    moved = true;
                }
                inputState.RightRepeatTime = ARR;
            }
        }

        if (inputState.DownHeld)
        {
            inputState.DownRepeatTime--;
            if (inputState.DownRepeatTime <= 0)
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
                inputState.DownRepeatTime = ARR;
            }
        }

        if (!anyKeyPressed)
        {
            inputState.LeftHeld = false;
            inputState.RightHeld = false;
            inputState.DownHeld = false;
            inputState.RotateHeld = false;
            inputState.LeftRepeatTime = 0;
            inputState.RightRepeatTime = 0;
            inputState.DownRepeatTime = 0;
            inputState.RotateRepeatTime = 0;
        }

        return moved;
    }

    //animation for rows to delete
    private void FlashRows(List<int> rows)
    {
        if (rows.Count == 0) return;

        for (int i = 0; i < 2; i++)
        {
            DrawBoard(rows);
            Thread.Sleep(30);
            DrawBoard(new List<int>());
            Thread.Sleep(15);
        }
    }

    private int CheckBoard()
    {
        // collect all full rows
        List<int> fullRows = new List<int>();
        for (int y = FIELD_HEIGHT - 1; y >= 0; y--)
        {
            bool full = true;
            for (int x = 0; x < FIELD_WIDTH; x++)
            {
                if (!GetField(x, y)) { full = false; break; }
            }
            if (full)
            {
                fullRows.Add(y);
            }
        }

        // flash full rows 
        if (fullRows.Count > 0)
        {
            FlashRows(fullRows);
        }

        // remove full rows from bottom to top one at a time
        int linesCleared = 0;
        for (int y = FIELD_HEIGHT - 1; y >= 0; y--)
        {
            bool full = true;
            for (int x = 0; x < FIELD_WIDTH; x++)
            {
                if (!GetField(x, y)) { full = false; break; }
            }

            if (full)
            {
                // shift all rows above down by one
                for (int ny = y; ny > 0; ny--)
                {
                    for (int x = 0; x < FIELD_WIDTH; x++)
                    {
                        SetField(x, ny, GetField(x, ny - 1));
                    }
                }
                // clear the new top row since it would be doubled elseway
                for (int x = 0; x < FIELD_WIDTH; x++)
                    SetField(x, 0, false);

                // re-check same y after shifting
                y++;
                linesCleared++;
            }
        }
        return linesCleared;
    }

    private void DrawBoard(List<int>? highlightRows = null)
    {
        highlightRows ??= new List<int>();

        ClearScreen();
        
        PrintPadding(PADDING);
        Console.WriteLine($"Next: {TetrominoTypeToString(nextType)}");
        Console.WriteLine();

        for (int y = 0; y < FIELD_HEIGHT; y++)
        {
            // highlight this row if it's in the highlight list
            bool rowHighlighted = highlightRows.Contains(y);

            PrintPadding(PADDING);
            Console.Write("|");
            for (int x = 0; x < FIELD_WIDTH; x++)
            {
                bool drawn = false;
                if (rowHighlighted)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                // active tetromino
                for (int by = 0; by < 4; by++)
                    for (int bx = 0; bx < 4; bx++)
                        if (currentBlock.GetShape(bx, by) && x == currentBlock.X + bx && y == currentBlock.Y + by)
                        {
                            Console.Write("[]");
                            drawn = true;
                        }

                // locked field blocks
                if (!drawn && GetField(x, y))
                {
                    Console.Write("[]");
                    drawn = true;
                }

                if (!drawn) Console.Write("  ");
            }
            // reset color after row if it was highlighted
            if (rowHighlighted)
            {
                Console.ResetColor();
            }
            Console.WriteLine("|");
        }

        PrintPadding(PADDING);
        Console.Write("+");
        for (int i = 0; i < FIELD_WIDTH * 2; i++) Console.Write("-");
        Console.WriteLine("+");
        
        PrintPadding(PADDING);
        Console.WriteLine($"Score: {score}");
    }

    private void ShowGameOverScreen()
    {
        Console.Clear();

        for (int i = 0; i < FIELD_HEIGHT / 2; i++) Console.WriteLine();
        PrintPadding(PADDING);
        Console.WriteLine("Game Over!");
        PrintPadding(PADDING);
        Console.WriteLine($"Your score: {score}");
        for (int i = 0; i < FIELD_HEIGHT / 2; i++) Console.WriteLine();
        Thread.Sleep(1000);
    }

    // called by kernel to run the game
    public void Run()
    {
        Console.Clear();
        HideCursor();

        score = 0;
        field = new bool[FIELD_HEIGHT * FIELD_WIDTH];
        currentBlock = SpawnTetromino(GetRandomTetrominoType());
        nextType = GetRandomTetrominoType();
        inputState = new InputState();

        int gravityCounter = 0;
        const int GRAVITY_DELAY = 25;
        int inputCooldown = 0; 

        bool running = true;
        bool needsRedraw = true;

        while (running)
        {
            bool locked = false;
            bool quit = false;
            bool moved = HandleInput(out locked, out quit, ref inputCooldown);
            if (quit) running = false;
            if (moved) needsRedraw = true;

            gravityCounter++;
            int gravitySpeed = GRAVITY_DELAY - (score / 5000); // speed up with score
            if (gravitySpeed < 1) gravitySpeed = 1; // minimum speed
            
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
                int linesCleared = CheckBoard();
                score += (linesCleared * linesCleared * 100);
                DrawBoard();
                needsRedraw = false;
            }
            Thread.Sleep(10);
        }
        ShowGameOverScreen();

        ShowCursor();

        // wait for input before ending the function --> will remove score from screen after
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey(true);
        Console.Clear();
    }
}
