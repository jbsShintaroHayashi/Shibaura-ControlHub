using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Shibaura_ControlHub.Models;

namespace Shibaura_ControlHub.Services
{
    /// <summary>
    /// INIファイルの読み込みクラス
    /// </summary>
    public class IniFileReader
    {
        private readonly Dictionary<string, Dictionary<string, string>> _sections = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// INIファイルを読み込む
        /// </summary>
        public static IniFileReader LoadFromFile(string filePath)
        {
            var reader = new IniFileReader();
            reader.Load(filePath);
            return reader;
        }

        /// <summary>
        /// INIファイルを読み込む
        /// </summary>
        private void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            string currentSection = "";
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // 空行またはコメント行をスキップ
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                // セクション行
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    if (!_sections.ContainsKey(currentSection))
                    {
                        _sections[currentSection] = new Dictionary<string, string>();
                    }
                }
                // キー=値の行
                else if (trimmedLine.Contains("="))
                {
                    int equalIndex = trimmedLine.IndexOf('=');
                    string key = trimmedLine.Substring(0, equalIndex).Trim();
                    string value = trimmedLine.Substring(equalIndex + 1).Trim();

                    if (!_sections.ContainsKey(currentSection))
                    {
                        _sections[currentSection] = new Dictionary<string, string>();
                    }

                    _sections[currentSection][key] = value;
                }
            }
        }

        /// <summary>
        /// 値を取得
        /// </summary>
        public string GetValue(string section, string key, string defaultValue = "")
        {
            if (_sections.ContainsKey(section) && _sections[section].ContainsKey(key))
            {
                return _sections[section][key];
            }
            return defaultValue;
        }

        /// <summary>
        /// セクションが存在するか確認
        /// </summary>
        public bool HasSection(string section)
        {
            return _sections.ContainsKey(section);
        }

        /// <summary>
        /// モード設定を読み込む
        /// </summary>
        public ModeConfiguration LoadModeConfiguration()
        {
            var config = new ModeConfiguration
            {
                Mode1Name = GetValue("ModeConfig", "Mode1_Name", "授業"),
                Mode2Name = GetValue("ModeConfig", "Mode2_Name", "遠隔"),
                Mode3Name = GetValue("ModeConfig", "Mode3_Name", "e-sports")
            };

            return config;
        }

        /// <summary>
        /// モード名を取得
        /// </summary>
        public string GetModeName(int modeNumber)
        {
            return GetValue("ModeConfig", $"Mode{modeNumber}_Name", $"モード{modeNumber}");
        }
    }
}

