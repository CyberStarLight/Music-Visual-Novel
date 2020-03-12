using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class StoryManager : MonoBehaviour {
    public const string READ_SCENE_KEY = "ReadStory_";

    public static StoryManager Instance = null;
    public static StoryPanel MainStoryPanel;
    public static string LastVisitedPlanetName;

    public StoryCharacter[] Characters;
    public StoryObject[] Objects;
    public CanvasGroup[] Backgrounds;

    public StoryCharacterSound DefaultSound;
    public TMP_FontAsset DefaultFont;
    public Sprite DefaultBackground;
    public float CharsPerSecond = 20f;
    public float CommaDelay = 0.5f;
    public float PeriodDelay = 1f;
    public float BackgroundSpeed = 10f;

    public StoryTrigger[] StoryTriggers;

    //state variables
    private bool stopIntroMovie = false;
    public static bool loadedStoryIsIntro { get; set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public static void MarkStoryAsRead(string screenName)
    {
        PlayerPrefs.SetInt(READ_SCENE_KEY + screenName.ToLower(), 1);
    }

    public static bool WasRead(string screenName)
    {
        int value = PlayerPrefs.GetInt(READ_SCENE_KEY + screenName.ToLower(), 0);
        return value == 1;
    }

    public void StopIntroMovie()
    {
        stopIntroMovie = true;
    }

}

[System.Serializable]
public class StoryBackground
{
    public string name;
    public RectTransform Background;
}

[System.Serializable]
public class StoryObject
{
    public string Name;
    public Sprite Sprite;
    public Vector2 Size = new Vector2(100, 100);
    public bool Flip;
}

[System.Serializable]
public class StoryCharacter
{
    public string Name;
    public string DisplayName;
    public Color TextColor;
    public TMP_FontAsset FontNew;
    public Portrait[] Portraits;
    public StoryCharacterSound Sound;
}

[System.Serializable]
public class StoryCharacterSound
{
    public FMODClip[] RandomizedBits;
    public FMODClip[] AskBits;
    public FMODClip[] SayBits;
}

[System.Serializable]
public class Portrait
{
    public string Name;
    public Sprite Sprite;
}

//Story triggers
[System.Serializable]
public class StoryTrigger
{
    public string ScreenName;
    public StoryTriggerType Type;
}

public enum StoryTriggerType
{
    Intro,
}