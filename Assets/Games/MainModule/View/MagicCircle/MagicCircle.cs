using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.UI.Description;
using Games.MainModule.UI.DescriptionMagicCircle;
using UnityEngine;
using UnityEngine.Serialization;
using XFGameFramework;

namespace Games.MainModule.View.MagicCircle
{
    [Serializable]
    public struct MagicCircleImage
    {
        public Sprite sprite;
        public SummonMagicType summonMagicType;
    }
    public class MagicCircle : XFGameFramework.View
    {
        public List<MagicCircleImage> magicCircleImages = new();
        public SpriteRenderer spriteRenderer;
        [HideInInspector]public SummonMagicType summonMagicType;
        private DescriptionMagicCircle _description;
        private Vector2Int _gridPos;
        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            summonMagicType = (SummonMagicType)param[0];
            _gridPos = (Vector2Int)param[1];
            var sprite = magicCircleImages.First(image => image.summonMagicType == summonMagicType).sprite;
            spriteRenderer.sprite = sprite;
        }
        private void OnMouseEnter()
        {
            if (_description != null || Module?.LoadController<FightController>().CanInspectBattle != true)
                return;

            var summonData = Module.LoadController<MapInfoController>().GetTileDetailsByGridPosition(_gridPos)?.SummonData;
            if (summonData == null)
                return;

            _description = Module.LoadPanel<DescriptionMagicCircle>("DescriptionMagicCircle", UIType.UI, null,
                this, summonData.summonName);
            if (_description != null)
                _description.onViewRemove += OnDescriptionRemoved;
        }

        private void OnMouseExit() => CloseDescription();

        protected override void OnDisable()
        {
            CloseDescription();
            base.OnDisable();
        }

        private void CloseDescription()
        {
            var panel = _description;
            OnDescriptionRemoved();
            if (panel != null)
                panel.Close();
        }

        private void OnDescriptionRemoved()
        {
            if (_description != null)
                _description.onViewRemove -= OnDescriptionRemoved;
            _description = null;
        }
    }
    
}
