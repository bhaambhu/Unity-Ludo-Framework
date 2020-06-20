using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.bhambhoo.fairludo
{
    /// <summary>
    /// By Sanjay.
    /// </summary>
    public class LudoAI : MonoBehaviour
    {
        public static LudoAI Instance;

        private void OnEnable()
        {
            Instance = this;
        }

        public static float delayBeforeRollingDice = 0.5f;
        public static float delayInChoosingToken = 0.5f;
        public static float delayPlusMinus = 0.5f;

        /*
         * When AI has to make a decision on which token to pick, it can do so by giving some decisions preference over others.
         * The order of preference is:
         * 1) If it's a 6, it's preffered to take a token out of home, rather than advancing token.
         * 2) 
         * 
         * The choices:
         * 1) When it's a 6, whether to advance token or move token out of home.
         * 2) To advance a token which is behind another token.
         * 3) To advance a token which has nothing ahead of it (there's no other token in next 5 blocks, if it's in 6th block it's ok).
         * 4) To advance a token, advancing which, will kill another player's token.
         * 5) If a token is standing at a safe waypoint, try letting that token stay there and move some other token.
         * 
         * 6) When multiple tokens are out on path but none is near the opponent, 
         * choose the one that is near the opponent (if previous one is 10 steps away and next one is 8, 
         * move previous one).
         */

        public void PlayTurn()
        {
            StartCoroutine(PlayAITurn());
        }

        IEnumerator PlayAITurn()
        {
            // Wait before rolling the dice
            yield return new WaitForSeconds(delayBeforeRollingDice + Random.Range(-0.5f, 0.5f));

            // Roll the dice
            Dice.Instance.BotDiceRoll();

            // Now rest of the call will be made by dice, when it stops rolling.
            yield return null;
        }

        public void ChooseToken(List<PlayerToken> tokensWeCanMove, int diceResult)
        {
            StartCoroutine(AIChooseToken(tokensWeCanMove, diceResult));
        }

        IEnumerator AIChooseToken(List<PlayerToken> tokensWeCanMove, int diceResult)
        {
            // Wait before choosing appropriate token
            yield return new WaitForSeconds(delayInChoosingToken + Random.Range(-0.5f, 0.5f));

            // Choose appropriate token
            // TODO improve this, right now we're doing it randomly
            tokensWeCanMove[Random.Range(0, tokensWeCanMove.Count)].Move(diceResult);

            // Done!
            yield return null;
        }

        private void Start()
        {
            if (delayPlusMinus > delayBeforeRollingDice
                || delayPlusMinus > delayInChoosingToken)
            {
                Debug.LogWarning("delayPlusMinus is larger than other delays. It results in negative time. Fix it.");
                delayPlusMinus = Mathf.Min(delayBeforeRollingDice, delayInChoosingToken);
            }
        }
    }
}