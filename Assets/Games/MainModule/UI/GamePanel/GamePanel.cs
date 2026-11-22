using System.Collections.Generic;
using System.Linq;
using Game;
using Games.MainModule.Configs.Summons;
using Games.MainModule.Scripts.Command;
using Games.MainModule.Scripts.Const;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Model;
using Games.MainModule.Scripts.Utility;
using Games.MainModule.View.Adventurer;
using Games.MainModule.View.Summons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XFGameFramework;

namespace Games.MainModule.UI.GamePanel
{
    public class GamePanel : Panel
    {
        public TextMeshProUGUI TurnLable;
        [SerializeField] private Image turnTitle;
        [SerializeField] private Sprite playerTurnTitle;
        [SerializeField] private Sprite enemyTurnTitle;
        [SerializeField] private RectTransform commandBar;
        [SerializeField] private TextMeshProUGUI commandHint;
        [SerializeField] private TextMeshProUGUI spellManaCost;
        [SerializeField] private SpellPreparationView preparationView;
        [SerializeField] private SummonDataSo[] manualSummons;
        [SerializeField] private TextMeshProUGUI recycleAmount;
        [SerializeField] private GameObject recycleLettering;
        [SerializeField] private TextMeshProUGUI battleHint;

        public Slider ManaSlider;
        public TextMeshProUGUI ManaCountText;
        public TextMeshProUGUI TurnCountText;
        public Button MagicBookClose;
        public GameObject MagicBookOpen;
        public Button MonsterManualClose;
        public GameObject MonsterManualOpen;
        public Button BtnPlayerTurnEnd;
        public Button BtnDelete;
        [SerializeField] private Button repositionButton;
        public TextMeshProUGUI roundRevertMagic;
        public List<GameObject> MagicCircles;
        public List<GameObject> Monsters;

        private int MagicCircleIndex;
        private int MonsterIndex;
        private OperationType _operationType;
        private bool _isPlayerTurn;
        private SummonBase _selectedSummon;
        private AdventurerBase _focusTarget;
        private PausePanel.PausePanel _pausePanel;
        private int _nextIncomeTurn;
        private int _refundHealth;
        private bool _hoveringRecycle;
        private SummonDataSo _linkPageUnit;
        private readonly List<float> _shownLinks = new(4);
        private readonly List<float> _liveLinks = new(4);
        private float _nextLinkRefresh;
        private static bool _seenGuard;
        private float _guardHintUntil;
        private Caster _warningCaster;
        private const string DefaultBattleHint = "点勇者集火，再点同一目标取消";
        private const string GuardHint = "护卫代挡：盾兵替紧靠身后的友军挡下了这一击。";
        private const string BlastHint = "<color=#FFD0B5>爆焱已锁格</color>，法师下次行动引爆，可调位或开护幕";
        public bool IsReadingMonsterManual => MonsterManualOpen.activeSelf;
        private SummonerController SummonerController => Module.LoadController<SummonerController>();
        private bool ControlsSummon => _operationType is OperationType.SummonControl or OperationType.Reposition;
        private bool IsResolvingPlayerSkill => Module.GetModel<FightModel>()?.IsResolvingPlayerSkill == true;

