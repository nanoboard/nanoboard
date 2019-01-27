using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using System.Linq;

namespace NServer
{
    /*
        Manages configuration storage.
        Configuration is key-value pairs kept in configuration json file within app's directory.
        Writing to file happens immediately after setting a value.
    */
    class Configurator
    {
        public const string DefaultPass = "nano3";
        private const string ConfigFileName = "config-3.json";
        public static readonly Configurator Instance = new Configurator();
        private Dictionary<string, string> _keyValues = new Dictionary<string, string>();

        private Configurator()
        {
            if (File.Exists(ConfigFileName))
            {
                _keyValues = JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(ConfigFileName));  
            }
        }

        public string[] GetParams()
        {
            return _keyValues.Keys.ToArray();
        }

        public bool HasValue(string key)
        {
            return _keyValues.ContainsKey(key);
        }

        public string GetValue(string key, string defaultValue)
        {
            if (_keyValues.ContainsKey(key)) 
                return _keyValues[key];
            SetValue(key, defaultValue);
            return defaultValue;
        }

        public void SetValue(string key, string value)
        {
            _keyValues[key] = value;
            var config = JsonConvert.SerializeObject(_keyValues, Formatting.Indented);
            File.WriteAllText(ConfigFileName, config);
        }
    }
}
