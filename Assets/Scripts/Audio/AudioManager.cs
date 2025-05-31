using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

namespace FPS.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource uiSource;
        
        [Header("Pooled Audio Sources")]
        [SerializeField] private int sfxSourcesPoolSize = 20;
        [SerializeField] private Transform sfxSourcesParent;
        
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        
        // Volume settings
        private float masterVolume = 1f;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;
        private float ambienceVolume = 1f;
        private float uiVolume = 1f;

        // Constants for mixer parameters
        private const string MIXER_MASTER = "MasterVolume";
        private const string MIXER_MUSIC = "MusicVolume";
        private const string MIXER_SFX = "SFXVolume";
        private const string MIXER_AMBIENCE = "AmbienceVolume";
        private const string MIXER_UI = "UIVolume";

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioPool();
                LoadAudioSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudioPool()
        {
            if (sfxSourcesParent == null)
            {
                GameObject parent = new GameObject("SFX_Pool");
                parent.transform.SetParent(transform);
                sfxSourcesParent = parent.transform;
            }

            for (int i = 0; i < sfxSourcesPoolSize; i++)
            {
                GameObject sfxObj = new GameObject($"SFX_Source_{i}");
                sfxObj.transform.SetParent(sfxSourcesParent);
                
                AudioSource source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];
                
                sfxPool.Add(source);
            }
        }

        private void LoadAudioSettings()
        {
            // Load from PlayerPrefs
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            ambienceVolume = PlayerPrefs.GetFloat("AmbienceVolume", 1f);
            uiVolume = PlayerPrefs.GetFloat("UIVolume", 1f);

            // Apply to mixer
            ApplyVolumesToMixer();
        }

        private void ApplyVolumesToMixer()
        {
            // Convert linear volume to logarithmic for the mixer (using decibels)
            audioMixer.SetFloat(MIXER_MASTER, ConvertToDecibels(masterVolume));
            audioMixer.SetFloat(MIXER_MUSIC, ConvertToDecibels(musicVolume));
            audioMixer.SetFloat(MIXER_SFX, ConvertToDecibels(sfxVolume));
            audioMixer.SetFloat(MIXER_AMBIENCE, ConvertToDecibels(ambienceVolume));
            audioMixer.SetFloat(MIXER_UI, ConvertToDecibels(uiVolume));
        }

        private float ConvertToDecibels(float linearVolume)
        {
            // -80dB is essentially silence, so we use that as our minimum
            return linearVolume > 0.001f ? 20f * Mathf.Log10(linearVolume) : -80f;
        }

        // Get an available audio source from pool
        private AudioSource GetAvailableAudioSource()
        {
            foreach (AudioSource source in sfxPool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }
            
            // If all sources are in use, reuse the oldest one
            AudioSource oldestSource = sfxPool[0];
            return oldestSource;
        }

        #region Public Methods

        // Play a one-shot sound effect at a specified position
        public AudioSource PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f, float minDistance = 1f, float maxDistance = 50f)
        {
            if (clip == null) return null;
            
            AudioSource source = GetAvailableAudioSource();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = spatialBlend; // 0 = 2D, 1 = 3D
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.transform.position = position;
            source.Play();
            
            return source;
        }
        
        // Play sound from audio source (attached to object)
        public void PlaySound(AudioSource source, AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || source == null) return;
            
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }
        
        // Play UI sound
        public void PlayUISound(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            
            uiSource.PlayOneShot(clip, volume);
        }
        
        // Play music track with optional crossfade
        public void PlayMusic(AudioClip musicClip, bool loop = true, float fadeTime = 1.0f)
        {
            if (musicClip == null) return;
            
            StartCoroutine(CrossfadeMusic(musicClip, loop, fadeTime));
        }
        
        // Play ambience track with optional crossfade
        public void PlayAmbience(AudioClip ambienceClip, bool loop = true, float fadeTime = 1.0f)
        {
            if (ambienceClip == null) return;
            
            StartCoroutine(CrossfadeAmbience(ambienceClip, loop, fadeTime));
        }
        
        // Preload audio clip
        public void PreloadAudioClip(string clipName, AudioClip clip)
        {
            if (!clipCache.ContainsKey(clipName) && clip != null)
            {
                clipCache.Add(clipName, clip);
            }
        }
        
        // Get cached audio clip
        public AudioClip GetCachedClip(string clipName)
        {
            if (clipCache.TryGetValue(clipName, out AudioClip clip))
            {
                return clip;
            }
            return null;
        }
        
        #region Volume Controls
        
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            audioMixer.SetFloat(MIXER_MASTER, ConvertToDecibels(masterVolume));
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            audioMixer.SetFloat(MIXER_MUSIC, ConvertToDecibels(musicVolume));
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            audioMixer.SetFloat(MIXER_SFX, ConvertToDecibels(sfxVolume));
        }
        
        public void SetAmbienceVolume(float volume)
        {
            ambienceVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("AmbienceVolume", ambienceVolume);
            audioMixer.SetFloat(MIXER_AMBIENCE, ConvertToDecibels(ambienceVolume));
        }
        
        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("UIVolume", uiVolume);
            audioMixer.SetFloat(MIXER_UI, ConvertToDecibels(uiVolume));
        }
        
        #endregion

        #endregion

        #region Coroutines
        
        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip, bool loop, float fadeTime)
        {
            // Create a temporary audio source for crossfading
            GameObject tempObj = new GameObject("Temp_Music_Crossfade");
            tempObj.transform.SetParent(transform);
            AudioSource tempSource = tempObj.AddComponent<AudioSource>();
            tempSource.outputAudioMixerGroup = musicSource.outputAudioMixerGroup;
            tempSource.loop = loop;
            tempSource.clip = newClip;
            tempSource.volume = 0;
            tempSource.Play();
            
            // Fade out current music while fading in new music
            float timer = 0;
            float startVolume = musicSource.volume;
            
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                float t = timer / fadeTime;
                
                musicSource.volume = Mathf.Lerp(startVolume, 0, t);
                tempSource.volume = Mathf.Lerp(0, 1, t);
                
                yield return null;
            }
            
            // Stop old music and transfer new music to main source
            AudioClip oldClip = musicSource.clip;
            bool oldLoop = musicSource.loop;
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.loop = loop;
            musicSource.volume = tempSource.volume;
            musicSource.time = tempSource.time; // Keep playback position
            musicSource.Play();
            
            // Destroy temporary source
            Destroy(tempObj);
        }
        
        private System.Collections.IEnumerator CrossfadeAmbience(AudioClip newClip, bool loop, float fadeTime)
        {
            // Similar crossfade logic for ambience
            GameObject tempObj = new GameObject("Temp_Ambience_Crossfade");
            tempObj.transform.SetParent(transform);
            AudioSource tempSource = tempObj.AddComponent<AudioSource>();
            tempSource.outputAudioMixerGroup = ambienceSource.outputAudioMixerGroup;
            tempSource.loop = loop;
            tempSource.clip = newClip;
            tempSource.volume = 0;
            tempSource.Play();
            
            float timer = 0;
            float startVolume = ambienceSource.volume;
            
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                float t = timer / fadeTime;
                
                ambienceSource.volume = Mathf.Lerp(startVolume, 0, t);
                tempSource.volume = Mathf.Lerp(0, 1, t);
                
                yield return null;
            }
            
            AudioClip oldClip = ambienceSource.clip;
            bool oldLoop = ambienceSource.loop;
            ambienceSource.Stop();
            ambienceSource.clip = newClip;
            ambienceSource.loop = loop;
            ambienceSource.volume = tempSource.volume;
            ambienceSource.time = tempSource.time;
            ambienceSource.Play();
            
            Destroy(tempObj);
        }
        
        #endregion
    }
}
