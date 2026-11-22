using System;
using System.Collections.Generic;
using Games.MainModule.Configs.AdventurerConfigs;
using Games.MainModule.Scripts.Character;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.UI.Description;
using Games.MainModule.UI.ToastPanel;
using Games.MainModule.View.FloatDamage;
using Games.MainModule.View.HealthBar;
using Games.MainModule.View.VFX;
using UnityEngine;
using UnityEngine.Serialization;
using XFGameFramework;
using Random = UnityEngine.Random;

namespace Games.MainModule.View.Adventurer
{
    public class AdventurerBase : CharacterBase
    {
        public const string MagicGuard = "LongAvoidance";
        public const string PhysicalGuard = "ShortAvoidance";
        private const string SacredWard = "Defence";
        public readonly Dictionary<string, StateInfo> StateBuff = new();
        public AdventurerConfigs adventurerConfigs;
        [Tooltip("冒险家的网格坐标")] public Vector2Int gridPos;
        private StateInfo _value;
        public Transform hpValue;
        public GameObject hpBar;

        private HealthBarFill healthBarFill;
        private Description _description;
        private static MaterialPropertyBlock _normalPropertyBlock;
        private static MaterialPropertyBlock _outlinePropertyBlock;
        private static readonly int OutLineWidth = Shader.PropertyToID("_OutlineWidth");
        public Renderer Renderer { get; private set; }
        public string DisplayName => this switch
        {
            Saber => "战士",
            Rider => "骑士",
            Pastor => "牧师",
            Caster => "法师",
            _ => "勇者"
        };
        public int SacredWardCharges => Mathf.RoundToInt(StateBuff[SacredWard].value);
        public static MaterialPropertyBlock NormalPropertyBlock => _normalPropertyBlock;
        public static MaterialPropertyBlock OutlinePropertyBlock => _outlinePropertyBlock;
        public float AttackMultiplier
        {
            get
            {
                var blessing = StateBuff["AddAttack"];
                return blessing.persistTurn > 0 ? 1f + blessing.value : 1f;
            }
        }

        protected virtual float DamageTakenMultiplier => 1f;
        
        public virtual void Move()
        {
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
        private void Awake()
        {
            StateBuff.Add(SacredWard, new StateInfo());
            StateBuff.Add(MagicGuard, new StateInfo());
            StateBuff.Add(PhysicalGuard, new StateInfo());
            StateBuff.Add("AddAttack", new StateInfo());
            StateBuff.Add("Dodge", new StateInfo());
            Renderer = GetComponent<Renderer>();
            if (_normalPropertyBlock == null)
            {
                _normalPropertyBlock = new MaterialPropertyBlock();
                _normalPropertyBlock.SetFloat(OutLineWidth,0);
            }

            if (_outlinePropertyBlock == null)
            {
                _outlinePropertyBlock = new MaterialPropertyBlock();
                _outlinePropertyBlock.SetFloat(OutLineWidth,12);
            }
        }
        

        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            foreach (var status in StateBuff.Values)
            {
                status.value = 0;
                status.persistTurn = 0;
            }
            hpBar.SetActive(true);
            healthBarFill = HealthBarFill.Attach(hpValue);
            healthBarFill.SetInstant(1f);
            if (_description != null)
            {
                _description.Close();
                _description = null;
            }
        }
        
        public void Init(string Name)
            //进行配置文件的关联
        {
            adventurerConfigs = Resources.Load("AdventureConfigs/" + Name) as AdventurerConfigs;
            AdventurerController.Adventurers.Add(this);
        }

        public void BeHurt(int attack, AttackType attackType)
        {
            float realDamage = attack;
            float dodgeChance = StateBuff["Dodge"].value;
            if (dodgeChance >= 1f || Random.value < dodgeChance)
            {
                //提示
                Module.LoadPanel<ToastPanel>("ToastPanel",UIType.UI,null,"勇者闪避了此次攻击",0.7f);
                return;
            }
            if (SacredWardCharges > 0)
            {
                var ward = StateBuff[SacredWard];
                ward.value--;
                if (ward.value == 0)
                    ward.persistTurn = 0;
                SkirmishEffects.BreakSacredWard(this, SacredWardCharges);
                return;
            }
            string guard = attackType.GetDamageType() == DamageType.Physical ? PhysicalGuard : MagicGuard;
            realDamage *= 1f - StateBuff[guard].value;
            realDamage *= DamageTakenMultiplier;
            //浮动伤害值
            var obj = Module.LoadView<FloatDamageView>("FloatDamage", null, (int)realDamage);
            obj.transform.position = transform.GetChild(0).position;
            adventurerConfigs.currentLife = Mathf.Max(0f, adventurerConfigs.currentLife - realDamage);
            UpdateHpBar();
            if (adventurerConfigs.currentLife <= 0f)
                Die();
        }

        public void Heal(int heal)
        {
            if (adventurerConfigs.currentLife + heal > adventurerConfigs.life)
            {
                adventurerConfigs.currentLife = adventurerConfigs.life;
            }
            else adventurerConfigs.currentLife += heal;
            UpdateHpBar();
        }

        private void Die()
        {
            TileDetails tileDetails = Module.LoadController<MapInfoController>().GetTileDetailsByGridPosition(gridPos);
            tileDetails.IsHaveCharacter = false;
            tileDetails.CharacterType = CharacterType.Monster;
            tileDetails.AboutToSummon = false;
            tileDetails.CanSummon = AdventurerController.Adventurers[0] == this;
            //关闭血条和状态信息
            hpBar.SetActive(false);
            if (_description != null)
            {
                _description.Close();
                _description = null;
            }
            Close();
        }

        public void ApplyBuff(string buff, float value, int turns)
        {
            var state = StateBuff[buff];
            state.persistTurn = turns;
            if (buff == SacredWard)
            {
                state.value = Mathf.Max(0, Mathf.RoundToInt(value));
                SkirmishEffects.ShowSacredWard(this, SacredWardCharges);
                return;
            }

            state.value = value;
        }

        
        private void UpdateHpBar()
        {
            healthBarFill.SetTarget((float)adventurerConfigs.currentLife / adventurerConfigs.life);
        }
    }
    [Serializable]
    public class StateInfo
    {
        [FormerlySerializedAs("Value")] public float value;
        [FormerlySerializedAs("PersistTurn")] public int persistTurn;
    }
}
