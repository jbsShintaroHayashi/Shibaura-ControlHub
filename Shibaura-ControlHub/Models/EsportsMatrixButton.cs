using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shibaura_ControlHub.Models
{
    /// <summary>
    /// esportsマトリクスボタン用のデータモデル
    /// </summary>
    public class EsportsMatrixButton : INotifyPropertyChanged
    {
        private int _row;
        private int _column;
        private string _displayText = string.Empty;
        private bool _isSelected;

        /// <summary>
        /// 行番号（1-7）
        /// </summary>
        public int Row
        {
            get => _row;
            set
            {
                _row = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 列番号（1-17）
        /// </summary>
        public int Column
        {
            get => _column;
            set
            {
                _column = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ボタンに表示するテキスト
        /// </summary>
        public string DisplayText
        {
            get => _displayText;
            set
            {
                _displayText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 選択状態
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

