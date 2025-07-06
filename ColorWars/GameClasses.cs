using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;
using VectorGui;

public class BoardPiece
{
    public Player Player { get; set; }

    public int PieceLevel { get; set; }
    public Vector2 BoardPosition { get; set; }
    public Gui.GuiRectangle GuiRectangle { get; set; }

    public BoardPiece(
        Player player,
        int pieceLevel,
        Vector2 boardPosition,
        Gui.GuiRectangle guiRectangle
    )
    {
        Player = player;
        BoardPosition = boardPosition;
        PieceLevel = pieceLevel;
        GuiRectangle = guiRectangle;
    }

    public static BoardPiece DefaultPiece()
    {
        return new BoardPiece(
            null,
            0,
            Vector2.Zero,
            new Gui.GuiRoundedRectangle(Vector2.Zero, Vector2.One * 50, 10, Color.White)
        );
    }

    public void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch, SpriteFont font)
    {
        if (GuiRectangle != null && GuiRectangle.IsVisible)
        {
            // Lighten color if hovered
            Color drawColor = GuiRectangle.Color;

            if (GuiRectangle.IsHovered(Mouse.GetState()))
            {
                if (Player != null)
                    drawColor = new Color(
                        (int)Math.Min(drawColor.R + 60, 255),
                        (int)Math.Min(drawColor.G + 60, 255),
                        (int)Math.Min(drawColor.B + 60, 255),
                        drawColor.A
                    );
                else
                    drawColor = new Color(
                        (int)Math.Min(drawColor.R - 30, 255),
                        (int)Math.Min(drawColor.G - 30, 255),
                        (int)Math.Min(drawColor.B - 30, 255),
                        drawColor.A
                    );
            }
            GuiRectangle.Color = drawColor;
            GuiRectangle.Draw(spriteBatch, primitiveBatch);
            if (Player != null)
            {
                spriteBatch.DrawString(
                    font,
                    PieceLevel.ToString(),
                    GuiRectangle.Position + new Vector2(10, 10),
                    Color.Black
                );
            }
        }
    }
}

public class Board
{
    public BoardPiece[][] BoardArray { get; set; }
    public Vector2 BoardSize { get; set; }

    public Board(int width, int height)
    {
        BoardSize = new Vector2(width, height);
        BoardArray = new BoardPiece[width][];
        for (int i = 0; i < width; i++)
        {
            BoardArray[i] = new BoardPiece[height];
            for (int j = 0; j < height; j++)
            {
                BoardArray[i][j] = BoardPiece.DefaultPiece();
                BoardArray[i][j].BoardPosition = new Vector2(i, j);
            }
        }
    }

    public bool PlacePiece(Player player, Vector2 position)
    {
        BoardPiece piece = BoardArray[(int)position.X][(int)position.Y];
        if (piece.Player == null)
        {
            piece.Player = player;
            piece.PieceLevel = 3;
            BoardArray[(int)position.X][(int)position.Y] = piece;

            return true;
        }
        else if (piece.Player == player)
        {
            throw new InvalidOperationException(
                "It should be first turn so this must not be able to happen"
            );
        }
        else
        {
            return false;
        }
    }

    public bool AddPiece(Player player, Vector2 position)
    {
        if (
            position.X <= 0
            && position.X > BoardArray.Length
            && position.Y <= 0
            && position.Y > BoardArray[0].Length
        )
        {
            throw new ArgumentOutOfRangeException(
                "Position is out of bounds of the board",
                $"Position: {position}, Board Size: {BoardSize}"
            );
        }

        BoardPiece piece = BoardArray[(int)position.X][(int)position.Y];
        if (piece.Player == null)
        {
            return false;
        }
        else if (piece.Player == player)
        {
            piece.PieceLevel++;
            return true;
        }
        else
        {
            return false; // Cannot place on an opponent's piece
        }
    }

    public BoardPiece[][] StealPiece(Player player, Vector2 position, BoardPiece[][] board)
    {
        BoardPiece piece = board[(int)position.X][(int)position.Y];

        piece.Player = player;
        piece.PieceLevel++;
        return board;
    }

    public BoardPiece GetPiece(Vector2 position)
    {
        if (
            position.X < 0
            || position.X >= BoardArray.Length
            || position.Y < 0
            || position.Y >= BoardArray[0].Length
        )
        {
            BoardArray[(int)position.X][(int)position.Y] = BoardPiece.DefaultPiece();
        }
        return BoardArray[(int)position.X][(int)position.Y];
    }

    public void RemovePiece(Vector2 position)
    {
        if (
            position.X >= 0
            && position.X < BoardArray.Length
            && position.Y >= 0
            && position.Y < BoardArray[0].Length
        )
        {
            BoardArray[(int)position.X][(int)position.Y].PieceLevel = 0;
            BoardArray[(int)position.X][(int)position.Y].Player = null;
        }
    }

    public void ClearBoard()
    {
        for (int i = 0; i < BoardArray.Length; i++)
        {
            for (int j = 0; j < BoardArray[i].Length; j++)
            {
                BoardArray[i][j].Player = null;
            }
        }
    }

