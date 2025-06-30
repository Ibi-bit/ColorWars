using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;

namespace ColorWars;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private PrimitiveBatch _primitiveBatch;

    private PrimitiveBatch.RoundedRectangle _background;

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

    private PrimitiveBatch.RoundedRectangle CreateBackground(int width, int height, Color color = default)
    {
        // The background should be 0.75 * width wide and 0.4 * height tall, centered
        Vector2 size = new Vector2(width * 0.75f, height * 0.4f);
        Vector2 position = new Vector2((width - size.X) / 2f, (height - size.Y) / 2f);
        return new PrimitiveBatch.RoundedRectangle(position, size, 10, Color.Red);
    }

    protected override void Initialize()

    {
        _primitiveBatch = new PrimitiveBatch(GraphicsDevice);
        _primitiveBatch.CreateTextures();


        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();
        _background = CreateBackground(Window.ClientBounds.Width, Window.ClientBounds.Height);

        _background.Draw(_spriteBatch, _primitiveBatch);
        _spriteBatch.End();

        // TODO: Add your drawing code here

        base.Draw(gameTime);
    }
}
