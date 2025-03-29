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
using Orbital2.Lighting;

namespace Orbital2;

public class Orbital : Microsoft.Xna.Framework.Game
{
    public Engine.Engine Engine { get; set; } = new();
    public GameWorld GameWorld => Engine.GameWorld;

    public float TimeScale { get; set; } = 1;
        
    private GraphicsDeviceManager graphics;
    private DrawHelper drawHelper;
    private SpriteBatch spriteBatch;
    private SpriteFont arial;
    private Effect voronoiEffect;

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
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    //TODO: SPH on collisions wouldn't actually be that hard to implement, especially if I'm aiming for a low planet count
    protected override void Initialize()
    {
        base.Initialize();
        arial = Content.Load<SpriteFont>("arial");
        var lineDistanceEffect = Content.Load<Effect>("Shaders/linedistance");
        var pointDistanceEffect = Content.Load<Effect>("Shaders/pointdistance");
        var occlusionShadowEffect = Content.Load<Effect>("Shaders/occlusionshadow");
        voronoiEffect = Content.Load<Effect>("Shaders/voronoi");
        
        GraphicsDevice.RasterizerState = new()
        {
            CullMode = CullMode.None
        };
        
        lightRenderer = new(GraphicsDevice, lineDistanceEffect, pointDistanceEffect, occlusionShadowEffect);
        drawHelper = new(GraphicsDevice);
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    private void UpdateInput(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
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
                TimeScale *= 1.001f;
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
                TimeScale /= 1.001f;
            }
            else
            {
                camera.Zoom /= 1.001f;
            }
        }

        if(Keyboard.GetState().IsKeyDown(Keys.LeftShift) && Keyboard.GetState().IsKeyDown(Keys.Enter))
        {
            TimeScale = 1;
        }

        if(Engine.Input.IsKeyJustPressed(Keys.Delete))
        {
            drawOutlines = !drawOutlines;
        }

        previousScrollValue = scrollValue;
    }

    private void UpdateEngine(GameTime gameTime)
    {
        Engine.Update(gameTime.GetElapsedSeconds() * TimeScale);
    }

    protected override void Update(GameTime gameTime)
    {
        time += (float)gameTime.ElapsedGameTime.TotalSeconds * TimeScale;
        
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
            
        if(body.Radius < 3)
        {
            //DrawCircle(body.InterpolatedPosition, 3, color, false);
        }
    }

    private void DrawPlanet(Planet planet, Gradient temperatureGradient)
    {
        Color color = temperatureGradient.GetColor(planet.Temperature);
        DrawBody(planet.Body, color);
            
        if(planet.Life != null)
        {
            Vector2 bottomPos = planet.Body.InterpolatedPosition - new Vector2(0, planet.Body.Radius);
            Vector2 bottomScreenPos = camera.TransformToScreen(bottomPos, Window.ClientBounds);
            spriteBatch.DrawString(arial, planet.Life.Population.ToString(), bottomScreenPos - new Vector2(0, -10), Color.White, 0, new(), camera.Zoom, new(), 0);
            spriteBatch.DrawString(arial, MathF.Round(planet.Temperature).ToString(), bottomScreenPos - new Vector2(0, -40), Color.White, 0, new(), camera.Zoom, new(), 0);
            spriteBatch.DrawString(arial, planet.Life.RawMaterials.Hydrogen.ToString(), bottomScreenPos - new Vector2(0, -70), Color.White, 0, new(), camera.Zoom, new(), 0);
        }
    }

    private void DrawStar(Star star)
    {
        var radius = star.Radius;
        var bottomLeft = star.InterpolatedPosition + new Vector2(-radius, -radius);
        var bottomRight = star.InterpolatedPosition + new Vector2(radius, -radius);
        var topLeft = star.InterpolatedPosition + new Vector2(-radius, radius);
        var topRight = star.InterpolatedPosition + new Vector2(radius, radius);
        
        voronoiEffect.Parameters["WorldViewProjection"].SetValue(camera.GetViewMatrix(GraphicsDevice.Viewport.Bounds));
        voronoiEffect.Parameters["position"].SetValue(star.InterpolatedPosition);
        voronoiEffect.Parameters["radius"].SetValue(star.Radius);
        voronoiEffect.Parameters["time"].SetValue(time);
        voronoiEffect.Parameters["color0"].SetValue(Color.Yellow.ToVector4());
        voronoiEffect.Parameters["color1"].SetValue(Color.OrangeRed.ToVector4());

        VertexPositionColor[] vertices =
        [
            new(new(bottomLeft, 0), Color.Black),
            new(new(bottomRight, 0), Color.Black),
            new(new(topLeft, 0), Color.Black),
            new(new(topRight, 0), Color.Black)
        ];
        
        drawHelper.DrawQuads(vertices, voronoiEffect, BlendState.AlphaBlend);
    }
    
    private void DrawObjects(IEnumerable<PhysicalGameObject> objects)
    {
        var gradient = new Gradient([new(0, new(0, 0, 1.0f)), new(300, new(0, 1.0f, 0)), new(600, new(1.0f, 0, 0))]);
        
        spriteBatch.Begin();
        
        foreach (var obj in objects)
        {
            if (!camera.GetWorldBounds(Window.ClientBounds).ContainsPoint(obj.InterpolatedPosition)) continue;

            if (obj is Star star)
            {
                //DrawStar(star);
            }
            else if (obj is Planet planet)
            {
                DrawPlanet(planet, gradient);
            }
            else
            {
                DrawBody(obj.Body, Color.Yellow);
            }
        }

        spriteBatch.End();
    }

    private void DrawLighting(LightRenderer lightRenderer)
    {
        var star = GameWorld.GameObjects.FirstOrDefault(o => o is Star) as Star;
        if (star == null) return;
        
        var occluders = GameWorld.PhysicalObjects.Cast<ILightingOccluder>().ToList();
        occluders.Remove(star);
        
        lightRenderer.DrawLight(star, camera);
        lightRenderer.DrawShadows(star, occluders, camera);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        
        if(lightRenderer != null)
        {
            DrawLighting(lightRenderer);
        }

        DrawObjects(GameWorld.PhysicalObjects);
        
        var star = GameWorld.GameObjects.FirstOrDefault(o => o is Star) as Star;

        if (star != null)
        {
            DrawStar(star);
        }

        base.Draw(gameTime);
    }
}