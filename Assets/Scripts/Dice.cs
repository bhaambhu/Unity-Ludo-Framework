using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.bhambhoo.fairludo
{
    public class Dice : MonoBehaviour
    {
        public Sprite[] diceSides;
        SpriteRenderer rend;

        bool coroutineAllowed = true;

        public PlayerToken testToken;

        public bool biasedDice = false;
        public int biasedOutcome = 1;
        public static bool RollAllowed = false;
        public static Dice Instance;

        // Start is called before the first frame update
        void Start()
        {
            rend = GetComponent<SpriteRenderer>();
            diceSides = Resources.LoadAll<Sprite>("DiceSides/");
            rend.sprite = diceSides[5];
        }

        private void OnEnable()
        {
            Instance = this;
        }

        public static void Highlight(bool on)
        {
            Constants.Instance.DiceTurnHighlight.SetActive(on);
        }

        private void OnMouseDown()
        {
            if (RollAllowed)
            {
                if (MatchManager.InputAllowed && coroutineAllowed)
                    StartCoroutine("RollTheDice");
            }
        }

        public void BotDiceRoll()
        {
            if (coroutineAllowed)
                StartCoroutine("RollTheDice");
        }

        IEnumerator RollTheDice()
        {
            RollAllowed = false;
            coroutineAllowed = false;
            Constants.Instance.DiceTurnHighlight.SetActive(false);
            int randomDiceSide = 0;
            for (int i = 0; i <= Constants.diceRollShuffles; i++)
            {
                randomDiceSide = Random.Range(0, 6);
                rend.sprite = diceSides[randomDiceSide];
                SanUtils.PlaySound(Constants.Instance.sfxDiceRoll, MatchManager.Instance.audioSource);
                yield return new WaitForSeconds(Constants.diceRollTime / Constants.diceRollShuffles);
            }

            // TODO delete before release
            if (biasedDice)
            {
                SanUtils.PlaySound(Constants.Instance.sfxDiceRoll, MatchManager.Instance.audioSource);
                randomDiceSide = biasedOutcome - 1;
                rend.sprite = diceSides[randomDiceSide];
            }

            yield return new WaitForSeconds(Constants.idleTimeAfterDiceRoll);

            MatchManager.DiceResult = randomDiceSide + 1;

            // Logic to allow user to select a token, and move that token
            // TODO
            if (testToken)
                testToken.Move(MatchManager.DiceResult);

            if (MatchManager.DiceResult == 6)
                SanUtils.PlaySound(Constants.Instance.sfxLocalPlayer6);

            MatchManager.Instance.OnDiceRolledLocally(randomDiceSide + 1);
            coroutineAllowed = true;
        }
    }
}