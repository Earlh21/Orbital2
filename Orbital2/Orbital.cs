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
using Orbital2.GameObjects;

namespace Orbital2
{
    public class Orbital : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch sprite_batch;

        private Engine.Engine engine = new(1f);

        private Camera camera = new(0, 0, 10);

        private bool started_panning = false;
        private int previous_scroll_value = 0;
        private Point start_mouse_pos = new(0, 0);
        private Point previous_mouse_pos = new(0, 0);

        private Stopwatch stopwatch = new();

        private float current_time = 0;
        private float physics_timestep = 0.4f;
        private float accumulator = 0.2f;
        private float time_scale = 1f;

        public Orbital()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            Random rand = new();

            Body body = new(new());
            body.Mass = 10;

            Body body2 = new(new(6, 0));
            body2.Velocity = new(0, 4);

            Body body3 = new(new(-9, 16));
            body3.Velocity = new(0, -2);

            engine.AddObject(new Planet(body));
            engine.AddObject(new Planet(body2));
            //engine.AddObject(new Planet(body3));

            Body body4 = new(new());

            Body body5 = new(new(5, 0));
            body5.Velocity = new(-10, 0);

            //engine.AddObject(new Planet(body4));
            //engine.AddObject(new Planet(body5));

            engine.AddObject(new GravityObject(new AllPairsGravity { GravitationalConstant = 5 }));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            sprite_batch = new SpriteBatch(GraphicsDevice);
        }

        private void UpdateInput(GameTime game_time)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (Mouse.GetState().RightButton == ButtonState.Pressed && IsActive)
            {
                if (!started_panning)
                {
                    start_mouse_pos = Mouse.GetState().Position;
                    started_panning = true;
                }

                Point mouse_diff = Mouse.GetState().Position - start_mouse_pos;
                Vector2 mouse_diff_v = new Vector2(mouse_diff.X, mouse_diff.Y);
                camera.Center -= mouse_diff_v / camera.Zoom * 0.4f;
                Mouse.SetPosition(start_mouse_pos.X, start_mouse_pos.Y);

                IsMouseVisible = false;
            }
            else
            {
                started_panning = false;
                IsMouseVisible = true;
            }

            int scroll_value = Mouse.GetState().ScrollWheelValue;

            for (int i = 0; i < scroll_value - previous_scroll_value; i++)
            {
                camera.Zoom *= 1.001f;
            }

            for (int i = 0; i < previous_scroll_value - scroll_value; i++)
            {
                camera.Zoom /= 1.001f;
            }

            previous_scroll_value = scroll_value;
        }

        private void UpdateEngine(GameTime game_time)
        {
            engine.Update(game_time.GetElapsedSeconds() * time_scale);
        }

        protected override void Update(GameTime game_time)
        {
            UpdateInput(game_time);
            UpdateEngine(game_time);

            base.Update(game_time);
        }

        private void DrawCircle(Vector2 world_pos, float radius, Color color)
        {
            Vector2 pos = camera.TransformToScreen(world_pos, Window.ClientBounds);
            sprite_batch.DrawCircle(pos, radius * camera.Zoom, 22, color, radius * camera.Zoom);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            sprite_batch.Begin();

            var bodies = engine.PhysicalObjects.Select(o => o.Body).ToArray();

            foreach (var body in bodies)
            {
                DrawCircle(body.InterpolatedPosition, body.Radius, new Color(0, 0.5f, 0, 0.05f));
            }

            foreach(var obj in engine.FindObjectsByName("marker"))
            {
                var marker = (CollisionMarker)obj;
                DrawCircle(marker.Position, marker.Radius, new Color(0.5f, 0, 0, 0.05f));
            }

            sprite_batch.End();

            base.Draw(gameTime);
        }
    }
}
