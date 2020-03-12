using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StoryPanel : MonoBehaviour {
    
    [Header("References")]
    public Transform Screens;
    public Image BackgroundContainer;
    public Image Background;
    public Transform DialogBox;
    public TextMeshProUGUI DialogText;
    public Image Character1;
    public Image Character1Face;
    public Image Character2;
    public Image Character2Face;
    public Button ContinueButton;
    public Transform BackgroundCharacters;
    public Transform ActiveCharacters;
    public Transform Points;
    public Transform BackgroundObjects;
    public Image BackgroundObjectPrefab;
    public Image FadeScreen;

    //State variables
    private Dictionary<string, StoryScreen> screens;
    private StoryScreen currentScreen;
    private StoryCharacter currentCharacter;
    private StoryCharacterSound currentCharacterSound;

    private bool isInitialized;
    private bool isFinished = false;
    private bool isScreenFinished = false;
    private bool isFastingForward = false;
    
    private int currBit = 0;

    private float LastPartTime = 0f;
    private float LastPartDelay = 0f;
    private bool backgroundMovingRight = false;

    private bool paused;
    private bool char1Active = true;

    private Dictionary<string, Image> CreatedObjects;
    private Dictionary<string, IEnumerator> MovingObjects;
    private Dictionary<string, Vector2> PointLocations;
    private Music OriginalMusic;

    public bool LoadPlanetAfter { get; set; }

    //Corutines
    IEnumerator Corutine_Fade;

    public StoryPanel()
    {
        StoryManager.MainStoryPanel = this;
    }

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        if (isFinished)
            return;

        MoveBackground();

        if (!isScreenFinished && !paused)
        {
            var SecondsPerPart = 1f / StoryManager.Instance.CharsPerSecond;

            bool isTimeForNewChar = Time.time - LastPartTime - LastPartDelay > SecondsPerPart;
            while (isFastingForward || isTimeForNewChar)
            {
                char nextChar = currentScreen.Dialog[currBit];
                int lastDialogCharIndex = (currentScreen.Dialog.Length - 1);

                //Handle command
                if (nextChar == '[')
                {
                    while (nextChar == '[')
                    {
                        //found a command, don't count this char for typing
                        //try to get all character until finding a ']' to end the command
                        List<char> commandChars = new List<char>();
                        currBit++;

                        bool foundCommandEnd = false;
                        for (; currBit <= lastDialogCharIndex; currBit++)
                        {
                            nextChar = currentScreen.Dialog[currBit];

                            if (nextChar == ']')
                            {
                                foundCommandEnd = true;

                                //skip ']' and set the char after it as the next char
                                if (currBit < lastDialogCharIndex)
                                {
                                    currBit++;
                                    nextChar = currentScreen.Dialog[currBit];
                                }
                                else
                                {
                                    //Last char in dialog was a ']'
                                    //no need to type anymore.
                                    isScreenFinished = true;
                                }

                                break;
                            }
                            else
                            {
                                commandChars.Add(currentScreen.Dialog[currBit]);
                            }
                        }

                        if (!foundCommandEnd)
                            throw new System.InvalidOperationException("Invalid dialog syntx! command was opened with a [ character, but never closed with a ] character.");

                        var commandParts =
                            new string(commandChars.ToArray())
                            .Split(':')
                            .Select(x => x.Trim().ToLower())
                            .Where(x => x != string.Empty)
                            .ToArray();
                        
                        var result = RunCommand(commandParts);

                        if (isScreenFinished || paused || result.IsStopping)
                            return;
                    }
                }
                else //handle next char
                {
                    //skip sounds if fasting forward
                    if (!isFastingForward)
                    {
                        FMODClip[] clipsSource;
                        switch (nextChar)
                        {
                            case ' ':
                            case '\n':
                            case '\t':
                            case '\r':
                                clipsSource = null;
                                LastPartDelay = 0f;
                                break;
                            case ',':
                                clipsSource = currentCharacterSound.RandomizedBits;
                                LastPartDelay = StoryManager.Instance.CommaDelay;
                                break;
                            case '.':
                                clipsSource = currentCharacterSound.RandomizedBits;
                                LastPartDelay = StoryManager.Instance.PeriodDelay;
                                break;
                            case '?':
                                if (currentCharacterSound.AskBits != null && currentCharacterSound.AskBits.Length > 0)
                                    clipsSource = currentCharacterSound.AskBits;
                                else
                                    clipsSource = currentCharacterSound.RandomizedBits;

                                LastPartDelay = StoryManager.Instance.PeriodDelay;
                                break;
                            case '!':
                                if (currentCharacterSound.SayBits != null && currentCharacterSound.SayBits.Length > 0)
                                    clipsSource = currentCharacterSound.SayBits;
                                else
                                    clipsSource = currentCharacterSound.RandomizedBits;

                                LastPartDelay = StoryManager.Instance.PeriodDelay;
                                break;
                            default:
                                clipsSource = currentCharacterSound.RandomizedBits;
                                LastPartDelay = 0f;
                                break;
                        }

                        if (clipsSource != null && clipsSource.Length > 0)
                            FMODManager.Play(clipsSource[Random.Range(0, clipsSource.Length - 1)]);
                    }

                    //DialogTextOld.text += nextChar;
                    DialogText.text += nextChar;
                    currBit++;
                    LastPartTime = Time.time;
                }

                //stop loop
                //if we're fasting forward and the dialog was not paused, continue loop
                isTimeForNewChar = false;
                if (paused)
                    isFastingForward = false;
            }
        }
    }
    
    public void Initialize()
    {
        if (isInitialized)
            return;

        OriginalMusic = FMODManager.CurrentMusic;

        var screenChildren = Screens.GetComponentsInChildren<StoryScreen>();

        //Dont start if there are no screens to play
        if (screenChildren == null || screenChildren.Length < 1)
        {
            isFinished = true;
            isScreenFinished = true;
            return;
        }

        screens = new Dictionary<string, StoryScreen>();
        foreach (var screen in screenChildren)
        {
            screens[screen.name.ToLower()] = screen;
        }

        PointLocations = new Dictionary<string, Vector2>();
        foreach (Transform point in Points)
        {
            PointLocations[point.name.ToLower()] = point.GetComponent<Image>().rectTransform.anchoredPosition;
            point.gameObject.SetActive(false);
        }

        CreatedObjects = new Dictionary<string, Image>();
        MovingObjects = new Dictionary<string, IEnumerator>();

        DialogText.text = string.Empty;
        isScreenFinished = false;
        isInitialized = true;
    }

    public void MoveBackground()
    {
        float magicNumber = 510f;

        if (backgroundMovingRight)
        {
            Background.rectTransform.anchoredPosition += new Vector2((Time.deltaTime * StoryManager.Instance.BackgroundSpeed), 0);

            if (Background.rectTransform.anchoredPosition.x > 0)
            {
                Background.rectTransform.anchoredPosition = Vector2.zero;
                backgroundMovingRight = false;
            }
        }
        else
        {
            Background.rectTransform.anchoredPosition -= new Vector2((Time.deltaTime * StoryManager.Instance.BackgroundSpeed), 0);

            if (Background.rectTransform.anchoredPosition.x < -magicNumber)
            {
                backgroundMovingRight = true;
                Background.rectTransform.anchoredPosition = new Vector2(-magicNumber, 0);

            }
        }
    }
    
    public CommandResult RunCommand(string[] info)
    {
        if (info.Length < 1)
            throw new System.ArgumentException("Empty command found!", "info");

        string command = info.First();

        switch (command)
        {
            case "click":
                paused = true;
                ContinueButton.gameObject.SetActive(true);
                return new CommandResult() { IsStopping = true };
            case "char1":
                string face1 = info.Length > 2 ? info[2] : null;
                Char1(info[1], face1);
                return new CommandResult();
            case "char2":
                string face2 = info.Length > 2 ? info[2] : null;
                Char2(info[1], face2);
                return new CommandResult();
            case "move":
                Move(info[1], info[2], info[3], info[4]);
                return new CommandResult();
            case "clear":
                DialogText.text = string.Empty;
                return new CommandResult();
            case "screen":
                LoadScreen(info[1]);
                return new CommandResult();
            case "end":
                EndStory();
                return new CommandResult();
            case "wait":
                LastPartDelay += float.Parse(info[1]);
                return new CommandResult() { IsStopping = true };
            case "icon":
                AddIcon(info[1], info[2]);
                return new CommandResult();
            case "fadecolor":
                FadeColor(info[1], info[2], info[3]);
                return new CommandResult();
            case "fade":
                Fade(info[1], info[2], info[3]);
                return new CommandResult();
            default:
                print("unknown command [" + command + "] found!");
                return new CommandResult();
        }
    }

    //Helpers
    public void ClearScreen()
    {
        currentScreen = null;
        DialogText.text = string.Empty;
        isScreenFinished = true;
        isFastingForward = false;
        paused = false;
        
        currBit = 0;

        StopAllCoroutines();
        foreach (var item in CreatedObjects)
        {
            Destroy(item.Value.gameObject);
        }
        CreatedObjects.Clear();

        Char1("none", null);
        Char2("none", null);
        UpdateCharacter();

        Background.sprite = StoryManager.Instance.DefaultBackground;

        ContinueButton.gameObject.SetActive(false);
    }

    public void LoadScreen(string screenName)
    {
        Initialize();

        screenName = screenName.ToLower();


        ClearScreen();
        
        if(!screens.ContainsKey(screenName))
        {
            Debug.LogError("Story Screen named [" + screenName + "] was not found!");
            return;
        }

        currentScreen = screens[screenName];
        StoryManager.MarkStoryAsRead(screenName);
        
        isScreenFinished = false;
        isFinished = false;
        
        Background.sprite = currentScreen.BackgroundImage != null ? currentScreen.BackgroundImage : StoryManager.Instance.DefaultBackground;

        if (currentScreen.Music != null)
        {
            FMODManager.Play(currentScreen.Music.Value);
        }
        else if(currentScreen.StopMusic)
        {
            FMODManager.StopMusic();
        }

        gameObject.SetActive(true);
    }

    public void UpdateCharacter(bool clearText = true)
    {
        if (clearText)
        {
            DialogText.text = string.Empty;
        }

        if (currentCharacter != null && currentCharacter.Sound != null && currentCharacter.Sound.RandomizedBits.Length > 0)
            currentCharacterSound = currentCharacter.Sound;
        else
            currentCharacterSound = StoryManager.Instance.DefaultSound;

        DialogText.color = currentCharacter != null ? currentCharacter.TextColor : Color.white;
        DialogText.font = currentCharacter != null && currentCharacter.FontNew != null ? currentCharacter.FontNew : StoryManager.Instance.DefaultFont;
    }

    IEnumerator MoveObject(string name, Image obj, Vector2 from, Vector2 to, float duration)
    {
        
        float distancePerSec = Vector2.Distance(from, to) / duration;

        float frametime = 1f / 30f;

        obj.rectTransform.anchoredPosition = from;

        while (
            !obj.rectTransform.anchoredPosition.x.AlmostEquals(to.x) ||
            !obj.rectTransform.anchoredPosition.y.AlmostEquals(to.y)
            )
        {
            obj.rectTransform.anchoredPosition = Vector2.MoveTowards(
                obj.rectTransform.anchoredPosition,
                to,
                distancePerSec * frametime
                );

            yield return new WaitForSeconds(frametime);
        }

        MovingObjects.Remove(name);
    }

    //Commands
    public void Char1(string charName, string charPortrait)
    {
        if(charName == "none")
        {
            Character1.gameObject.SetActive(false);
            currentCharacter = null;

            if(char1Active)
                UpdateCharacter();

            return;
        }

        char1Active = true;

        charName = charName.ToLower();
        charPortrait = charPortrait.ToLower();

        currentCharacter = StoryManager.Instance.Characters.FirstOrDefault(x => x.Name.ToLower() == charName);
        if (currentCharacter == null)
            throw new System.ArgumentException("failed change of character 1, character \"" + charName + "\" was not found!", "charName");

        var currentPortrait = currentCharacter.Portraits.FirstOrDefault(x => x.Name.ToLower() == charPortrait);
        if (currentPortrait == null)
            throw new System.ArgumentException("failed change of character 1, portrait \"" + charPortrait + "\" was not found!", "charPortrait");
        Character1Face.sprite = currentPortrait.Sprite;

        Character1.transform.SetParent(ActiveCharacters, false);
        Character2.transform.SetParent(BackgroundCharacters, false);

        Character1.gameObject.SetActive(true);

        UpdateCharacter();
    }

    public void Char2(string charName, string charPortrait)
    {
        if (charName == "none")
        {
            Character2.gameObject.SetActive(false);
            currentCharacter = null;

            if (!char1Active)
                UpdateCharacter();

            return;
        }

        char1Active = false;

        charName = charName.ToLower();
        charPortrait = charPortrait.ToLower();

        currentCharacter = StoryManager.Instance.Characters.FirstOrDefault(x => x.Name.ToLower() == charName);
        if (currentCharacter == null)
            throw new System.ArgumentException("failed change of character 2, character \"" + charName + "\" was not found!", "charName");

        var currentPortrait = currentCharacter.Portraits.FirstOrDefault(x => x.Name.ToLower() == charPortrait);
        if (currentPortrait == null)
            throw new System.ArgumentException("failed change of character 2, portrait \"" + charPortrait + "\" was not found!", "charPortrait");
        Character2Face.sprite = currentPortrait.Sprite;

        Character2.transform.SetParent(ActiveCharacters, false);
        Character1.transform.SetParent(BackgroundCharacters, false);

        Character2.gameObject.SetActive(true);

        UpdateCharacter();
    }

    public void Move(string objectName, string from, string to, string seconds)
    {
        objectName = objectName.ToLower();

        Image existingObject;
        if(!CreatedObjects.TryGetValue(objectName, out existingObject))
        {
            var storyObject = StoryManager.Instance.Objects.First(x => x.Name.ToLower() == objectName);
            existingObject = Instantiate(BackgroundObjectPrefab, BackgroundObjects);
            existingObject.sprite = storyObject.Sprite;
            existingObject.rectTransform.sizeDelta = storyObject.Size;

            if(storyObject.Flip)
                existingObject.rectTransform.localScale = new Vector3(existingObject.rectTransform.localScale.x * -1, existingObject.rectTransform.localScale.y, existingObject.rectTransform.localScale.z);

            CreatedObjects.Add(objectName, existingObject);
        }

        var fromPos = PointLocations[from.ToLower()];
        var toPos = PointLocations[to.ToLower()];
        var duration = float.Parse(seconds);
        
        MovingObjects[objectName] = MoveObject(objectName, existingObject, fromPos, toPos, duration);
        StartCoroutine(MovingObjects[objectName]);
    }

    public void EndStory()
    {
        ClearScreen();
        isFinished = true;

        gameObject.SetActive(false);
    }

    public void AddIcon(string packName, string iconName)
    {
        DialogText.text += string.Format("<sprite=\"{0}\" name=\"{1}\">", packName, iconName);
    }

    public void FadeColor(string r, string g, string b)
    {
        FadeScreen.color = new Color(int.Parse(r), int.Parse(g), int.Parse(b), FadeScreen.color.a);
    }

    public void Fade(string fadeOut, string solid, string fadeIn)
    {
        float fadeOutF = float.Parse(fadeOut);
        float solidF = float.Parse(solid);
        float fadeInF = float.Parse(fadeIn);

        Corutine_Fade = FadeCorutine(fadeOutF, solidF, fadeInF);
        StartCoroutine(Corutine_Fade);

        //wait until fade is done
        LastPartDelay += fadeOutF + solidF + fadeInF;
    }
    
    //Buttons
    public void Continue()
    {
        paused = false;
        isFastingForward = false;
        ContinueButton.gameObject.SetActive(false);
    }

    public void FastForward()
    {
        if(!isScreenFinished)
            isFastingForward = true;
    }

    public void Skip()
    {
        if (StoryManager.loadedStoryIsIntro)
        {
            StoryManager.loadedStoryIsIntro = false;
        }
        else
        {
            EndStory();
        }
    }

    public void TestStory()
    {
        LoadScreen("Screen01");
    }

    //corutines
    IEnumerator FadeCorutine(float fadeOut, float solid, float fadeIn)
    {
        FadeScreen.color = new Color(FadeScreen.color.r, FadeScreen.color.g, FadeScreen.color.b, 0f);
        FadeScreen.gameObject.SetActive(true);

        float alphaPerSecond = 1f / fadeOut;
        float alphaPerFrame = alphaPerSecond / 30f;

        //start fading out
        while (FadeScreen.color.a < 0.999f)
        {
            FadeScreen.color = new Color(FadeScreen.color.r, FadeScreen.color.g, FadeScreen.color.b, FadeScreen.color.a + alphaPerFrame);
            yield return new WaitForSeconds(1f / 30f);
        }

        //set as solid color
        FadeScreen.color = new Color(FadeScreen.color.r, FadeScreen.color.g, FadeScreen.color.b, 1f);
        yield return new WaitForSeconds(solid);

        //start fading in
        alphaPerSecond = 1f / fadeIn;
        alphaPerFrame = alphaPerSecond / 30f;

        while (FadeScreen.color.a > 0f)
        {
            FadeScreen.color = new Color(FadeScreen.color.r, FadeScreen.color.g, FadeScreen.color.b, FadeScreen.color.a - alphaPerFrame);
            yield return new WaitForSeconds(1f / 30f);
        }

        //set as transparent
        FadeScreen.color = new Color(FadeScreen.color.r, FadeScreen.color.g, FadeScreen.color.b, 0f);
        FadeScreen.gameObject.SetActive(false);
    }
}

public struct CommandResult
{
    public bool IsStopping;
}