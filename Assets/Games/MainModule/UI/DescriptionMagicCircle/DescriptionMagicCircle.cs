using System;
using Game;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.Adventurer;
using Games.MainModule.View.MagicCircle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XFGameFramework;

namespace Games.MainModule.UI.DescriptionMagicCircle
{
    public class DescriptionMagicCircle : Panel
    {
        public Image bgImage;
        public TextMeshProUGUI line1;
        public TextMeshProUGUI line2;
        private MagicCircle _magicCircle;
        private string _summonName;
        private readonly Vector3[] _tooltipCorners = new Vector3[4];

        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            if (param == null || param.Length == 0)
            {
                throw new Exception("参数传递为空");
            }
            _magicCircle = param[0] as MagicCircle;
            _summonName = param[1] as string;
            if (Canvas != null && Canvas.transform == transform)
                Canvas.sortingOrder = 200;
            //设置位置
            UpdateTooltipPosition();

            //设置文本
            if (_magicCircle != null)
            {
                line1.text = "当前法阵类型：" + _magicCircle.summonMagicType switch
                {
                    SummonMagicType.Shieldguard => "防护召唤术",
                    SummonMagicType.LongRange => "远程召唤术",
                    SummonMagicType.Attacker => "战士召唤术",
                    SummonMagicType.Skull => "骷髅召唤术",
                    SummonMagicType.Wizard => "巫师召唤术",
                    SummonMagicType.Slime => "史莱姆召唤术",
                    SummonMagicType.Random => "大随机术",
                    SummonMagicType.Orientation => "祈愿召唤术",
                    _ => string.Empty
                };
                line2.text = $"即将召唤：{_summonName}";
            }
        }

        private void Update()
        {
            if (_magicCircle == null || Module?.GetModel<FightModel>()?.BattleEnd == true)
            {
                Close();
                return;
            }

            UpdateTooltipPosition();
        }

        private void UpdateTooltipPosition()
        {
            if (_magicCircle == null || Camera.main == null)
                return;

            Games.MainModule.UI.Description.Description.AnchorTooltip(Canvas, bgImage.rectTransform,
                _magicCircle.transform.position + Vector3.up * 0.5f, _tooltipCorners);
        }
    }
}
