using UnityEngine;
using UnityEngine.Serialization;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("#BGM")] 
    public AudioClip BgmClip;
    public float bgmVolume;
    AudioSource bgmPlayer;

    [Header("#SFX")]
    public AudioClip[] sfxClip;
    public float sfxVolume;
    public int channels;
    AudioSource[] sfxPlayers;
    int channelIndex;

    public enum Sfx {PlayerSwordSound, RatAttackSound, DogAttackSound, SkeletonSwordSound, GhostAttackSound, 
                     BallSound, HulahoopSound, FireSound, JumpUpSound, JumpDownSound, DamageSound, DungeonSound}

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Init()
    {
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = true;
        bgmPlayer.volume = bgmVolume;
        bgmPlayer.clip = BgmClip;

        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];

        for (int i = 0; i < channels; i++)
        {
            sfxPlayers[i] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[i].playOnAwake = false;
            sfxPlayers[i].volume = sfxVolume;
        }

        if (!PlayerPrefs.HasKey("BGM"))
            PlayerPrefs.SetFloat("BGM", 0.3f);

        if (!PlayerPrefs.HasKey("SFX")) 
            PlayerPrefs.SetFloat("SFX", 0.3f);

        bgmPlayer.volume = bgmVolume;

        foreach (AudioSource player in sfxPlayers)
        {
            player.volume = sfxVolume;
        }

        bgmPlayer.Play();
    }

    public void SetBgmVolume(float volume)
    {
        bgmVolume = volume;
        bgmPlayer.volume = volume;

        PlayerPrefs.SetFloat("BGM", volume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = volume;

        foreach (AudioSource player in sfxPlayers)
        {
            player.volume = volume;
        }

        PlayerPrefs.SetFloat("SFX", volume);
        PlayerPrefs.Save();
    }

    public void PlaySfx(Sfx sfx)
    {
        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            int loopIndex = (i + channelIndex) % sfxPlayers.Length;

            if (sfxPlayers[loopIndex].isPlaying)
                continue;

            channelIndex = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClip[(int)sfx];
            sfxPlayers[loopIndex].Play();
            break;
        }
    }
}