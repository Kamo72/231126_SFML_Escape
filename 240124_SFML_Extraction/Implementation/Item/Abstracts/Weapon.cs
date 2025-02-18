﻿using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
 
using static _231109_SFML_Test.Weapon;
using static _231109_SFML_Test.WeaponStatus;

namespace _231109_SFML_Test
{
    internal abstract class Weapon : Equipable, IHandable, IDurable, IAttachable
    {
        static Random random = new Random();
        static Weapon() { }
        public Weapon(string weaponCode)
        {
            //기존 스텟 가져오기
            this.weaponCode = weaponCode;
            //status = statusOrigin;
            selectorNow = status.typeDt.selectorList[1];
            this.attachments = status.attachDt.CopyList();
            chambers = new List<Ammo>(status.detailDt.chamberSize);

            commandsReact = new Dictionary<InputManager.CommandType, Action<Humanoid.Hands, bool>>()
            {
                { InputManager.CommandType.FIRE, (hands, isTrue) => {

                    GamemodeIngame gm = Program.tm.gmNow as GamemodeIngame;
                    
                    //격발 딜레이
                    delayNow -= 1f/(float)gm.logicFps;

                    //과열 시간 제어
                    muzzleHeat -= gm.deltaTime * muzzleHeatReduce;
                    muzzleHeat = Mathf.Clamp(0f, muzzleHeat, muzzleHeatMax);

                    //과열 파티클 이펙트
                    float muzzleSmokeRatio = Math.Max(muzzleHeat, 0f) * 1f;
                    if(muzzleSmokeRatio > (float)random.NextDouble())
                    {
                        Vector2f muzzlePos = hands.master.Position + hands.handPos + hands.AnimationGetPos(specialPos.muzzlePos).RotateFromZero(hands.handRot);
                        new MuzzleSmoke(gm, muzzlePos, hands.handRot);
                    }

                    //격발 판단
                    switch (selectorNow)
                    {
                        case SelectorType.SEMI:
                            if(isTrue == false) break;
                            if(delayNow > 0f) break;
                            if(triggeredBefore == true) break;
                            if(isFireableState(hands.state) == false) break;

                            if(IsValidAttachments(this) == false) break;

                            Fire(hands);

                            break;

                        case SelectorType.AUTO:
                            if(isTrue == false) break;
                            if(delayNow > 0f) break;
                            if(isFireableState(hands.state) == false) break;

                            if(IsValidAttachments(this) == false) break;

                            Fire(hands);

                            break;

                        case SelectorType.BURST2:
                            if(isFireableState(hands.state) == false) break;

                            if(IsValidAttachments(this) == false) break;

                            Console.WriteLine("SelectorType.BURST2 구현안됨");
                            break;

                        case SelectorType.BURST3:
                            if(isFireableState(hands.state) == false) break;

                            if(IsValidAttachments(this) == false) break;

                            Console.WriteLine("SelectorType.BURST3 구현안됨");
                            break;
                    }

                    triggeredBefore = isTrue;

                } },
                { InputManager.CommandType.AIM, (hands, isTrue) =>
                {
                    hands.master.aim.isAds =false;

                    if(isTrue)
                    {
                        if(isFireableState(hands.state) )
                        {
                           hands.master.aim.isAds = true;
                        }
                    }
                } },
                { InputManager.CommandType.BOLT_ROUND, (hands, isTrue) => {

                    if(isTrue){

                        if ((int)hands.master.movement.targetIndex >= 1.99f
                            && hands.master.movement.nowValue > 0.99f)
                            hands.master.movement.targetIndex = Humanoid.Movement.MovementState.IDLE;

                        if(isFireableState(hands.state))
                        {
                            if(magazineAttached != null)
                                hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.BOLT_ROUND);
                        }
                    }
                } },
                { InputManager.CommandType.MAGAZINE_REMOVE, (hands, isTrue) => {

                    if(isTrue){

                        if ((int)hands.master.movement.targetIndex >= 1.99f
                            && hands.master.movement.nowValue > 0.99f)
                            hands.master.movement.targetIndex = Humanoid.Movement.MovementState.IDLE;


                        if(isFireableState(hands.state))
                        {
                            if(magazineAttached != null)
                                hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.MAGAZINE_REMOVE);
                        }
                    }
                } },
                { InputManager.CommandType.MAGAZINE_INSPECT, (hands, isTrue) => {
                    if(isTrue)
                    {
                        hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.MAGAZINE_INSPECT);
                    }
                } },
                { InputManager.CommandType.MAGAZINE_CHANGE, (hands, isTrue) =>
                {
                    if(isTrue){

                        if ((int)hands.master.movement.targetIndex >= 1.99f
                            && hands.master.movement.nowValue > 0.99f)
                            hands.master.movement.targetIndex = Humanoid.Movement.MovementState.IDLE;


                        if(isFireableState(hands.state))
                        {
                            if(magazineAttached != null)
                                hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.MAGAZINE_CHANGE);
                            else
                                hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.MAGAZINE_ATTACH);
                        }
                    }

