using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public StoryManager StoryManager;
    public StoryPanel StoryPanel;
    public string FirstScreen;

    void Start()
    {
        StoryPanel.LoadScreen(FirstScreen);
    }

}
