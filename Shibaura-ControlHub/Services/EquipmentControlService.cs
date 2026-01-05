using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Shibaura_ControlHub.Utils;

namespace Shibaura_ControlHub.Services
{
    /// <summary>
    /// 機材制御処理を担当するサービス
    /// </summary>
    public class EquipmentControlService
    {
        public EquipmentControlService()
        {
        }

        /// <summary>
        /// モード1を実行
        /// </summary>
        public void ExecuteMode1()
        {
            // TODO: 実際の機材制御処理を実装
            // 例: PLC通信、制御信号送信など
            Utils.ActionLogger.LogAction("モード1実行", "モード1を実行しました");
        }

        /// <summary>
        /// モード2を実行
        /// </summary>
        public void ExecuteMode2()
        {
            // TODO: 実際の機材制御処理を実装
            Utils.ActionLogger.LogAction("モード2実行", "モード2を実行しました");
        }

        /// <summary>
        /// 指定されたモードを実行（モード番号で指定）
        /// 内部処理ではモード番号（1、2、3）で扱う
        /// </summary>
        /// <param name="modeNumber">モード番号（1、2、3）</param>
        public void ExecuteMode(int modeNumber)
        {
            switch (modeNumber)
            {
                case 1:
                    ExecuteMode1();
                    break;
                case 2:
                    ExecuteMode2();
                    break;
                case 3:
                    // TODO: モード3の実装
                    Utils.ActionLogger.LogAction("モード3実行", "モード3を実行しました");
                    break;
                default:
                    throw new ArgumentException($"未知のモード番号: {modeNumber}");
            }
        }
        
        /// <summary>
        /// 指定されたモードを実行（モード名で指定、互換性のため）
        /// </summary>
        /// <param name="mode">モード名</param>
        public void ExecuteMode(string mode)
        {
            // モード名からモード番号に変換して実行
            int modeNumber = Utils.ModeSettingsManager.GetModeNumber(mode);
            ExecuteMode(modeNumber);
        }

        /// <summary>
        /// 機材状態を取得
        /// </summary>
        public void RefreshEquipmentStatus(List<Models.EquipmentStatus> equipmentList)
        {
            var timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            foreach (var equipment in equipmentList)
            {
                equipment.CurrentValue = "-";
                equipment.StatusCode = 2;
                equipment.StatusDetailCode = 0;
                equipment.LastUpdate = timestamp;
            }
        }

        public async Task<IReadOnlyList<EquipmentStatusUpdate>> CheckEquipmentStatusAsync(IEnumerable<Models.EquipmentStatus> equipmentList)
        {
            var timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            var tasks = new List<Task<EquipmentStatusUpdate>>();
            foreach (var equipment in equipmentList)
            {
                tasks.Add(CheckSingleEquipmentAsync(equipment, timestamp));
            }

            return await Task.WhenAll(tasks);
        }

        private static async Task<EquipmentStatusUpdate> CheckSingleEquipmentAsync(Models.EquipmentStatus equipment, string timestamp)
        {
            bool isOnline = false;
            long? roundtrip = null;

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(equipment.IpAddress, 1000);
                if (reply != null && reply.Status == IPStatus.Success)
                {
                    isOnline = true;
                    roundtrip = reply.RoundtripTime;
                }
            }
            catch
            {
                isOnline = false;
            }

            var statusCode = isOnline ? 1 : 0;
            var detailCode = isOnline ? 3 : 5;
            var currentValue = isOnline && roundtrip.HasValue ? $"{roundtrip.Value}ms" : "-";

            return new EquipmentStatusUpdate(equipment, statusCode, detailCode, currentValue, timestamp);
        }

        public sealed class EquipmentStatusUpdate
        {
            public Models.EquipmentStatus Equipment { get; }
            public int StatusCode { get; }
            public int StatusDetailCode { get; }
            public string CurrentValue { get; }
            public string LastUpdate { get; }

            public EquipmentStatusUpdate(Models.EquipmentStatus equipment, int statusCode, int statusDetailCode, string currentValue, string lastUpdate)
            {
                Equipment = equipment;
                StatusCode = statusCode;
                StatusDetailCode = statusDetailCode;
                CurrentValue = currentValue;
                LastUpdate = lastUpdate;
            }

            public void Apply()
            {
                if (Equipment.StatusCode != StatusCode)
                {
                    Equipment.StatusCode = StatusCode;
                }

                if (Equipment.StatusDetailCode != StatusDetailCode)
                {
                    Equipment.StatusDetailCode = StatusDetailCode;
                }

                if (!string.Equals(Equipment.CurrentValue, CurrentValue, StringComparison.Ordinal))
                {
                    Equipment.CurrentValue = CurrentValue;
                }

                if (!string.Equals(Equipment.LastUpdate, LastUpdate, StringComparison.Ordinal))
                {
                    Equipment.LastUpdate = LastUpdate;
                }
            }
        }
    }
}

