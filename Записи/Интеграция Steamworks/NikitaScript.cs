using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using UnityEngine;

namespace Assets.Scripts.Steamwork.NET
{
    public class SteamController : MonoBehaviour
    {
        private static SteamController instance;

        private bool isSteamInit
        {
            get
            {
                if (SteamManager.Initialized)
                    return true;

                GameObject steamObj = new GameObject("Steam");
                steamObj.AddComponent<SteamManager>();
                DontDestroyOnLoad(steamObj);
                return SteamManager.Initialized;
            }
        }

        public static SteamController Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject steamObj = new GameObject("Steam");
                    instance = steamObj.AddComponent<SteamController>();
                    DontDestroyOnLoad(steamObj);
                }

                return instance;
            }
        }

        protected Callback<GameOverlayActivated_t> m_GameOverlayActivated;

        // Use this for initialization
        void OnEnable()
        {
            if (isSteamInit)
            {
                m_GameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
                m_LeaderboardFindResult = CallResult<LeaderboardFindResult_t>.Create(LeaderboardFind);
                m_LeaderboardFindResultForGet = CallResult<LeaderboardFindResult_t>.Create(LeaderboardFindForGet);
                m_LeaderboardScoreUploaded = CallResult<LeaderboardScoreUploaded_t>.Create(LeaderboardScoreUploaded);
                m_LeaderboardScoresDownloaded = CallResult<LeaderboardScoresDownloaded_t>.Create(LeaderboardScoresDownloaded);
            }
        }

        public string GetUserName()
        {
            if (isSteamInit)
                return SteamFriends.GetPersonaName();

            return null;
        }

        #region Overlay

        private float oldTimeScale;

        protected void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
        {
            if (pCallback.m_bActive != 0)
            {
                oldTimeScale = Time.timeScale;
                Time.timeScale = 0;
            }
            else
            {
                Time.timeScale = oldTimeScale;
            }
        }

        #endregion

        #region SetLeaderboard

        private Queue<KeyValuePair<string, int>> newScores = new Queue<KeyValuePair<string, int>>();

        private CallResult<LeaderboardFindResult_t> m_LeaderboardFindResult;

        protected void LeaderboardFind(LeaderboardFindResult_t pCallback, bool bIOFailure)
        {
            if (pCallback.m_bLeaderboardFound != 1 || bIOFailure)
            {
                Debug.LogError("LeaderBoard not found " + newScores.Dequeue());
            }
            else
            {
                SteamAPICall_t handle = SteamUserStats.UploadLeaderboardScore(pCallback.m_hSteamLeaderboard,
                    ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, newScores.Dequeue().Value,
                    new int[0], 0);
                m_LeaderboardScoreUploaded.Set(handle);
            }
        }

        private CallResult<LeaderboardScoreUploaded_t> m_LeaderboardScoreUploaded;
        protected void LeaderboardScoreUploaded(LeaderboardScoreUploaded_t pCallback, bool bIOFailure)
        {
            if (pCallback.m_bSuccess != 1 || bIOFailure)
            {
                Debug.LogError("Can't upload stats");
            }
            else
            {
                SteamUserStats.StoreStats();
                //TODO Show raiting
            }
        }

        public void SaveResult(string setting, int result)
        {
            if (isSteamInit)
            {
                newScores.Enqueue(new KeyValuePair<string, int>(setting, result));
                SteamAPICall_t handle = SteamUserStats.FindOrCreateLeaderboard(setting,
                    ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending,
                    ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
                m_LeaderboardFindResult.Set(handle);
            }
        }

        #endregion

        #region GetLeaderboard
        //TODO: Rewrite, for cancel request

        private const int size = 10;
        private bool isReady;

        private ScoreRequestPattern currentPattern;
        private CallResult<LeaderboardFindResult_t> m_LeaderboardFindResultForGet;
        private LeaderBoardRecord[] requestedRecords;

        protected void LeaderboardFindForGet(LeaderboardFindResult_t pCallback, bool bIOFailure)
        {
            if (pCallback.m_bLeaderboardFound != 1 || bIOFailure)
            {
                Debug.LogError("LeaderBoard not found for" + currentPattern);
            }
            else
            {
                SteamAPICall_t handle = new SteamAPICall_t();
                switch (currentPattern)
                {
                    case ScoreRequestPattern.Top:
                        handle = SteamUserStats.DownloadLeaderboardEntries(pCallback.m_hSteamLeaderboard,
                            ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 0, size);
                        break;
                    case ScoreRequestPattern.Around:
                        handle = SteamUserStats.DownloadLeaderboardEntries(pCallback.m_hSteamLeaderboard,
                            ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, -(size / 2) + 1, size / 2);
                        break;
                    case ScoreRequestPattern.Friends:
                        handle = SteamUserStats.DownloadLeaderboardEntries(pCallback.m_hSteamLeaderboard,
                            ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends, -size / 2, size / 2);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                m_LeaderboardScoresDownloaded.Set(handle);
            }
        }

        private CallResult<LeaderboardScoresDownloaded_t> m_LeaderboardScoresDownloaded;

        private void LeaderboardScoresDownloaded(LeaderboardScoresDownloaded_t pCallback, bool bIOFailure)
        {
            if (bIOFailure)
            {
                Debug.LogError("Can't load scores");
            }
            else
            {
                requestedRecords = new LeaderBoardRecord[pCallback.m_cEntryCount];
                for (int i = 0; i < pCallback.m_cEntryCount; i++)
                {
                    LeaderboardEntry_t entity;
                    SteamUserStats.GetDownloadedLeaderboardEntry(pCallback.m_hSteamLeaderboardEntries, i, out entity,
                        new int[0], 0);
                    requestedRecords[i] = new LeaderBoardRecord(entity);
                }
                isReady = true;
            }
        }

        public void RequestScores(string setting, ScoreRequestPattern pattern)
        {
            if (isSteamInit)
            {
                isReady = false;
                m_LeaderboardScoresDownloaded.Cancel();
                currentPattern = pattern;
                SteamAPICall_t handle = SteamUserStats.FindOrCreateLeaderboard(setting,
                    ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending,
                    ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
                m_LeaderboardFindResultForGet.Set(handle);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// Null, if not ready
        /// </returns>
        public LeaderBoardRecord[] GetDownloaded()
        {
            if (isSteamInit)
            {
                if (!isReady)
                    return null;

                return requestedRecords;
            }
            return null;
        }

        public enum ScoreRequestPattern
        {
            Top,
            Around,
            Friends
        }

        public class LeaderBoardRecord
        {
            public int Rank;
            public int Value;
            public string Name;

            public LeaderBoardRecord(LeaderboardEntry_t entity)
            {
                Rank = entity.m_nGlobalRank;
                Value = entity.m_nScore;
                Name = SteamFriends.GetFriendPersonaName(entity.m_steamIDUser);
            }
        }

        #endregion

        #region Achivement

        public void SetAchivement(string name)
        {
            Debug.Log("Get - " + name);
            if (isSteamInit)
            {
                bool isAchivementAlreadyGet;
                if (SteamUserStats.GetAchievement(name, out isAchivementAlreadyGet) && !isAchivementAlreadyGet)
                {
                    SteamUserStats.SetAchievement(name);
                    SteamUserStats.StoreStats();
                }
            }
        }

        #endregion
    }
}