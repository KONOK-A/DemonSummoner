using System;
using System.Collections.Generic;
using DG.Tweening;
using Games.MainModule.Configs.Summons;
using Games.MainModule.Scripts.Character;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.UI.Description;
using Games.MainModule.UI.ToastPanel;
using Games.MainModule.View.FloatDamage;
using Games.MainModule.View.HealthBar;
using UnityEngine;
using UnityEngine.Serialization;
using XFGameFramework;
using Random = UnityEngine.Random;
// ReSharper disable All

namespace Game
{
    public abstract class SummonBase : CharacterBase
    {
        /// <summary>
        /// 上一轮的Buff
        /// </summary>
        private readonly Dictionary<EffectType, float> _lastTurnBuff = new();
        private float protectiveWardReduction;
        protected SummonDataSo summonData;
        protected Vector2Int gridPos;
        public GameObject Hpbar;
        [FormerlySerializedAs("hp_Value")] public Transform hpValue;
        private HealthBarFill healthBarFill;
        private Tween repositionTween;
        private Vector3 CellCenter => new(gridPos.x + 0.5f, gridPos.y + 0.5f, 0f);
        /// <summary>
        /// 当前召唤物在此轮是否攻击过
        /// </summary>
        private bool isAttacked;
        public bool IsAttacked
        {
            get => isAttacked;
            set
            {
                isAttacked = value;
                if (value)
                    Renderer.SetPropertyBlock(NormalPropertyBlock);
                else
                    Renderer.SetPropertyBlock(OutlinePropertyBlock);
                gameObject.SetActive(false);
                gameObject.SetActive(true);
            }
        }
        public Vector2Int GridPos => gridPos;
        public SummonDataSo SummonData => summonData;
        public int PaidManaCost { get; private set; }
        // 护幕只减免经过原有抗性后剩下的伤害。
        public override float PhysicDamageReduction => Mathf.Lerp(base.PhysicDamageReduction, 1f, protectiveWardReduction);
        public override float MagicDamageReduction => Mathf.Lerp(base.MagicDamageReduction, 1f, protectiveWardReduction);
        private Description _description;
        //高亮显示相关
        private static MaterialPropertyBlock _normalPropertyBlock;
        private static MaterialPropertyBlock _outlinePropertyBlock;
        private static readonly int OutLineWidth = Shader.PropertyToID("_OutlineWidth");
        public Renderer Renderer { get; private set; }
        public static MaterialPropertyBlock NormalPropertyBlock => _normalPropertyBlock;
        public static MaterialPropertyBlock OutlinePropertyBlock => _outlinePropertyBlock;

        private void Awake()
        {
            Renderer = GetComponent<Renderer>();
            if (_normalPropertyBlock == null)
            {
                _normalPropertyBlock = new MaterialPropertyBlock();
                _normalPropertyBlock.SetFloat(OutLineWidth,0);
            }
            if (_outlinePropertyBlock == null)
            {
                _outlinePropertyBlock = new MaterialPropertyBlock();
                _outlinePropertyBlock.SetFloat(OutLineWidth,18);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }
        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            summonData = param[0] as SummonDataSo;
            if (SummonData == null)
            {
                throw new Exception("参数传递为空");
            }

            if (param.Length < 2 || param[1] == null)
            {
                throw new Exception("参数传递为空");
            }

            repositionTween?.Kill();
            gridPos = (Vector2Int)param[1];
            PaidManaCost = param.Length > 2 && param[2] is int paid ? paid : summonData.magicPoint;
            transform.position = CellCenter;
            _maxHp = SummonData.maxHp;
            _currentHp = _maxHp;
            protectiveWardReduction = 0f;
            _attackBuff = _evasionValue =
                _physicFragility = _magicFragility = _physicDamageReduction = _magicDamageReduction = 0;
            Hpbar.SetActive(true);
            healthBarFill = HealthBarFill.Attach(hpValue);
            healthBarFill.SetInstant(1f);
            isAttacked = false;
            if (_description != null)
            {
                _description.Close();
                _description = null;
            }
        }

