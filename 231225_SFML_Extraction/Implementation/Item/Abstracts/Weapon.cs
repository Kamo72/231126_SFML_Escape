﻿using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace _231109_SFML_Test
{
    internal abstract class Weapon : Equipable, IHandable, IDurable
    {
        static Random random = new Random();
        static Weapon() { }
        public Weapon(string weaponCode)
        {
            //기존 스텟 가져오기
            this.weaponCode = weaponCode;
            status = statusOrigin;
            selectorNow = status.typeDt.selectorList[1];

            commandsReact = new Dictionary<InputManager.CommandType, Action<Humanoid.Hands, bool>>()
            {
                { InputManager.CommandType.FIRE, (hands, isTrue) => {

                    GamemodeIngame gm = Program.tm.gmNow as GamemodeIngame;
                    delayNow -= 1f/(float)gm.logicFps;

                    switch (selectorNow)
                    {
                        case SelectorType.SEMI:
                            if(isTrue == false) break;
                            if(delayNow > 0f) break;
                            if(triggeredBefore == true) break;

                            Fire(hands);

                            break;
                        case SelectorType.AUTO:
                            if(isTrue == false) break;
                            if(delayNow > 0f) break;

                            Fire(hands);

                            break;
                        case SelectorType.BURST2:
                            Console.WriteLine("SelectorType.BURST2 구현안됨");
                            break;
                        case SelectorType.BURST3:
                            Console.WriteLine("SelectorType.BURST3 구현안됨");
                            break;
                    }

                    triggeredBefore = isTrue;


                    muzzleHeat -= VideoManager.GetTimeDelta();
                    muzzleHeat = Mathf.Clamp(0f, muzzleHeat, muzzleHeatMax);

                    float muzzleSmokeRatio = Math.Max(muzzleHeat, 0f) * 1f;
                    if(muzzleSmokeRatio > (float)random.NextDouble())
                    {
                        Vector2f muzzleSep = (-90f <= hands.handRot && hands.handRot <= 90f) ? specialPos["muzzlePos"] : new Vector2f(specialPos["muzzlePos"].X, -specialPos["muzzlePos"].Y);
                        Vector2f muzzlePos = hands.master.Position + hands.handPos + muzzleSep.RotateFromZero(hands.handRot);
                        new MuzzleSmoke(gm, muzzlePos, hands.handRot);
                    }
                } },
                { InputManager.CommandType.AIM, (hands, isTrue) => { } },
                //{ InputManager.CommandType.MAGAZINE_CHANGE, (hands, isTrue) => { } },
                //{ InputManager.CommandType.SPRINT, (hands, isTrue) => { } },
                //{ InputManager.CommandType.MELEE, (hand, isTrue) => { } }
            };
        }

        #region [변수 목록]
        //인터페이스 구현
        public float equipTime => status.timeDt.swapTime;
        public float equipValue { get; set; } = 0f;
        public float durableNow { get; set; } = 100f;
        public float durableMax { get; set; } = 0f;
        public bool zeroToDestruct { get; set; } = true;
        
        //허용된 조작 목록
        public Dictionary<InputManager.CommandType, Action<Humanoid.Hands, bool>> commandsReact { get; set; }
            

        //스테이터스
        public WeaponStatus statusOrigin { get { return WeaponLibrary.Get(weaponCode); } }
        public WeaponStatus status;
        string weaponCode;
        #endregion

        #region [탑뷰 스프라이트]
        protected void InitHandableParts(Vector2i topSpriteSize, params Texture[] textures)
        {
            this.topSpriteSize = topSpriteSize;

            RectangleShape[] rects = new RectangleShape[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                rects[i] = new RectangleShape((Vector2f)topSpriteSize);
                rects[i].Texture = textures[i];
                rects[i].Origin = new Vector2f(50, 50);
            }

            topParts = rects;
        }

        //탑뷰 드로우를 위한 Rects
        protected RectangleShape[] topParts;
        public Vector2i topSpriteSize;

        //인게임 총기 스프라이트를 생성형으로 반환
        public virtual void DrawHandable(RenderTexture texture, Vector2f position, float direction, Vector2f scaleRatio, RenderStates renderStates)
        {

            for (int i = topParts.Length - 1; i >= 0; i--)
            {
                RectangleShape shape = topParts[i];
                DrawHandablePart(texture, shape, position, direction, scaleRatio, renderStates);
            }
        }
        public void DrawHandable(RenderTexture texture, Vector2f position, float direction, Vector2f scaleRatio) { DrawHandable(texture, position, direction, scaleRatio, RenderStates.Default); }

        protected void DrawHandablePart(RenderTexture texture, RectangleShape shape, Vector2f position, float direction, Vector2f scaleRatio, RenderStates renderStates)
        {
            shape.Scale = scaleRatio;
            shape.Position = position + direction.ToRadian().ToVector() * 1f;
            shape.Rotation = direction;

            texture.Draw(shape, renderStates);
        }
        #endregion

        #region [격발 시스템]

        float delayMax { get { return status.detailDt.RoundDelay; } }
        float delayNow = 0f;
        bool triggeredBefore = false;
        SelectorType selectorNow;
        float muzzleHeat = 0f, muzzleHeatDelta = 0.12f, muzzleHeatMax = 1f;

        void Fire(Humanoid.Hands hands) 
        {
            CameraManager.GetShake(10f);

            GamemodeIngame gm = Program.tm.gmNow as GamemodeIngame;
            //Y값이 뒤집힌경우 총구 편차 보정
            Vector2f muzzleSep = (-90f <= hands.handRot && hands.handRot <= 90f) ? specialPos["muzzlePos"] : new Vector2f(specialPos["muzzlePos"].X, -specialPos["muzzlePos"].Y);

            //총구 위치를 세계 좌표로 변환
            Vector2f muzzlePos = hands.master.Position + hands.handPos + muzzleSep.RotateFromZero(hands.handRot);

            //AmmoStatus ammoStatus = status.typeDt.mechanismType == MechanismType.CLOSED_BOLT? magazineAttached.AmmoPeek() : chamber[0];
            Ammo ammo = magazineAttached.AmmoPeek();
            if (ammo == null) return;

            for (int i = 0; i < ammo.status.lethality.pellitCount; i++)
            {
                //총탄 생성 및 저장
                float moaSpray = ((float)random.NextDouble() - 0.5f) * status.aimDt.ads.moa;

                Projectile bullet = new Bullet(gm, ammo.status, muzzlePos, hands.handRot + moaSpray, status.detailDt.muzzleVelocity / 240f);
                gm.projs.Add(bullet);
            }
            delayNow = delayMax;

            for (int i = 0; i < 30; i++)
                new MuzzleSmoke(gm, muzzlePos, hands.handRot);

            new MuzzleFlash(gm, muzzlePos, 300);

            //Y값이 뒤집힌경우 배출구 편차 보정
            Vector2f chamberSep = (-90f <= hands.handRot && hands.handRot <= 90f) ? specialPos["ejectPos"] : new Vector2f(specialPos["ejectPos"].X, -specialPos["ejectPos"].Y);

            //배출구 위치를 세계 좌표로 변환
            Vector2f chamberPos = hands.master.Position + hands.handPos + chamberSep.RotateFromZero(hands.handRot);

            new CartridgeBig(gm, chamberPos, 50f);
            for (int i = 0; i < 5; i++)
                new MuzzleSmoke(gm, chamberPos, hands.handRot - 180f);


            muzzleHeat += muzzleHeatDelta;
        }

        #endregion

        #region [파츠 시스템]
        protected Magazine magazineAttached;

        protected Dictionary<string, Vector2f> specialPos;
        //Vector2f magPos, muzzlePos, ejectPos, pistolPos, secGripPos, boltPos;
        #endregion

        #region [주무기, 보조무기 장착 조건]
        //주무기 장착 조건, 보조무기 장착 조건
        public bool AbleSub()
        {
            if (size.X <= 3 && size.Y <= 1)
                return true;

            return false;
        }
        public bool AbleMain()
        {
            if (size.X >= 3 || size.Y >= 2)
                return true;

            return false;
        }
        #endregion

        public override void Dispose()
        {
            base.Dispose();

            foreach(RectangleShape r in topParts)
                r?.Dispose();

        }
    }

    #region [총기 기본 정보]
    public enum CaliberType
    {
        p22,
        p45,
        mm9x18,
        mm9x19,
        mm5p56x45,
        mm7p62x51,
    }
    public enum SelectorType
    {
        SEMI,
        AUTO,
        BURST2,
        BURST3
    }
    public enum MechanismType
    {
        CLOSED_BOLT,    //폐쇄 노리쇠
        OPEN_BOLT,      //개방 노리쇠
        MANUAL_RELOAD,  //수동 약실 장전
        NONE,           //볼트 없음
    }
    public enum MagazineType
    {
        MAGAZINE,   //박스형 탄창 - 빠른 교체
        SYLINDER,   //탄창X 약실만 - 중절식, 리볼버 등 탄창 개념이 없음.
        TUBE,       //튜브형 탄창 - 전탄 소진 시, 약실 장전
        INTERNAL,   //내장형 탄창 - 장전 시 볼트 재낌    
    }
    public enum BoltLockerType
    {
        ACTIVATE,       //전탄 소진 시 노리쇠 후퇴 고정 ex M4A1
        ONLY_MANUAL,    //수동으로만 노리쇠 후퇴 고정 ex MP5
        NONE,           //노리쇠 후퇴 고정 불가 ex AK47
    }

    public struct WeaponStatus
    {
        public WeaponStatus(TypeData typeData, AimData aimData, TimeData timeData, MovementData movementData, DetailData detailData)
        { 
            this.typeDt = typeData;
            this.aimDt = aimData;
            this.timeDt = timeData;
            this.moveDt = movementData;
            this.detailDt = detailData;
        }


        //무장 유형 정보
        public TypeData typeDt;
        public struct TypeData 
        {
            public TypeData(MechanismType mechanismType, MagazineType magazineType, BoltLockerType boltLockerType, List<SelectorType> selectorList, CaliberType caliberType) 
            {
                this.mechanismType = mechanismType;
                this.magazineType = magazineType;
                this.boltLockerType = boltLockerType;
                this.selectorList = selectorList;
                this.caliberType = caliberType;
            }

            public MechanismType mechanismType;     //작동 방시 
            public MagazineType magazineType;       //탄창 방식
            public BoltLockerType boltLockerType;   //노리쇠멈치 방식
            public List<SelectorType> selectorList; //조정간
            public CaliberType caliberType;         //구경
        }

        //조준 정보
        public AimData aimDt;
        public struct AimData
        {
            public struct HipData   //지향 사격
            {
                public struct HipStancelData    //자세 값
                {
                    /// <summary>
                    /// 지향 자세 회복 속도
                    /// </summary>
                    public float recovery;
                    /// <summary>
                    /// 지향 자세 정확도
                    /// </summary>
                    public float accuracy;
                    /// <summary>
                    ///  정확도 변인 [엄폐, 걷기]
                    /// </summary>
                    public (float crounch, float walk) accuracyAdjust;
                }
                /// <summary>
                /// 자세 값
                /// </summary>
                public HipStancelData stance;

                public struct HipRecoilData     //반동 값
                {
                    /// <summary>
                    /// 지향 반동 회복 속도
                    /// </summary>
                    public float recovery;
                    /// <summary>
                    /// 회복 속도 변인 [엄폐, 걷기]
                    /// </summary>
                    public (float crounch, float walk) recoveryAdjust;
                    /// <summary>
                    /// 지향 반동 크기
                    /// </summary>
                    public float strength;
                }
                /// <summary>
                /// 반동 값
                /// </summary>
                public HipRecoilData recoil;

                /// <summary>
                /// 트래깅 속도
                /// </summary>
                public float traggingSpeed;     
            };
            /// <summary>
            /// 지향 사격
            /// </summary>
            public HipData hip;

            public struct AdsData   //조준 사격
            {
                public struct AdsStancelData    //자세 값
                {
                    /// <summary>
                    /// 조준 자세 정확도
                    /// </summary>
                    public float accuracy;
                    /// <summary>
                    /// 정확도 변인 [엄폐, 걷기]
                    /// </summary>
                    public (float crounch, float walk) accuracyAdjust;
                }
                /// <summary>
                /// 자세 값
                /// </summary>
                public AdsStancelData stance;

                public struct AdsRecoilData    //반동 값
                {
                    /// <summary>
                    /// 조준점 반동 고정
                    /// </summary>
                    public Vector2f fix;   
                    /// <summary>
                    /// 조준점 반동 랜덤
                    /// </summary>
                    public Vector2f random;
                    /// <summary>
                    /// 조준점 반동 회복 속도
                    /// </summary>
                    public float recovery;
                    /// <summary>
                    /// 지향 반동 크기 변인
                    /// </summary>
                    public (float crounch, float walk) strengthAdjust;
                }
                /// <summary>
                /// 반동 값
                /// </summary>
                public AdsRecoilData recoil;

                /// <summary>
                /// 절대 명중률(거리 1000 기준)
                /// </summary>
                public float moa;
            };
            /// <summary>
            /// 조준 사격
            /// </summary>
            public AdsData ads;

        }

        //행동 소요 시간 정보 
        public TimeData timeDt;
        public struct TimeData
        {
            public float adsTime;       //조준 속도
            public float sprintTime;    //질주 후 사격 전환 속도
            /// <summary>
            /// 재장전 속도 - 박스(분리 / 결합 (장전)) - 실린더(사출 / 장전 / 결합) - 내부(볼트 재낌 /장전) - (장전 준비 / (약실 장전) / 튜브 장전 )
            /// </summary>
            public (float, float, float) reloadTime;
            public float swapTime;      //무기 교체 속도
        }

        //이동 속도 정보
        public MovementData moveDt;
        public struct MovementData 
        {
            /// <summary>
            /// 기본 이동 속도 배율
            /// </summary>
            public float speed;
            /// <summary>
            /// 이속 변인 (엄폐, 조준, 질주)
            /// </summary>
            public (float crounch, float ads, float sprint) speedAdjust;
        }

        //상세 정보
        public DetailData detailDt;
        public struct DetailData 
        {
            public float RoundDelay { get { return 60f / roundPerMinute; } }
            public float roundPerMinute;
            public int chamberSize; //약실 크기
            public List<Type> magazineWhiteList;  //장착 가능한 탄창리스트
            public float muzzleVelocity;    //총구 속도
            public float effectiveRange;    //유효 사거리
            public float barrelLength;      //총기 전장
        }


    }
    #endregion

    #region [총기 옵션 정보]

    public enum WeaponAdjustType
    {
        PROS,   //장
        CONS,   //단
        NONE,   //?
    }
    public struct WeaponAdjust
    {
        public WeaponAdjust(string description, WeaponAdjustType adjustType, Action<WeaponStatus> adjustFun)
        {
            this.description = description;
            this.adjustType = adjustType;
            this.adjustFun = adjustFun;
        }

        public string description;
        public WeaponAdjustType adjustType;
        public Action<WeaponStatus> adjustFun;
    }

    #endregion


    #region [총기 데이터셋 제공자]
    internal static class WeaponLibrary
    {
        static WeaponLibrary() 
        {
            weaponLib = new Dictionary<string, WeaponStatus>();
            WeaponDataLoad();
        }

        static Dictionary<string, WeaponStatus> weaponLib;
        static void WeaponDataLoad() 
        {
            //정적 생성자를 불러오는 역할?
            FN_FAL fn_fal;
        }
        public static WeaponStatus Get(string weaponName)
        {
            return weaponLib[weaponName];
        }
        public static void Set(string weaponName, WeaponStatus weaponStatus)
        {
            if (weaponLib.ContainsKey(weaponName)) throw new Exception("weaponLib - 중복된 키 삽입!");
            weaponLib.Add(weaponName, weaponStatus);
        }

    }

    #endregion
}
