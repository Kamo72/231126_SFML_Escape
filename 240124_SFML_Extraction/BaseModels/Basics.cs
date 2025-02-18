﻿using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace _231109_SFML_Test
{
    internal abstract class Entity : IDisposable
    {
        protected Gamemode gamemode;
        public bool isDisposed = false;

        public Entity(Gamemode gamemode, Vector2f position, ICollision collision)
        {
            mask = collision;
            Position = position;
            Direction = 0f;
            
            this.gamemode = gamemode;
            
            gamemode.DisposablesAdd(this);
            gamemode.logicEvent += LogicProcess;
            gamemode.logicEvent += PhysicsProcess;
            gamemode.drawEvent += () => 
            {
                float drawableRange = CameraManager.size.Magnitude() * CameraManager.zoomValue;
                float dist = (this.Position - CameraManager.position).Magnitude();
                if (drawableRange > dist) DrawProcess();
            };
        }

        public float Direction;
        public Vector2f Position {
            set
            {
                try {
                    if (mask is Box box)
                        box.Position = value;
                    else if (mask is Circle circle)
                        circle.Position = value;
                    else if (mask is Point point)
                        point.position = value;
                    else
                        throw new NotImplementedException();
                }
                catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                }
            get
            {
                try {
                    if (mask is Box box)
                        return box.Position;
                    else if (mask is Circle circle)
                        return circle.Position;
                    else if (mask is Point point)
                        return point.position;
                    else
                        throw new NotImplementedException();
                }
                catch (Exception ex) { Console.WriteLine(ex.ToString()); return Vector2fEx.Zero; }
            }
        }

        public ICollision mask;

        protected abstract void LogicProcess();
        protected abstract void PhysicsProcess();
        protected abstract void DrawProcess();

        ~Entity() { Dispose(); }
        public virtual void Dispose()
        {
            isDisposed = true;

            gamemode.DisposablesRemove(this);
            gamemode.logicEvent -= LogicProcess;
            gamemode.logicEvent -= PhysicsProcess;
            gamemode.drawEvent -= DrawProcess;
            GC.SuppressFinalize(this);
        }
    }

    internal abstract class Ui : IDisposable
    {
        protected Gamemode gamemode;

        public Box mask;
        public bool isDisposed = false;

        public Vector2f Position { get { return mask.Position; } set { mask.Position = value; } }

        public Ui(Gamemode gamemode, Vector2f position, Vector2f size)
        {
            mask = new Box(position, size);
            mask.Origin = size / 2f;

            this.gamemode = gamemode;
            gamemode.DisposablesAdd(this);
            gamemode.logicEvent += LogicProcess;
            gamemode.logicEvent += ClickProcess;
            gamemode.drawEvent += DrawProcess;
        }

        public bool IsMouseOn()
        {
            try
            {
                Vector2f mousePos = (Vector2f)Mouse.GetPosition();
                FloatRect floatRect = mask.GetGlobalBounds();
                bool IsOn = floatRect.Contains(mousePos.X, mousePos.Y);
                return IsOn;
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                return false;
            }
        }
        protected abstract void DrawProcess();
        protected abstract void LogicProcess();

        bool pressedBefore = false;

        void ClickProcess()
        {
            if (isDisposed) return;

            if (pressedBefore == false)
                if (IsMouseOn())
                    if (Mouse.IsButtonPressed(Mouse.Button.Left))
                    {
                        Clicked?.Invoke();
                        pressedBefore = true;
                        return;
                    }
                
            pressedBefore = Mouse.IsButtonPressed(Mouse.Button.Left);
        }
        public event Action Clicked;

        ~Ui() { Dispose(); }

        public virtual void Dispose()
        {
            isDisposed = true;
            gamemode.DisposablesRemove(this);
            gamemode.logicEvent -= LogicProcess;
            gamemode.logicEvent -= ClickProcess;
            gamemode.drawEvent -= DrawProcess;
            
            mask.Dispose();

            GC.SuppressFinalize(this);
        }

    }

    internal abstract class Particle : IDisposable
    {
        protected Gamemode gamemode;
        public bool isDisposed = false;
        public Particle(Gamemode gamemode, int lifeTime, Vector2f position, Vector2f scale, float rotation = 0f) 
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;

            lifeMax = lifeTime;
            lifeNow = lifeTime;

            this.gamemode = gamemode;
            gamemode.DisposablesAdd(this);
            gamemode.drawEvent += DrawProcess;
            gamemode.logicEvent += LogicProcess;

        }

        public int lifeMax, lifeNow;
        public float lifeRatio { get { return (float)lifeMax / lifeNow; } }
        
        public Vector2f position;
        public Vector2f scale;
        public float rotation;


        protected static Random random = new Random();


        public abstract void DrawProcess();
        public virtual void LogicProcess() 
        {

            lifeNow--;
            if (lifeNow == 0) Dispose();
            
            float drawableRange = CameraManager.size.Magnitude() * CameraManager.zoomValue;
            float dist = (this.position - CameraManager.position).Magnitude();
            if (drawableRange < dist) { 
                Dispose(); 
                
            }
            //if (lifeNow % 20 != 1) return; dist) Dispose();
             

                //    float dis = (position - CameraManager.position).Magnitude();
                //    float range = (CameraManager.size * 0.5f).Magnitude();
                //    if (dis > range) Dispose();
        }


        ~Particle() { Dispose(); }
        public virtual void Dispose()
        {
            isDisposed = true;
            gamemode.DisposablesRemove(this);
            gamemode.drawEvent -= DrawProcess;
            gamemode.logicEvent -= LogicProcess;

            GC.SuppressFinalize(this);
        }
    }

    internal abstract class Projectile : IDisposable
    {
        protected Gamemode gamemode;
        public bool isDisposed = false;

        public Projectile(Gamemode gamemode, int lifeTime, ICollision mask, Vector2f position, float rotation = 0f, float speed = 0f)
        {
            this.mask = mask;
            this.position = position;
            this.rotation = rotation;
            this.speed = rotation.ToRadian().ToVector() * speed;

            lifeMax = lifeTime;
            lifeNow = lifeTime;

            this.gamemode = gamemode;
            gamemode.DisposablesAdd(this);
            gamemode.drawEvent += DrawProcess;
            gamemode.logicEvent += LogicProcess;
            gamemode.logicEvent += PhysicProcess;
            gamemode.logicEvent += LifeProcess;
        }

        public int lifeMax, lifeNow;
        public float lifeRatio { get { return (float)lifeMax / lifeNow; } }

        public Action collisionCheck;

        public Vector2f position
        {
            set
            {
                if (mask is Box box)
                    box.Position = value;
                else if (mask is Circle circle)
                    circle.Position = value;
                else if (mask is Point point)
                    point.position = value;
                else if (mask is Line line)
                {
                    Vector2f differ = value - position;
                    line.positionFrom += differ;
                    line.positionTo += differ;
                }
                else
                    throw new NotImplementedException();
            }
            get
            {
                if (mask is Box box)
                    return box.Position;
                else if (mask is Circle circle)
                    return circle.Position;
                else if (mask is Point point)
                    return point.position;
                else if (mask is Line line)
                    return line.positionFrom;
                else
                    throw new NotImplementedException();
            }
        }
        public float rotation;
        public Vector2f speed;

        public ICollision mask;
        static Random random = new Random();
        void LifeProcess()
        {
            lifeNow--;
            if (lifeNow == 0) Dispose();
        }
        void PhysicProcess()
        {
            int collisionDensity = 5;
            float deltaTime = 1000f / (float)gamemode.logicFps;

            for (int i = 0; i < collisionDensity; i++)
            {
                if (isDisposed) break;
                collisionCheck?.Invoke();
                position += speed * (float)deltaTime / collisionDensity * ((float)random.NextDouble() * 0.2f + 0.9f);
            }
        }

        public abstract void DrawProcess();
        public abstract void LogicProcess();


        ~Projectile(){ Dispose(); }
        public virtual void Dispose()
        {
            if (gamemode == null) gamemode = Program.tm.gmNow;
            gamemode.DisposablesRemove(this);
            gamemode.drawEvent -= DrawProcess;
            gamemode.logicEvent -= LogicProcess;
            gamemode.logicEvent -= PhysicProcess;
            gamemode.logicEvent -= LifeProcess;

            GC.SuppressFinalize(this);

            isDisposed = true;
        }
    }

    internal abstract class Structure : IDisposable
    {
        protected Gamemode gamemode;
        public bool isDisposed = false;

        public Structure(Gamemode gamemode, Vector2f position, ICollision collision, DestructLevel destructLevel, bool isTransparent, float hardness)
        {
            mask = collision;
            this.Position = position;

            this.gamemode = gamemode;
            gamemode.DisposablesAdd(this);
            gamemode.drawEvent += DrawProcess;

            this.destructLevel = destructLevel;
            this.isTransparent = isTransparent;
            this.hardness = hardness;
        }

        public ICollision mask;
        public Vector2f Position
        {
            set
            {
                if (mask is Box box)
                    box.Position = value;
                else if (mask is Circle circle)
                    circle.Position = value;
                else if (mask is Point point)
                    point.position = value;
                else
                    throw new NotImplementedException();
            }
            get
            {
                if (mask is Box box)
                    return box.Position;
                else if (mask is Circle circle)
                    return circle.Position;
                else if (mask is Point point)
                    return point.position;
                else
                    throw new NotImplementedException();
            }
        }

        //물질 성질
        public bool isTransparent = false;   //물질 투명도
        public float hardness = 0.0f;       //물질 경도
        public DestructLevel destructLevel = DestructLevel.NONE; //물질 파괴 조건
        public float destructValue = 1.0f;  //물질 파괴 정도
        
        //파괴 속성
        public enum DestructLevel 
        {
            NONE,       //파괴불가
            FRAGILE,    //투사체로 깨짐
            DETONATABLE,//폭파로 파괴 가능
        }

        protected abstract void DrawProcess();
        public abstract List<Vector2f> GetPoligon();

        ~Structure() { Dispose(); }
        public virtual void Dispose()
        {
            isDisposed = true;
            gamemode.DisposablesRemove(this);
            gamemode.drawEvent -= DrawProcess;

            GC.SuppressFinalize(this);
        }
    }

}