        internal void MoveToGrid(Vector2Int cell)
        {
            repositionTween?.Kill();
            gridPos = cell;
            repositionTween = transform.DOMove(CellCenter, 0.2f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnKill(() => repositionTween = null);
        }

        public override void OnRemove()
        {
            repositionTween?.Kill();
            base.OnRemove();
        }

        private void OnMouseDown()
        {
            if (Module?.LoadController<FightController>().CanInspectBattle != true)
                return;

            EventManager.TriggerEvent(EventConst.SelectControlSummonToCursor, this);
        }
        private void OnMouseEnter()
        {
            if (_description != null || Module?.LoadController<FightController>().CanInspectBattle != true)
                return;

            _description = Module.LoadPanel<Description>("Description", UIType.UI, null, this);
            if (_description != null)
                _description.onViewRemove += ClearDescription;
        }

        private void OnMouseExit()
        {
            if (_description != null)
                _description.Close();
        }

        private void ClearDescription()
        {
            _description.onViewRemove -= ClearDescription;
            _description = null;
        }
        public abstract void ReleaseSkill(Vector2Int releasePos);

        public int GetDamageOnHit(float damage, AttackType attackType)
        {
            return attackType.GetDamageType() == DamageType.Physical
                ? ApplyDamageModifiers(damage, PhysicDamageReduction, _physicFragility)
                : ApplyDamageModifiers(damage, MagicDamageReduction, _magicFragility);
        }

        private static int ApplyDamageModifiers(float damage, float reduction, float fragility)
        {
            int reducedDamage = (int)((1f - reduction) * damage);
            return (int)((1f + fragility) * reducedDamage);
        }

        public virtual void TakeDamage(float damage, AttackType attackType)
        {
            float dodgeRoll = Random.Range(0, 1f);
            bool isSkull = summonData.occupationType == OccupationType.Skull;
            float dodgeChance = isSkull
                ? Module.LoadController<SummonsController>().SkullAdjoinDodgeGain(gridPos) * (1f + _evasionValue)
                : _evasionValue;
            if (dodgeRoll < dodgeChance)
            {
                if (isSkull)
                    Module.LoadPanel<ToastPanel>("ToastPanel", UIType.UI, null, "召唤物闪避了这次攻击!", 0.75f, true);
                return;
            }

            int damageTaken = GetDamageOnHit(damage, attackType);
            if (protectiveWardReduction > 0f)
            {
                bool physical = attackType.GetDamageType() == DamageType.Physical;
                int withoutWard = ApplyDamageModifiers(damage, physical ? _physicDamageReduction : _magicDamageReduction,
                    physical ? _physicFragility : _magicFragility);
                int prevented = Mathf.Min(_currentHp, withoutWard) - Mathf.Min(_currentHp, damageTaken);
                if (prevented > 0)
                    EventManager.TriggerEvent<SummonBase, int>(EventConst.ProtectiveWardPreventedDamage, this, prevented);
            }
            _currentHp -= damageTaken;
            var obj = Module.LoadView<FloatDamageView>("FloatDamage", null, damageTaken);
            obj.transform.position = transform.GetChild(0).position;
            if (_currentHp < 0)
            {
                _currentHp = 0;
            }
            UpdateHpView();
        }
        
        public void TakeEffect(float value, EffectType effectType)
        {
            switch (effectType)
            {
                case EffectType.ProtectiveWard:
                    protectiveWardReduction = Mathf.Clamp01(value);
                    break;
                case EffectType.PhysicDamageReduction:
                    _physicDamageReduction = 1f - (1f - _physicDamageReduction) * (1f - value);
                    _physicDamageReduction = Mathf.Max(0, _physicDamageReduction);
                    break;
                case EffectType.MagicDamageReduction:
                    _magicDamageReduction = 1f - (1f - _magicDamageReduction) * (1f - value);
                    _magicDamageReduction = Mathf.Max(0, _magicDamageReduction);
                    break;
                case EffectType.PhysicFragility:
                    _physicFragility += value;
                    _physicFragility = Mathf.Max(0, _physicFragility);
                    break;
                case EffectType.MagicFragility:
                    _magicFragility += value;
                    _magicFragility = Mathf.Max(0, _magicFragility);
                    break;
                case EffectType.AttackBuff:
                    _attackBuff += value;
                    _attackBuff = Mathf.Max(0, _attackBuff);
                    break;
                case EffectType.EvasionValue:
                    _evasionValue += value;
                    _evasionValue = Mathf.Max(0, _evasionValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effectType), effectType, null);
            }
        }

        
        
        protected override void OnDisable()
        {
            base.OnDisable();
            if (_description == null) return;
            _description.Close();
            _description = null;
        }

        public virtual void RecordBuff()
        {
            _lastTurnBuff[EffectType.MagicFragility] = _magicFragility;
            _lastTurnBuff[EffectType.PhysicFragility] = _physicFragility;
            _lastTurnBuff[EffectType.AttackBuff] = _attackBuff;
            _lastTurnBuff[EffectType.EvasionValue] = _evasionValue;
            _lastTurnBuff[EffectType.MagicDamageReduction] = _magicDamageReduction;
            _lastTurnBuff[EffectType.PhysicDamageReduction] = _physicDamageReduction;
        }

        protected void UpdateHpView()
        {
            healthBarFill.SetTarget((float)_currentHp / _maxHp);
        }
        public virtual void BuffEffectClear()
        {
            protectiveWardReduction = 0f;
            _magicFragility = _lastTurnBuff[EffectType.MagicFragility];
            _physicFragility = _lastTurnBuff[EffectType.PhysicFragility];
            _attackBuff = _lastTurnBuff[EffectType.AttackBuff];
            _evasionValue = _lastTurnBuff[EffectType.EvasionValue];
            _magicDamageReduction = _lastTurnBuff[EffectType.MagicDamageReduction];
            _physicDamageReduction = _lastTurnBuff[EffectType.PhysicDamageReduction];
        }

        
        public virtual void OnDead()
        {
            Hpbar.SetActive(false);   
            if (_description != null)
            {
                _description.Close();
                _description = null;
            }
        }
    }
}
