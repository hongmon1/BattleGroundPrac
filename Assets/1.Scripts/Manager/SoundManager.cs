using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Audio;
using System;

/// <summary>
/// audio mixer 조정
/// 볼륨 초기화, 저장, 불러오기 등
/// fade in,out 등
/// </summary>
public class SoundManager : SingletonMonobehaviour<SoundManager>
{
    public const string MasterGroupName = "Master";
    public const string EffectGroupName = "Effect";
    public const string BGMGroupName = "BGM";
    public const string UIGroupName = "UI";
    public const string MixerName = "AudioMixer";
    public const string ContainerName = "SoundContainer"; //오디오 리스너와 오디오 소스 갖고 있음.
    //보통 카메라에 오디오 리스너 달려있음. 한 씬에는 하나의 오디오 리스너 존재
    public const string FadeA = "FadeA";
    public const string FadeB = "FadeB";
    public const string UI = "UI";
    public const string EffectVolumeParam = "Volume_Effect";
    public const string BGMVolumeParam = "Volume_BGM";
    public const string UIVolumeParam = "Volume_UI";

    public enum MusicPlayingType
    {
        None = 0,
        SourceA = 1,
        SourceB = 2,
        AtoB = 3,
        BtoA = 4
    }

    public AudioMixer mixer = null;
    public Transform audioRoot = null;
    public AudioSource fadeA_audio = null;
    public AudioSource fadeB_audio = null;
    public AudioSource[] effect_audios = null;//동시에 너무 많은 이펙스 사운드 재생시 음질안좋음 -> 채널 개수 저장
    public AudioSource UI_audio = null;

    public float[] effect_PlayStartTime = null; //채널 재생하다가 개수 다 차면 오래된 사운드 끄고 다시 재생
    private int EffectChannelCount = 5;
    private MusicPlayingType currentPlayingType = MusicPlayingType.None;
    private bool isTicking = false; 
    private SoundClip currentSound = null; //현재 재생되고 있는 사운드
    private SoundClip lastSound = null;// 마지막에 재생한 사운드
    private float minVolume = -80.0f;
    private float maxVolume = 0.0f;



    //초기화 작업(믹서 등) 
    //볼륨 세팅 (마지막 유저 세팅 or 옵션값 세팅)
    void Start()
    {
        if(this.mixer == null)
        {
            this.mixer = Resources.Load(MixerName) as AudioMixer;
        }

        //최상위객체
        if (this.audioRoot == null)
        {
            audioRoot = new GameObject(ContainerName).transform;
            audioRoot.SetParent(transform);
            audioRoot.localPosition = Vector3.zero;
        }

        if(fadeA_audio == null)
        {
            GameObject fadeA = new GameObject(FadeA, typeof(AudioSource)); //audiosource component 붙어서 생성됨
            fadeA.transform.SetParent(audioRoot);
            this.fadeA_audio = fadeA.GetComponent<AudioSource>();
            this.fadeA_audio.playOnAwake = false; //자동재생 끔
        }

        if (fadeB_audio == null)
        {
            GameObject fadeB = new GameObject(FadeB, typeof(AudioSource)); //audiosource component 붙어서 생성됨
            fadeB.transform.SetParent(audioRoot);
            this.fadeB_audio = fadeB.GetComponent<AudioSource>();
            this.fadeB_audio.playOnAwake = false; //자동재생 끔
        }

        if (UI_audio == null)
        {
            GameObject ui = new GameObject(UI, typeof(AudioSource));
            ui.transform.SetParent(audioRoot);
            UI_audio = ui.GetComponent<AudioSource>();
            UI_audio.playOnAwake = false;
        }
        if(this.effect_audios==null || this.effect_audios.Length == 0)
        {
            //채널개수만큼 만든다
            this.effect_PlayStartTime = new float[EffectChannelCount];
            this.effect_audios = new AudioSource[EffectChannelCount];
            for(int i = 0; i < EffectChannelCount; i++)
            {
                effect_PlayStartTime[i] = 0.0f;
                GameObject effect = new GameObject("Effect" + i.ToString(), typeof(AudioSource));
                effect.transform.SetParent(audioRoot);
                this.effect_audios[i] = effect.GetComponent<AudioSource>();
                this.effect_audios[i].playOnAwake = false;
            }
        }

        //믹서가 있으면
        if (this.mixer != null)
        {
            //이거 설정 해야 볼륨 조절이 먹음(스크립트에서 조절 가능해짐)
            this.fadeA_audio.outputAudioMixerGroup = mixer.FindMatchingGroups(BGMGroupName)[0]; //그룹이름을 넣은 것 중 첫번째
            this.fadeB_audio.outputAudioMixerGroup = mixer.FindMatchingGroups(BGMGroupName)[0];
            this.UI_audio.outputAudioMixerGroup = mixer.FindMatchingGroups(UIGroupName)[0];
            for(int i = 0; i < this.effect_audios.Length; i++)
            {
                this.effect_audios[i].outputAudioMixerGroup = mixer.FindMatchingGroups(EffectGroupName)[0];
            }
        }

        VolumeInit();
    }