    private (BoardPiece[][], Dictionary<Vector2, Vector2[]>) ExplodePiece(
        BoardPiece[][] board,
        BoardPiece piece
    )
    {
        Dictionary<Vector2, Vector2[]> explodedPieces = new Dictionary<Vector2, Vector2[]>();
        explodedPieces[piece.BoardPosition] = new Vector2[0];

        if (piece.PieceLevel < 4)
        {
            return (board, explodedPieces);
        }

        Vector2[] directions =
        {
            new Vector2(1, 0),
            new Vector2(-1, 0),
            new Vector2(0, 1),
            new Vector2(0, -1),
        };
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 direction = directions[i];
            Vector2 newPosition = piece.BoardPosition + direction;
            if (
                newPosition.X >= 0
                && newPosition.X < board.Length
                && newPosition.Y >= 0
                && newPosition.Y < board[0].Length
            )
            {
                BoardPiece targetPiece = board[(int)newPosition.X][(int)newPosition.Y];
                if (targetPiece.Player != null)
                {
                    explodedPieces[newPosition] = new Vector2[] { targetPiece.BoardPosition };
                    board = StealPiece(piece.Player, newPosition, board);
                }
            }
        }

        RemovePiece(piece.BoardPosition);
        return (board, explodedPieces);
    }

    protected List<(Vector2, Vector2[])> CheckExplosionsAndScoreList()
    {
        bool isExplosivePieces = false;
        var explodedPieces = new List<(Vector2, Vector2[])>();
        foreach (BoardPiece[] row in BoardArray)
        {
            foreach (BoardPiece piece in row)
            {
                if (piece.Player != null)
                {
                    if (piece.Player.IsActive)
                        piece.Player.AddScore(piece.PieceLevel);
                    if (piece.PieceLevel >= 4)
                    {
                        (BoardPiece[][] newBoard, List<(Vector2, Vector2[])> newExplodedPieces) =
                            ExplodePieceList(BoardArray, piece);
                        BoardArray = newBoard;
                        explodedPieces.AddRange(newExplodedPieces);
                        isExplosivePieces = true;
                    }
                }
            }
        }
        if (isExplosivePieces)
        {
            explodedPieces.AddRange(CheckExplosionsAndScoreList());
        }
        return explodedPieces;
    }

    protected (BoardPiece[][], List<(Vector2, Vector2[])>) ExplodePieceList(
        BoardPiece[][] board,
        BoardPiece piece
    )
    {
        var explodedPieces = new List<(Vector2, Vector2[])>();
        var from = piece.BoardPosition;
        var toList = new List<Vector2>();
        if (piece.PieceLevel < 4)
        {
            return (board, explodedPieces);
        }
        Vector2[] directions =
        {
            new Vector2(1, 0),
            new Vector2(-1, 0),
            new Vector2(0, 1),
            new Vector2(0, -1),
        };
        foreach (var direction in directions)
        {
            Vector2 newPosition = piece.BoardPosition + direction;
            if (
                newPosition.X >= 0
                && newPosition.X < board.Length
                && newPosition.Y >= 0
                && newPosition.Y < board[0].Length
            )
            {
                BoardPiece targetPiece = board[(int)newPosition.X][(int)newPosition.Y];

                toList.Add(targetPiece.BoardPosition);
                board = StealPiece(piece.Player, newPosition, board);
            }
        }
        explodedPieces.Add((from, toList.ToArray()));
        RemovePiece(piece.BoardPosition);
        return (board, explodedPieces);
    }

    public (bool, Dictionary<Vector2, Vector2[]>) CheckExplosionsAndScore()
    {
        bool isExplosivePieces = true;
        Dictionary<Vector2, Vector2[]> explodedPieces = new Dictionary<Vector2, Vector2[]>();
        foreach (BoardPiece[] row in BoardArray)
        {
            foreach (BoardPiece piece in row)
            {
                if (piece.Player != null)
                {
                    if (piece.Player.IsActive)
                        piece.Player.AddScore(piece.PieceLevel);
                    if (piece.PieceLevel >= 4)
                    {
                        (
                            BoardPiece[][] newBoard,
                            Dictionary<Vector2, Vector2[]> newExplodedPieces
                        ) = ExplodePiece(BoardArray, piece);
                        BoardArray = newBoard;
                        explodedPieces = newExplodedPieces;
                        isExplosivePieces = true;
                    }
                    else if (piece.PieceLevel < 4)
                        isExplosivePieces = false;
                    else
                        isExplosivePieces = false;
                }
            }
        }
        return (isExplosivePieces, explodedPieces);
    }
}

public class GuiBoard : Board
{
    public Vector2 Position { get; set; }
    public int Spacing { get; set; } = 10;
    public int Border { get; set; } = 5;
    public Vector2 Size { get; set; }
    public Gui.GuiRectangle Background { get; set; }

    public GuiBoard(
        int width,
        int height,
        Vector2 position,
        Vector2 size,
        Gui.GuiRectangle background
    )
        : base(width, height)
    {
        Position = position;
        Size = size;
        Background = background;
        Background.Position = position;
        Background.Size = size;
    }

