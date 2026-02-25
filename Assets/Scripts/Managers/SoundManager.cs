using System.Collections.Generic;
using UnityEngine;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages all game audio (music, sound effects)
    /// Handles volume settings and audio playback
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameMusic;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip prizeCapture;
        [SerializeField] private AudioClip rewardClaim;
        [SerializeField] private AudioClip achievementUnlock;
        [SerializeField] private AudioClip pointsEarned;
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip notificationSound;

        private Dictionary<string, AudioClip> soundEffects = new Dictionary<string, AudioClip>();

        private float musicVolume = 0.7f;
        private float sfxVolume = 1.0f;
        private bool musicEnabled = true;
        private bool sfxEnabled = true;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            RegisterSoundEffects();
            PlayMusic("menu");
        }

        #endregion

        #region Initialization

        private void InitializeAudioSources()
        {
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        private void RegisterSoundEffects()
        {
            if (buttonClick != null) soundEffects["button_click"] = buttonClick;
            if (prizeCapture != null) soundEffects["prize_capture"] = prizeCapture;
            if (rewardClaim != null) soundEffects["reward_claim"] = rewardClaim;
            if (achievementUnlock != null) soundEffects["achievement_unlock"] = achievementUnlock;
            if (pointsEarned != null) soundEffects["points_earned"] = pointsEarned;
            if (errorSound != null) soundEffects["error"] = errorSound;
            if (notificationSound != null) soundEffects["notification"] = notificationSound;
        }

        #endregion

        #region Music

        public void PlayMusic(string musicName)
        {
            if (!musicEnabled || musicSource == null)
                return;

            AudioClip clip = null;

            switch (musicName.ToLower())
            {
                case "menu":
                    clip = menuMusic;
                    break;
                case "game":
                    clip = gameMusic;
                    break;
            }

            if (clip != null && musicSource.clip != clip)
            {
                musicSource.clip = clip;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            if (musicSource != null)
            {
                musicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (musicSource != null && musicEnabled)
            {
                musicSource.UnPause();
            }
        }

        #endregion

        #region Sound Effects

        public void PlaySound(string soundName)
        {
            if (!sfxEnabled || sfxSource == null)
                return;

            if (soundEffects.ContainsKey(soundName))
            {
                AudioClip clip = soundEffects[soundName];
                if (clip != null)
                {
                    sfxSource.PlayOneShot(clip, sfxVolume);
                }
            }
            else
            {
                Debug.LogWarning($"Sound effect not found: {soundName}");
            }
        }

        public void PlaySoundWithPitch(string soundName, float pitch)
        {
            if (!sfxEnabled || sfxSource == null)
                return;

            if (soundEffects.ContainsKey(soundName))
            {
                AudioClip clip = soundEffects[soundName];
                if (clip != null)
                {
                    float originalPitch = sfxSource.pitch;
                    sfxSource.pitch = pitch;
                    sfxSource.PlayOneShot(clip, sfxVolume);
                    sfxSource.pitch = originalPitch;
                }
            }
        }

        #endregion

        #region Predefined Sounds

        public void PlayButtonClick()
        {
            PlaySound("button_click");
        }

        public void PlayPrizeCapture()
        {
            PlaySound("prize_capture");
        }

        public void PlayRewardClaim()
        {
            PlaySound("reward_claim");
        }

        public void PlayAchievementUnlock()
        {
            PlaySound("achievement_unlock");
        }

        public void PlayPointsEarned()
        {
            PlaySound("points_earned");
        }

        public void PlayError()
        {
            PlaySound("error");
        }

        public void PlayNotification()
        {
            PlaySound("notification");
        }

        #endregion

        #region Settings

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
            SaveSettings();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SaveSettings();
        }

        public void SetMusicEnabled(bool enabled)
        {
            musicEnabled = enabled;
            
            if (musicSource != null)
            {
                if (enabled)
                {
                    if (!musicSource.isPlaying)
                    {
                        musicSource.Play();
                    }
                }
                else
                {
                    musicSource.Pause();
                }
            }
            
            SaveSettings();
        }

        public void SetSFXEnabled(bool enabled)
        {
            sfxEnabled = enabled;
            SaveSettings();
        }

        public float GetMusicVolume() => musicVolume;
        public float GetSFXVolume() => sfxVolume;
        public bool IsMusicEnabled() => musicEnabled;
        public bool IsSFXEnabled() => sfxEnabled;

        #endregion

        #region Persistence

        private void LoadSettings()
        {
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
            musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
            sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;

            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetInt("MusicEnabled", musicEnabled ? 1 : 0);
            PlayerPrefs.SetInt("SFXEnabled", sfxEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion
    }
}