        protected override void Awake()
        {
            base.Awake();
            RegisterEvent(EventConst.PlayerTurnBegin, OnPlayerTurnBegin);
            RegisterEvent(EventConst.PlayerTurnEnd, OnPlayerTurnEnd);
            RegisterEvent(EventConst.EnemyTurnBegin, OnEnemyTurnBegin);
            RegisterEvent(EventConst.MagicBookClose, CloseMagicBook);
            RegisterEvent(EventConst.CancelBattleOperation, OnMonsterManualClose);
            RegisterEvent<int>(EventConst.ManaValueChange, _ =>
            {
                UpdateMana();
                RefreshControls();
            });
            RegisterEvent(EventConst.SummonPreparationChanged, OnPreparationChanged);
            RegisterEvent<SummonBase, SummonBase>(EventConst.GuardIntercepted, (_, _) => ShowFirstGuardHint());
            RegisterEvent(EventConst.BlastWarningRaised, TrackBlastWarning);
            RegisterEvent<AdventurerBase>(EventConst.FocusTargetChanged, target =>
            {
                _focusTarget = target;
                RefreshControls();
            });
            RegisterEvent<SummonBase>(EventConst.SelectControlSummonToGame, summon =>
            {
                if (!_isPlayerTurn || _operationType == OperationType.ReleaseMagic)
                    return;

                _operationType = OperationType.SummonControl;
                _selectedSummon = summon;
                MonsterManualOpen.SetActive(false);
                RefreshControls();
            });
            RegisterEvent(EventConst.ControlSummonAttackEnd, () =>
            {
                if (!ControlsSummon)
                    return;

                _operationType = OperationType.None;
                _selectedSummon = null;
                RefreshControls();
            });
        }

        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            _nextIncomeTurn = 1;
            SetRoundCount(1);
            MagicCircleIndex = 0;
            MonsterIndex = 0;
            ShowSpellPage();
            ShowPage(Monsters, MonsterIndex);
            ShowManualEntry();
            TrackBlastWarning();
            OnPlayerTurnEnd();
            UpdateMana();
        }

        private void UpdateMana()
        {
            var summoner = SummonerController;
            ManaSlider.value = summoner.CurrentMana / (float)summoner.MaxMana;
            ManaCountText.text = $"{summoner.CurrentMana}/{summoner.MaxMana}";
            UpdateIncome();
        }

        private void UpdateIncome()
        {
            if (!roundRevertMagic || _nextIncomeTurn < 1) return;
            var summoner = SummonerController;
            int income = summoner.GetIncomeForPlayerTurn(_nextIncomeTurn);
            int receivable = Mathf.Min(income, summoner.MaxMana - summoner.CurrentMana);
            roundRevertMagic.text = _nextIncomeTurn == 1 ? $"开局魔力 <color=#B9F0E5>{summoner.CurrentMana}</color>" :
                receivable < income ? $"第 {_nextIncomeTurn} 回合 +{income}　可收 <color=#B9F0E5>+{receivable}</color>" :
                $"第 {_nextIncomeTurn} 回合魔力 <color=#B9F0E5>+{income}</color>";
        }

        private void ShowFirstGuardHint()
        {
            if (_seenGuard || !isActiveAndEnabled || !battleHint) return;
            _seenGuard = true;
            _guardHintUntil = Time.time + 6f;
            RefreshBattleHint();
        }

        private void TrackBlastWarning()
        {
            _warningCaster = AdventurerController.Adventurers.Find(member => member is Caster { ExplosionCenter: not null }) as Caster;
            RefreshBattleHint();
        }

        private void RefreshBattleHint()
        {
            if (!battleHint) return;
            bool showGuard = Time.time < _guardHintUntil;
            string hint = showGuard ? GuardHint : DefaultBattleHint;
            if (_warningCaster && _warningCaster.ExplosionCenter.HasValue)
                hint = showGuard ? BlastHint + "\n" + GuardHint : BlastHint;
            if (battleHint.text != hint) battleHint.text = hint;
        }

        public void OnBtnPauseClick()
        {
            if (Time.timeScale <= 0f) return;
            if (_pausePanel == null || !_pausePanel.isActiveAndEnabled)
            {
                EventManager.TriggerEvent(EventConst.ClearFocusTarget);
                _pausePanel = Module.LoadPanel<PausePanel.PausePanel>();
            }
        }

        protected override void OnDisable()
        {
            var pausePanel = _pausePanel;
            _pausePanel = null;
            if (pausePanel != null)
                pausePanel.Close();
            _guardHintUntil = 0f;
            _warningCaster = null;
            base.OnDisable();
        }

        private void OnEnemyTurnBegin()
        {
            int round = Module.LoadController<FightController>().GetFightModel().BoutCount;
            SetRoundCount(round);
            if (_isPlayerTurn)
                OnPlayerTurnEnd();
            _nextIncomeTurn = round;
            UpdateIncome();
        }

        private void OnPlayerTurnBegin()
        {
            _isPlayerTurn = true;
            _nextIncomeTurn = Module.GetModel<FightModel>().BoutCount + 1;
            UpdateIncome();
            ShowSpellPage();
            ShowTurnTitle(true);
            RefreshControls();
        }

