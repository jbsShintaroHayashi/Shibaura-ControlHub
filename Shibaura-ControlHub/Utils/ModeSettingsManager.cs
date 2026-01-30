using System;
using System.IO;
using System.Windows;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Services;
using Shibaura_ControlHub.Views;

namespace Shibaura_ControlHub.Utils
{
    /// <summary>
    /// モードごとの設定ファイル管理クラス
    /// Mode1、Mode2、Mode3それぞれのJSONファイルの読み込み・保存処理を行う
    /// 内部処理ではモード番号（1、2、3）で扱う
    /// </summary>
    public static class ModeSettingsManager
    {
        private static readonly string SettingsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        private static readonly JsonFileService _jsonFileService = new JsonFileService();

        /// <summary>
        /// モード名からモード番号を取得
        /// </summary>
        /// <param name="modeName">モード名（"授業"、"遠隔"、"e-sports"など）</param>
        /// <returns>モード番号（1、2、3）。該当しない場合は0</returns>
        public static int GetModeNumber(string modeName)
        {
            // モード名とモード番号のマッピング
            return modeName switch
            {
                "授業" => 1,
                "遠隔" => 2,
                "e-sports" => 3,
                _ => 0
            };
        }

        /// <summary>
        /// モード番号からモード名を取得
        /// </summary>
        /// <param name="modeNumber">モード番号（1、2、3）</param>
        /// <returns>モード名。該当しない場合は空文字列</returns>
        public static string GetModeName(int modeNumber)
        {
            // モード番号とモード名のマッピング
            return modeNumber switch
            {
                1 => string.IsNullOrEmpty(App.ModeConfig.Mode1Name) ? "授業" : App.ModeConfig.Mode1Name,
                2 => string.IsNullOrEmpty(App.ModeConfig.Mode2Name) ? "遠隔" : App.ModeConfig.Mode2Name,
                3 => string.IsNullOrEmpty(App.ModeConfig.Mode3Name) ? "e-sports" : App.ModeConfig.Mode3Name,
                _ => string.Empty
            };
        }

        /// <summary>
        /// モード番号からファイル名を取得
        /// 内部処理ではモード番号（1、2、3）をそのままファイル名に使用
        /// </summary>
        /// <param name="modeNumber">モード番号（1、2、3）</param>
        /// <returns>ファイル名（例: "1"、"2"、"3"）</returns>
        private static string GetEnglishFileNameFromModeNumber(int modeNumber)
        {
            // モード番号をそのままファイル名に使用（例: 1 → "1", 2 → "2", 3 → "3"）
            return modeNumber.ToString();
        }

        /// <summary>
        /// モード設定ファイルのパスを取得（モード番号から）
        /// 内部処理ではモード番号（1、2、3）を使用し、ファイル名は"1_settings.json"、"2_settings.json"、"3_settings.json"となる
        /// </summary>
        /// <param name="modeNumber">モード番号（1、2、3）</param>
        /// <returns>ファイルパス（例: "C:\cnf\1_settings.json"）</returns>
        public static string GetModeSettingsFilePath(int modeNumber)
        {
            var fileName = GetEnglishFileNameFromModeNumber(modeNumber);
            return Path.Combine(SettingsDirectory, $"{fileName}_settings.json");
        }

        /// <summary>
        /// モード設定ファイルのパスを取得（モード名から）
        /// </summary>
        /// <param name="mode">モード名</param>
        /// <returns>ファイルパス</returns>
        public static string GetModeSettingsFilePath(string mode)
        {
            // モード名から英語ファイル名に変換
            var englishFileName = GetEnglishFileName(mode);
            return Path.Combine(SettingsDirectory, $"{englishFileName}_settings.json");
        }

        /// <summary>
        /// モード名を英語ファイル名に変換
        /// </summary>
        private static string GetEnglishFileName(string modeName)
        {
            // モード名と英語ファイル名のマッピング
            return modeName switch
            {
                "授業" => "lecture",
                "遠隔" => "remote",
                "e-sports" => "esports",
                _ => MakeSafeFileName(modeName) // その他の場合は安全なファイル名に変換
            };
        }

