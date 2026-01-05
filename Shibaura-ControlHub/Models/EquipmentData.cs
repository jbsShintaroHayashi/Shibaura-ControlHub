using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shibaura_ControlHub.Models
{
    /// <summary>
    /// JSONから読み込む機器データ
    /// </summary>
    public class EquipmentData
    {
        [JsonPropertyName("equipment")]
        public List<EquipmentStatus> Equipment { get; set; } = new List<EquipmentStatus>();
    }
}