    //볼륨 조절
    #region volume adjust
    public void SetBGMVolume(float currentRatio)
    {
        currentRatio = Mathf.Clamp01(currentRatio); //0,1 외에는 나올 수 없음
        float volume = Mathf.Lerp(minVolume, maxVolume, currentRatio); //볼륨이 슬라이더라 비율로 정한다
        this.mixer.SetFloat(BGMVolumeParam, volume);
        PlayerPrefs.SetFloat(BGMVolumeParam, volume);
    }

    public float GetBGMVolume()
    {
        if (PlayerPrefs.HasKey(BGMVolumeParam))
        {
            return Mathf.Lerp(minVolume, maxVolume, PlayerPrefs.GetFloat(BGMVolumeParam));
        }
        else
        {
            return maxVolume;
        }
    }

    public void SetEffectVolume(float currentRatio)
    {
        currentRatio = Mathf.Clamp01(currentRatio); //0,1 외에는 나올 수 없음
        float volume = Mathf.Lerp(minVolume, maxVolume, currentRatio); //볼륨이 슬라이더라 비율로 정한다
        this.mixer.SetFloat(EffectVolumeParam, volume);
        PlayerPrefs.SetFloat(EffectVolumeParam, volume);
    }

    public float GetEffectVolume()
    {
        if (PlayerPrefs.HasKey(EffectVolumeParam))
        {
            return Mathf.Lerp(minVolume, maxVolume, PlayerPrefs.GetFloat(EffectVolumeParam));
        }
        else
        {
            return maxVolume;
        }
    }

    public void SetUIVolume(float currentRatio)
    {
        currentRatio = Mathf.Clamp01(currentRatio); //0,1 외에는 나올 수 없음
        float volume = Mathf.Lerp(minVolume, maxVolume, currentRatio); //볼륨 설정이 슬라이더라 비율로 정한다
        this.mixer.SetFloat(UIVolumeParam, volume);
        PlayerPrefs.SetFloat(UIVolumeParam, volume);
    }

    public float GetUIVolume()
    {
        if (PlayerPrefs.HasKey(UIVolumeParam))
        {
            return Mathf.Lerp(minVolume, maxVolume, PlayerPrefs.GetFloat(UIVolumeParam));
        }
        else
        {
            return maxVolume;
        }
    }

    //볼륨 초기화
    void VolumeInit()
    {
        if(this.mixer != null)
        {
            this.mixer.SetFloat(BGMVolumeParam, GetBGMVolume());
            this.mixer.SetFloat(EffectVolumeParam, GetEffectVolume());
            this.mixer.SetFloat(UIVolumeParam, GetUIVolume());
        }
    }
    #endregion

    //오디오 플레이하는 가장 기본적인 함수
    void PlayAudioSource(AudioSource source, SoundClip clip, float volume)
    {
        if (source == null || clip == null)
        {
            return;
        }

        source.Stop();
        source.clip = clip.GetClip();
        source.volume = volume;
        source.loop = clip.isLoop;
        source.pitch = clip.pitch;
        source.dopplerLevel = clip.dopplerLevel;
        source.rolloffMode = clip.rolloffMode;
        source.minDistance = clip.minDistance;
        source.maxDistance = clip.maxDistance;
        source.spatialBlend = clip.spartialBlend;
        source.Play();
    }


    //특정 포인트에서 오디오소스 재생
    void PlayAudioSourceAtPoint(SoundClip clip, Vector3 position, float volume)
    {
        AudioSource.PlayClipAtPoint(clip.GetClip(), position, volume);
    }

    //재생 중인지
    public bool isPlaying()
    {
        return (int)this.currentPlayingType > 0;
        //playtype이 none이 아니면 재생 중임
    }

    //재생하는 사운드가 다른 사운드인가
    public bool IsDifferentSound(SoundClip clip)
    {
        if(clip == null){
            return false;
        }
        if(currentSound!=null && currentSound.realId == clip.realId && isPlaying() && currentSound.isFadeOut == false)
        {
            //동일한 사운드
            return false;
        }
        else
        {
            //다른 사운드
            return true;
        }
    }

