using System;
using System.IO;
using log4net;

namespace Shibaura_ControlHub.Utils
{
    /// <summary>
    /// アクションログの種類
    /// </summary>
    public enum LogActionType
    {
        /// <summary>
        /// ユーザーアクション（ボタンクリックなど）
        /// </summary>
        Action,
        
        /// <summary>
        /// 処理の開始
        /// </summary>
        ProcessingStart,
        
        /// <summary>
        /// 処理の進行中
        /// </summary>
        Processing,
        
        /// <summary>
        /// 処理の完了
        /// </summary>
        ProcessingComplete,
        
        /// <summary>
        /// 処理結果
        /// </summary>
        Result,
        
        /// <summary>
        /// エラー
        /// </summary>
        Error,
        
        /// <summary>
        /// 情報
        /// </summary>
        Info
    }

    /// <summary>
    /// アクションと処理を区別したログ出力クラス
    /// </summary>
    public static class ActionLogger
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ActionLogger));

        /// <summary>
        /// ユーザーアクション（ボタンクリックなど）をログ出力
        /// </summary>
        /// <param name="action">アクション名（例: "カメラ移動", "プリセット呼出"）</param>
        /// <param name="details">詳細情報（オプション）</param>
        public static void LogAction(string action, string? details = null)
        {
            var message = FormatMessage(LogActionType.Action, action, details);
            _logger.Info(message);
        }

        /// <summary>
        /// 処理の開始をログ出力
        /// </summary>
        /// <param name="process">処理名（例: "カメラ設定保存", "プリセット読み込み"）</param>
        /// <param name="details">詳細情報（オプション）</param>
        public static void LogProcessingStart(string process, string? details = null)
        {
            var message = FormatMessage(LogActionType.ProcessingStart, process, details);
            _logger.Info(message);
        }

        /// <summary>
        /// 処理の進行中をログ出力
        /// </summary>
        /// <param name="process">処理名</param>
        /// <param name="details">詳細情報</param>
        public static void LogProcessing(string process, string? details = null)
        {
            var message = FormatMessage(LogActionType.Processing, process, details);
            _logger.Info(message);
        }

        /// <summary>
        /// 処理の完了をログ出力
        /// </summary>
        /// <param name="process">処理名</param>
        /// <param name="details">詳細情報（オプション）</param>
        public static void LogProcessingComplete(string process, string? details = null)
        {
            var message = FormatMessage(LogActionType.ProcessingComplete, process, details);
            _logger.Info(message);
        }

        /// <summary>
        /// 処理結果をログ出力
        /// </summary>
        /// <param name="result">結果名</param>
        /// <param name="details">詳細情報</param>
        public static void LogResult(string result, string? details = null)
        {
            var message = FormatMessage(LogActionType.Result, result, details);
            _logger.Info(message);
        }

        /// <summary>
        /// エラーをログ出力
        /// </summary>
        /// <param name="error">エラー名</param>
        /// <param name="details">詳細情報</param>
        /// <param name="exception">例外（オプション）</param>
        public static void LogError(string error, string? details = null, Exception? exception = null)
        {
            var message = FormatMessage(LogActionType.Error, error, details);
            if (exception != null)
            {
                _logger.Error(message, exception);
            }
            else
            {
                _logger.Error(message);
            }
        }

        /// <summary>
        /// 情報をログ出力
        /// </summary>
        /// <param name="info">情報名</param>
        /// <param name="details">詳細情報</param>
        public static void LogInfo(string info, string? details = null)
        {
            var message = FormatMessage(LogActionType.Info, info, details);
            _logger.Info(message);
        }

        /// <summary>
        /// メッセージをフォーマット
        /// </summary>
        private static string FormatMessage(LogActionType type, string message, string? details)
        {
            var prefix = GetPrefix(type);
            
            var prefixPart = $"[{prefix}]";
            var prefixPartLength = prefixPart.Length;
            
            const int MessageStartPosition = 25; 
            
            var spacesNeeded = MessageStartPosition - prefixPartLength;
            var spaces = spacesNeeded > 0 ? new string(' ', spacesNeeded) : " ";
            
            var formattedMessage = $"{prefixPart}{spaces}{message}";
            
            if (!string.IsNullOrEmpty(details))
            {
                formattedMessage += $" | {details}";
            }
            
            return formattedMessage;
        }

        /// <summary>
        /// ログタイプに応じたプレフィックスを取得（可変長）
        /// </summary>
        private static string GetPrefix(LogActionType type)
        {
            return type switch
            {
                LogActionType.Action => "action",
                LogActionType.ProcessingStart => "processingStart",
                LogActionType.Processing => "processing",
                LogActionType.ProcessingComplete => "processingComplete",
                LogActionType.Result => "result",
                LogActionType.Error => "error",
                LogActionType.Info => "info",
                _ => "log"
            };
        }

    }
}

