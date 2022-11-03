using System;
using System.Collections.Generic;
using System.Reflection;

namespace P4T {
    internal class Config {
        protected Utilities.Profile profile;
        protected struct ConfigValue {
            private string _value;
            public bool Modified { get; private set; }
            public string Value {
                get => _value;
                set {
                    if (_value != value) {
                        _value = value;
                        Modified = true;
                    }
                }
            }
            public ConfigValue(string original) {
                Modified = false;
                _value = original;
            }
        }
        protected SortedDictionary<string, SortedDictionary<string, ConfigValue>> cached;
        public Config(string ProfileFile) {
            profile = new Utilities.Profile(ProfileFile);
            cached = new SortedDictionary<string, SortedDictionary<string, ConfigValue>>();
        }
        ~Config() {
            SaveProfile();
        }
        public void SaveProfile() {
            foreach (string section in cached.Keys) {
                if (cached[section] != null) {
                    SortedDictionary<string, ConfigValue> keyValuePairs = cached[section];
                    foreach (string key in keyValuePairs.Keys) {
                        ConfigValue pv = keyValuePairs[key];
                        if (pv.Modified) {
                            profile.WriteValue(section, key, pv.Value);
                        }
                    }
                }
            }
        }

        public string this[string section, string key] {
            get {
                SortedDictionary<string, ConfigValue> sectionKvp;
                if (cached.ContainsKey(section)) {
                    sectionKvp = cached[section];
                    if (sectionKvp == null) {
                        sectionKvp = new SortedDictionary<string, ConfigValue>();
                        cached[section] = sectionKvp;
                    }
                }
                else {
                    sectionKvp = new SortedDictionary<string, ConfigValue>();
                    cached.Add(section, sectionKvp);
                }
                if (sectionKvp.ContainsKey(key)) {
                    return sectionKvp[key].Value;
                }
                else {
                    string data = profile.ReadValue(section, key);
                    sectionKvp.Add(key, new ConfigValue(data));
                    return data;
                }
            }
            set {
                SortedDictionary<string, ConfigValue> sectionKvp;
                if (cached.ContainsKey(section)) {
                    sectionKvp = cached[section];
                    if (sectionKvp == null) {
                        sectionKvp = new SortedDictionary<string, ConfigValue>();
                        cached[section] = sectionKvp;
                    }
                }
                else {
                    sectionKvp = new SortedDictionary<string, ConfigValue>();
                    cached.Add(section, sectionKvp);
                }
                ConfigValue pv;
                if (sectionKvp.ContainsKey(key)) {
                    pv = sectionKvp[key];
                }
                else {
                    pv = new ConfigValue(null);
                }
                pv.Value = value;
                sectionKvp[key] = pv;
            }
        }
        public string GetString(string Section, string Key, string DefaultValue) {
            string result = this[Section, Key];
            if (result == null) {
                return this[Section, Key] = DefaultValue;
            }
            return result;
        }
        public T GetValue<T>(string Section, string Key, T DefaultValue) {
            Type type = typeof(T);
            MethodInfo method = type.GetMethod("TryParse", new Type[] { typeof(string), type.MakeByRefType() });
            if (method == null) {
                return DefaultValue;
            }
            object[] parameters = new object[] { this[Section, Key], null };
            if ((bool)method.Invoke(null, parameters)) {
                return (T)parameters[1];
            }
            this[Section, Key] = DefaultValue.ToString();
            return DefaultValue;
        }
        public void SetValue<T>(string Section, string Key, T TargetValue) {
            this[Section, Key] = TargetValue.ToString();
        }
    }
}
