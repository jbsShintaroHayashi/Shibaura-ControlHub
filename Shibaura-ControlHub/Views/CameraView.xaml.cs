using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfUserControl = System.Windows.Controls.UserControl;
using Shibaura_ControlHub.ViewModels;
using Shibaura_ControlHub.Utils;

namespace Shibaura_ControlHub.Views
{
    /// <summary>
    /// CameraView.xaml の相互作用ロジック
    /// </summary>
    public partial class CameraView : WpfUserControl
    {
        public CameraView()
        {
            InitializeComponent();
        }

        // 移動ボタン：押下時
        private void MoveButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // タッチイベントが処理済みの場合は、マウスイベントを無視
            if (e.StylusDevice != null)
            {
                return;
            }
            
            if (sender is Button button && button.CommandParameter is string direction)
            {
                if (DataContext is CameraViewModel viewModel)
                {
                    viewModel.StartMoveCamera(direction);
                }
            }
        }

        // 移動ボタン：離れたとき
        private void MoveButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // タッチイベントが処理済みの場合は、マウスイベントを無視
            if (e.StylusDevice != null)
            {
                return;
            }
            
            if (DataContext is CameraViewModel viewModel)
            {
                viewModel.StopMoveCamera();
            }
        }

        // 移動ボタン：タッチ押下時
        private void MoveButton_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            // マウスイベントの発火を防ぐため、イベントを処理済みとしてマーク
            e.Handled = true;
            
            if (sender is Button button && button.CommandParameter is string direction)
            {
                // タッチデバイスをキャプチャ
                button.CaptureTouch(e.TouchDevice);
                
                if (DataContext is CameraViewModel viewModel)
                {
                    viewModel.StartMoveCamera(direction);
                }
            }
        }

        // 移動ボタン：タッチ離れたとき
        private void MoveButton_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            // マウスイベントの発火を防ぐため、イベントを処理済みとしてマーク
            e.Handled = true;
            
            if (sender is Button button)
            {
                // タッチデバイスのキャプチャを解放
                if (e.TouchDevice?.Captured == button)
                {
                    button.ReleaseTouchCapture(e.TouchDevice);
                }
                
                if (DataContext is CameraViewModel viewModel)
                {
                    viewModel.StopMoveCamera();
                }
            }
        }

        // 移動ボタン：タッチキャプチャが失われたとき
        private void MoveButton_LostTouchCapture(object sender, TouchEventArgs e)
        {
            if (DataContext is CameraViewModel viewModel)
            {
                viewModel.StopMoveCamera();
            }
        }

        // ズームボタン：押下時
        private void ZoomButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // タッチイベントが処理済みの場合は、マウスイベントを無視
            if (e.StylusDevice != null)
            {
                return;
            }
            
            if (sender is Button button && button.CommandParameter is string direction)
            {
                if (DataContext is CameraViewModel viewModel)
                {
                    viewModel.StartZoom(direction);
                }
            }
        }

        // ズームボタン：離れたとき
        private void ZoomButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // タッチイベントが処理済みの場合は、マウスイベントを無視
            if (e.StylusDevice != null)
            {
                return;
            }
            
            if (DataContext is CameraViewModel viewModel)
            {
                viewModel.StopZoom();
            }
        }

        // ズームボタン：タッチ押下時
        private void ZoomButton_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            // マウスイベントの発火を防ぐため、イベントを処理済みとしてマーク
            e.Handled = true;
            
            if (sender is Button button && button.CommandParameter is string direction)
            {
                // タッチデバイスをキャプチャ
                button.CaptureTouch(e.TouchDevice);
                
                if (DataContext is CameraViewModel viewModel)
                {
                    viewModel.StartZoom(direction);
                }
            }
        }

        // ズームボタン：タッチ離れたとき
        private void ZoomButton_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            // マウスイベントの発火を防ぐため、イベントを処理済みとしてマーク
            e.Handled = true;
            
            if (sender is Button button)
            {
                // タッチデバイスのキャプチャを解放
                if (e.TouchDevice?.Captured == button)
                {
                    button.ReleaseTouchCapture(e.TouchDevice);
                }
                
                if (DataContext is CameraViewModel viewModel)
                {
                    viewModel.StopZoom();
                }
            }
        }

        // ズームボタン：タッチキャプチャが失われたとき
        private void ZoomButton_LostTouchCapture(object sender, TouchEventArgs e)
        {
            if (DataContext is CameraViewModel viewModel)
            {
                viewModel.StopZoom();
            }
        }
    }

    public static class VisualTreeHelperExtensions
    {
        public static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}

