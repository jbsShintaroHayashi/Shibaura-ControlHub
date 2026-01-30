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
        private bool _isSelected;

        /// <summary>
        /// 行番号（1始まり）
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
        /// 列番号（1始まり）
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

