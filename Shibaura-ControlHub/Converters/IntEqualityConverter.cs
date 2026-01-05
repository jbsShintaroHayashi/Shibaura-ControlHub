using System;
using System.Globalization;
using System.Windows.Data;

namespace Shibaura_ControlHub.Converters
{
    /// <summary>
    /// 2つの整数値が等しいかどうかを判定するコンバーター
    /// </summary>
    public class IntEqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return false;
            }

            if (values[0] is int value1 && values[1] is int value2)
            {
                return value1 == value2;
            }

            // 文字列やその他の型の場合も試行
            string? value1Str = values[0] != null ? values[0].ToString() : null;
            string? value2Str = values[1] != null ? values[1].ToString() : null;
            if (value1Str != null && value2Str != null &&
                int.TryParse(value1Str, out int parsedValue1) &&
                int.TryParse(value2Str, out int parsedValue2))
            {
                return parsedValue1 == parsedValue2;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