        private void SetRoundCount(int round)
        {
            TurnCountText.text = commandBar == null ? round.ToString() : $"第 {round} 回合";
        }

        private void LateUpdate()
        {
            RefreshBattleHint();
            if (_selectedSummon && _selectedSummon.CurrentHp != _refundHealth)
                RefreshControls();

            if (!_linkPageUnit || !MonsterManualOpen.activeSelf || Time.unscaledTime < _nextLinkRefresh)
                return;
            _nextLinkRefresh = Time.unscaledTime + 0.1f;
            ReadLiveLinks(_linkPageUnit, _liveLinks);
            if (!_shownLinks.SequenceEqual(_liveLinks))
                ShowManualEntry();
        }

        private void OnPlayerTurnEnd()
        {
            _isPlayerTurn = false;
            _operationType = OperationType.None;
            _selectedSummon = null;
            _hoveringRecycle = false;
            _focusTarget = null;
            MagicBookOpen.SetActive(false);
            MonsterManualOpen.SetActive(false);
            ShowTurnTitle(false);
            RefreshControls();
        }

        private void RefreshControls()
        {
            bool placing = MagicBookOpen.activeSelf;
            bool reading = MonsterManualOpen.activeSelf;
            MagicBookClose.gameObject.SetActive(!placing);
            MonsterManualClose.gameObject.SetActive(!reading);
            bool canOpenBook = _isPlayerTurn && !ControlsSummon;
            MagicBookClose.interactable = canOpenBook;
            SetActionAvailable(BtnPlayerTurnEnd, _isPlayerTurn, 0.62f);
            bool canRecycle = _isPlayerTurn && ControlsSummon && _selectedSummon && !IsResolvingPlayerSkill;
            SetActionAvailable(BtnDelete, canRecycle, 0.50f, blockWhenDisabled: _focusTarget != null);
            _refundHealth = _selectedSummon ? _selectedSummon.CurrentHp : 0;
            int refund = canRecycle ? Module.LoadController<SummonsController>().GetRecycleMana(_selectedSummon) : 0;
            if (recycleAmount)
            {
                recycleLettering.SetActive(!canRecycle);
                recycleAmount.gameObject.SetActive(canRecycle);
                if (canRecycle) recycleAmount.SetText("回收 +{0}", refund);
            }
            if (repositionButton != null)
            {
                bool show = _isPlayerTurn && ControlsSummon && !placing && !reading;
                repositionButton.gameObject.SetActive(show);
                if (show)
                    SetActionAvailable(repositionButton, Module.LoadController<SummonsController>().HasRepositionTarget(_selectedSummon),
                        0.50f, blockWhenDisabled: true);
            }
            var summoner = SummonerController;
            var page = (SummonMagicType)MagicCircleIndex;
            bool canPrepare = placing && canOpenBook && summoner.CanPrepareSummon(page);
            if (preparationView != null)
                preparationView.Refresh(placing, page, summoner.PendingSummon, summoner.PendingChoices,
                    summoner.HasPendingContract && page == summoner.PendingMagicType, summoner.PendingManaCost, canPrepare, canOpenBook);
            if (commandHint == null) return;

            if (_hoveringRecycle && canRecycle)
                commandHint.SetText("回收可得\n<color=#B9F0E5>+{0} 魔力</color>", refund);
            else if (ControlsSummon && _selectedSummon)
            {
                string hint = _operationType == OperationType.Reposition ? "点邻格调位" : _selectedSummon switch
                {
                    { IsAttacked: true } => "本回合已行动",
                    ProtectiveMage => "点地格施放护幕",
                    BlastingMaster => "点地格施放爆破",
                    _ => "点勇者攻击"
                };
                commandHint.text = $"<color=#F6EDF9>{_selectedSummon.SummonData.summonName}</color> <size=88%>· {hint}\n点其他魔物即可指挥</size>";
            }
            else if (_focusTarget && !placing && !reading)
                commandHint.text = $"<color=#F6EDF9>集火 · {_focusTarget.DisplayName}</color>\n<size=88%>点魔物出手</size>";
            else if (placing && !reading && !summoner.HasPendingContract && !canPrepare && _isPlayerTurn && Time.timeScale > 0f)
                commandHint.text = summoner.HasAvailableSummonCell ? "魔力不足" : "没有可布阵的空格";
            else
                commandHint.text = string.Empty;
        }

