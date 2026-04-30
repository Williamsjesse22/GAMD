using BepuPhysics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpaceRace.GameObjects;
using SpaceRace.Graphics;
using SpaceRace.Physics;
using SpaceRace.Systems;
using NumQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace SpaceRace;

public class Game1 : Game
{
    private enum GameState { PreRace, Racing, Finished }

    private const float CountdownSeconds = 3f;
    private static readonly NumericsVector3 SpawnPosition = NumericsVector3.Zero;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private BepuWorld _world = null!;
    private Camera _camera = null!;
    private PrimitiveRenderer _renderer = null!;
    private Skybox _skybox = null!;
    private Ship _ship = null!;
    private ShipController _shipController = null!;
    private Course _course = null!;
    private HudComponent _hud = null!;
    private Texture2D _pixel = null!;
    private DebrisSpawner _debrisSpawner = null!;
    private readonly AudioManager _audio = new();

    private GameState _state = GameState.PreRace;
    private float _countdownRemaining = CountdownSeconds;
    private float _raceTime;
    private KeyboardState _previousKeyboard;

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

    protected override void Initialize()
    {
        _world = new BepuWorld(gravity: NumericsVector3.Zero);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = TextureFactory.CreatePixel(GraphicsDevice);

        _camera = new Camera { Aspect = (float)GraphicsDevice.Viewport.Width / GraphicsDevice.Viewport.Height };
        _renderer = new PrimitiveRenderer(GraphicsDevice);

        _skybox = new Skybox(this, _camera);
        Components.Add(_skybox);

        _ship = new Ship(this, _world, _renderer, _camera, SpawnPosition);
        Components.Add(_ship);

        _shipController = new ShipController(this, _ship, _camera)
        {
            // Bonus #4: fuel mode on. Ship burns 5%/sec at full thrust; pitstops refuel.
            RequireFuel = true,
            FuelBurnRate = 0.05f,
        };
        Components.Add(_shipController);

        _audio.Load(Content);

        _course = new Course(this, _ship);
        _course.Build(_renderer);
        foreach (var ring in _course.Rings) Components.Add(ring);
        Components.Add(_course);
        _course.RingPassed += _audio.PlayRingPass;
        _course.RaceFinished += OnRaceFinished;

        // Bonus #4: pitstops along the course. Two midcourse refuel rings, oriented
        // perpendicular to the flight path so the ship can fly through naturally.
        var pitstop1 = new Pitstop(this, _ship, _renderer,
            position: new Vector3(8, 0, -75),
            orientation: Quaternion.Identity);
        var pitstop2 = new Pitstop(this, _ship, _renderer,
            position: new Vector3(-10, 4, -160),
            orientation: Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.3f));
        Components.Add(pitstop1);
        Components.Add(pitstop2);

        // Bonus #5: gravity wells. Two purple wells pull the ship if it strays close.
        var well1 = new GravityWell(this, _ship, _renderer,
            position: new NumericsVector3(-5, -3, -45), strength: 280f);
        var well2 = new GravityWell(this, _ship, _renderer,
            position: new NumericsVector3(12, 2, -135), strength: 320f);
        Components.Add(well1);
        Components.Add(well2);

        // Bonus #3: space debris drifting across the course.
        _debrisSpawner = new DebrisSpawner(this, _world, _renderer)
        {
            SpawnsPerSecond = 0.5f,
            CourseCenter = new NumericsVector3(0, 0, -110),
            CourseRadius = 220f,
        };
        Components.Add(_debrisSpawner);

        _hud = new HudComponent(this, _spriteBatch, _pixel)
        {
            RingCount = _course.Rings.Count,
            CountdownSeconds = CountdownSeconds,
            ShowFuel = true,
        };
        Components.Add(_hud);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape)) Exit();

        bool rJustPressed = keyboard.IsKeyDown(Keys.R) && !_previousKeyboard.IsKeyDown(Keys.R);
        if (rJustPressed && _state == GameState.Finished) RestartRace();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _world.Step(dt);
        _camera.Follow(_ship.Pose);
        _audio.Tick(dt);

        switch (_state)
        {
            case GameState.PreRace:
                _countdownRemaining -= dt;
                if (_countdownRemaining <= 0f)
                {
                    _state = GameState.Racing;
                    _raceTime = 0f;
                }
                break;
            case GameState.Racing:
                _raceTime += dt;
                break;
        }

        // HUD mirrors race state.
        _hud.State = _state switch
        {
            GameState.PreRace => HudComponent.RaceState.PreRace,
            GameState.Racing => HudComponent.RaceState.Racing,
            _ => HudComponent.RaceState.Finished,
        };
        _hud.ElapsedSeconds = _raceTime;
        _hud.CountdownSeconds = _countdownRemaining;
        _hud.MissedCount = _course.MissedCount;
        _hud.CurrentTargetIndex = _course.CurrentTargetIndex;
        _hud.Score = HudComponent.ComputeScore(_raceTime, _course.MissedCount);
        _hud.Fuel = _ship.Fuel;

        _previousKeyboard = keyboard;
        base.Update(gameTime);
    }

    private void OnRaceFinished()
    {
        _state = GameState.Finished;
        _audio.PlayFinish();
    }

    private void RestartRace()
    {
        // Reset ship to spawn pose + zero velocity. This direct write is a setup
        // operation, not an in-race steering input — the spec's "forces only"
        // rule applies to gameplay, not race resets.
        var bodyRef = _ship.Body;
        bodyRef.Pose = new RigidPose(SpawnPosition, NumQuaternion.Identity);
        bodyRef.Velocity.Linear = NumericsVector3.Zero;
        bodyRef.Velocity.Angular = NumericsVector3.Zero;
        bodyRef.Awake = true;

        _course.Reset();
        _ship.Fuel = 1f;
        _raceTime = 0f;
        _countdownRemaining = CountdownSeconds;
        _state = GameState.PreRace;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(8, 10, 22));
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

        _renderer.SetCamera(_camera.View, _camera.Projection);

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _spriteBatch?.Dispose();
            _pixel?.Dispose();
            _world?.Dispose();
        }
        base.Dispose(disposing);
    }
}
