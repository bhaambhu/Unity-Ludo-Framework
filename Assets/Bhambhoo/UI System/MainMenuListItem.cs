using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

namespace Bhambhoo
{
    public class MainMenuListItem : MonoBehaviour
    {
        public Text Title;
        public Image Icon;
        public Button button;

        public RectTransform titleRectTransform;

        public void Enable(string title, ButtonClickedEvent onClick, Sprite icon = null)
        {
            Title.text = title;
            gameObject.SetActive(true);

            if(icon == null)
            {
                Icon.enabled = false;
                titleRectTransform.SetLeft(20);
            }
            else
            {
                Icon.overrideSprite = icon;
                titleRectTransform.SetLeft(200);
                Debug.Log("Set left offset to 200 in " + title);
                Icon.enabled = true;
            }

            button.onClick = onClick;
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }
    }
}