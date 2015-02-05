﻿using Newtonsoft.Json.Linq;
using Summerset.SemanticVersion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IO = System.IO;

namespace KarmaTestAdapter.Commands
{
    public class KarmaVsReporter
    {
        public KarmaVsReporter(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentNullException("directory");
            }

            Directory = directory;
            Reporter = IO.Path.Combine(Directory, "node_modules", "karma-vs-reporter", "karma-vs-reporter");
            ConfigFile = IO.Path.Combine(Directory, "node_modules", "karma-vs-reporter", "package.json");
            Timestamp = Exists ? IO.File.GetLastWriteTime(ConfigFile) : (DateTime?)null;
        }

        public string Reporter { get; private set; }
        public string ConfigFile { get; private set; }
        public string Directory { get; private set; }
        public DateTime? Timestamp { get; private set; }

        private ISemanticVersion _version = null;
        public ISemanticVersion Version
        {
            get
            {
                if (Exists)
                {
                    var newTimestamp = GetTimestamp();
                    if (_version == null || newTimestamp != Timestamp)
                    {
                        Timestamp = newTimestamp;
                        var config = JObject.Parse(IO.File.ReadAllText(ConfigFile));
                        JToken version;
                        if (config.TryGetValue("version", out version) && version.Type == JTokenType.String)
                        {
                            _version = new SemanticVersion(version.ToString());
                        }
                        else
                        {
                            _version = null;
                        }
                    }
                }
                else
                {
                    _version = null;
                }
                return _version;
            }
        }

        public bool Exists { get { return IO.File.Exists(Reporter) && IO.File.Exists(ConfigFile); } }

        private DateTime? GetTimestamp()
        {
            return Exists ? IO.File.GetLastWriteTime(ConfigFile) : (DateTime?)null;
        }

        private ISemanticVersion GetVersion(ISemanticVersion previousVersion)
        {
            if (Exists)
            {
                var newTimestamp = GetTimestamp();
                if (previousVersion == null || newTimestamp != Timestamp)
                {
                    Timestamp = newTimestamp;
                    var config = JObject.Parse(IO.File.ReadAllText(ConfigFile));
                    JToken version;
                    if (config.TryGetValue("version", out version))
                    {
                        if (version.Type == JTokenType.String)
                        {
                            return new SemanticVersion(version.ToString());
                        }
                    }
                }
            }
            return null;
        }
    }
}