        private void OnPreparationChanged()
        {
            if (SummonerController.HasPendingContract)
                MagicCircleIndex = (int)SummonerController.PendingMagicType;
            ShowSpellPage();
            RefreshControls();
        }

        public void OnPrepareSpellClick()
        {
            if (!_isPlayerTurn || _operationType != OperationType.ReleaseMagic || !MagicBookOpen.activeSelf || Time.timeScale <= 0f)
                return;
            if (SummonerController.HasPendingContract)
                OnPreparationChanged();
            else
                SummonerController.TryPrepareSummon((SummonMagicType)MagicCircleIndex);
        }

        public void OnChoosePreparedSummon(int pageIndex)
        {
            if (_isPlayerTurn && _operationType == OperationType.ReleaseMagic && Time.timeScale > 0f &&
                preparationView.GetDisplayedChoice(pageIndex) is { } choice)
                SummonerController.TryChoosePreparedSummon(choice);
        }

        public void OnRecyclePointerEnter()
        {
            _hoveringRecycle = true;
            RefreshControls();
        }

        public void OnRecyclePointerExit()
        {
            _hoveringRecycle = false;
            RefreshControls();
        }

        private static void SetActionAvailable(Button action, bool available, float disabledAlpha, bool blockWhenDisabled = false)
        {
            action.interactable = available;
            if (action.TryGetComponent<CanvasGroup>(out var group))
            {
                group.alpha = available ? 1f : disabledAlpha;
                group.blocksRaycasts = available || blockWhenDisabled;
            }
        }

        private void ShowTurnTitle(bool isPlayerTurn)
        {
            if (turnTitle != null)
            {
                turnTitle.sprite = isPlayerTurn ? playerTurnTitle : enemyTurnTitle;
            }
            else if (TurnLable != null)
            {
                // Older battle HUD bundles still have a TMP label.
                TurnLable.text = isPlayerTurn ? "玩家回合" : "敌方回合";
                TurnLable.color = isPlayerTurn ? Color.white : Color.red;
            }
        }

        public void OnBtnPlayerTurnEndClick()
        {
            if (_isPlayerTurn && Time.timeScale != 0)
                new PlayerTurnEndCommand(Module).Execute();
        }

        public void OnMagicBookClose()
        {
            EventManager.TriggerEvent(EventConst.MagicBookClose);
        }

        private void CloseMagicBook()
        {
            if (_operationType != OperationType.ReleaseMagic)
                return;

            _operationType = OperationType.None;
            MagicBookOpen.SetActive(false);
            RefreshControls();
        }

        public void OnMagiCBookOpen()
        {
            if (!_isPlayerTurn || ControlsSummon || IsResolvingPlayerSkill || Time.timeScale <= 0f)
                return;

            if (_operationType == OperationType.ReleaseMagic)
            {
                OnMagicBookClose();
                return;
            }

            EventManager.TriggerEvent(EventConst.ClearFocusTarget);
            _operationType = OperationType.ReleaseMagic;
            if (SummonerController.HasPendingContract)
                MagicCircleIndex = (int)SummonerController.PendingMagicType;
            MagicBookOpen.SetActive(true);
            ShowSpellPage();
            RefreshControls();
            EventManager.TriggerEvent(EventConst.MagicBookOpen, MagicCircleIndex);
        }

        public void OnBtnDeleteClick()
        {
            EventManager.TriggerEvent(EventConst.DeleteSummon);
        }

        public void OnBtnRepositionClick()
        {
            if (!_isPlayerTurn || !ControlsSummon || IsResolvingPlayerSkill ||
                !Module.LoadController<SummonsController>().HasRepositionTarget(_selectedSummon))
                return;

            bool targeting = _operationType == OperationType.SummonControl;
            _operationType = targeting ? OperationType.Reposition : OperationType.SummonControl;
            EventManager.TriggerEvent(EventConst.RepositionTargetingChanged, targeting);
            RefreshControls();
        }

