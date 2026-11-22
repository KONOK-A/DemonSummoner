using System;
using System.Collections.Generic;
using Games.MainModule.Configs.Summons;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.UI.GamePanel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XFGameFramework;

namespace Games.MainModule.UI.SpecificCallPanel
{
    [Serializable]
    public struct SelectSummonData
    {
        public Button Button;
        public SummonDataSo SummonData;
        public TextMeshProUGUI Price;
    }
    public class SpecificCallPanel : Panel
    {
        private static readonly Color SelectedTint = new(200 / 255f, 200 / 255f, 200 / 255f, 0.5f);
        private static readonly Color UnaffordableTint = new(0.45f, 0.45f, 0.45f, 0.4f);

        public Action<SummonDataSo> CallBackEvent;
        public List<SelectSummonData> SelectSummonDataList = new();
        public Button btnSubmit;
        [SerializeField] private TextMeshProUGUI selectionTitle;
        [SerializeField] private TextMeshProUGUI selectedCost;
        private SummonDataSo _summonData;
        private float _previousTimeScale;

        protected override void Awake()
        {
            base.Awake();
            RegisterEvent<int>(EventConst.ManaValueChange, RefreshChoices);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            Time.timeScale = _previousTimeScale;
            var callback = CallBackEvent;
            CallBackEvent = null;
            _summonData = null;
            callback?.Invoke(null);
        }
        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            CallBackEvent = null;
            _summonData = null;
            foreach (var selectSummon in SelectSummonDataList)
            {
                selectSummon.Button.onClick.RemoveAllListeners();
                selectSummon.Button.onClick.AddListener(() => OnSummonClicked(selectSummon));
                selectSummon.Price.SetText("{0} 魔力", selectSummon.SummonData.magicPoint);
            }
            RefreshChoices(Module.LoadController<SummonerController>().CurrentMana);
        }

        private void OnSummonClicked(SelectSummonData option)
        {
            if (!option.Button.interactable)
                return;

            _summonData = option.SummonData;
            RefreshChoices(Module.LoadController<SummonerController>().CurrentMana);
        }

        private void RefreshChoices(int mana)
        {
            if (_summonData != null && _summonData.magicPoint > mana)
                _summonData = null;

            btnSubmit.interactable = _summonData != null;
            selectionTitle.text = _summonData == null ? "祈愿召唤" :
                $"{_summonData.summonName}　<size=68%>{SpellPreparationView.RoleOf(_summonData)} · {_summonData.attackRadius} 格</size>";
            selectedCost.text = _summonData == null ? "选择魔物，确认时付费" : $"魔力消耗：{_summonData.magicPoint}";
            foreach (var option in SelectSummonDataList)
            {
                bool affordable = option.SummonData != null && option.SummonData.magicPoint <= mana;
                bool selected = affordable && option.SummonData == _summonData;
                option.Button.interactable = affordable && !selected;
                var icon = option.Button.transform.GetChild(0).GetComponent<Image>();
                icon.color = affordable ? (selected ? SelectedTint : Color.white) : UnaffordableTint;
            }
        }
        public void OnBtnSubmitClicked()
        {
            if (_summonData == null)
                return;

            var choice = _summonData;
            var callback = CallBackEvent;
            CallBackEvent = null;
            Close();
            callback?.Invoke(choice);
        }
        public void OnBtnCancelClicked()
        {
            Close();
        }
    }
}
