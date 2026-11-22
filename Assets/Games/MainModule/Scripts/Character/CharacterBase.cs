namespace Games.MainModule.Scripts.Character
{
    public abstract class CharacterBase : XFGameFramework.View
    {
        protected int _currentHp;
        protected int _maxHp;
        /// <summary>
        /// 物理减伤
        /// </summary>
        protected float _physicDamageReduction;
        /// <summary>
        /// 法术减伤
        /// </summary>
        protected float _magicDamageReduction;
        /// <summary>
        /// 物理脆弱
        /// </summary>
        protected float _physicFragility;
        /// <summary>
        /// 法术脆弱
        /// </summary>
        protected float _magicFragility;
        /// <summary>
        /// 攻击增益
        /// </summary>
        protected float _attackBuff;
        /// <summary>
        /// 闪避
        /// </summary>
        protected float _evasionValue;
        public int CurrentHp => _currentHp;

        public int MaxHp => _maxHp;

        public virtual float PhysicDamageReduction => _physicDamageReduction;

        public virtual float MagicDamageReduction => _magicDamageReduction;

        public float PhysicFragility => _physicFragility;

        public float MagicFragility => _magicFragility;

        public float AttackBuff => _attackBuff;

        public float EvasionValue => _evasionValue;
    }
}
