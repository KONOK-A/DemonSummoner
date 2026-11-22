using System;
using System.Globalization;
using System.Text;
using Game;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.Adventurer;
using Games.MainModule.View.Summons;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using XFGameFramework;

namespace Games.MainModule.UI.Description
{
    public class Description : Panel
    {
        [SerializeField] private Image bgImage;
        [FormerlySerializedAs("line1")]
        [SerializeField] private TextMeshProUGUI unitName;
        [FormerlySerializedAs("line2")]
        [SerializeField] private TextMeshProUGUI healthValue;
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private GameObject statusSection;
        [SerializeField] private TextMeshProUGUI buffContent;

        private readonly Vector3[] tooltipCorners = new Vector3[4];
        private readonly StringBuilder statusLines = new StringBuilder();
        private SummonBase summon;
        private AdventurerBase adventurer;
        private float nextStatusRefresh;

        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            if (param == null || param.Length == 0)
                throw new ArgumentException("角色详情缺少目标。", nameof(param));

            summon = param[0] as SummonBase;
            adventurer = param[0] as AdventurerBase;
            if (Canvas != null && Canvas.transform == transform)
                Canvas.sortingOrder = 200;

            unitName.text = summon != null ? summon.SummonData.summonName : adventurer?.DisplayName ?? "勇者";
            RefreshStatus();
            Update();
        }

        private void Update()
        {
            if (Module?.GetModel<FightModel>()?.BattleEnd == true)
            {
                Close();
                return;
            }

            if (summon == null && adventurer == null)
                return;

            float maxHealth = summon != null ? summon.MaxHp : adventurer.adventurerConfigs.life;
            float currentHealth = summon != null ? summon.CurrentHp : adventurer.adventurerConfigs.currentLife;
            healthValue.SetText("{0:0} / {1:0}", Mathf.CeilToInt(Mathf.Clamp(currentHealth, 0f, maxHealth)), Mathf.CeilToInt(maxHealth));
            float healthRatio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            var fillAnchor = new Vector2(healthRatio, 1f);
            if (healthFill.anchorMax != fillAnchor)
                healthFill.anchorMax = fillAnchor;

            if (Time.unscaledTime >= nextStatusRefresh)
                RefreshStatus();

            if (Camera.main == null)
                return;

            var healthBar = summon != null ? summon.Hpbar.transform : adventurer.hpBar.transform;
            AnchorTooltip(Canvas, bgImage.rectTransform, healthBar.position, tooltipCorners, besideTarget: true);
        }

