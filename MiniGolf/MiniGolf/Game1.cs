using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MiniGolf.GameObjects;
using MiniGolf.Physics;
using MiniGolf.Systems;

namespace MiniGolf;

public class Game1 : Game
{
    private enum GameState { BallReady, BallMoving, HoleComplete, GameComplete }

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    private readonly PhysicsWorld _world = new();
    private readonly AudioManager _audio = new();
    private readonly Random _rng = new();

    private Texture2D _pixel = null!;
    private Texture2D _holeTex = null!;

    private Ball _ball = null!;
    private Course? _course;
    private InputManager _input = null!;
    private HudComponent _hud = null!;

    private List<HoleLayout> _holes = new();
    private int _currentHoleIndex;
    private GameState _state = GameState.BallReady;
    private double _holeCompleteTimer;
    private double _gameCompleteTimer;
    private double _lastUpdateSeconds;
    private KeyboardState _previousKeyboard;
    private const double HoleResetDelaySeconds = 1.8;
    private const double GameCompleteHoldSeconds = 5.0;
    private const int PhysicsSubSteps = 4;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = TextureFactory.CreatePixel(GraphicsDevice);
        var ballTex = TextureFactory.CreateCircle(GraphicsDevice, 9);
        _holeTex = TextureFactory.CreateCircle(GraphicsDevice, 14);

        _audio.Load(Content);

        _ball = new Ball(this, _spriteBatch, ballTex, radius: 9f) { DrawOrder = 50 };
        Components.Add(_ball);

        _world.Ball = _ball.Collider;
        _world.BallBounced += () => _audio.PlayBounce(_lastUpdateSeconds);
        _world.BallStopped += () =>
        {
            if (_state == GameState.BallMoving) _state = GameState.BallReady;
        };

        _input = new InputManager(this) { BallPosition = _ball.Body.Position, BallAtRest = true };
        _input.ShotFired += impulse =>
        {
            _ball.Body.ApplyImpulse(impulse);
            _audio.PlayStrike();
            _hud.StrokeCount++;
            _hud.TotalStrokes++;
            _state = GameState.BallMoving;
        };
        Components.Add(_input);

        _hud = new HudComponent(this, _spriteBatch, _pixel, _input) { DrawOrder = 100 };
        Components.Add(_hud);

        StartNewSession();
    }

    /// <summary>Roll fresh holes (procedural one re-seeds) and load hole 1.</summary>
    private void StartNewSession()
    {
        _holes = HoleFactory.BuildAll(_rng);
        _hud.TotalStrokes = 0;
        LoadHole(0);
    }

    /// <summary>Tear down the previous course's components and load the layout at <paramref name="index"/>.</summary>
    private void LoadHole(int index)
    {
        UnloadCurrentHole();

        _currentHoleIndex = index;
        var newCourse = new Course(this, _spriteBatch, _pixel);
        newCourse.LoadLayout(_holes[index], _holeTex);
        _course = newCourse;

        Components.Add(_course);
        foreach (var ob in _course.Obstacles) { ob.DrawOrder = -50; Components.Add(ob); }
        Components.Add(_course.Tee);
        Components.Add(_course.Hole);

        _world.StaticBoxes.Clear();
        _world.Slopes.Clear();
        _world.StaticBoxes.AddRange(_course.StaticBoxes);
        _world.Slopes.AddRange(_course.Slopes);

        _ball.Reset(_course.Tee.Center);
        _hud.StrokeCount = 0;
        _hud.HoleIndex = index;
        _hud.HoleCount = _holes.Count;
        _hud.HoleName = _course.LayoutName;
        _state = GameState.BallReady;
    }

    private void UnloadCurrentHole()
    {
        if (_course is null) return;
        Components.Remove(_course);
        foreach (var ob in _course.Obstacles) Components.Remove(ob);
        Components.Remove(_course.Tee);
        Components.Remove(_course.Hole);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape)) Exit();
        _lastUpdateSeconds = gameTime.TotalGameTime.TotalSeconds;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_state == GameState.BallMoving)
        {
            float subDt = dt / PhysicsSubSteps;
            for (int i = 0; i < PhysicsSubSteps; i++) _world.Step(subDt);

            // Hole capture: only meaningful while the ball is moving.
            float speed = _ball.Body.Velocity.Length();
            if (_course is not null && _course.Hole.TryCapture(_ball.Body.Position, speed))
            {
                _audio.PlayDrop();
                _ball.Body.Velocity = Vector2.Zero;
                _ball.Body.Position = _course.Hole.Position;
                _state = GameState.HoleComplete;
                _holeCompleteTimer = HoleResetDelaySeconds;
            }
        }

        if (_state == GameState.HoleComplete)
        {
            _holeCompleteTimer -= dt;
            if (_holeCompleteTimer <= 0)
            {
                if (_currentHoleIndex + 1 < _holes.Count)
                {
                    LoadHole(_currentHoleIndex + 1);
                }
                else
                {
                    _state = GameState.GameComplete;
                    _gameCompleteTimer = GameCompleteHoldSeconds;
                }
            }
        }

        if (_state == GameState.GameComplete)
        {
            _gameCompleteTimer -= dt;
            bool rPressed = keyboard.IsKeyDown(Keys.R) && !_previousKeyboard.IsKeyDown(Keys.R);
            if (_gameCompleteTimer <= 0 || rPressed)
            {
                StartNewSession();
            }
        }

        // Feed the input manager and HUD what they need from the world.
        _input.BallPosition = _ball.Body.Position;
        _input.BallAtRest = _state == GameState.BallReady;
        _hud.GameComplete = _state == GameState.GameComplete;
        _hud.HoleComplete = _state == GameState.HoleComplete;

        _previousKeyboard = keyboard;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(28, 36, 28));
        base.Draw(gameTime);
    }
}