    //BGM 재생 위한 프로세스 함수
    private IEnumerator CheckProcess()
    {
        //매 프레임마다 체크는 비효율적 -> IEnumerator 사용
        //루프 체크
        while(this.isTicking==true && isPlaying() == true)
        {
            yield return new WaitForSeconds(0.05f);
            //현재 사운드에서 루프를 갖고 있고
            if (this.currentSound.HasLoop())
            {
                
                if(currentPlayingType == MusicPlayingType.SourceA)
                {
                    currentSound.CheckLoop(fadeA_audio);
                }
                else if(currentPlayingType == MusicPlayingType.SourceB)
                {
                    currentSound.CheckLoop(fadeB_audio);
                }
                else if(currentPlayingType == MusicPlayingType.AtoB)
                {
                    this.lastSound.CheckLoop(this.fadeA_audio);
                    this.currentSound.CheckLoop(this.fadeB_audio);
                }
                else if (currentPlayingType == MusicPlayingType.BtoA)
                {
                    this.lastSound.CheckLoop(this.fadeB_audio);
                    this.currentSound.CheckLoop(this.fadeA_audio);
                } 
            }
        }
    }

    public void DoCheck()
    {
        StartCoroutine(CheckProcess());
    }

    public void FadeIn(SoundClip clip, float time, Interpolate.EaseType ease)
    {
        if (this.IsDifferentSound(clip))
        {
            this.fadeA_audio.Stop();
            this.fadeB_audio.Stop();
            this.lastSound = this.currentSound;
            this.currentSound = clip;
            PlayAudioSource(fadeA_audio, currentSound, 0.0f);
            this.currentSound.FadeIn(time, ease);
            this.currentPlayingType = MusicPlayingType.SourceA;
            if (this.currentSound.HasLoop() == true)
            {
                this.isTicking = true;
                DoCheck();
            }
        }
    }

    //무슨 사운드 클립인지 모를떄
    //인덱스 넣어주면 가져와줌
    public void FadeIn(int index, float time, Interpolate.EaseType ease)
    {
        this.FadeIn(DataManager.SoundData().GetCopy(index), time, ease);
    }
    
    public void FadeOut(float time, Interpolate.EaseType ease)
    {
        if(this.currentSound != null)
        {
            this.currentSound.FadeOut(time, ease);
        }
    }

    void Update()
    {
        //볼륨 조절
        if(currentSound == null)
        {
            return;
        }
        if(currentPlayingType == MusicPlayingType.SourceA)
        {
            currentSound.DoFade(Time.deltaTime, fadeA_audio);
        }
        else if(currentPlayingType == MusicPlayingType.SourceB)
        {
            currentSound.DoFade(Time.deltaTime, fadeB_audio);
        }
        else if (currentPlayingType == MusicPlayingType.AtoB)
        {
            this.lastSound.DoFade(Time.deltaTime, fadeA_audio);
            this.currentSound.DoFade(Time.deltaTime, fadeB_audio);
        }
        else if (currentPlayingType == MusicPlayingType.BtoA)
        {
            this.lastSound.DoFade(Time.deltaTime, fadeB_audio);
            this.currentSound.DoFade(Time.deltaTime, fadeA_audio);
        }

        
        if(fadeA_audio.isPlaying && this.fadeB_audio.isPlaying == false)
        {
            this.currentPlayingType = MusicPlayingType.SourceA;
        }
        else if(fadeB_audio.isPlaying && this.fadeA_audio.isPlaying == false)
        {
            this.currentPlayingType = MusicPlayingType.SourceB;
        }
        else if (fadeB_audio.isPlaying == false && this.fadeA_audio.isPlaying == false)
        {
            this.currentPlayingType = MusicPlayingType.None;
        }
    }

    public void FadeTo(SoundClip clip, float time, Interpolate.EaseType ease)
    {
        if(currentPlayingType == MusicPlayingType.None)
        {
            FadeIn(clip, time, ease);
        }
        else if (this.IsDifferentSound(clip))
        {
            if(this.currentPlayingType == MusicPlayingType.AtoB)
            {
                this.fadeA_audio.Stop();
                this.currentPlayingType = MusicPlayingType.SourceB;
            }
            else if(this.currentPlayingType == MusicPlayingType.BtoA)
            {
                this.fadeB_audio.Stop();
                this.currentPlayingType = MusicPlayingType.SourceA;
            }
            lastSound = currentSound;
            currentSound = clip;
            this.lastSound.FadeOut(time, ease);
            this.currentSound.FadeIn(time, ease);

            if(currentPlayingType == MusicPlayingType.SourceA)
            {
                PlayAudioSource(fadeB_audio, currentSound, 0.0f);
                currentPlayingType = MusicPlayingType.AtoB;
            }
            else if(currentPlayingType == MusicPlayingType.SourceB)
            {
                PlayAudioSource(fadeA_audio, currentSound, 0.0f);
                currentPlayingType = MusicPlayingType.BtoA;
            }

            if (currentSound.HasLoop())
            {
                this.isTicking = true;
                DoCheck();
            }
        }
    }

