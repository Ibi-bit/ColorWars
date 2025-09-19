using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;
using VectorGui;

namespace ColorWars;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private PrimitiveBatch _primitiveBatch;
    private List<Player> _players = new List<Player>
    {
        new Player(Color.Blue),
        new Player(Color.Green),
        new Player(Color.Yellow),
    };
    private bool _isFirstTurn = true;
    private int _currentPlayerIndex = 0;

    private List<(Vector2, Vector2[])> _explodedPieces;
    private SpriteFont _font;

    private GuiBoard _board;
    private GuiBoard _preExplosionBoard; // Holds the board state before explosion for animation

    private int _explosionIndex = 0;
    private float _explosionTimer = 0f;
    private float _explosionDuration = 0.3f; // seconds per explosion
    private bool _isAnimatingExplosions = false;
    private bool _pendingTurnAdvance = false;

    private Gui.GuiRoundedRectangle _resetButton = new Gui.GuiRoundedRectangle(
        new Vector2(50f, 50f),
        new Vector2(2f, 1f),
        27f,
        Color.Red
    );

    private Texture2D _debugRedTexture;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Color Wars";

        _graphics.PreferredBackBufferWidth = 540;
        _graphics.PreferredBackBufferHeight = 960;
        Window.AllowUserResizing = true;
        
        // Set minimum window size to prevent crashes
        Window.ClientSizeChanged += OnWindowSizeChanged;

        //9:16 aspect ratio
        _graphics.IsFullScreen = false;
        _graphics.ApplyChanges();
    }

    private void OnWindowSizeChanged(object sender, EventArgs e)
    {
        // Set minimum window size constraints
        const int minWidth = 405;  // Minimum width to ensure UI fits
        const int minHeight = 720; // Minimum height to ensure board + reset button fits
        
        if (_graphics.PreferredBackBufferWidth < minWidth)
        {
            _graphics.PreferredBackBufferWidth = minWidth;
        }
        if (_graphics.PreferredBackBufferHeight < minHeight)
        {
            _graphics.PreferredBackBufferHeight = minHeight;
        }
        
        
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        _primitiveBatch = new PrimitiveBatch(GraphicsDevice);
        _primitiveBatch.CreateTextures();

        _board = new GuiBoard(
            7,
            7,
            Vector2.Zero,
            new Vector2(0.75f, 0.4f),
            new Gui.GuiRoundedRectangle(
                Vector2.Zero,
                new Vector2(0.75f, 0.4f)
                    * new Vector2(Window.ClientBounds.Width, Window.ClientBounds.Height),
                10f,
                Color.BlanchedAlmond
            )
        );
        _players = [new Player(Color.Blue), new Player(Color.Green), new Player(Color.Yellow)];

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("DefaultFont");

        // Create a 1x1 red texture for debug drawing
        _debugRedTexture = new Texture2D(GraphicsDevice, 1, 1);
        _debugRedTexture.SetData(new[] { Color.Red });
    }

    protected override void Update(GameTime gameTime)
    {
        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();

        if (_isAnimatingExplosions)
        {
            _explosionTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_explosionTimer >= _explosionDuration)
            {
                _explosionIndex++;
                _explosionTimer = 0f;
                if (_explosionIndex >= (_explodedPieces?.Count ?? 0))
                {
                    _isAnimatingExplosions = false;
                    _explodedPieces?.Clear();
                    // if (_preExplosionBoard != null)
                    // {
                    //     // Copy the exploded state from _board (which was updated in UpdatePlayer)
                    //     // to _preExplosionBoard so next draw is correct
                    //     _preExplosionBoard = null;
                    // }
                    if (_pendingTurnAdvance)
                    {
                        if (_isFirstTurn && _currentPlayerIndex == _players.Count - 1)
                        {
                            _isFirstTurn = false;
                        }
                        _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;

                        for (int i = 0; i < _players.Count; i++)
                        {
                            if (!_isFirstTurn && _players[i].Score == 0)
                            {
                                _players[i].IsActive = false;
                            }
                            _players[i].Score = 0;
                        }
                        _pendingTurnAdvance = false;
                    }
                }
            }
            return;
        }
        _preExplosionBoard = CloneBoard(_board);

        MouseState mouseState = Mouse.GetState();
        bool turn = false;
        if (!IsPlayerDead(_players[_currentPlayerIndex]) || _isFirstTurn)
            (turn, _explodedPieces) = _board.UpdatePlayer(
                _players[_currentPlayerIndex],
                mouseState,
                _isFirstTurn
            );
        else
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;

        if (_explodedPieces != null && _explodedPieces.Count > 0)
        {
            // Make a deep copy of the board before explosion for animation
            _isAnimatingExplosions = true;
            _explosionIndex = 0;
            _explosionTimer = 0f;
            // Save the turn flag so we can advance after explosions
            _pendingTurnAdvance = turn;
            return;
        }
        if (turn)
        {
            if (_isFirstTurn && _currentPlayerIndex == _players.Count - 1)
            {
                _isFirstTurn = false;
            }
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
            // Skip inactive players
            int safety2 = 0;
            while (!_players[_currentPlayerIndex].IsActive && safety2 < _players.Count)
            {
                _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
                safety2++;
            }
            for (int i = 0; i < _players.Count; i++)
            {
                if (!_isFirstTurn && _players[i].Score == 0)
                {
                    _players[i].IsActive = false;
                }
                _players[i].Score = 0;
            }
        }
        _resetButton.Size = new Vector2(
            _board.Background.Size.X * 0.8f,
            _board.Background.Size.Y * 0.2f
        );
        _resetButton.Position = new Vector2(
            _board.Background.Position.X + _board.Background.Size.X / 2 - _resetButton.Size.X / 2,
            _board.Background.Position.Y + _board.Background.Size.Y + _resetButton.Size.Y / 2
        );
        if (_resetButton.IsPressed(mouseState))
        {
            ResetGame();
        }
        else if (_resetButton.IsHovered(mouseState))
        {
            _resetButton.Color = Color.LightPink;
        }
        else
        {
            _resetButton.Color = Color.Red;
        }

        base.Update(gameTime);
    }

    private bool IsPlayerDead(Player player)
    {
        foreach (var row in _board.BoardArray)
        {
            foreach (var piece in row)
            {
                if (piece.Player == player)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private GuiBoard CloneBoard(GuiBoard board)
    {
        var newBoard = new GuiBoard(
            (int)board.BoardSize.X,
            (int)board.BoardSize.Y,
            board.Position,
            board.Size,
            new Gui.GuiRoundedRectangle(
                board.Background.Position,
                board.Background.Size,
                10f,
                board.Background.Color
            )
        );
        for (int i = 0; i < board.BoardArray.Length; i++)
        {
            for (int j = 0; j < board.BoardArray[i].Length; j++)
            {
                var piece = board.BoardArray[i][j];
                var newPiece = new BoardPiece(
                    piece.Player,
                    piece.PieceLevel,
                    piece.BoardPosition,
                    new Gui.GuiRoundedRectangle(
                        piece.GuiRectangle.Position,
                        piece.GuiRectangle.Size,
                        10f,
                        piece.GuiRectangle.Color
                    )
                );
                newBoard.BoardArray[i][j] = newPiece;
            }
        }
        return newBoard;
    }

    private void ResetGame()
    {
        _board = new GuiBoard(
            7,
            7,
            Vector2.Zero,
            new Vector2(0.75f, 0.4f),
            new Gui.GuiRoundedRectangle(
                Vector2.Zero,
                new Vector2(0.75f, 0.4f)
                    * new Vector2(Window.ClientBounds.Width, Window.ClientBounds.Height),
                10f,
                Color.BlanchedAlmond
            )
        );
        _isFirstTurn = true;
        _currentPlayerIndex = 0;
        _explodedPieces?.Clear();
        _isAnimatingExplosions = false;
        _pendingTurnAdvance = false;
        foreach (var player in _players)
        {
            player.IsActive = true;
            player.Score = 0;
        }
        _preExplosionBoard = null;
        _explosionIndex = 0;
        _explosionTimer = 0f;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();
        if (
            _isAnimatingExplosions
            && _explodedPieces != null
            && _explosionIndex < _explodedPieces.Count
        )
        {
            var (from, _) = _explodedPieces[_explosionIndex];
            float t = _explosionTimer / _explosionDuration;
            Color color = _players[_currentPlayerIndex].Color;
            // Draw the pre-explosion board, not the updated one
            (_preExplosionBoard ?? _board).Draw(
                _spriteBatch,
                _primitiveBatch,
                _font,
                new Vector2(Window.ClientBounds.Width, Window.ClientBounds.Height),
                (from, t, color)
            );
            if (_explosionIndex > 0)
            {
                Vector2[] explodedPieces = _explodedPieces[_explosionIndex - 1].Item2;
                Vector2 centre = _explodedPieces[_explosionIndex - 1].Item1;
                _preExplosionBoard.BoardArray[(int)centre.X][(int)centre.Y] = _board.BoardArray[
                    (int)centre.X
                ][(int)centre.Y];

                for (int i = 0; i < explodedPieces.Length; i++)
                {
                    Vector2 piece = explodedPieces[i];

                    _preExplosionBoard.BoardArray[(int)piece.X][(int)piece.Y] = _board.BoardArray[
                        (int)piece.X
                    ][(int)piece.Y];
                }
                { }
            }
        }
        else
        {
            _board.Draw(
                _spriteBatch,
                _primitiveBatch,
                _font,
                new Vector2(Window.ClientBounds.Width, Window.ClientBounds.Height)
            );
        }
        _resetButton.Draw(_spriteBatch, _primitiveBatch);
        string resetText = "Reset Game";
        Vector2 textSize = _font.MeasureString(resetText);
        Vector2 buttonCenter = _resetButton.Position + _resetButton.Size / 2f;
        Vector2 textPos = buttonCenter - textSize / 2f;
        Vector2 scale = new Vector2(
            Math.Min(_resetButton.Size.X / textSize.X, _resetButton.Size.Y / textSize.Y) * 0.8f // 80% of button
        );
        _spriteBatch.DrawString(
            _font,
            resetText,
            buttonCenter,
            Color.White,
            0f,
            textSize / 2f,
            scale,
            SpriteEffects.None,
            0f
        );
        _spriteBatch.DrawString(
            _font,
            $"Current Player: {_players[_currentPlayerIndex].Color}",
            new Vector2(10, 10),
            _players[_currentPlayerIndex].Color
        );
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
