using System;
using System.Globalization;
using System.Windows.Data;

namespace Shibaura_ControlHub.Converters
{
    /// <summary>
    /// 2つのオブジェクトが等しいかどうかを判定するコンバーター
    /// </summary>
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return false;
            }

            // オブジェクト参照の比較
            return ReferenceEquals(values[0], values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

