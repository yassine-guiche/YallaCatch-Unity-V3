using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YallaCatch.Models;

namespace YallaCatch.API
{
    public class GamificationAPI : MonoBehaviour
    {
        public static GamificationAPI Instance { get; private set; }


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public IEnumerator GetAchievements(Action<List<Achievement>> callback)
        {
            yield return APIClient.Instance.Get<List<Achievement>>(APIEndpoints.GAME_CHALLENGES, (response) =>
            {
                if (response.success)
                {
                    callback?.Invoke(response.data);
                }
                else
                {
                    Debug.LogError("Failed to load achievements: " + response.message);
                    callback?.Invoke(null);
                }
            });
        }

        public IEnumerator UnlockAchievement(string achievementId, Action<bool, int> callback)
        {
            string endpoint = APIEndpoints.Format(APIEndpoints.GAME_CHALLENGE_COMPLETE, achievementId);
            yield return APIClient.Instance.Post<UnlockResponse>(endpoint, null, (response) =>
            {
                if (response.success)
                {
                    callback?.Invoke(true, response.data.pointsAwarded);
                }
                else
                {
                    Debug.LogError("Failed to unlock achievement: " + response.message);
                    callback?.Invoke(false, 0);
                }
            });
        }
    }

    [Serializable]
    public class UnlockResponse
    {
        public bool success;
        public int pointsAwarded;
        public string message;
    }
}
