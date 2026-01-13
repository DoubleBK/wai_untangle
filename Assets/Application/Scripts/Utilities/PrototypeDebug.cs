using UnityEngine;

namespace Game.Utilities
{
    /// <summary>
    /// 프로토타입용 디버깅 도구
    /// 빌드 시 디버그 로그 자동 비활성화
    /// </summary>
    public static class PrototypeDebug
    {
        private static bool debugMode = true;

        public static void Log(string message, Object context = null)
        {
            if (debugMode)
                Debug.Log($"[PROTOTYPE] {message}", context);
        }

        public static void LogWarning(string message, Object context = null)
        {
            if (debugMode)
                Debug.LogWarning($"[PROTOTYPE] {message}", context);
        }

        public static void LogError(string message, Object context = null)
        {
            Debug.LogError($"[PROTOTYPE] {message}", context);
        }

        public static void SetDebugMode(bool enabled)
        {
            debugMode = enabled;
        }
    }
}