    public void FadeTo(int index, float time, Interpolate.EaseType ease)
    {
        this.FadeTo(DataManager.SoundData().GetCopy(index), time, ease);
    }

    public void PlayBGM(SoundClip clip)
    {
        if (this.IsDifferentSound(clip))
        {
            this.fadeB_audio.Stop();
            this.lastSound = this.currentSound;
            this.currentSound = clip;
            PlayAudioSource(fadeA_audio, clip, clip.maxVolume);
            if (currentSound.HasLoop())
            {
                this.isTicking = true;
                DoCheck();
            }
        
        }
    }

    public void PlayBGM(int index)
    {
        SoundClip clip = DataManager.SoundData().GetCopy(index);
        PlayBGM(clip);
    }

    public void PlayUISound(SoundClip clip)
    {
        PlayAudioSource(UI_audio, clip, clip.maxVolume);
    }

    public void PlayEffectSound(SoundClip clip)
    {
        bool isPlaySuccess = false;

        //채널 수 초과하지 않게 재생
        for (int i = 0; i < this.EffectChannelCount; i++)
        {
            if (this.effect_audios[i].isPlaying == false)
            {
                PlayAudioSource(this.effect_audios[i], clip, clip.maxVolume);
                this.effect_PlayStartTime[i] = Time.realtimeSinceStartup;
                isPlaySuccess = true;
                break;
            }
            else if(this.effect_audios[i].clip == clip.GetClip())
            {
                this.effect_audios[i].Stop();
                PlayAudioSource(effect_audios[i], clip, clip.maxVolume);
                this.effect_PlayStartTime[i] = Time.realtimeSinceStartup;
                isPlaySuccess = true;
                break;
            }
        }
        if (isPlaySuccess == false)
        {
            float maxTime = 0.0f;
            int selectionIndex = 0;
            for(int i = 0; i < EffectChannelCount; i++)
            {
                if (this.effect_PlayStartTime[i] > maxTime)
                {
                    maxTime = this.effect_PlayStartTime[i];
                    selectionIndex = i;
                }
            }
            PlayAudioSource(this.effect_audios[selectionIndex], clip, clip.maxVolume);
        }
    }

    //원하는 위치에 원하는 볼륨으로
    public void PlayEffectSound(SoundClip clip, Vector2 position, float volume)
    {
        bool isPlaySuccess = false;
        for (int i = 0; i < this.EffectChannelCount; i++)
        {
            if (this.effect_audios[i].isPlaying == false)
            {
                PlayAudioSourceAtPoint(clip,position,volume);
                this.effect_PlayStartTime[i] = Time.realtimeSinceStartup;
                isPlaySuccess = true;
                break;
            }
            else if (this.effect_audios[i].clip == clip.GetClip())
            {
                this.effect_audios[i].Stop();
                PlayAudioSourceAtPoint(clip, position, volume);
                this.effect_PlayStartTime[i] = Time.realtimeSinceStartup;
                isPlaySuccess = true;
                break;
            }
        }
        if (isPlaySuccess == false)
        {
           
            PlayAudioSourceAtPoint(clip, position, volume);
        }
    }

    public void PlayOneShotEffect(int index, Vector3 position, float volume)
    {
        //none 재생 불가
        if (index == (int)SoundList.None)
        {
            return;
        }

        SoundClip clip = DataManager.SoundData().GetCopy(index);
        if(clip == null)
        {
            return;
        }

        PlayEffectSound(clip, position, volume);
    }

    public void PlayOneShot(SoundClip clip)
    {
        if(clip == null)
        {
            return;
        }

        switch (clip.playType)
        {
            case SoundPlayType.EFFECT:
                PlayEffectSound(clip);
                break;
            case SoundPlayType.BGM:
                PlayBGM(clip);
                break;
            case SoundPlayType.UI:
                PlayUISound(clip);
                break;
        }
    }

    public void Stop(bool allStop = false)
    {
        if (allStop)
        {
            this.fadeA_audio.Stop();
            this.fadeB_audio.Stop();
        }

        this.FadeOut(0.5f, Interpolate.EaseType.Linear);
        this.currentPlayingType = MusicPlayingType.None;
        StopAllCoroutines();
    }

    //적이 총 쏠때 적 아이디로 어떤 총인지 알아내서 PlayOneShotEffect 재생
    /// <summary>
    /// enemy 클래스에 따라 사격 사운드를 다르게
    /// </summary>
    public void PlayShotSound(string ClassID, Vector3 position, float volume)
    {
        SoundList sound = (SoundList)Enum.Parse(typeof(SoundList), ClassID.ToLower());
        PlayOneShotEffect((int)sound, position, volume);
    }
}
