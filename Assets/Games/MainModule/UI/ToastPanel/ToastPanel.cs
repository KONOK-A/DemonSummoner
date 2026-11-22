using System;
using TMPro;
using UnityEngine;
using XFGameFramework;

namespace Games.MainModule.UI.ToastPanel
{
    public class ToastPanel : Panel
    {
        [SerializeField]
        private TextMeshProUGUI content;
        private RectTransform _background;
        private Vector2 _defaultPosition;

        protected override void Awake()
        {
            base.Awake();
            _background = (RectTransform)content.rectTransform.parent;
            _defaultPosition = _background.anchoredPosition;
        }

        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            if (param == null || param.Length == 0)
            {
                throw new Exception("初始化参数为空！");
            }
            content.text = param[0].ToString();
            var time = 1f;
            if (param.Length > 1)
            {
                time = (float)param[1];
            }
            TimerManager.DelayInvoke(Close,time);
            bool isSummon = param.Length > 2 && (bool)param[2];
            _background.anchoredPosition = _defaultPosition + (isSummon ? Vector2.up * 100f : Vector2.zero);
        }
    }
}