        public void OnMonsterManualClose()
        {
            if (!MonsterManualOpen.activeSelf)
                return;

            MonsterManualOpen.SetActive(false);
            RefreshControls();
        }

        public void OnMonsterManualOpen()
        {
            if (IsResolvingPlayerSkill)
                return;

            if (MonsterManualOpen.activeSelf)
            {
                OnMonsterManualClose();
                return;
            }

            if (ControlsSummon)
                EventManager.TriggerEvent(EventConst.CancelBattleOperation);
            EventManager.TriggerEvent(EventConst.ClearFocusTarget);
            MonsterManualOpen.SetActive(true);
            ShowManualEntry();
            RefreshControls();
        }

        public void OnBtnNextClick()
        {
            ChangeSpellPage(1);
        }

        public void OnBtnLastClick()
        {
            ChangeSpellPage(-1);
        }

        private void ChangeSpellPage(int offset)
        {
            MagicCircleIndex = (MagicCircleIndex + offset + MagicCircles.Count) % MagicCircles.Count;
            ShowSpellPage();
            RefreshControls();
        }

        private void ShowManualEntry()
        {
            var unit = manualSummons[MonsterIndex];
            var description = Monsters[MonsterIndex].transform.Find("描述").GetComponent<TextMeshProUGUI>();
            string mainStat = unit.summonName == "防护法师" ? $"护幕 {unit.skillValue * 100f:0.#}%" : $"攻击 {unit.skillValue:0.#}";
            string stats = $"血量 <color=#572B6B>{unit.maxHp}</color><pos=145>{mainStat}\n" +
                $"范围 <color=#572B6B>{unit.attackRadius}</color> 格<pos=145>标价 <color=#572B6B>{unit.magicPoint}</color>\n<size=8> </size>\n";
            string skill = unit.summonName switch
            {
                "骷髅盾兵" => $"物理减伤 {SkullShield.BasePhysicalReduction * 100f:0.#}%。每回合替身后一格友军承受首次单体直击。",
                "骷髅武士" => $"贴身连斩两次，每刀 {unit.CloseStrikeDamage:0.#}。隔格单斩 {unit.skillValue:0.#}。",
                "骷髅射手" => "物理单箭可点射后排。",
                "爆破法师" => $"可点勇者或空格，三格内须有勇者。中心 {unit.skillValue:0.#}，两侧各 {unit.BlastSplashDamage:0.#}。法术减伤 {BlastingMaster.BaseMagicReduction * 100f:0.#}%。",
                "防护法师" => "给中心与两侧的友军护幕，减少物理与法术伤害，直到下个玩家回合。重复施放取一份。",
                "持旗手" => $"自身增伤 {FlagBearer.BaseAttackBonus * 100f:0.#}%。\n旗帜为 {SummonsController.FlagAuraRange} 格内友军增伤 {SummonsController.FlagAuraBonus * 100f:0.#}%，包括自身。多旗取一份光环。",
                "大史莱姆" => "战死后原格分裂成小史莱姆，再变成小小史莱姆。主动回收会结束分裂。",
                "小史莱姆" => "战死后原格分裂成小小史莱姆。主动回收会结束分裂。",
                "小小史莱姆" => "贴身撞击，可挡路或打掉一次神圣盾。随机显形或分裂可得。",
                "酸液史莱姆" => "酸弹造成法术伤害。接触命中并扣血后，对前方两格内的勇者各反击 60 法伤。",
                "魔铠甲" => "物理减伤 60%，法术易伤 30%。护幕按剩余伤害减免。",
                _ => string.Empty
            };
            bool skull = unit.occupationType == OccupationType.Skull;
            _linkPageUnit = skull || unit.summonName == "爆破法师" ? unit : null;
            if (_linkPageUnit)
            {
                ReadLiveLinks(unit, _shownLinks);
                skill += DescribeLink(unit, _shownLinks);
            }
            description.text = $"{stats}<size=23>{skill}</size>";
        }

