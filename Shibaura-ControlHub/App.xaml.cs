using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using Shibaura_ControlHub.Models;
using Shibaura_ControlHub.Services;
using log4net;
using log4net.Config;
using WpfApplication = System.Windows.Application;
using Shibaura_ControlHub.Views;

namespace Shibaura_ControlHub
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : WpfApplication
    {
        /// <summary>
        /// モード設定情報（グローバル）
        /// </summary>
        public static ModeConfiguration ModeConfig { get; private set; } = new ModeConfiguration();

        /// <summary>
        /// アプリケーション起動時の処理
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // log4netの初期化
            InitializeLogging();

            // INIファイルからモード設定を読み込む
            LoadModeConfiguration();
        }

        /// <summary>
        /// log4netの初期化
        /// </summary>
        private void InitializeLogging()
        {
            try
            {
                // ログディレクトリのパス
                string logDirectory = @"C:\cnf\log";
                
                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // log4net.xmlファイルのパス
                string logConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.xml");

                if (File.Exists(logConfigPath))
                {
                    // XMLファイルから設定を読み込む
                    var fileInfo = new FileInfo(logConfigPath);
                    XmlConfigurator.Configure(fileInfo);

                    // ログの開始を記録
                    ILog logger = LogManager.GetLogger(typeof(App));
                    logger.Info("ログシステムが初期化されました。");
                    logger.Info($"ログファイルの出力先: {logDirectory}");
                }
                else
                {
                    // log4net.xmlが存在しない場合は基本設定を使用
                    BasicConfigurator.Configure();
                }
            }
            catch
            {
                // log4netの初期化に失敗した場合でも、アプリケーションは継続
            }
        }

        /// <summary>
        /// モード設定を読み込む
        /// </summary>
        private void LoadModeConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "config.ini");
                
                if (File.Exists(configPath))
                {
                    var iniReader = IniFileReader.LoadFromFile(configPath);
                    ModeConfig = iniReader.LoadModeConfiguration();
                }
                else
                {
                    // INIファイルが存在しない場合はデフォルト値を使用
                    ModeConfig = new ModeConfiguration();
                }
            }
            catch (Exception ex)
            {
                // エラーが発生した場合はデフォルト値を使用
                CustomDialog.Show(
                    $"設定ファイルの読み込みに失敗しました: {ex.Message}\nデフォルト設定を使用します。",
                    "設定読み込みエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ModeConfig = new ModeConfiguration();
            }
        }
    }

}
