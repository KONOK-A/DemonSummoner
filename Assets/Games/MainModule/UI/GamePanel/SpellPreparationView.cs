using System;
using System.Collections.Generic;
using Games.MainModule.Configs.Summons;
using Games.MainModule.Scripts.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Games.MainModule.UI.GamePanel
{
    public sealed class SpellPreparationView : MonoBehaviour, ICanvasRaycastFilter
    {
        [Serializable]
        private struct Illustration
        {
            [Tooltip("与魔物配置的 prefabName 同名的原画。")]
            public Sprite artwork;
            [Tooltip("全身在原画内的归一化取景范围，以左下角为原点。")]
            public Rect figureRegion;
            [Tooltip("书签头像在同一张原画内的归一化取景范围。")]
            public Rect bookmarkRegion;
        }

        [Serializable]
        private class ChoicePage
        {
            public Button button;
            public Image figure;
            public TextMeshProUGUI name;
            public TextMeshProUGUI role;
            public SummonDataSo Shown { get; set; }
        }

        [SerializeField] private GameObject[] browsingElements;
        [SerializeField] private Image[] pageSeals;
        [SerializeField] private Button sealButton;
        [SerializeField] private Button prepareButton;
        [SerializeField] private TextMeshProUGUI prepareLabel;
        [SerializeField] private CanvasGroup resultPage;
        [SerializeField] private TextMeshProUGUI summonName;
        [SerializeField] private TextMeshProUGUI paidCaption;
        [SerializeField] private TextMeshProUGUI summonStats;
        [SerializeField] private Image figure;
        [SerializeField] private Button bookmark;
        [SerializeField] private Image bookmarkPortrait;
        [SerializeField] private TextMeshProUGUI bookmarkLabel;
        [SerializeField] private GameObject doublePageMark;
        [SerializeField] private GameObject choiceSpread;
        [SerializeField] private ChoicePage[] choicePages;
        [SerializeField] private TextMeshProUGUI choiceHint;
        [SerializeField] private Illustration[] illustrations;

        private SummonDataSo shownSummon;

        public SummonDataSo GetDisplayedChoice(int index) =>
            choiceSpread.activeInHierarchy && index >= 0 && index < choicePages.Length ? choicePages[index].Shown : null;

        public void Refresh(bool bookOpen, SummonMagicType page, SummonDataSo pending, IReadOnlyList<SummonDataSo> choices,
            bool contractPage, int paidMana, bool canPrepare, bool canOpen)
        {
            bool choosing = choices.Count > 0;
            bool hasContract = choosing || pending;
            foreach (var element in browsingElements)
                element.SetActive(!contractPage);

            resultPage.gameObject.SetActive(contractPage && pending);
            choiceSpread.SetActive(contractPage && choosing);
            bookmark.gameObject.SetActive(hasContract && !bookOpen);
            bookmark.interactable = canOpen;
            bookmarkLabel.text = choosing ? "双选" : "布阵";
            doublePageMark.SetActive(choosing);
            bookmarkPortrait.gameObject.SetActive(!choosing);

            if (!contractPage)
            {
                sealButton.targetGraphic = pageSeals[(int)page];
                bool available = hasContract ? canOpen : canPrepare;
                sealButton.interactable = prepareButton.interactable = available;
                prepareLabel.text = hasContract ? "回书签" : page == SummonMagicType.Orientation ? "祈愿" : "显形";
                prepareLabel.alpha = available ? 1f : 0.55f;
            }

            if (choosing)
            {
                choiceHint.SetText("已付 {0} 魔力", paidMana);
                for (int i = 0; i < choicePages.Length; i++)
                {
                    var slot = choicePages[i];
                    var candidate = choices[i];
                    slot.button.interactable = canOpen;
                    if (slot.Shown == candidate) continue;
                    slot.Shown = candidate;
                    slot.name.text = candidate.summonName;
                    slot.role.text = $"{RoleOf(candidate)} · 射程 {candidate.attackRadius} 格";
                    ShowIllustration(slot.figure, candidate, false);
                }
            }
            else if (pending)
            {
                paidCaption.SetText("魔力已付\n<size=82%>{0} 魔力</size>", paidMana);
                summonStats.SetText("生命 {0}    范围 {1} 格", pending.maxHp, pending.attackRadius);
                if (pending != shownSummon)
                {
                    summonName.text = pending.summonName;
                    ShowIllustration(figure, pending, false);
                    ShowIllustration(bookmarkPortrait, pending, true);
                    resultPage.alpha = bookOpen && isActiveAndEnabled ? 0.25f : 1f;
                }
            }

            shownSummon = pending;
        }

        internal static string RoleOf(SummonDataSo data) => data.summonName switch
        {
            "骷髅盾兵" => "护卫",
            "骷髅武士" => "连斩",
            "骷髅射手" => "远射",
            "爆破法师" => "爆破",
            "防护法师" => "护幕",
            "持旗手" => "增伤",
            "魔铠甲" => "铁甲",
            "酸液史莱姆" => "反击",
            "大史莱姆" or "小史莱姆" => "分裂",
            _ => "挡路"
        };

        private void ShowIllustration(Image image, SummonDataSo summon, bool portrait)
        {
            foreach (var entry in illustrations)
            {
                if (!entry.artwork || entry.artwork.name != summon.prefabName)
                    continue;

                FrameIllustration(image, entry.artwork, portrait ? entry.bookmarkRegion : entry.figureRegion);
                return;
            }

            image.enabled = false;
            Debug.LogWarning($"法术书缺少魔物立绘：{summon.prefabName}", this);
        }

        private static void FrameIllustration(Image image, Sprite artwork, Rect region)
        {
            var frame = (RectTransform)image.rectTransform.parent;
            var source = artwork.rect.size;
            if (region.width <= 0f || region.height <= 0f) region = new Rect(0, 0, 1, 1);
            float scale = Mathf.Min(frame.rect.width / (source.x * region.width),
                frame.rect.height / (source.y * region.height));
            var size = source * scale;
            image.sprite = artwork;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = Vector2.Scale(Vector2.one * 0.5f - region.center, size);
            image.enabled = true;
        }

        // Keep the corners of the sigil's rectangle available for closing the book.
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            var seal = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(seal, screenPoint, eventCamera, out var point))
                return false;

            var rect = seal.rect;
            var offset = point - rect.center;
            return new Vector2(offset.x / rect.width, offset.y / rect.height).sqrMagnitude <= 0.25f;
        }

        private void Update()
        {
            if (resultPage.alpha < 1f)
                resultPage.alpha = Mathf.Min(1f, resultPage.alpha + Time.deltaTime * 4.5f);
        }

        private void OnDisable()
        {
            resultPage.alpha = 1f;
        }
    }
}
