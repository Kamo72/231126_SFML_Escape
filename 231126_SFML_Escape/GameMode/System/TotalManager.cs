﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;

using SFML.Graphics;
using SFML.System;

using Rm = _231109_SFML_Test.ResourceManager;
using Cm = _231109_SFML_Test.CameraManager;
using Dm = _231109_SFML_Test.DrawManager;
using Im = _231109_SFML_Test.InputManager;
using Sm = _231109_SFML_Test.SoundManager;
using Vm = _231109_SFML_Test.VideoManager;
using System.Runtime.InteropServices;

using dy = dynamic;

namespace _231109_SFML_Test
{
    internal class TotalManager
    {
        public TotalManager()
        {
            gmEnumToType[GamemodeType.LOGO] = typeof(GamemodeLogo);
            gmEnumToType[GamemodeType.MAIN_MENU] = typeof(GamemodeMainMenu);
            gmEnumToType[GamemodeType.INGAME] = typeof(GamemodeIngame);
            gmEnumToType[GamemodeType.RESULT] = typeof(void);

            SetGamemodeType(GamemodeType.LOGO);
        }

        public void DrawAll()
        {
            //카메라 위상에 맞게 Transform 최신화
            CameraManager.RefreshTransform();
            //카메라 흔들림 적용
            CameraManager.ShakeProcess();

            //그릴거 다 그리기
            gmNow?.DoDraw();
            
            //결과 텍스쳐 최신화
            DrawManager.ResultTexture();
        }

        public Gamemode gmNow;
        public GamemodeType gamemode = GamemodeType.NONE;

        public static Dictionary<GamemodeType, Type> gmEnumToType = new Dictionary<GamemodeType, Type>();

        public void SetGamemodeType(GamemodeType gamemode)
        {
            if (this.gamemode == gamemode) return;
            
            gmNow?.Dispose();
            this.gamemode = gamemode;

            Type type = gmEnumToType[this.gamemode];
            gmNow = (Gamemode)Activator.CreateInstance(type, this);
        }

    }


 

    public enum GamemodeType
    {
        NONE,
        LOGO,
        MAIN_MENU,
        INGAME,
        RESULT,
    }

}
