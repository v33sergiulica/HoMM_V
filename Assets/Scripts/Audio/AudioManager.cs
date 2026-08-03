using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HommClone.Audio
{
    /// <summary>
    /// Centralized Audio Manager controlling combat SFX (Melee, Ranged, Retaliation, Hits, UI)
    /// and Background Music (BGM) playlist auto-cycling.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource moveSource;

        [Header("Combat Sound Effects")]
        [SerializeField] private AudioClip meleeSound;
        [SerializeField] private AudioClip rangedSound;
        [SerializeField] private AudioClip retaliationSound;
        [SerializeField] private AudioClip moveSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip deathSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip victorySound;
        [SerializeField] private AudioClip defeatSound;
        [SerializeField] private AudioClip pickupNotificationSound;

        [Header("SFX Timing Offsets (Seconds Delay)")]
        [SerializeField] private float meleeSoundDelay = 0.0f;
        [SerializeField] private float rangedSoundDelay = 0.0f;
        [SerializeField] private float retaliationSoundDelay = 0.0f;
        [SerializeField] private float moveSoundDelay = 0.0f;
        [SerializeField] private float hitSoundDelay = 0.0f;
        [SerializeField] private float deathSoundDelay = 0.0f;
        [SerializeField] private float buttonClickSoundDelay = 0.0f;

        [Header("Background Music Playlists")]
        [SerializeField] private List<AudioClip> worldMapPlaylist = new List<AudioClip>();
        [SerializeField] private List<AudioClip> combatPlaylist = new List<AudioClip>();
        [SerializeField] private List<AudioClip> bgmPlaylist = new List<AudioClip>();
        [SerializeField] private AudioClip worldMapMusic;
        [SerializeField] private AudioClip combatMusic;
        [SerializeField] private float fadeDuration = 0.8f;
        [SerializeField] private bool shufflePlaylist = false;
        [SerializeField] private bool loopSingleTrack = false;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1.0f;
        [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.5f;

        private int _currentTrackIndex = -1;
        private Coroutine _playlistCoroutine;
        private Coroutine _fadeCoroutine;
        private Coroutine _moveDelayCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioSources();
        }

        private void Start()
        {
            if (bgmPlaylist.Count > 0 && (bgmSource != null && !bgmSource.isPlaying))
            {
                StartBGMPlaylist();
            }
        }

        private void EnsureAudioSources()
        {
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX_Source");
                sfxObj.transform.SetParent(transform, false);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (bgmSource == null)
            {
                GameObject bgmObj = new GameObject("BGM_Source");
                bgmObj.transform.SetParent(transform, false);
                bgmSource = bgmObj.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = false; // We handle track cycling manually
            }

            if (moveSource == null)
            {
                GameObject moveObj = new GameObject("Move_Source");
                moveObj.transform.SetParent(transform, false);
                moveSource = moveObj.AddComponent<AudioSource>();
                moveSource.playOnAwake = false;
                moveSource.loop = true;
            }

            sfxSource.volume = sfxVolume;
            bgmSource.volume = bgmVolume;
            moveSource.volume = sfxVolume;
        }

        #region SFX Triggers
        public void PlayMeleeSound()
        {
            PlaySFXWithDelay(meleeSound, meleeSoundDelay);
        }

        public void PlayRangedSound()
        {
            PlaySFXWithDelay(rangedSound, rangedSoundDelay);
        }

        public void PlayRetaliationSound()
        {
            PlaySFXWithDelay(retaliationSound, retaliationSoundDelay);
        }

        public void PlayMoveSound()
        {
            if (moveSound == null) return;
            EnsureAudioSources();

            if (moveSoundDelay <= 0f)
            {
                StartMoveAudioInternal();
            }
            else
            {
                if (_moveDelayCoroutine != null) StopCoroutine(_moveDelayCoroutine);
                _moveDelayCoroutine = StartCoroutine(PlayMoveSoundDelayedCoroutine());
            }
        }

        private IEnumerator PlayMoveSoundDelayedCoroutine()
        {
            yield return new WaitForSeconds(moveSoundDelay);
            StartMoveAudioInternal();
        }

        private void StartMoveAudioInternal()
        {
            if (moveSound == null || moveSource == null) return;
            moveSource.clip = moveSound;
            moveSource.loop = true;
            moveSource.volume = sfxVolume;

            if (!moveSource.isPlaying)
            {
                moveSource.Play();
            }
        }

        public void StopMoveSound()
        {
            if (_moveDelayCoroutine != null)
            {
                StopCoroutine(_moveDelayCoroutine);
                _moveDelayCoroutine = null;
            }

            if (moveSource != null && moveSource.isPlaying)
            {
                moveSource.Stop();
            }
        }

        public void PlayHitSound()
        {
            PlaySFXWithDelay(hitSound, hitSoundDelay);
        }

        public void PlayDeathSound()
        {
            PlaySFXWithDelay(deathSound, deathSoundDelay);
        }

        public void PlayButtonClickSound()
        {
            PlaySFXWithDelay(buttonClickSound, buttonClickSoundDelay);
        }

        public void StopMusic()
        {
            if (_playlistCoroutine != null) StopCoroutine(_playlistCoroutine);
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            if (bgmSource != null) bgmSource.Stop();
        }

        public void StopSFX()
        {
            if (sfxSource != null) sfxSource.Stop();
        }

        public void PlayVictorySound()
        {
            StopMusic();
            PlaySFXWithDelay(victorySound, 0f);
        }

        public void PlayDefeatSound()
        {
            StopMusic();
            PlaySFXWithDelay(defeatSound, 0f);
        }

        public void PlayNotificationSound(AudioClip customClip = null)
        {
            AudioClip clip = customClip != null ? customClip : pickupNotificationSound;
            if (clip != null) PlaySFXWithDelay(clip, 0f);
        }

        public void PlaySFXWithDelay(AudioClip clip, float delay, float volumeScale = 1.0f)
        {
            if (clip == null) return;
            if (delay <= 0f)
            {
                PlaySFX(clip, volumeScale);
            }
            else
            {
                StartCoroutine(PlaySFXDelayedCoroutine(clip, delay, volumeScale));
            }
        }

        private IEnumerator PlaySFXDelayedCoroutine(AudioClip clip, float delay, float volumeScale)
        {
            yield return new WaitForSeconds(delay);
            PlaySFX(clip, volumeScale);
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip == null) return;
            EnsureAudioSources();
            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume);
        }
        #endregion

        #region BGM Playlist Management
        public void StartBGMPlaylist()
        {
            EnsureAudioSources();
            if (_playlistCoroutine != null) StopCoroutine(_playlistCoroutine);
            _playlistCoroutine = StartCoroutine(BGMPlaylistCoroutine());
        }

        private IEnumerator BGMPlaylistCoroutine()
        {
            if (bgmPlaylist == null || bgmPlaylist.Count == 0) yield break;

            while (true)
            {
                if (shufflePlaylist)
                {
                    _currentTrackIndex = Random.Range(0, bgmPlaylist.Count);
                }
                else
                {
                    _currentTrackIndex = (_currentTrackIndex + 1) % bgmPlaylist.Count;
                }

                AudioClip currentClip = bgmPlaylist[_currentTrackIndex];
                if (currentClip != null)
                {
                    bgmSource.clip = currentClip;
                    bgmSource.volume = bgmVolume;
                    bgmSource.Play();

                    // Wait while music track is playing
                    while (bgmSource.isPlaying)
                    {
                        if (loopSingleTrack)
                        {
                            bgmSource.loop = true;
                        }
                        else
                        {
                            bgmSource.loop = false;
                        }
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }

        public void NextBGM()
        {
            if (bgmPlaylist.Count == 0) return;
            if (bgmSource != null) bgmSource.Stop();
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
            if (sfxSource != null) sfxSource.volume = sfxVolume;
        }

        public void SetMusicVolume(float vol)
        {
            bgmVolume = Mathf.Clamp01(vol);
            if (bgmSource != null) bgmSource.volume = bgmVolume;
        }

        public void PlayWorldMapMusic()
        {
            EnsureAudioSources();
            if (worldMapPlaylist != null && worldMapPlaylist.Count > 0)
            {
                AudioClip clip = shufflePlaylist ? worldMapPlaylist[Random.Range(0, worldMapPlaylist.Count)] : worldMapPlaylist[0];
                CrossfadeMusic(clip);
            }
            else if (worldMapMusic != null)
            {
                CrossfadeMusic(worldMapMusic);
            }
            else if (bgmPlaylist != null && bgmPlaylist.Count > 0)
            {
                AudioClip clip = shufflePlaylist ? bgmPlaylist[Random.Range(0, bgmPlaylist.Count)] : bgmPlaylist[0];
                CrossfadeMusic(clip);
            }
            else
            {
                Debug.LogWarning("[AudioManager] PlayWorldMapMusic called, but no music clips assigned in worldMapPlaylist, worldMapMusic, or bgmPlaylist!");
            }
        }

        public void PlayCombatMusic()
        {
            EnsureAudioSources();
            if (combatPlaylist != null && combatPlaylist.Count > 0)
            {
                AudioClip clip = shufflePlaylist ? combatPlaylist[Random.Range(0, combatPlaylist.Count)] : combatPlaylist[0];
                CrossfadeMusic(clip);
            }
            else if (combatMusic != null)
            {
                CrossfadeMusic(combatMusic);
            }
            else if (bgmPlaylist != null && bgmPlaylist.Count > 0)
            {
                AudioClip clip = shufflePlaylist ? bgmPlaylist[Random.Range(0, bgmPlaylist.Count)] : bgmPlaylist[0];
                CrossfadeMusic(clip);
            }
            else
            {
                Debug.LogWarning("[AudioManager] PlayCombatMusic called, but no music clips assigned in combatPlaylist, combatMusic, or bgmPlaylist!");
            }
        }

        public void CrossfadeMusic(AudioClip newClip)
        {
            EnsureAudioSources();
            if (bgmSource == null || newClip == null) return;
            if (bgmSource.clip == newClip && bgmSource.isPlaying) return;

            if (_playlistCoroutine != null) StopCoroutine(_playlistCoroutine);
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeToNewClip(newClip));
        }

        private IEnumerator FadeToNewClip(AudioClip newClip)
        {
            float startVolume = bgmSource.volume;
            float timer = 0f;
            float halfFade = Mathf.Max(0.1f, fadeDuration * 0.5f);

            while (timer < halfFade)
            {
                timer += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / halfFade);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.clip = newClip;
            bgmSource.loop = true;
            bgmSource.Play();

            timer = 0f;
            while (timer < halfFade)
            {
                timer += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0f, bgmVolume, timer / halfFade);
                yield return null;
            }

            bgmSource.volume = bgmVolume;
        }
        #endregion

        /// <summary>
        /// Helper to ensure an AudioManager instance exists in the current scene.
        /// </summary>
        public static AudioManager GetOrCreateInstance()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<AudioManager>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject prefab = Resources.Load<GameObject>("AudioManager");
                    if (prefab != null)
                    {
                        GameObject instantiated = Instantiate(prefab);
                        instantiated.name = "AudioManager";
                        Instance = instantiated.GetComponent<AudioManager>();
                    }
                    else
                    {
                        GameObject audioObj = new GameObject("AudioManager");
                        Instance = audioObj.AddComponent<AudioManager>();
                    }
                }
            }
            return Instance;
        }
    }
}
