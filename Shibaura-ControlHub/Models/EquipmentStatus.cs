using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Shibaura_ControlHub.Models
{
    /// <summary>
    /// 機材状態を表すデータモデル
    /// </summary>
    public class EquipmentStatus : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _statusCode = 0;
        private string _currentValue = string.Empty;
        private int _statusDetailCode = 0;
        private string _lastUpdate = string.Empty;
        private string _ipAddress = string.Empty;
        private int _port = 0;
        private int _deviceType = 0;
        private int _modeDisplayFlag = 0;

        [JsonPropertyName("name")]
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("statusCode")]
        public int StatusCode
        {
            get => _statusCode;
            set
            {
                if (_statusCode != value)
                {
                    _statusCode = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonPropertyName("currentValue")]
        public string CurrentValue
        {
            get => _currentValue;
            set { _currentValue = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("statusDetailCode")]
        public int StatusDetailCode
        {
            get => _statusDetailCode;
            set
            {
                if (_statusDetailCode != value)
                {
                    _statusDetailCode = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonPropertyName("lastUpdate")]
        public string LastUpdate
        {
            get => _lastUpdate;
            set { _lastUpdate = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("ipAddress")]
        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("port")]
        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("modeDisplayFlag")]
        public int ModeDisplayFlag
        {
            get => _modeDisplayFlag;
            set
            {
                if (_modeDisplayFlag != value)
                {
                    _modeDisplayFlag = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonPropertyName("deviceType")]
        public int DeviceType
        {
            get => _deviceType;
            set { _deviceType = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