                    if(hands.state != Humanoid.Hands.AnimationState.IDLE) return;
                    if(magazineAttached == null) return;
                    if(magazineAttached?.AmmoPeek() == null) return;
                    if(status.typeDt.mechanismType == MechanismType.NONE) return;
                    if(status.typeDt.magazineType == MagazineType.SYLINDER) return;
                    if(chambers.Capacity == 0) return;
                    if(chambers.Capacity == chambers.FindAll(a => a.isUsed == false).Count) return;


                    if(status.typeDt.mechanismType == MechanismType.OPEN_BOLT)
                    {
                        hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.BOLT_BACK);
                    }
                    else{
                        if(boltValue.backwardValue > 0.99f)
                        {
                            hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.BOLT_FOWARD);
                        }
                        else
                        {
                            hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.BOLT_ROUND);
                        }
                    }
                } },

                { InputManager.CommandType.TDEVICE_CHANGE, (hands, isTrue) => {

                } },
                { InputManager.CommandType.TDEVICE_POWER, (hands, isTrue) =>{

                    if(isTrue)
                        if(isFireableState(hands.state))
                        {
                            if(magazineAttached != null)
                                hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.TDEVICE_INTERACTION);
                        }
                } },
                { InputManager.CommandType.SELECTOR_INSPECT, (hands, isTrue) => {

                    if(isTrue)
                        if(isFireableState(hands.state))
                        {
                            if(magazineAttached != null)
                                hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.WEAPON_INSPECT_SHORT);
                        }
                } },
                { InputManager.CommandType.SELECTOR_CHANGE, (hands, isTrue) => {

                    if(isTrue)
                        if(isFireableState(hands.state))
                        {
                            if(magazineAttached != null)
                                hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.SELECTOR_CHANGE);
                        }
                } },
                //{ InputManager.CommandType.MELEE, (hand, isTrue) => {

                //} },
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

        public WeaponStatus status
        {
            get
            {
                WeaponStatus retStat = statusOrigin;

                foreach (AttachSocket socket in attachments)
                    if (socket.attachment != null)
                        if (socket.attachment is Attachment tAttachment)
                            retStat = GetAttachmentAdjustedStatus(retStat, tAttachment);

                return retStat;
            }
        }
        WeaponStatus GetAttachmentAdjustedStatus(WeaponStatus oriStat, Attachment attachment)
        {
            WeaponStatus retStat = oriStat;

            foreach (WeaponAdjust adjust in attachment.weaponAdjusts)
                retStat = adjust.adjustFun(retStat);

            if (attachment is IAttachable iattachable)
                foreach (AttachSocket socket in iattachable.attachments)
                    if (socket.attachment != null)
                        if (socket.attachment is Attachment tAttachment)
                            retStat = GetAttachmentAdjustedStatus(retStat, tAttachment);

            return retStat;
        }
        string weaponCode;

        //size 재정의
        public override Vector2i size
        {
            get
            {
                (int top, int bottom, int left, int right) GetAllSizeAdjust(AttachSocket socket) 
                {

                    if (socket.attachment == null) return (0, 0, 0, 0);
                    if (socket.attachment is Attachment atc)
                    {
                        var retSize = atc.sizeAdjust;
                        if (atc is IAttachable attachable) 
                        {
                            foreach (var sAtc in attachable.attachments)
                            {
                                var tSize = GetAllSizeAdjust(sAtc);
                                retSize = (retSize.top + tSize.top, retSize.bottom + tSize.bottom, retSize.left + tSize.left, retSize.right + tSize.right);
                            }
                        }

                        return retSize;
                    }
                    return (0, 0, 0, 0);
                }
                
                Vector2i ret = sizeOrigin;

                foreach (var item in attachments) 
                {
                    var ttSize = GetAllSizeAdjust(item);
                    ret += new Vector2i(ttSize.right + ttSize.left, ttSize.top + ttSize.bottom);
                }

                return ret;
            }
        }

        #endregion

        #region [탑뷰 스프라이트]
        protected void InitHandableParts(Vector2i topSpriteSize, List<Texture> textures, List<Texture> boltTexs)
        {
            this.topSpriteSize = topSpriteSize;

            RectangleShape[] norms = new RectangleShape[textures.Count];
            List<RectangleShape> bolts = new List<RectangleShape>();

            for (int i = 0; i < textures.Count; i++)
            {
                RectangleShape shape = new RectangleShape((Vector2f)topSpriteSize);
                norms[i] = shape;
                norms[i].Texture = textures[i];
                norms[i].Origin = new Vector2f(50, 50);


                if (boltTexs.Contains(textures[i]) == true) bolts.Add(shape);
            }

            topParts = norms;
            boltParts = bolts;
        }

        //탑뷰 드로우를 위한 Rects
        protected RectangleShape[] topParts;
        protected List<RectangleShape> boltParts;
        public Vector2i topSpriteSize;

        //인게임 총기 스프라이트를 생성형으로 반환


        void DrawAttachmentFunc(IAttachment iatchment, RenderTexture texture, Vector2f position, float direction, Vector2f scaleRatio, RenderStates renderStates)
        {
            if (iatchment == null) return;
            DrawHandablePart(texture, iatchment.attachmentShape, position, direction, scaleRatio, renderStates);

            if (iatchment is IAttachable iattachable)
                foreach (var socket in iattachable.attachments)
                {
                    if (socket.attachment == null) continue;
                    if (socket.attachment.attachmentShape == null) continue;

                    Vector2f sepPos = new Vector2f(socket.attachPos.X * scaleRatio.X, socket.attachPos.Y * scaleRatio.Y).RotateFromZero(direction);
                    Vector2f tPosition = position + sepPos;
                    DrawAttachmentFunc(socket.attachment, texture, tPosition, direction, scaleRatio, renderStates);
                }
        }

        public virtual void DrawHandable(RenderTexture texture, Vector2f position, float direction, Vector2f scaleRatio, RenderStates renderStates)
        {
            Vector2f boltAdjust = boltVec.backwardVec * boltValue.backwardValue
                + boltVec.lockVec * boltValue.lockValue;

            for (int i = topParts.Length - 1; i >= 0; i--)
            {
                Vector2f pos = boltParts.Contains(topParts[i]) == true ? position
                    + boltAdjust.RotateFromZero(direction) : position;
                DrawHandablePart(texture, topParts[i], pos, direction, scaleRatio, renderStates);
            }
            foreach (var socket in attachments)
            {
                if (socket.attachment == null) continue;
                if (socket.attachment.attachmentShape == null) continue;

                //Vector2f sepPos = Mathf.InRange(-90f, direction, 90f) ? socket.attachPos.RotateFromZero(direction) : new Vector2f(socket.attachPos.X, -socket.attachPos.Y).RotateFromZero(direction);
                Vector2f sepPos = new Vector2f(socket.attachPos.X * scaleRatio.X, socket.attachPos.Y * scaleRatio.Y).RotateFromZero(direction);
                Vector2f tPosition = position + sepPos;
                DrawAttachmentFunc(socket.attachment, texture, tPosition, direction, scaleRatio, renderStates);
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
        
        public List<Ammo> chambers;

        float delayMax => status.detailDt.RoundDelay;

        List<Humanoid.Hands.AnimationState> nonFireableStates = new List<Humanoid.Hands.AnimationState>
        {
            Humanoid.Hands.AnimationState.FIRE,
            Humanoid.Hands.AnimationState.SPRINT,

            Humanoid.Hands.AnimationState.INVENTORY,

            Humanoid.Hands.AnimationState.EQUIP,
            Humanoid.Hands.AnimationState.UNEQUIP,

            Humanoid.Hands.AnimationState.BOLT_BACK,
            Humanoid.Hands.AnimationState.BOLT_ROUND,
            Humanoid.Hands.AnimationState.BOLT_FOWARD,

            Humanoid.Hands.AnimationState.MAGAZINE_REMOVE,
            Humanoid.Hands.AnimationState.MAGAZINE_ATTACH,
            Humanoid.Hands.AnimationState.MAGAZINE_INSPECT,
            Humanoid.Hands.AnimationState.MAGAZINE_CHANGE,

            Humanoid.Hands.AnimationState.SELECTOR_CHANGE,
            Humanoid.Hands.AnimationState.WEAPON_INSPECT_SHORT,

            Humanoid.Hands.AnimationState.TDEVICE_INTERACTION,
        };
        public bool isFireableState(Humanoid.Hands.AnimationState state) =>
            nonFireableStates.Contains(state) == false;
        
        float delayNow = 0f;
        bool triggeredBefore = false;
        public SelectorType selectorNow;
        float muzzleHeat = 0f, muzzleHeatDelta = 0.10f, muzzleHeatReduce = 0.20f, muzzleHeatMax = 1f;

        void Fire(Humanoid.Hands hands) 
        {
            GamemodeIngame gm = Program.tm.gmNow as GamemodeIngame;
            if(gm == null) throw new NullReferenceException("Weapon - fire 함수에서 gm이 null값입니다!" + nameof(gm));

            //총구 위치를 세계 좌표로 변환
            Vector2f muzzlePos = hands.master.Position + hands.handPos + hands.AnimationGetPos(specialPos.muzzlePos).RotateFromZero(hands.handRot);

            Ammo ammo = status.typeDt.mechanismType == MechanismType.OPEN_BOLT ? magazineAttached.AmmoPop() : chambers.Find(a => a.isUsed == false);
            if (ammo == null) return;

            ammo.isUsed = true;

            //투사체 생성
            for (int i = 0; i < ammo.status.lethality.pellitCount; i++)
            {
                //총탄 생성 및 저장
                Vector2f moaSpray = ((float)random.NextDouble() * 360f).ToRadian().ToVector()
                    * status.aimDt.ads.moa /10f;
                Vector2f hipSpray = ((float)random.NextDouble() * 360f).ToRadian().ToVector()
                    * hands.master.aim.hipSpray;
                Vector2f dynamicDot = hands.master.aim.dynamicDot - (Vector2f)VideoManager.resolutionNow / 2f + CameraManager.position;

                Projectile bullet = new Bullet(gm, ammo.status, muzzlePos, dynamicDot + moaSpray + hipSpray, status.detailDt.muzzleVelocity / 240f);
                gm.projs.Add(bullet);
            }
            
            //격발 간격 설정
            delayNow = delayMax;

            //Y값이 뒤집힌경우 배출구 편차 보정
            Vector2f chamberSep = (-90f <= hands.handRot && hands.handRot <= 90f) ? specialPos.ejectPos : new Vector2f(specialPos.ejectPos.X, -specialPos.ejectPos.Y);

            //배출구 위치를 세계 좌표로 변환
            Vector2f chamberPos = hands.master.Position + hands.handPos + chamberSep.RotateFromZero(hands.handRot);

            //파티클 이펙트
            for (int i = 0; i < 5; i++)
                new MuzzleSmoke(gm, chamberPos, hands.handRot - 180f);

            for (int i = 0; i < 30; i++)
                new MuzzleSmoke(gm, muzzlePos, hands.handRot);

            new MuzzleFlash(gm, muzzlePos, 300);

            //에임에 반동 적용
            hands.master.aim.GetHipRecoil();
            hands.master.aim.GetRecoilVector();

            //총열 과열 적용
            muzzleHeat += muzzleHeatDelta;

            //애니메이션 적용
            hands.nowAnimator.ChangeState(Humanoid.Hands.AnimationState.FIRE);

            //사운드 이펙트
            SoundManager.waveEffect.AddSound(ResourceManager.sfxs["FireDmr"], muzzlePos, status.detailDt.loudness, 0.1f);
        }

        #endregion

        #region [파츠 시스템]
        public Magazine magazineAttached;

        public (Vector2f magPos,
            Vector2f muzzlePos,
            Vector2f ejectPos,
            Vector2f pistolPos,
            Vector2f secGripPos,
            Vector2f boltPos) specialPos;

        public (Vector2f backwardVec, Vector2f lockVec) boltVec;
        public (float backwardValue, float lockValue) boltValue = (0f, 0f);

        public List<AttachSocket> attachments { get; set; } = new List<AttachSocket> ();

        public bool IsValidAttachments(IAttachable iatc)
        {
            foreach (var item in iatc.attachments)
            {
                if (item.isNeccesary == true && item.attachment == null)
                    return false;

                if(item.attachment == null) continue;

                if(item.attachment is IAttachable iatcInner)
                    if(IsValidAttachments(iatcInner) == false) return false;
            }

            return true;
        }

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
        LOCK_TO_FIRE,   //볼트를 잠궈야 사격 가능 ex karabiner 98
        NONE,           //노리쇠 후퇴 고정 불가 ex AK47
    }

    public struct WeaponStatus
    {
        public WeaponStatus(WeaponStatus status)
        {
            this.typeDt = status.typeDt;
            this.aimDt = status.aimDt;
            this.timeDt = status.timeDt;
            this.moveDt = status.moveDt;
            this.detailDt = status.detailDt;
            this.attachDt = status.attachDt;
        }
        public WeaponStatus(TypeData typeData, AimData aimData, TimeData timeData, MovementData movementData, DetailData detailData, AttachData attachData)
        { 
            this.typeDt = typeData;
            this.aimDt = aimData;
            this.timeDt = timeData;
            this.moveDt = movementData;
            this.detailDt = detailData;
            this.attachDt = attachData;
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

                /// <summary>
                /// adsData 라이브러리에서 adsData를 찾는 키값
                /// </summary>
                public string adsName;
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
            public float RoundDelay =>  60f / roundPerMinute;
            public float roundPerMinute;
            public int chamberSize; //약실 크기
            public List<Type> magazineWhiteList;  //장착 가능한 탄창리스트
            public float muzzleVelocity;    //총구 속도
            public float effectiveRange;    //유효 사거리
            public float barrelLength;      //총기 전장
            public float loudness;          //소음 크기 (거리로 ex 10000)
        }

        public AttachData attachDt;
        public struct AttachData
        {
            internal List<AttachSocket> socketList;
        }
    }
    public static class WeaponStatusEx
    {
        internal static List<AttachSocket> CopyList(this AttachData attachDt) 
        {
            List < AttachSocket >  list = new List<AttachSocket>(attachDt.socketList);

            for (int i = 0; i < list.Count; i++) 
            {
                AttachSocket socket = list[i];
                if (socket.attachment != null) 
                {
                    AttachSocket originSocket = attachDt.socketList[i];
                    Type originType = originSocket.attachment.GetType();
                    var newItem = Activator.CreateInstance(originType);
                    socket.attachment = newItem as IAttachment;
                }
            }

            return list;
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
        public WeaponAdjust(string description, WeaponAdjustType adjustType, Func<WeaponStatus, WeaponStatus> adjustFun)
        {
            this.description = description;
            this.adjustType = adjustType;
            this.adjustFun = adjustFun;
        }

        public string description;
        public WeaponAdjustType adjustType;
        public Func<WeaponStatus, WeaponStatus> adjustFun;
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
