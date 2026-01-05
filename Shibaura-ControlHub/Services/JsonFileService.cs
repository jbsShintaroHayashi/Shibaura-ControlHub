using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Shibaura_ControlHub.Services
{
    /// <summary>
    /// JSONファイルの読み書きを担当するサービス
    /// </summary>
    public class JsonFileService
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// JSONファイルを読み込む
        /// </summary>
        /// <typeparam name="T">読み込むデータの型</typeparam>
        /// <param name="filePath">ファイルパス</param>
        /// <param name="encoding">エンコーディング（デフォルト: UTF-8）</param>
        /// <returns>読み込んだデータ。読み込みに失敗した場合はnull</returns>
        public T? ReadFromFile<T>(string filePath, Encoding? encoding = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("ファイルパスが指定されていません。", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                return default(T);
            }

            try
            {
                var actualEncoding = encoding ?? Encoding.UTF8;
                string jsonContent = File.ReadAllText(filePath, actualEncoding);
                return JsonSerializer.Deserialize<T>(jsonContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"JSONファイルの読み込みに失敗しました: {ex.Message}\nパス: {filePath}", ex);
            }
        }

        /// <summary>
        /// JSONファイルに書き込む
        /// </summary>
        /// <typeparam name="T">書き込むデータの型</typeparam>
        /// <param name="filePath">ファイルパス</param>
        /// <param name="data">書き込むデータ</param>
        /// <param name="encoding">エンコーディング（デフォルト: UTF-8）</param>
        /// <param name="options">JsonSerializerOptions（デフォルト: インデント付き、UnsafeRelaxedJsonEscaping）</param>
        public void WriteToFile<T>(string filePath, T data, Encoding? encoding = null, JsonSerializerOptions? options = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("ファイルパスが指定されていません。", nameof(filePath));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "書き込むデータがnullです。");
            }

            try
            {
                var actualEncoding = encoding ?? Encoding.UTF8;
                var actualOptions = options ?? DefaultOptions;
                string jsonContent = JsonSerializer.Serialize(data, actualOptions);
                File.WriteAllText(filePath, jsonContent, actualEncoding);
            }
            catch (Exception ex)
            {
                throw new Exception($"JSONファイルの書き込みに失敗しました: {ex.Message}\nパス: {filePath}", ex);
            }
        }

        /// <summary>
        /// ファイルが存在するか確認
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>ファイルが存在する場合はtrue</returns>
        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}

