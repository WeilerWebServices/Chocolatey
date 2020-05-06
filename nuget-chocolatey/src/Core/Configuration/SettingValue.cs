﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet
{
    /// <summary>
    /// Represents a single setting value in a settings file
    /// </summary>
    public class SettingValue
    {
        public SettingValue(string key, string value, bool isMachineWide, int priority = 0)
        {
            Key = key;
            Value = value;
            IsMachineWide = isMachineWide;
            Priority = priority;
            AdditionalData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Represents the key of the setting
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Represents the value of the setting
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// IsMachineWide tells if the setting is machine-wide or not
        /// </summary>
        public bool IsMachineWide { get; set; }

        /// <summary>
        /// The priority of this setting in the nuget.config hierarchy. Bigger number means higher priority
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets additional values with the specified setting.
        /// </summary>
        /// <remarks>When reading from an XML based settings file, this includes all attributes on the element
        /// other than the <c>Key</c> and <c>Value</c>.</remarks>
        public IDictionary<string, string> AdditionalData { get; private set; }

        public override bool Equals(object obj)
        {
            var rhs = obj as SettingValue;

            if (rhs != null &&
                string.Equals(rhs.Key, Key, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(rhs.Value, Value, StringComparison.OrdinalIgnoreCase) &&
                rhs.IsMachineWide == rhs.IsMachineWide &&
                rhs.AdditionalData.Count == AdditionalData.Count)
            {
                return Enumerable.SequenceEqual(
                    AdditionalData.OrderBy(data => data.Key, StringComparer.OrdinalIgnoreCase),
                    rhs.AdditionalData.OrderBy(data => data.Key, StringComparer.OrdinalIgnoreCase));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(Key, Value, IsMachineWide).GetHashCode();
        }
    }
}