        /// <summary>
        /// ファイル名に使用可能な文字列に変換
        /// </summary>
        private static string MakeSafeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        /// <summary>
        /// 設定ディレクトリが存在するか確認し、存在しない場合は作成
        /// </summary>
        private static void EnsureSettingsDirectoryExists()
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                try
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }
                catch (Exception ex)
                {
                    CustomDialog.Show(
                        $"設定ディレクトリの作成に失敗しました: {ex.Message}\nパス: {SettingsDirectory}",
                        "設定ディレクトリ作成エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        /// <summary>
        /// モード設定を読み込む（モード番号から）
        /// 内部処理ではモード番号（1、2、3）で扱う
        /// </summary>
        /// <param name="modeNumber">モード番号（1、2、3）</param>
        /// <returns>設定データ（存在しない場合は新規作成）</returns>
        public static ModeSettingsData LoadModeSettings(int modeNumber)
        {
            EnsureSettingsDirectoryExists();
            
            var filePath = GetModeSettingsFilePath(modeNumber);
            
            try
            {
                if (_jsonFileService.FileExists(filePath))
                {
                    var settings = _jsonFileService.ReadFromFile<ModeSettingsData>(filePath);
                    if (settings != null)
                    {
                        EnsureCollections(settings);
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                CustomDialog.Show(
                    $"設定ファイルの読み込みに失敗しました: {ex.Message}\nパス: {filePath}",
                    "設定読み込みエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            // ファイルが存在しない、または読み込みに失敗した場合は新規作成
            return CreateDefaultSettings();
        }

        /// <summary>
        /// モード設定を読み込む（モード名から）
        /// </summary>
        /// <param name="mode">モード名</param>
        /// <returns>設定データ（存在しない場合は新規作成）</returns>
        public static ModeSettingsData LoadModeSettings(string mode)
        {
            EnsureSettingsDirectoryExists();
            
            var filePath = GetModeSettingsFilePath(mode);
            
            try
            {
                if (_jsonFileService.FileExists(filePath))
                {
                    var settings = _jsonFileService.ReadFromFile<ModeSettingsData>(filePath);
                    if (settings != null)
                    {
                        EnsureCollections(settings);
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                CustomDialog.Show(
                    $"設定ファイルの読み込みに失敗しました: {ex.Message}\nパス: {filePath}",
                    "設定読み込みエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            // ファイルが存在しない、または読み込みに失敗した場合は新規作成
            return CreateDefaultSettings();
        }

        /// <summary>
        /// モード設定を保存（モード番号から）
        /// 内部処理ではモード番号（1、2、3）で扱う
        /// </summary>
        /// <param name="modeNumber">モード番号（1、2、3）</param>
        /// <param name="settings">設定データ</param>
        public static void SaveModeSettings(int modeNumber, ModeSettingsData settings)
        {
            EnsureSettingsDirectoryExists();
            
            var filePath = GetModeSettingsFilePath(modeNumber);
            
            try
            {
                _jsonFileService.WriteToFile(filePath, settings);
            }
            catch (Exception ex)
            {
                CustomDialog.Show(
                    $"設定ファイルの保存に失敗しました: {ex.Message}\nパス: {filePath}",
                    "設定保存エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// モード設定を保存（モード名から）
        /// </summary>
        /// <param name="mode">モード名</param>
        /// <param name="settings">設定データ</param>
        public static void SaveModeSettings(string mode, ModeSettingsData settings)
        {
            EnsureSettingsDirectoryExists();
            
            var filePath = GetModeSettingsFilePath(mode);
            
            try
            {
                _jsonFileService.WriteToFile(filePath, settings);
            }
            catch (Exception ex)
            {
                CustomDialog.Show(
                    $"設定ファイルの保存に失敗しました: {ex.Message}\nパス: {filePath}",
                    "設定保存エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 設定データのコレクションがnullの場合に初期化
        /// </summary>
        private static void EnsureCollections(ModeSettingsData settings)
        {
            if (settings.SwitcherSelections == null)
                settings.SwitcherSelections = new Dictionary<int, int>();
            if (settings.SwitcherPresets == null)
                settings.SwitcherPresets = new Dictionary<int, Dictionary<int, int>>();
            if (settings.RecordingSelections == null)
                settings.RecordingSelections = new Dictionary<int, int>();
            if (settings.MicrophoneFaderValues == null || settings.MicrophoneFaderValues.Length == 0)
                settings.MicrophoneFaderValues = new double[4] { 50.0, 50.0, 50.0, 50.0 };
            if (settings.OutputFaderValues == null || settings.OutputFaderValues.Length == 0)
                settings.OutputFaderValues = new double[2] { 50.0, 50.0 };
            if (settings.OutputMuteStates == null)
                settings.OutputMuteStates = new bool[2] { false, false };
            if (settings.MicrophoneMuteStates == null)
                settings.MicrophoneMuteStates = new bool[4] { false, false, false, false };
        }

        /// <summary>
        /// 既定値を持つ設定データを作成
        /// </summary>
        private static ModeSettingsData CreateDefaultSettings()
        {
            var settings = new ModeSettingsData();
            EnsureCollections(settings);
            return settings;
        }
    }
}

