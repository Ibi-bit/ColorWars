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
    private Player[] _players = new Player[4];
    private bool _isFirstTurn = true;
    private int _currentPlayerIndex = 0;

    private List<(Vector2, Vector2[])> _explodedPieces;
    private SpriteFont _font;

    private GuiBoard _board;

    private int _explosionIndex = 0;
    private float _explosionTimer = 0f;
    private float _explosionDuration = 0.3f; // seconds per explosion
    private bool _isAnimatingExplosions = false;
    private bool _pendingTurnAdvance = false;

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

        //9:16 aspect ratio
        _graphics.IsFullScreen = false;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        _primitiveBatch = new PrimitiveBatch(GraphicsDevice);
        _primitiveBatch.CreateTextures();

        _board = new GuiBoard(
            10,
            10,
            Vector2.Zero,
            new Vector2(0.75f, 0.4f),
            new Gui.GuiRectangle(
                Vector2.Zero,
                new Vector2(0.75f, 0.4f)
                    * new Vector2(
                        _graphics.PreferredBackBufferWidth,
                        _graphics.PreferredBackBufferHeight
                    ),
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
                    if (_pendingTurnAdvance)
                    {
                        if (_isFirstTurn && _currentPlayerIndex == _players.Length - 1)
                        {
                            _isFirstTurn = false;
                        }
                        _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Length;
                        for (int i = 0; i < _players.Length; i++)
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
            return; // Skip input/turn logic while animating
        }

        MouseState mouseState = Mouse.GetState();
        (bool turn, _explodedPieces) = _board.UpdatePlayer(
            _players[_currentPlayerIndex],
            mouseState,
            _isFirstTurn
        );
        if (_explodedPieces != null && _explodedPieces.Count > 0)
        {
            _isAnimatingExplosions = true;
            _explosionIndex = 0;
            _explosionTimer = 0f;
            // Save the turn flag so we can advance after explosions
            _pendingTurnAdvance = turn;
            return;
        }
        if (turn)
        {
            if (_isFirstTurn && _currentPlayerIndex == _players.Length - 1)
            {
                _isFirstTurn = false;
            }
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Length;
            for (int i = 0; i < _players.Length; i++)
            {
                if (!_isFirstTurn && _players[i].Score == 0)
                {
                    _players[i].IsActive = false;
                }
                _players[i].Score = 0;
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();
        // Draw explosion animation using Board.Draw
        if (
            _isAnimatingExplosions
            && _explodedPieces != null
            && _explosionIndex < _explodedPieces.Count
        )
        {
            var (from, _) = _explodedPieces[_explosionIndex];
            float t = _explosionTimer / _explosionDuration;

            Color color = _players[_currentPlayerIndex].Color;
            // DEBUG: Draw a red rectangle at the explosion location

            _board.Draw(
                _spriteBatch,
                _primitiveBatch,
                _font,
                new Vector2(
                    _graphics.PreferredBackBufferWidth,
                    _graphics.PreferredBackBufferHeight
                ),
                (from, t, color)
            );
        }
        else
        {
            _board.Draw(
                _spriteBatch,
                _primitiveBatch,
                _font,
                new Vector2(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight)
            );
        }
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
