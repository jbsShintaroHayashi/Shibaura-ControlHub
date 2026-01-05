using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Shibaura_ControlHub.Views
{
    /// <summary>
    /// カテゴリが一致する場合に表示するコンバーター
    /// </summary>
    public class CategoryMatchConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                return Visibility.Collapsed;
            }

            if (value is string selectedCategory && parameter is string targetCategory)
            {
                if (string.IsNullOrEmpty(selectedCategory))
                {
                    return Visibility.Collapsed;
                }

                bool isMatch = selectedCategory == targetCategory;
                return isMatch ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// カテゴリが一致しない場合に表示するコンバーター
    /// </summary>
    public class CategoryNotMatchConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string selectedCategory && parameter is string targetCategory)
            {
                bool isMatch = selectedCategory == targetCategory;
                return !isMatch ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// カテゴリが一致し、かつモードが指定された値のいずれかに一致する場合に表示するコンバーター
    /// </summary>
    public class CategoryAndModeMatchConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
            {
                return Visibility.Collapsed;
            }

            // values[0]: SelectedEquipmentCategory (string)
            // values[1]: CurrentMode (int)
            // values[2]: TargetCategory (string)
            // values[3+]: TargetModes (int...)

            if (values[0] is not string selectedCategory || values[1] is not int currentMode || values[2] is not string targetCategory)
            {
                return Visibility.Collapsed;
            }

            // カテゴリが一致しない場合は非表示
            if (selectedCategory != targetCategory)
            {
                return Visibility.Collapsed;
            }

            // モードが指定された値のいずれかに一致するかチェック
            for (int i = 3; i < values.Length; i++)
            {
                if (values[i] is int targetMode && currentMode == targetMode)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

