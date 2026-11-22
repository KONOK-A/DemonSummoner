using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Games.MainModule.Scripts.Startup
{
    [MovedFrom(true, sourceNamespace: "Games.MainModule.Scripts.UI", sourceClassName: "TipPanel")]
    public class StartupErrorPanel : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI Title;
        [SerializeField]
        private TextMeshProUGUI Content;
        [SerializeField]
        private TextMeshProUGUI Btn_Confirm_Text;
        [SerializeField]
        private TextMeshProUGUI Btn_Cancel_Text;
        [SerializeField]
        private Button Btn_Confirm;
        [SerializeField]
        private Button Btn_Cancel;
        public void ShowTips(string Title, string Content, UnityAction onConfirm = null, UnityAction onCancel = null,
            bool isShowCancel = true, string btnConfirmText = "确定", string btnCancelText = "取消")
        {
            gameObject.SetActive(true);
            this.Title.text = Title;
            this.Content.text = Content;
            Btn_Confirm.onClick.RemoveAllListeners();
            Btn_Cancel.onClick.RemoveAllListeners();
            if (onConfirm != null)
            {
                Btn_Confirm.onClick.AddListener(onConfirm);
            }
            if (onCancel != null)
            {
                Btn_Cancel.onClick.AddListener(onCancel);
            }
            Btn_Confirm_Text.text = btnConfirmText;
            Btn_Cancel_Text.text = btnCancelText;

            Btn_Cancel.gameObject.SetActive(isShowCancel);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
