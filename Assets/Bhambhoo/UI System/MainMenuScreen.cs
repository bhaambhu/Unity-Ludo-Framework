using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Bhambhoo
{
    public class MainMenuScreen : MonoBehaviour
    {
        public UnityEvent BeforeLoadingThisScreen;

        [Header("Fade Animation Properties")]
        public CanvasGroup[] FadableObjects;
        public float fadeDuration = 0.15f;
        public Ease fadeEaseType = Ease.OutQuint;
        public int fadeMoveDistance = 200;
        public float delaysAmongObjects = 0.05f;

        /// <summary>
        /// Calling this will fade-in this screen. This is usually called after any "current screen" has been faded out.
        /// </summary>
        float delay = 0f;
        Tween myTween;

        /// <summary>
        /// This function will fadeout this screen and fade in new screen. Reason why this function is called in the old (current) screen is that we need to fade out this screen, which takes time, and loading of the next screen should happen after this time passes.
        /// </summary>
        /// <param name="newScreen"></param>
        public void SwitchTo(MainMenuScreen newScreen, TweenCallback OnFadeOutComplete)
        {
            // Block screen touches during this transition.
            UIManager.Instance.AllowUITouches(false);

            // Fade out the current screen
            delay = 0;
            myTween = null;
            foreach (CanvasGroup oneItem in FadableObjects)
            {
                if (oneItem == null) continue;

                oneItem.GetComponent<RectTransform>()
                    .DOMoveX(oneItem.GetComponent<RectTransform>().position.x - fadeMoveDistance, fadeDuration)
                    .SetEase(fadeEaseType)
                    .SetDelay(delay)
                    .SetUpdate(true);

                oneItem.alpha = 1;
                myTween = oneItem.DOFade(0, fadeDuration)
                    .SetEase(fadeEaseType)
                    .SetUpdate(true)
                    .SetDelay(delay);
                delay += delaysAmongObjects;
            }

            myTween.OnComplete(new TweenCallback(() =>
            {
                // Disable this screen's gameobject
                gameObject.SetActive(false);

                // Reset UI elements' positions to their original ones,
                // I wonder if this is necessary : TODO
                foreach (CanvasGroup oneItem in FadableObjects)
                {
                    if (oneItem == null) continue;
                    oneItem.GetComponent<RectTransform>()
                        .Translate(new Vector3(fadeMoveDistance, 0, 0));
                    oneItem.alpha = 1;
                }
                OnFadeOutComplete?.Invoke();

                if (newScreen != null)
                    newScreen.LoadThisScreen();
            }));
        }

        /// <summary>
        /// Beware this will also set UIManager.CurrentScreen to the new screen.
        /// </summary>
        public void LoadThisScreen()
        {
            UIManager.Instance.CurrentScreen = this;
            gameObject.SetActive(true);
            delay = 0f;
            myTween = null;

            BeforeLoadingThisScreen.Invoke();

            foreach (CanvasGroup oneItem in FadableObjects)
            {
                if (oneItem == null) continue;

                oneItem.GetComponent<RectTransform>()
                    .DOMoveX(oneItem.GetComponent<RectTransform>().position.x + fadeMoveDistance, fadeDuration)
                    .From()
                    .SetEase(fadeEaseType)
                    .SetDelay(delay)
                    .SetUpdate(true);

                oneItem.alpha = 1;
                myTween = oneItem.DOFade(0, fadeDuration)
                    .From()
                    .SetUpdate(true)
                    .SetEase(fadeEaseType)
                    .SetDelay(delay);
                delay += delaysAmongObjects;
            }
            myTween.OnComplete(new TweenCallback(() =>
            {
                UIManager.Instance.AllowUITouches(true);
            }));
        }
    }
}