        private void RefreshStatus()
        {
            nextStatusRefresh = Time.unscaledTime + 0.1f;
            statusLines.Clear();

            if (summon != null)
                DescribeSummon();
            else if (adventurer != null)
                DescribeAdventurer();

            bool hasStatus = statusLines.Length > 0;
            var description = statusLines.ToString();
            if (statusSection.activeSelf == hasStatus && buffContent.text == description)
                return;

            buffContent.text = description;
            statusSection.SetActive(hasStatus);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bgImage.rectTransform);
        }

        private void DescribeSummon()
        {
            var data = summon.SummonData;
            var controller = Module.LoadController<SummonsController>();
            if (Module.LoadController<FightController>().CanIssuePlayerCommand)
                AddStatus("回收", $"+{controller.GetRecycleMana(summon)} 魔力");
            if (summon is SkullShield shield)
                AddStatus("护卫", shield.GuardReady ? "可替身后一格友军挡击" : "本轮已代挡");
            if (controller.GetReadyGuard(summon.GridPos))
                AddStatus("受护卫", "前方盾兵将承受下次单体直击");
            if (data.occupationType == OccupationType.Skull)
            {
                var dodge = controller.SkullAdjoinDodgeGain(summon.GridPos);
                AddStatus("骷髅连携", $"闪避 +{Percent(dodge)}");
            }

            if (data.occupationType == OccupationType.Wizard && data.summonAttackType == SummonType.LongRange)
            {
                var damage = controller.BlastingMasterAdjoinGain(summon.GridPos);
                AddStatus("爆破连携", $"伤害 +{Percent(damage)}");
            }
            if (summon is FlagBearer)
                AddStatus("固有增伤", $"+{Percent(FlagBearer.BaseAttackBonus)}");
            if (summon.MagicFragility > 0.01f)
                AddStatus("法术易伤", $"受到法术伤害 +{Percent(summon.MagicFragility)}", negative: true);

            var banner = controller.FlagBearerProvidedBuff(summon.GridPos);
            if (banner > 0.1f)
                AddStatus("旗帜光环", $"伤害 +{Percent(banner)}");
            if (summon.PhysicDamageReduction > 0.01f)
                AddStatus("物理防御", $"受到物理伤害 -{Percent(summon.PhysicDamageReduction)}");
            if (summon.MagicDamageReduction > 0.01f)
                AddStatus("法术防御", $"受到法术伤害 -{Percent(summon.MagicDamageReduction)}");
        }

        private void DescribeAdventurer()
        {
            if (adventurer is Caster caster)
            {
                if (caster.ExplosionCenter.HasValue)
                    AddStatus("爆焱咏唱", "下次行动引爆地面焰印", "中心伤害最高");
                AddStatus("护盾范围", $"自身与左右 {caster.adventurerConfigs.CasterWardRadius} 格");
            }

            int losses = AdventurerController.DieTeammateNumber;
            if (losses > 0 && losses < 4)
            {
                string revenge = adventurer switch
                {
                    Caster => $"所有伤害 +{Percent(Mathf.Pow(1.2f, losses) - 1f)}",
                    Pastor => $"向前方 {adventurer.adventurerConfigs.PastorRevengeRange} 格内生命最低的魔物施法 {losses} 次",
                    Rider => $"{Percent(losses * 0.2f)} 概率再次行动",
                    Saber => $"受到伤害 -{Percent(1f - Mathf.Pow(0.8f, losses))}",
                    _ => null
                };
                if (revenge != null)
                    AddStatus("复仇", revenge);
            }

            foreach (var entry in adventurer.StateBuff)
            {
                var buff = entry.Value;
                if (buff.persistTurn <= 0 || buff.value <= 0f)
                    continue;

                string left = $"<color=#AAA3B8>{buff.persistTurn} 回合</color>";
                switch (entry.Key)
                {
                    case "Defence":
                        AddStatus("守护之盾", $"抵挡 {buff.value:0} 次攻击", left);
                        break;
                    case AdventurerBase.MagicGuard:
                        AddStatus("魔法护盾", $"法术减伤 {Percent(buff.value)}", left);
                        break;
                    case AdventurerBase.PhysicalGuard:
                        AddStatus("坚韧", $"物理减伤 {Percent(buff.value)}", left);
                        break;
                    case "AddAttack":
                        AddStatus("祝福", $"攻击伤害 +{Percent(adventurer.AttackMultiplier - 1f)}", left);
                        break;
                    case "Dodge":
                        AddStatus("瞬身", $"闪避 +{Percent(buff.value)}", left);
                        break;
                }
            }
        }

        private void AddStatus(string label, string effect, string duration = null, bool negative = false)
        {
            if (statusLines.Length > 0)
                statusLines.AppendLine();

            statusLines.Append(label).Append("  ")
                .Append(negative ? "<color=#FF9297>" : "<color=#F5D99F>")
                .Append(effect).Append("</color>");
            if (duration != null)
                statusLines.Append("  ").Append(duration);
        }

        private static string Percent(float value)
        {
            return value.ToString("0.##%", CultureInfo.InvariantCulture);
        }

        internal static void AnchorTooltip(Canvas canvas, RectTransform tooltipRect, Vector3 worldPosition, Vector3[] corners, bool besideTarget = false)
        {
            var canvasRect = (RectTransform)canvas.transform;
            var screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPosition, uiCamera, out var position))
                return;

            const float margin = 8f;
            var bounds = canvasRect.rect;
            if (besideTarget)
            {
                const float sideGap = 72f;
                var target = canvasRect.InverseTransformPoint(position);
                float width = canvasRect.InverseTransformVector(tooltipRect.TransformVector(Vector3.right * tooltipRect.rect.width)).x;
                if (target.x + sideGap + width <= bounds.xMax - margin)
                    target.x += sideGap;
                else
                    target.x -= sideGap + width;
                target.y += 47f;
                position = canvasRect.TransformPoint(target);
            }

            tooltipRect.position = position;
            tooltipRect.GetWorldCorners(corners);
            var min = (Vector2)canvasRect.InverseTransformPoint(corners[0]);
            var max = min;
            for (var i = 1; i < corners.Length; i++)
            {
                var point = (Vector2)canvasRect.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            var offset = Vector3.zero;
            if (min.x < bounds.xMin + margin)
                offset.x = bounds.xMin + margin - min.x;
            else if (max.x > bounds.xMax - margin)
                offset.x = bounds.xMax - margin - max.x;
            if (min.y < bounds.yMin + margin)
                offset.y = bounds.yMin + margin - min.y;
            else if (max.y > bounds.yMax - margin)
                offset.y = bounds.yMax - margin - max.y;

            tooltipRect.position += canvasRect.TransformVector(offset);
        }
    }
}
