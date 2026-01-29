using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Shibaura_ControlHub.ViewModels
{
    /// <summary>
    /// メンテナンス画面のViewModel
    /// </summary>
    public class MaintenanceViewModel : INotifyPropertyChanged
    {
        private const string DefaultMaintenanceUrl = "http://192.168.0.51";
        private const string DefaultUsername = "admin";
        private const string DefaultPassword = "Qweasd123";

        private string _webUrl = BuildUrlWithCredentials(DefaultMaintenanceUrl, DefaultUsername, DefaultPassword);
        private string _username = DefaultUsername;
        private string _password = DefaultPassword;

        public string WebUrl
        {
            get => _webUrl;
            set
            {
                // URLに認証情報を含める
                _webUrl = BuildUrlWithCredentials(value, _username, _password);
                OnPropertyChanged();
            }
        }

        private static string BuildUrlWithCredentials(string url, string username, string password)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            // URLに認証情報が既に含まれている場合はそのまま返す
            if (url.Contains("@"))
                return url;

            // URLを解析して認証情報を追加
            var uri = new Uri(url);
            var builder = new UriBuilder(uri)
            {
                UserName = username,
                Password = password
            };
            return builder.ToString();
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public ICommand ReturnToMenuCommand { get; }
        public event Action? ReturnToMenu;

        public MaintenanceViewModel()
        {
            ReturnToMenuCommand = new RelayCommand(() => ReturnToMenu?.Invoke());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
