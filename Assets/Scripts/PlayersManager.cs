using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.bhambhoo.fairludo
{

    public class PlayersManager : MonoBehaviour
    {
        public static PlayersManager Instance;
        public static List<Player> Players = new List<Player>();

        private void OnEnable()
        {
            Instance = this;
        }

        public bool ChangePlayerType(Player player, Constants.PlayerType newType)
        {
            if (!MatchManager.MatchRunning)
            {
                Debug.LogError("Match not running.");
                return false;
            }

            if (player != null)
            {
                player.type = newType;
                Debug.LogWarning(player.name + "'s playerType changed to " + newType);
                return true;
            }
            else
            {
                Debug.LogError("Player does not exist.");
                return false;
            }
        }

        public void Initialize(int numberOfPlayers, Constants.MatchType matchType)
        {
            Players.Clear();

            // Initialize Player 1 which will always be there
            Players.Add(new Player(1, Constants.PlayerType.LocalPlayer, "Sanjay"));

            Constants.PlayerType restPlayerTypes;
            switch (matchType)
            {
                case Constants.MatchType.VsComputer:
                    restPlayerTypes = Constants.PlayerType.Bot;
                    break;
                case Constants.MatchType.Online:
                    restPlayerTypes = Constants.PlayerType.OnlinePlayer;
                    break;
                case Constants.MatchType.PassNPlay:
                    restPlayerTypes = Constants.PlayerType.LocalPlayer;
                    break;
                default:
                    restPlayerTypes = Constants.PlayerType.Bot;
                    break;
            }

            Players.Add(new Player(3, restPlayerTypes));
            if (numberOfPlayers > 2)
            {
                Players.Add(new Player(2, restPlayerTypes));
                if (numberOfPlayers > 3)
                {
                    Players.Add(new Player(4, restPlayerTypes));
                }
            }
        }

        public static Player GetPlayer(byte playerIndex)
        {
            foreach (Player onePlayer in Players)
            {
                if (onePlayer.playerIndex == playerIndex)
                    return onePlayer;
            }
            return null;
        }

        static List<PlayerToken> tokensAt = new List<PlayerToken>();
        /// <summary>
        /// Waypoint indexes are relative to player index. Get tokens that are already present at a certain waypoint.
        /// </summary>
        /// <param name="waypointIndex"></param>
        /// <param name="withRespectTo"></param>
        /// <returns></returns>
        public static List<PlayerToken> GetTokensAt(int givenWaypointIndex, Player withRespectTo)
        {
            tokensAt.Clear();
            //Debug.Log("Searching tokens at player " + withRespectTo + "'s " + givenWaypointIndex + "th waypoint.");
            // The differences between player waypoints is 13.
            // Therefore if withRespectTo is player 3, and we want to know player 1's tokens, we take the difference (|1-3| = 2), therefore their waypoint index is 2x13 + givenWaypointIndex.
            int waypointDifference;
            foreach (Player onePlayer in Players)
            {
                waypointDifference = withRespectTo.playerIndex - onePlayer.playerIndex;
                if (waypointDifference < 0)
                    waypointDifference = 4 + waypointDifference;
                waypointDifference = waypointDifference * 13 + givenWaypointIndex;
                if (waypointDifference > 51)
                    waypointDifference = waypointDifference - 52;
                // Now withRespectTo's givenWaypointIndex is onePlayer's waypointDifference waypoint index.
                //Debug.Log("wrt player " + onePlayer.playerIndex + ", it's waypoint " + waypointDifference);
                foreach (PlayerToken oneToken in onePlayer.playerTokens)
                {
                    if (oneToken.localWaypointIndex == waypointDifference)
                        tokensAt.Add(oneToken);
                }
            }
            return tokensAt;
        }
    }

    public class Player
    {
        public Constants.PlayerType type;
        public byte playerIndex = 1;
        public string name = "Sanjay";
        public PlayerToken[] playerTokens = new PlayerToken[4];
        public GameObject turnHighlighter
        {
            get
            {
                return Constants.Instance.PlayerTurnHighlighters[playerIndex - 1];

            }
        }
        public bool IsLocal
        {
            get
            {
                return type == Constants.PlayerType.LocalPlayer;
            }
        }

        public Player(byte playerIndex, Constants.PlayerType playerType, string displayName = "")
        {
            this.playerIndex = playerIndex;
            type = playerType;

            if (displayName == "")
                displayName = Constants.Instance.BotNames[playerIndex - 1];
            name = displayName;

            int i = 0;
            foreach (Transform oneBase in Constants.Instance.GetBases(playerIndex))
            {
                playerTokens[i] = GameObject.Instantiate(Constants.Instance.playerToken).GetComponent<PlayerToken>().Initialize(this, oneBase, Constants.Instance.GetTokenColor(playerIndex));
                i++;
            }

            Debug.Log("Initialized " + name);
        }
    }
}