using UnityEngine;

namespace EndlessRunner
{
    /// <summary>
    /// Nhạc nền + hiệu ứng âm thanh. Mọi file âm thanh gán trong RunnerTheme > Âm thanh.
    /// Ô nào trống thì bỏ qua, không lỗi.
    /// </summary>
    public class RunnerAudio : MonoBehaviour
    {

        RunnerGame game;
        RunnerTheme theme;
        AudioSource music, sfx;
        float musicTargetVolume, musicTargetPitch = 1f;

        public bool Muted { get; private set; }

        public void Setup(RunnerGame game, RunnerTheme theme)
        {
            this.game = game;
            this.theme = theme;

            music = gameObject.AddComponent<AudioSource>();
            music.clip = theme.music;
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f; // 2D
            music.volume = 0f;

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;

            Muted = RunnerSave.Muted;
            AudioListener.volume = Muted ? 0f : 1f;

            game.StateChanged += OnStateChanged;
            game.CoinCollected += () => Play(theme.coinSfx, 0.7f, Random.Range(0.95f, 1.1f));
            game.Player.Moved += OnPlayerMoved;
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.StateChanged -= OnStateChanged;
            if (game.Player != null) game.Player.Moved -= OnPlayerMoved;
        }

        void OnStateChanged(RunnerGame.GameState s)
        {
            switch (s)
            {
                case RunnerGame.GameState.Ready:
                    musicTargetVolume = theme.musicVolume * theme.menuVolumeScale; // màn chờ: nhạc nhỏ hơn
                    musicTargetPitch = 1f;
                    StartMusic();
                    break;
                case RunnerGame.GameState.Running:
                    musicTargetVolume = theme.musicVolume;
                    StartMusic();
                    break;
                case RunnerGame.GameState.Paused:
                    if (music.clip != null) music.Pause();
                    break;
                case RunnerGame.GameState.Dead:
                    Play(theme.deathSfx, 1f, 1f);
                    musicTargetVolume = theme.musicVolume * 0.35f; // nhạc chùng xuống khi thua
                    musicTargetPitch = 0.85f;
                    break;
            }
        }

        void StartMusic()
        {
            if (music.clip == null) return;
            if (!music.isPlaying) music.UnPause();
            if (!music.isPlaying) music.Play();
        }

        void OnPlayerMoved(RunnerAction a)
        {
            switch (a)
            {
                case RunnerAction.Left:
                case RunnerAction.Right: Play(theme.laneSfx, 0.6f, Random.Range(0.95f, 1.05f)); break;
                case RunnerAction.Jump: Play(theme.jumpSfx, 0.8f, Random.Range(0.97f, 1.03f)); break;
                case RunnerAction.Slide: Play(theme.slideSfx, 0.8f, Random.Range(0.97f, 1.03f)); break;
            }
        }

        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            sfx.pitch = pitch;
            sfx.PlayOneShot(clip, volume * theme.sfxVolume);
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            AudioListener.volume = Muted ? 0f : 1f;
            RunnerSave.Muted = Muted;
        }

        void Update()
        {
            if (music.clip == null) return;
            float dt = Time.unscaledDeltaTime;

            // Nhạc nhanh dần theo tốc độ chạy
            if (game.State == RunnerGame.GameState.Running && theme.musicSpeedsUp)
            {
                float t = Mathf.InverseLerp(game.startSpeed, game.maxSpeed, game.Speed);
                musicTargetPitch = Mathf.Lerp(1f, theme.maxMusicPitch, t);
            }
            else if (game.State == RunnerGame.GameState.Running)
            {
                musicTargetPitch = 1f;
            }

            music.volume = Mathf.MoveTowards(music.volume, musicTargetVolume, dt * 1.5f);
            music.pitch = Mathf.MoveTowards(music.pitch, musicTargetPitch, dt * 0.5f);
        }
    }
}