        private void ReadLiveLinks(SummonDataSo unit, List<float> gains)
        {
            var summons = Module.LoadController<SummonsController>();
            bool skull = unit.occupationType == OccupationType.Skull;
            gains.Clear();
            foreach (var ally in summons.GetAllSummonsOnScene().Values)
            {
                if (!ally || ally.SummonData.summonName != unit.summonName) continue;
                float gain = skull ? summons.SkullAdjoinDodgeGain(ally.GridPos) : summons.BlastingMasterAdjoinGain(ally.GridPos);
                if (!gains.Contains(gain)) gains.Add(gain);
            }
            gains.Sort();
        }

        private static float LinkGain(bool skull, int neighbors) => skull ?
            SummonsController.GetSkullLinkDodge(neighbors) : SummonsController.GetBlastLinkBonus(neighbors);

        private static string DescribeLink(SummonDataSo unit, List<float> gains)
        {
            bool skull = unit.occupationType == OccupationType.Skull;
            bool archer = skull && unit.summonAttackType == SummonType.LongRange;
            string effect = skull ? "闪避" : "增伤";
            string neighbors = archer ? " 盾兵/武士 0/1/2只" : "  邻格 0/1/2 只";
            string text = $"\n<color=#733952>连携{effect}</color>{neighbors}\n{LinkPercent(LinkGain(skull, 0))} / {LinkPercent(LinkGain(skull, 1))} / {LinkPercent(LinkGain(skull, 2))}";
            if (archer)
                text += $"\n邻格射手只取一份 {LinkPercent(unit.ArcherPairDodge)}。\n两类连携取高值。";
            if (gains.Count > 0)
                text += "\n场上 " + string.Join(" / ", gains.Select(LinkPercent).Distinct());
            return text;
        }

        private static string LinkPercent(float gain) => $"{gain * 100f:0.##}%";

        private void RefreshSpellOdds(SummonMagicType page)
        {
            var description = MagicCircles[MagicCircleIndex].transform.Find("描述").GetComponent<TextMeshProUGUI>();
            if (page == SummonMagicType.Orientation)
            {
                description.text = "直接选定一只魔物\n可按职责与范围点名\n确认付费，取消免费";
                return;
            }
            var bank = Module.LoadController<SummonBankController>();
            var weights = bank.GetSummonWeights(page);
            if (page == SummonMagicType.Random)
            {
                var pool = Module.GetModel<SummonBankModel>().SummonDataSoList;
                string next = SummonerController.HasRandomChoiceThisTurn ? "本回合首张，两只选一" : "本回合下张显形一只";
                description.text = $"全 {weights.Count} 种等权，各 1/{weights.Count}\n{next}\n前排、后排或支援\n范围 {pool.Min(unit => unit.attackRadius)} 至 {pool.Max(unit => unit.attackRadius)} 格";
                return;
            }
            int total = weights.Sum(entry => entry.Weight);
            description.text = string.Join("\n", weights.Select(entry =>
            {
                var unit = entry.Name == null ? null : bank.GetSummonDataByName(entry.Name);
                string role = unit ? $"{SpellPreparationView.RoleOf(unit)} {unit.attackRadius}格" : "其余均分";
                return $"{entry.Name ?? "其他"}<pos=128><color=#733952>{role}</color><pos=227><color=#255550>{100f * entry.Weight / total:0.#}%</color>";
            }));
        }

        private void ShowSpellPage()
        {
            ShowPage(MagicCircles, MagicCircleIndex);
            var magicType = (SummonMagicType)MagicCircleIndex;
            RefreshSpellOdds(magicType);
            if (spellManaCost == null) return;
            if (magicType == SummonMagicType.Orientation)
                spellManaCost.text = "按所选魔物付费";
            else
                spellManaCost.SetText("魔力消耗：<color=#562579>{0}</color>", SummonerController.GetManaCost(magicType));
        }

        public void OnBtnManualNextClick()
        {
            MonsterIndex = (MonsterIndex + 1) % Monsters.Count;
            ShowPage(Monsters, MonsterIndex);
            ShowManualEntry();
        }

        public void OnBtnManualLastClick()
        {
            MonsterIndex = (MonsterIndex - 1 + Monsters.Count) % Monsters.Count;
            ShowPage(Monsters, MonsterIndex);
            ShowManualEntry();
        }

        private static void ShowPage(List<GameObject> pages, int current)
        {
            for (int i = 0; i < pages.Count; i++)
                pages[i].SetActive(i == current);
        }
    }
}
