using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using Orbital2.Physics;
using System.Diagnostics;
using System.Linq;
using Orbital2.Physics.Gravity;
using Orbital2.Physics.Collision;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orbital2.Engine;
using Orbital2.Game;
using Orbital2.Game.Astrobodies;
using MonoGame.Extended.Collections;
using Orbital2.Engine.Graphics;
using Orbital2.Engine.Graphics.Shaders;
using Orbital2.Lighting;

namespace Orbital2;

public class Orbital : Microsoft.Xna.Framework.Game
{
    public Engine.Engine Engine { get; set; } = new();
    public GameWorld GameWorld => Engine.GameWorld;

    private GraphicsDeviceManager graphics;
    private DrawHelper drawHelper;
    private SpriteBatch spriteBatch;
    private SpriteFont arial;

    private CircleEffect circleEffect;
    private VoronoiEffect voronoiEffect;

    private float time = 0;

    private Camera camera = new(0, 0, 10);
    private LightRenderer? lightRenderer;

    private bool startedPanning = false;
    private int previousScrollValue = 0;
    private Point startMousePos = new(0, 0);

    private bool drawOutlines = false;

    public Orbital()
    {
        graphics = new GraphicsDeviceManager(this);

        graphics.PreferredBackBufferFormat = SurfaceFormat.Bgra32;
        graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
        graphics.GraphicsProfile = GraphicsProfile.HiDef;

        graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    //TODO: SPH on collisions wouldn't actually be that hard to implement, especially if I'm aiming for a low planet count
    protected override void Initialize()
    {
        base.Initialize();

        arial = Content.Load<SpriteFont>("arial");

        var occlusionShadowEffect = Content.Load<Effect>("Shaders/occlusionshadow");

        var pointDistanceEffect = new PointDistanceEffect(
            Content.Load<Effect>("Shaders/pointdistance"), camera,
            () => GraphicsDevice.Viewport.Bounds
        );

        voronoiEffect = new(
            Content.Load<Effect>("Shaders/voronoi"),
            camera,
            () => GraphicsDevice.Viewport.Bounds,
            Engine.Clock
        );
        
        circleEffect = new(
            Content.Load<Effect>("Shaders/circle"),
            camera,
            () => GraphicsDevice.Viewport.Bounds
        );

        lightRenderer = new(GraphicsDevice, pointDistanceEffect, occlusionShadowEffect);
        drawHelper = new(GraphicsDevice);
    }

    protected override void LoadContent()
    {
        spriteBatch = new (GraphicsDevice);
    }

    private void UpdateInput(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (Mouse.GetState().RightButton == ButtonState.Pressed && IsActive)
        {
            if (!startedPanning)
            {
                startMousePos = Mouse.GetState().Position;
                startedPanning = true;
            }

            Point mouseDiff = Mouse.GetState().Position - startMousePos;
            Vector2 mouseDiffV = new Vector2(mouseDiff.X, mouseDiff.Y);
            camera.Center -= mouseDiffV / camera.Zoom * 0.4f;
            Mouse.SetPosition(startMousePos.X, startMousePos.Y);

            IsMouseVisible = false;
        }
        else
        {
            startedPanning = false;
            IsMouseVisible = true;
        }

        int scrollValue = Mouse.GetState().ScrollWheelValue;

        for (int i = 0; i < scrollValue - previousScrollValue; i++)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
            {
                Engine.Clock.TimeScale *= 1.001f;
            }
            else
            {
                camera.Zoom *= 1.001f;
            }
        }

        for (int i = 0; i < previousScrollValue - scrollValue; i++)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
            {
                Engine.Clock.TimeScale /= 1.001f;
            }
            else
            {
                camera.Zoom /= 1.001f;
            }
        }

        if (Keyboard.GetState().IsKeyDown(Keys.LeftShift) && Keyboard.GetState().IsKeyDown(Keys.Enter))
        {
            Engine.Clock.TimeScale = 1;
        }

        if (Engine.Input.IsKeyJustPressed(Keys.Delete))
        {
            drawOutlines = !drawOutlines;
        }

