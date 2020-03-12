using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryScreen : MonoBehaviour
{
    [TextArea(10, 999)]
    public string Dialog;

    public Sprite BackgroundImage;
    public Music? Music;
    public bool StopMusic;
}