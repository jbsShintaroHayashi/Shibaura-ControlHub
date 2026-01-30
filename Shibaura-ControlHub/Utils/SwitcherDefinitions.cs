using System.Collections.Generic;
using Shibaura_ControlHub.Models;

namespace Shibaura_ControlHub.Utils
{
    /// <summary>
    /// スイッチャー／録画のマトリクス定義を一箇所で管理。入出力のラベルとATEM番号をまとめて参照する。
    /// </summary>
    public static class SwitcherDefinitions
    {
        public const string MatrixIdSwitcherEsports = "Switcher_Esports";
        public const string MatrixIdSwitcherRemote = "Switcher_Remote";
        public const string MatrixIdRecording = "Recording";

        private static readonly IReadOnlyDictionary<string, SwitcherMatrixDef> Matrices = BuildMatrices();

        private static IReadOnlyDictionary<string, SwitcherMatrixDef> BuildMatrices()
        {
            var dict = new Dictionary<string, SwitcherMatrixDef>();

            // eスポーツモード: 9行×9列（大型LED1, 100型1-4, キャプチャPC1-2, web1, web2 | PGM, CLN, Cam1-3, HCam1-2, AUX9, AUX10）
            dict[MatrixIdSwitcherEsports] = new SwitcherMatrixDef(
                MatrixIdSwitcherEsports,
                new List<SwitcherOutputPort>
                {
                    new("大型LED1", 4),
                    new("100型モニタ1", 0),
                    new("100型モニタ2", 1),
                    new("100型モニタ3", 2),
                    new("100型モニタ4", 3),
                    new("キャプチャPC1", 9),
                    new("キャプチャPC2", 10),
                    new("web1", 5),
                    new("web2", 6),
                },
                new List<SwitcherInputPort>
                {
                    new("PGM", 6),
                    new("CLN", 7),
                    new("Cam1", 1),
                    new("Cam2", 2),
                    new("Cam3", 3),
                    new("HCam1", 4),
                    new("HCam2", 5),
                    new("AUX9", 19),
                    new("AUX10", 20),
                });

            // 遠隔モード: 8行×10列
            dict[MatrixIdSwitcherRemote] = new SwitcherMatrixDef(
                MatrixIdSwitcherRemote,
                new List<SwitcherOutputPort>
                {
                    new("大型LED1", 4),
                    new("100型モニタ1", 0),
                    new("100型モニタ2", 1),
                    new("100型モニタ3", 2),
                    new("100型モニタ4", 3),
                    new("キャプチャPC1", 9),
                    new("キャプチャPC2", 10),
                    new("教卓・実況席", 11),
                },
                new List<SwitcherInputPort>
                {
                    new("PGM", 6),
                    new("CLN", 7),
                    new("Cam1", 1),
                    new("Cam2", 2),
                    new("Cam3", 3),
                    new("HCam1", 4),
                    new("HCam2", 5),
                    new("講師", 10),
                    new("PC-A", 8),
                    new("PC-B", 9),
                });

            // 録画マトリクス: 2行×10列（録画1/2 → REC1/REC2）
            dict[MatrixIdRecording] = new SwitcherMatrixDef(
                MatrixIdRecording,
                new List<SwitcherOutputPort>
                {
                    new("録画1", 7),
                    new("録画2", 8),
                },
                new List<SwitcherInputPort>
                {
                    new("PGM", 6),
                    new("CLN", 7),
                    new("Cam 1", 1),
                    new("Cam 2", 2),
                    new("Cam 3", 3),
                    new("HCam 1", 4),
                    new("HCam 2", 5),
                    new("講師", 10),
                    new("PC-A", 8),
                    new("PC-B", 9),
                });

            return dict;
        }

        /// <summary>
        /// マトリクスIDから定義を取得する。
        /// </summary>
        public static SwitcherMatrixDef? GetMatrix(string matrixId)
        {
            return Matrices.TryGetValue(matrixId, out var def) ? def : null;
        }
    }
}
