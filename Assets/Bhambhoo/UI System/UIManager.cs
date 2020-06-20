using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

namespace Bhambhoo
{
    /// <summary>
    /// Before using this, please set these:
    /// Canvas Scaler Parameters:
    /// UI Scale Mode: Scale With Screen Size
    /// Reference Resolution: 1080x1920
    /// Screen Match Mode: Match Width or Height
    /// Match Width: 0
    /// Reference Pixels Per Unit: 100
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
        public GameObject TouchBlockerPlane;

        public MainMenuScreen[] MainMenuScreens;
        public MainMenuScreen CurrentScreen;

        [Header("Sign In Buttons")]
        public Color signedOutColor;
        public Color signedInColor;
        public string textIfSignedOut = "Sign In With Google Play",
            textIfSignedIn = "Sign Out";
        public Text[] SignInButtonTexts;
        public Image[] SignInButtonBackgroundImages;
        public Text[] PlayerNameTexts;
        public Image[] ProfilePicImages;

        private void OnEnable()
        {
            if (Instance == null)
                Instance = this;
            else
                Debug.LogError("Two UIManagers exist, either solve instancing by destroying a new object if instance is already present, or find out why they're instancing again.");

            foreach (MainMenuScreen oneScreen in MainMenuScreens)
            {
                oneScreen.gameObject.SetActive(false);
            }
            CurrentScreen.LoadThisScreen();
        }

        public void ButtonClickTester()
        {
            Debug.Log("Button Click Detected");
        }

        public InputField debugInputField;
        public void DebugInputField(string text)
        {
            Debug.Log("Setting debug input field");
            debugInputField.text = text;
        }
        
        public void SwitchMainMenuTo(int index)
        {
            SwitchMainMenuTo(MainMenuScreens[index], null);
        }

        public void SwitchMainMenuTo(MainMenuScreen newScreen, TweenCallback OnFadeOutComplete)
        {
            CurrentScreen.SwitchTo(newScreen, OnFadeOutComplete);
        }

        public void AllowUITouches(bool allow)
        {
            TouchBlockerPlane.SetActive(!allow);
        }

        // Centralized Updates
        public void SetUserNameDisplay(string username)
        {
            Debug.Log("Setting username display to "+username);
            foreach (Text oneText in PlayerNameTexts)
            {
                oneText.text = username;
            }
        }

        public void SetProfilePicDisplay(string uri)
        {
            Debug.Log("Setting profile pic display");
            DownloadAndSetImage(uri, ProfilePicImages);
        }

        // Update, not downloading image anymore, using avatar icon selection now
        public void DownloadAndSetImage(string url, Image[] targets)
        {
            Debug.Log("starting coroutine for downloading image");
            StartCoroutine(DownloadImageCoroutine(url, targets));
        }

        IEnumerator DownloadImageCoroutine(string url, Image[] targets)
        {
            string chopped = url.Substring(4);
            //url = "https" + chopped;
            Debug.Log("Downloading... url = " + url);
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();

                Debug.Log("after send request...url= " + url);

                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    Debug.LogError(uwr.error);
                }
                else
                {
                    // Get downloaded asset bundle
                    var texture = DownloadHandlerTexture.GetContent(uwr);
                    Sprite finalSprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0f, 0f));

                    foreach (Image oneImage in targets)
                    {
                        oneImage.overrideSprite = finalSprite;
                        oneImage.SetAllDirty();
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class MenuScreen
    {
        public string screenTitle;
        public MenuListItem[] menuListItems;
        public UnityEvent BeforeFadeIn;
    }

    [System.Serializable]
    public class MenuListItem
    {
        public string Title;
        public Sprite Icon;
        public ButtonClickedEvent buttonClickedEvent;
    }

    /// <summary>
    /// Helper method to scale any rect transform
    /// </summary>
    public static class RectTransformExtensions
    {
        public static void SetLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }
    }

}