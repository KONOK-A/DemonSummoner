using UnityEngine;
using XFABManager;
using XFGameFramework;
using AudioType = Games.MainModule.Scripts.Utility.AudioType;

namespace Games.MainModule.Scripts.Controller
{
    public class AudioController : XFGameFramework.Controller
    {
        public override void OnInit()
        {
            base.OnInit();
            AudioPlayer.RegisterAudioController(AudioType.BGM.ToString(), true);
            AudioPlayer.RegisterAudioController(AudioType.Sound.ToString(), false);
            
        }

        public void PlayBGM(string asset_name)
        {
            AudioSource audioSource = AudioPlayer.GetAuidoController(AudioType.BGM.ToString()).AudioSource;
            
            if (audioSource.clip != null && audioSource.clip.name == asset_name)
            {
                return;
            }

            AudioClip audioClip = AssetBundleManager.LoadAsset<AudioClip>(Module.ProjectName, asset_name);
            if (audioClip == null) return;
            //最后一个参数设置为true，可以让音效只播放一次，且可同时播放多个
            AudioPlayer.Play(AudioType.BGM.ToString(), audioClip, false);
        }

        public void PlaySound(string asset_name)
        {
            AudioClip audioClip = AssetBundleManager.LoadAsset<AudioClip>(Module.ProjectName, asset_name);
            if (audioClip == null) return;
            AudioPlayer.Play(AudioType.Sound.ToString(), audioClip, true);
        }

        public void SetBGMVolume(float amount)
        {
            XFGameFramework.AudioController music = AudioPlayer.GetAuidoController(AudioType.BGM.ToString());
            music.Volume = amount;
        }

        public void SetSFXVolume(float amount)
        {
            XFGameFramework.AudioController music = AudioPlayer.GetAuidoController(AudioType.Sound.ToString());
            music.Volume = amount;
        }

        public float GetBGMVolume()
        {
            XFGameFramework.AudioController music = AudioPlayer.GetAuidoController(AudioType.BGM.ToString());
            return music.Volume;
        }

        public float GetSFXVolume()
        {
            XFGameFramework.AudioController music = AudioPlayer.GetAuidoController(AudioType.Sound.ToString());
            return music.Volume;
        }
        public void SetMute(AudioType audioType, bool mute)
        {
            //这里设置的音效开关是持久化的
            AudioPlayer.GetAuidoController(audioType.ToString()).Mute = mute;
        }

        public bool GetMute(AudioType audioType)
        {
            return AudioPlayer.GetAuidoController(audioType.ToString()).Mute;
        }
    }
}
