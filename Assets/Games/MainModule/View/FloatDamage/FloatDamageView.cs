using TMPro;
using XFGameFramework;

namespace Games.MainModule.View.FloatDamage
{
    public class FloatDamageView : XFGameFramework.View
    {
        public TextMeshPro valueText;
        public override void OnLoaded(params object[] param)
        {
            base.OnLoaded(param);
            int value = (int)param[0];
            valueText.text = value.ToString();
            TimerManager.DelayInvoke(Close,1f);
        }
    }
}