    private Gui.GuiRectangle CreateBackground(
        float xPercentageOfScreen,
        float yPercentageOfScreen,
        Vector2 screen,
        Gui.GuiRectangle guiRectangle
    )
    {
        // The background should be 0.75 * width wide and 0.4 * height tall, centered
        Vector2 size = new Vector2(screen.X * xPercentageOfScreen, screen.Y * yPercentageOfScreen);
        Vector2 position = new Vector2((screen.X - size.X) / 2f, (screen.Y - size.Y) / 2f);
        guiRectangle.Size = size;
        guiRectangle.Position = position;
        return guiRectangle;
    }

    public bool UpdateMouse(MouseState mouseState, Player player, bool isFirstTurn)
    {
        bool turn = false;
        if (player != null)
        {
            foreach (BoardPiece[] row in BoardArray)
            {
                foreach (BoardPiece piece in row)
                {
                    if (piece.GuiRectangle != null && piece.GuiRectangle.IsPressed(mouseState))
                    {
                        if (!isFirstTurn)
                            turn = AddPiece(player, piece.BoardPosition);
                        else
                            turn = PlacePiece(player, piece.BoardPosition);
                    }
                }
            }
        }
        if (!player.IsActive)
            return true;

        return turn;
    }

    public (bool, List<(Vector2, Vector2[])>) UpdatePlayer(
        Player player,
        MouseState mouseState,
        bool isFirstTurn
    )
    {
        var explodedPieces = new List<(Vector2, Vector2[])>();
        bool turn = false;
        if (player == null || !player.IsActive)
            return (false, explodedPieces);

        if (UpdateMouse(mouseState, player, isFirstTurn))
        {
            turn = true;
            explodedPieces = CheckExplosionsAndScoreList();
        }

        return (turn, explodedPieces);
    }

    // Draws an explosion animation at the given position, using the same logic as Game1
    public void DrawExplosion(
        SpriteBatch spriteBatch,
        PrimitiveBatch primitiveBatch,
        Vector2 from,
        float t,
        Vector2 cellSize,
        Color color
    )
    {
        Vector2[] directions = new Vector2[]
        {
            new Vector2(1, 0),
            new Vector2(-1, 0),
            new Vector2(0, 1),
            new Vector2(0, -1),
        };
        Vector2 fromPixel = new Vector2(
            Background.Position.X + Border + from.X * (cellSize.X + Spacing) + cellSize.X / 2f,
            Background.Position.Y + Border + from.Y * (cellSize.Y + Spacing) + cellSize.Y / 2f
        );
        for (int i = 0; i < 4; i++)
        {
            Vector2 to = from + directions[i];
            Vector2 toPixel = new Vector2(
                Background.Position.X + Border + to.X * (cellSize.X + Spacing) + cellSize.X / 2f,
                Background.Position.Y + Border + to.Y * (cellSize.Y + Spacing) + cellSize.Y / 2f
            );
            Vector2 pos = Vector2.Lerp(fromPixel, toPixel, t);
            var rect = new PrimitiveBatch.Rectangle(pos - cellSize / 2f, cellSize, color);
            rect.Draw(spriteBatch, primitiveBatch);
        }
    }

    public void Draw(
        SpriteBatch spriteBatch,
        PrimitiveBatch primitiveBatch,
        SpriteFont font,
        Vector2 screen,
        (Vector2, float, Color)? explosion = null
    )
    {
        Background = CreateBackground(0.75f, 0.4f, screen, Background);
        Background.Draw(spriteBatch, primitiveBatch);
        Vector2 cellSize = new Vector2(
            (Background.Size.X - Border * 2 - (BoardArray[0].Length - 1) * Spacing)
                / BoardArray[0].Length,
            (Background.Size.Y - Border * 2 - (BoardArray.Length - 1) * Spacing) / BoardArray.Length
        );

        for (int i = 0; i < BoardArray.Length; i++)
        {
            for (int j = 0; j < BoardArray[i].Length; j++)
            {
                BoardPiece piece = BoardArray[i][j];
                piece.GuiRectangle.Position = new Vector2(
                    Background.Position.X + Border + i * (cellSize.X + Spacing),
                    Background.Position.Y + Border + j * (cellSize.Y + Spacing)
                );
                piece.GuiRectangle.Color = piece.Player != null ? piece.Player.Color : Color.White;
                piece.GuiRectangle.Size = cellSize;
                piece.Draw(spriteBatch, primitiveBatch, font);
            }
        }
        if (explosion.HasValue)
        {
            var (from, t, col) = explosion.Value;
            DrawExplosion(spriteBatch, primitiveBatch, from, t, cellSize, col);
        }
    }
}

public class Player
{
    public Color Color { get; set; }
    public int Score { get; set; }
    public bool IsActive { get; set; } = true;

    public Player(Color color)
    {
        Color = color;
        Score = 0;
    }

    public void AddScore(int points)
    {
        Score += points;
    }
}
