using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using Foundation.Events;

namespace Presentation.Audio
{
    /// <summary>
    /// Main audio manager for the game
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixerGroup masterMixerGroup;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup voiceMixerGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource[] sfxSources;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] musicTracks;
        [SerializeField] private AudioClip[] ambientSounds;
        [SerializeField] private SoundEffect[] soundEffects;

        [Header("Settings")]
        [SerializeField] private int maxSFXSources = 10;
        [SerializeField] private float defaultFadeTime = 1f;

        // State
        private readonly Dictionary<string, AudioClip> soundLibrary = new();
        private readonly Queue<AudioSource> availableSFXSources = new();
        private readonly List<AudioSource> activeSFXSources = new();
        private int currentMusicTrack = -1;

        public void Initialize()
        {
            BuildSoundLibrary();
            SetupAudioSources();
            SubscribeToEvents();
            
            Debug.Log("Audio Manager initialized");
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void BuildSoundLibrary()
        {
            soundLibrary.Clear();

            // Add music tracks
            for (int i = 0; i < musicTracks.Length; i++)
            {
                if (musicTracks[i] != null)
                {
                    soundLibrary[$"music_{i}"] = musicTracks[i];
                    soundLibrary[musicTracks[i].name] = musicTracks[i];
                }
            }

            // Add ambient sounds
            for (int i = 0; i < ambientSounds.Length; i++)
            {
                if (ambientSounds[i] != null)
                {
                    soundLibrary[$"ambient_{i}"] = ambientSounds[i];
                    soundLibrary[ambientSounds[i].name] = ambientSounds[i];
                }
            }

            // Add sound effects
            foreach (var sfx in soundEffects)
            {
                if (sfx.clip != null)
                {
                    soundLibrary[sfx.name] = sfx.clip;
                }
            }
        }

        private void SetupAudioSources()
        {
            // Setup music source
            if (musicSource == null)
            {
                var musicObj = new GameObject("Music Source");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
            }
            
            musicSource.outputAudioMixerGroup = musicMixerGroup;
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            // Setup ambient source
            if (ambientSource == null)
            {
                var ambientObj = new GameObject("Ambient Source");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
            }
            
            ambientSource.outputAudioMixerGroup = sfxMixerGroup;
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;

            // Setup SFX sources
            if (sfxSources == null || sfxSources.Length == 0)
            {
                sfxSources = new AudioSource[maxSFXSources];
                for (int i = 0; i < maxSFXSources; i++)
                {
                    var sfxObj = new GameObject($"SFX Source {i}");
                    sfxObj.transform.SetParent(transform);
                    sfxSources[i] = sfxObj.AddComponent<AudioSource>();
                    sfxSources[i].outputAudioMixerGroup = sfxMixerGroup;
                    sfxSources[i].playOnAwake = false;
                }
            }

            // Initialize SFX source pool
            availableSFXSources.Clear();
            foreach (var source in sfxSources)
            {
                availableSFXSources.Enqueue(source);
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<Infrastructure.Scene.GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<Entities.Player.PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<Entities.Player.PlayerJumpEvent>(OnPlayerJump);
            EventBus.Subscribe<PlaySoundEvent>(OnPlaySoundRequested);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<Infrastructure.Scene.GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Unsubscribe<Entities.Player.PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<Entities.Player.PlayerJumpEvent>(OnPlayerJump);
            EventBus.Unsubscribe<PlaySoundEvent>(OnPlaySoundRequested);
        }

        private void OnGameStateChanged(Infrastructure.Scene.GameStateChangedEvent gameStateEvent)
        {
            switch (gameStateEvent.NewState)
            {
                case Infrastructure.Scene.GameState.MainMenu:
                    PlayMusic("menu_music");
                    break;
                case Infrastructure.Scene.GameState.Playing:
                    PlayMusic("game_music");
                    break;
                case Infrastructure.Scene.GameState.GameOver:
                    PlayMusic("gameover_music");
                    break;
            }
        }

        private void OnPlayerDamaged(Entities.Player.PlayerDamagedEvent damageEvent)
        {
            PlaySFX("player_hurt");
        }

        private void OnPlayerJump(Entities.Player.PlayerJumpEvent jumpEvent)
        {
            PlaySFX("player_jump");
        }

        private void OnPlaySoundRequested(PlaySoundEvent soundEvent)
        {
            switch (soundEvent.SoundType)
            {
                case SoundType.Music:
                    PlayMusic(soundEvent.SoundName);
                    break;
                case SoundType.SFX:
                    PlaySFX(soundEvent.SoundName, soundEvent.Volume);
                    break;
                case SoundType.Ambient:
                    PlayAmbient(soundEvent.SoundName);
                    break;
            }
        }

        /// <summary>
        /// Play music track
        /// </summary>
        public void PlayMusic(string musicName, bool fadeIn = true)
        {
            if (!soundLibrary.TryGetValue(musicName, out var clip)) return;

            if (fadeIn && musicSource.isPlaying)
            {
                StartCoroutine(FadeMusicTransition(clip));
            }
            else
            {
                musicSource.clip = clip;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Play sound effect
        /// </summary>
        public void PlaySFX(string sfxName, float volume = 1f)
        {
            if (!soundLibrary.TryGetValue(sfxName, out var clip)) return;

            var source = GetAvailableSFXSource();
            if (source != null)
            {
                source.clip = clip;
                source.volume = volume;
                source.Play();
                
                StartCoroutine(ReturnSFXSourceWhenDone(source));
            }
        }

        /// <summary>
        /// Play 3D sound effect at position
        /// </summary>
        public void PlaySFX3D(string sfxName, Vector3 position, float volume = 1f)
        {
            if (!soundLibrary.TryGetValue(sfxName, out var clip)) return;

            var source = GetAvailableSFXSource();
            if (source != null)
            {
                source.transform.position = position;
                source.clip = clip;
                source.volume = volume;
                source.spatialBlend = 1f; // 3D
                source.Play();
                
                StartCoroutine(ReturnSFXSourceWhenDone(source));
            }
        }

        /// <summary>
        /// Play ambient sound
        /// </summary>
        public void PlayAmbient(string ambientName, bool loop = true)
        {
            if (!soundLibrary.TryGetValue(ambientName, out var clip)) return;

            ambientSource.clip = clip;
            ambientSource.loop = loop;
            ambientSource.Play();
        }

        /// <summary>
        /// Stop music
        /// </summary>
        public void StopMusic(bool fadeOut = true)
        {
            if (fadeOut)
            {
                StartCoroutine(FadeOutMusic());
            }
            else
            {
                musicSource.Stop();
            }
        }

        /// <summary>
        /// Set master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            if (masterMixerGroup != null)
            {
                masterMixerGroup.audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
            }
        }

        /// <summary>
        /// Set music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            if (musicMixerGroup != null)
            {
                musicMixerGroup.audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
            }
        }

        /// <summary>
        /// Set SFX volume
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            if (sfxMixerGroup != null)
            {
                sfxMixerGroup.audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
            }
        }

        private AudioSource GetAvailableSFXSource()
        {
            if (availableSFXSources.Count > 0)
            {
                var source = availableSFXSources.Dequeue();
                activeSFXSources.Add(source);
                return source;
            }
            
            return null;
        }

        private System.Collections.IEnumerator ReturnSFXSourceWhenDone(AudioSource source)
        {
            while (source.isPlaying)
            {
                yield return null;
            }
            
            activeSFXSources.Remove(source);
            availableSFXSources.Enqueue(source);
            source.spatialBlend = 0f; // Reset to 2D
        }

        private System.Collections.IEnumerator FadeMusicTransition(AudioClip newClip)
        {
            // Fade out current music
            yield return FadeOutMusic();
            
            // Change clip and fade in
            musicSource.clip = newClip;
            musicSource.Play();
            yield return FadeInMusic();
        }

        private System.Collections.IEnumerator FadeOutMusic()
        {
            var startVolume = musicSource.volume;
            var elapsed = 0f;
            
            while (elapsed < defaultFadeTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / defaultFadeTime);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            musicSource.volume = 0f;
            musicSource.Stop();
        }

        private System.Collections.IEnumerator FadeInMusic()
        {
            var targetVolume = 1f;
            var elapsed = 0f;
            musicSource.volume = 0f;
            
            while (elapsed < defaultFadeTime)
            {
                musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / defaultFadeTime);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            musicSource.volume = targetVolume;
        }
    }

    /// <summary>
    /// Sound effect configuration
    /// </summary>
    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
    }

    /// <summary>
    /// Sound types
    /// </summary>
    public enum SoundType
    {
        Music,
        SFX,
        Ambient,
        Voice
    }

    /// <summary>
    /// Play sound event
    /// </summary>
    public class PlaySoundEvent : GameEvent<PlaySoundData>
    {
        public string SoundName => Data.SoundName;
        public SoundType SoundType => Data.SoundType;
        public float Volume => Data.Volume;

        public PlaySoundEvent(string soundName, SoundType soundType, float volume = 1f) 
            : base(new PlaySoundData(soundName, soundType, volume))
        {
        }
    }

    [System.Serializable]
    public class PlaySoundData
    {
        public string SoundName { get; }
        public SoundType SoundType { get; }
        public float Volume { get; }

        public PlaySoundData(string soundName, SoundType soundType, float volume)
        {
            SoundName = soundName;
            SoundType = soundType;
            Volume = volume;
        }
    }
}
