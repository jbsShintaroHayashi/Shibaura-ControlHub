using System;
using System.Globalization;
using System.Windows.Data;

namespace Shibaura_ControlHub.Converters
{
    /// <summary>
    /// プリセットボタンの状態を判定するコンバーター
    /// 戻り値: 0=未選択, 1=選択済み（オレンジ）, 2=呼び出し済み（濃い青）
    /// </summary>
    public class PresetStateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
            {
                return 0;
            }

            int selectedPresetNumber = 0;
            int calledPresetNumber = 0;
            int buttonTag = 0;

            if (values[0] != null && int.TryParse(values[0].ToString(), out int selected))
            {
                selectedPresetNumber = selected;
            }

            if (values[1] != null && int.TryParse(values[1].ToString(), out int called))
            {
                calledPresetNumber = called;
            }

            if (values[2] != null && int.TryParse(values[2].ToString(), out int tag))
            {
                buttonTag = tag;
            }

            // 呼び出し済み（濃い青）かどうかを先にチェック
            if (calledPresetNumber == buttonTag)
            {
                return 2;
            }

            // 選択されているが呼び出されていない（オレンジ）
            if (selectedPresetNumber == buttonTag)
            {
                return 1;
            }

            // 選択されていない
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

