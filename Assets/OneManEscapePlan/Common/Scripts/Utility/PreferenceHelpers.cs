/// ©2025 Kevin Foley.
/// See accompanying license file.

using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace OneManEscapePlan.Common.Scripts.Utility
{
    /// <summary>
    /// Helpers to make it easier to work with PlayerPrefs.
    /// </summary>
    /// <example>
    /// public float MusicVolume {
    ///     get => PreferencesHelpers.GetFloat(1);
    ///     set => PrefenceHelpers.SetFloat(value);
    /// }
    /// </example>
    public static class PreferencesHelpers
    {
        public static float GetFloat(float defaultValue, [CallerMemberName] string caller = "")
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(caller));
            return PlayerPrefs.GetFloat(caller, defaultValue);
        }

        public static void SetFloat(float value, [CallerMemberName] string caller = "")
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(caller));
            PlayerPrefs.SetFloat(caller, value);
        }

        public static float GetInt(int defaultValue, [CallerMemberName] string caller = "")
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(caller));
            return PlayerPrefs.GetInt(caller, defaultValue);
        }

        public static void SetInt(int value, [CallerMemberName] string caller = "")
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(caller));
            PlayerPrefs.SetInt(caller, value);
        }

        public static bool GetBool(bool defaultValue, [CallerMemberName] string caller = "")
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(caller));
            int defaultInt = defaultValue ? 1 : 0;
            return PlayerPrefs.GetInt(caller, defaultInt) == 1;
        }

        public static void SetBool(bool value, [CallerMemberName] string caller = "")
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(caller));
            PlayerPrefs.SetInt(caller, value ? 1 : 0);
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