        previousScrollValue = scrollValue;
    }

    private void UpdateEngine(GameTime gameTime)
    {
        Engine.Update(gameTime.GetElapsedSeconds());
    }

    protected override void Update(GameTime gameTime)
    {
        time += (float)gameTime.ElapsedGameTime.TotalSeconds;

        UpdateInput(gameTime);
        UpdateEngine(gameTime);

        base.Update(gameTime);
    }

    private void DrawCircle(Vector2 worldPos, float radius, Color color, bool filled)
    {
        float thickness = filled ? radius * camera.Zoom : radius * camera.Zoom * 0.1f;

        Vector2 pos = camera.TransformToScreen(worldPos, Window.ClientBounds);
        spriteBatch.DrawCircle(pos, radius * camera.Zoom, 22, color, thickness);
    }

    private void DrawBody(Body body, Color color)
    {
        DrawCircle(body.InterpolatedPosition, body.Radius, color, true);

        if (body.Radius < 3)
        {
            //DrawCircle(body.InterpolatedPosition, 3, color, false);
        }
    }

    private void DrawPlanet(Planet planet, Gradient temperatureGradient)
    {
        Color color = temperatureGradient.GetColor(planet.Temperature);
        DrawBody(planet.Body, color);

        if (planet.Life != null)
        {
            Vector2 bottomPos = planet.Body.InterpolatedPosition - new Vector2(0, planet.Body.Radius);
            Vector2 bottomScreenPos = camera.TransformToScreen(bottomPos, Window.ClientBounds);
            spriteBatch.DrawString(arial, planet.Life.Population.ToString(), bottomScreenPos - new Vector2(0, -10),
                Color.White, 0, new(), camera.Zoom, new(), 0);
            spriteBatch.DrawString(arial, MathF.Round(planet.Temperature).ToString(),
                bottomScreenPos - new Vector2(0, -40), Color.White, 0, new(), camera.Zoom, new(), 0);
            spriteBatch.DrawString(arial, planet.Life.RawMaterials.Hydrogen.ToString(),
                bottomScreenPos - new Vector2(0, -70), Color.White, 0, new(), camera.Zoom, new(), 0);
        }
    }

    private readonly FixedList<VertexPositionCircleColor> simpleCircleQuads = new();
    
    private void DrawObjects(IEnumerable<PhysicalGameObject> objects)
    {
        var gradient = new Gradient([new(0, new(0, 0, 1.0f)), new(300, new(0, 1.0f, 0)), new(600, new(1.0f, 0, 0))]);

        //count * 4 is guaranteed to be enough
        simpleCircleQuads.Resize(objects.Count() * 4);
        simpleCircleQuads.Reset();

        foreach (var obj in objects)
        {
            //TODO: Cull based on bounds, not center
            //And could accelerate with a structure easily, just spatial hashing really
            if (!camera.GetWorldBounds(Window.ClientBounds).ContainsPoint(obj.InterpolatedPosition)) continue;

            if (obj is Planet planet)
            {
                var color = gradient.GetColor(planet.Temperature);
                
                simpleCircleQuads.Add(new(new(planet.Body.BottomLeft, 0), color.ToVector4(), planet.InterpolatedPosition, planet.Radius));
                simpleCircleQuads.Add(new(new(planet.Body.BottomRight, 0), color.ToVector4(), planet.InterpolatedPosition, planet.Radius));
                simpleCircleQuads.Add(new(new(planet.Body.TopLeft, 0), color.ToVector4(), planet.InterpolatedPosition, planet.Radius));
                simpleCircleQuads.Add(new(new(planet.Body.TopRight, 0), color.ToVector4(), planet.InterpolatedPosition, planet.Radius));
            }
            else if (obj is Star star)
            {
                voronoiEffect.Position = star.InterpolatedPosition;
                voronoiEffect.Radius = star.Radius;
                voronoiEffect.Color0 = Color.Yellow.ToVector4();
                voronoiEffect.Color1 = Color.OrangeRed.ToVector4();
                voronoiEffect.WarpStrength = 0.85f;
                voronoiEffect.VoronoiScale = 3;
                voronoiEffect.VoronoiJitter = 1;

                var starVertices = new[]
                {
                    new VertexPosition(new(star.Body.BottomLeft, 0)),
                    new VertexPosition(new(star.Body.BottomRight, 0)),
                    new VertexPosition(new(star.Body.TopLeft, 0)),
                    new VertexPosition(new(star.Body.TopRight, 0))
                };
                
                drawHelper.DrawQuads(starVertices, voronoiEffect, BlendState.AlphaBlend);
            }
            else
            {
                var color = Color.Aqua;
                simpleCircleQuads.Add(new(new(obj.Body.BottomLeft, 0), color.ToVector4(), obj.InterpolatedPosition, obj.Radius));
                simpleCircleQuads.Add(new(new(obj.Body.BottomRight, 0), color.ToVector4(), obj.InterpolatedPosition, obj.Radius));
                simpleCircleQuads.Add(new(new(obj.Body.TopLeft, 0), color.ToVector4(), obj.InterpolatedPosition, obj.Radius));
                simpleCircleQuads.Add(new(new(obj.Body.TopRight, 0), color.ToVector4(), obj.InterpolatedPosition, obj.Radius));
            }
        }
        
        drawHelper.DrawQuads(simpleCircleQuads.Array, circleEffect, BlendState.AlphaBlend, simpleCircleQuads.Used / 4);
    }

    private void DrawLighting(LightRenderer lightRenderer)
    {
        var stars = GameWorld.GameObjects.OfType<Star>().ToArray();
        var occluders = GameWorld.PhysicalObjects.Cast<ILightingOccluder>().Where(o => o is not Star);
        
        lightRenderer.DrawLighting(stars[0], occluders, camera);
        
        //List<RenderTarget2D> targets = [];
        
        /**foreach (var star in stars)
        {
            var renderTarget = new RenderTarget2D(
                GraphicsDevice,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                false,
                SurfaceFormat.Bgra4444,
                DepthFormat.None,
                0,
                RenderTargetUsage.PreserveContents
            );
            
            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(Color.Black);
            
            lightRenderer.DrawLighting(star, occluders, camera);
            
            targets.Add(renderTarget);
        }
        
        GraphicsDevice.SetRenderTarget(null);
        
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        
        foreach (var target in targets)
        {
            spriteBatch.Draw(target, Vector2.Zero, Color.White);
        }
        
        spriteBatch.End();**/
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (lightRenderer != null)
        {
            DrawLighting(lightRenderer);
        }

        //TODO: Culling, for shadows (gonna be some thinking and scheming there) and planets and star
        DrawObjects(GameWorld.PhysicalObjects);

        base.Draw(gameTime);
    }
}