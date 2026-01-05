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
        private double _faderValue = 0.0;
        private double _panValue = 0.0;
        private double _tiltValue = 0.0;
        private double _zoomValue = 0.0;

        [JsonPropertyName("name")]
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private static readonly Dictionary<int, string> StatusLabels = new()
        {
            { 0, "未稼働" },
            { 1, "稼働中" },
            { 2, "待機中" },
            { 3, "正常" },
            { 4, "警告" },
            { 5, "異常" }
        };

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
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        [JsonIgnore]
        public string Status
        {
            get => StatusLabels.TryGetValue(StatusCode, out var name) ? name : "不明";
            set
            {
                StatusCode = GetStatusCode(value);
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
                    OnPropertyChanged(nameof(StatusDetail));
                }
            }
        }

        [JsonIgnore]
        public string StatusDetail
        {
            get => StatusLabels.TryGetValue(StatusDetailCode, out var name) ? name : "不明";
            set
            {
                StatusDetailCode = GetStatusCode(value);
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

        /// <summary>
        /// フェーダー値（0-100）
        /// </summary>
        public double FaderValue
        {
            get => _faderValue;
            set 
            { 
                _faderValue = value; 
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// パン値（水平方向、-100〜100）
        /// </summary>
        public double PanValue
        {
            get => _panValue;
            set 
            { 
                _panValue = value; 
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// チルト値（垂直方向、-100〜100）
        /// </summary>
        public double TiltValue
        {
            get => _tiltValue;
            set 
            { 
                _tiltValue = value; 
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ズーム値（0-100）
        /// </summary>
        public double ZoomValue
        {
            get => _zoomValue;
            set 
            { 
                _zoomValue = value; 
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// デバイスタイプの文字列表現を取得
        /// </summary>
        public string GetDeviceTypeName()
        {
            return DeviceType switch
            {
                0 => "マイク",
                1 => "カメラ",
                2 => "スイッチャー",
                _ => "不明"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static int GetStatusCode(string label)
        {
            foreach (var pair in StatusLabels)
            {
                if (pair.Value == label)
                {
                    return pair.Key;
                }
            }
            return 0;
        }
    }
}
