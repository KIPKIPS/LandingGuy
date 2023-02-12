﻿// author:KIPKIPS
// date:2023.02.08 21:47
// describe:音效系统

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Framework.Pool;
using Framework.Singleton;

namespace Framework.Manager {
    public class AudioManager : Singleton<AudioManager> {
        public void Launch() {
            //TODO:读取用户配置文件
            Mute = false;
            GlobalVolume = 1;
            EffectVolume = 1;
            BgmVolume = 1;
            Utils.Log(_logTag,"the audio initialization settings are complete");
        }
        private AudioSource _bgmAudioSource;
        private readonly string _logTag = "AudioManager";
        //特效音乐列表
        private readonly List<AudioSource> effectAudioList = new();
        //特效音乐对象池
        private PrefabPool<AudioSource> _audioSourcePool;
        private PrefabPool<AudioSource> AudioSourcePool => _audioSourcePool ??= new PrefabPool<AudioSource>();
        
        private Transform _audioRoot;
        private Transform AudioRoot {
            get {
                if (_audioRoot == null) {
                    _audioRoot= new GameObject().transform;
                    var trs = _audioRoot;
                    trs.UDontDestroyOnLoad();
                    trs.position = Vector3.zero;
                    trs.localPosition = Vector3.zero;
                    trs.localRotation = Quaternion.identity;
                    trs.localScale = Vector3.one;
                    trs.name = "AudioRoot";
                }
                return _audioRoot;
            }
        }
        public AudioSource BgmAudioSource {
            get {
                if (_bgmAudioSource is null) {
                    var trs = new GameObject().transform;
                    trs.SetParent(AudioRoot);
                    trs.localPosition = Vector3.zero;
                    trs.localRotation = Quaternion.identity;
                    trs.localScale = Vector3.one;
                    trs.name = DEF.AudioType.BGM.ToString();
                    _bgmAudioSource = trs.UAddComponent<AudioSource>() as AudioSource;
                    _bgmAudioSource.loop = true;
                    _bgmAudioSource.playOnAwake = false;
                }
                return _bgmAudioSource;
            }
        }

        #region 音量,播放控制
        //全局音量
        private float _globalVolume = 1f;
        public float GlobalVolume {
            get => _globalVolume;
            set {
                _globalVolume = value;
                UpdateGlobalVolume();
            }
        }
        
        //bgm音量
        private float _bgmVolume;
        public float BgmVolume {
            get => _bgmVolume;
            set {
                _bgmVolume = value;
                UpdateBgmVolume();
            }
        }
        
        //特效音量
        private float _effectVolume = 1;
        public float EffectVolume {
            get => _effectVolume;
            set {
                _effectVolume = value;
                UpdateEffectVolume();
            }
        }
        
        //是否静音
        private bool _mute;
        public bool Mute {
            get => _mute;
            set {
                _mute = value;
                SetMute();
            }   
        }
        
        private bool _pause;
        public bool Pause {
            get => _pause;
            set {
                _pause = value;
                SetPause();
            }   
        }

        public void SetPause() {
            
        }

        /// <summary>
        /// 更新全局音量
        /// </summary>
        void UpdateGlobalVolume() {
            UpdateBgmVolume();
            UpdateEffectVolume();
        }
        /// <summary>
        /// 更新背景音乐音量
        /// </summary>
        void UpdateBgmVolume() {
            BgmAudioSource.volume = BgmVolume * GlobalVolume;
        }
        /// <summary>
        /// 更新特效音乐音量
        /// </summary>
        void UpdateEffectVolume() {
            for (int i = effectAudioList.Count - 1; i >= 0; i--) {
                if (effectAudioList[i] != null) {
                    SetEffectAudioPlay(effectAudioList[i]);
                }
            }
        }

        public void SetEffectAudioPlay(AudioSource audioSource,float spatial = -1) {
            audioSource.mute = Mute;
            audioSource.volume = EffectVolume * GlobalVolume;
            if (spatial != -1) {
                audioSource.spatialBlend = spatial;
            }
            if (Pause) {
                audioSource.Pause();
            }
        }
        
        private void RecycleAudioPlay(AudioSource audioSource) {
            AudioSourcePool.Recycle(audioSource);
            effectAudioList.Remove(audioSource);
        }
        
        private AudioSource GetAudioPlay(bool is3d,Vector3 position) {
            AudioSource audioSource = AudioSourcePool.Allocate();
            var t = audioSource.transform;
            t.SetParent(AudioRoot);
            t.position = position;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            if (!t.name.StartsWith($"{DEF.AudioType.EFFECT}")) {
                t.name = $"{DEF.AudioType.EFFECT}_{effectAudioList.Count}";
            }
            audioSource.spatialBlend = is3d ? 1 : 0;
            effectAudioList.Add(audioSource);
            return audioSource;
        }

        /// <summary>
        /// 播放特效音乐
        /// </summary>
        /// <param name="clip">音频资源</param>
        /// <param name="position">播放的位置</param>
        /// <param name="volumeScale">音量调节</param>
        /// <param name="is3d">是否3D音效</param>
        /// <param name="callback">播放完回调</param>
        /// <param name="callbackDelaySecond">回调延时</param>
        public void PlayAudio(AudioClip clip,Vector3 position,float volumeScale = 1,bool is3d = true,UnityAction callback = null,float callbackDelaySecond = 0) {
            AudioSource audioSource = GetAudioPlay(is3d,position);
            audioSource.mute = Mute;
            audioSource.PlayOneShot(clip,volumeScale);
            var clipMillisecond = clip != null ? clip.length * 1000 : 0;
            Timer.New(e => {
                callback?.Invoke();
                RecycleAudioPlay(audioSource);
                e.Destroy();
            }, (int)(callbackDelaySecond * 1000 + clipMillisecond)).Start();
        }

        /// <summary>
        /// 设置静音
        /// </summary>
        public void SetMute() {
            UpdateEffectVolume();
            UpdateBgmVolume();
        }
        #endregion
    }

}