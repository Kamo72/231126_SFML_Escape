﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML;
using SFML.System;
using SFML.Audio;
using SFML.Graphics;
using SFML.Window;
using System.Threading;
 

namespace _231109_SFML_Test
{
    internal class Program
    {
        public static RenderWindow window;
        public static TotalManager tm;
        static void Main(string[] args)
        {
            VideoManager.SetWindow();

            tm = new TotalManager();

            int gcStack = 0;
            while (window.IsOpen)
            {
                lock (window)
                {
                    // 이벤트 처리
                    window.DispatchEvents();

                    // 화면 지우기
                    window.Clear(Color.Black);

                    {

                        //Do Something
                        tm.DrawAll();

                        VideoManager.FrameReset();

                    }
                    // 화면 업데이트
                    window.Display();

                    if (gcStack++ > 30)
                    {
                        GC.Collect();
                    }
                }
            }

        }
    }
}
