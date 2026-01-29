using System.Collections.Generic;

namespace Shibaura_ControlHub.Models
{
    /// <summary>
    /// スイッチャー1行（出力）の定義。表示ラベルとATEMのAUX番号（0始まり）を一括管理。
    /// </summary>
    public sealed class SwitcherOutputPort
    {
        public string Label { get; }
        public int AuxIndex0Based { get; }

        public SwitcherOutputPort(string label, int auxIndex0Based)
        {
            Label = label;
            AuxIndex0Based = auxIndex0Based;
        }
    }

    /// <summary>
    /// スイッチャー1列（入力）の定義。表示ラベルとATEMの入力番号（1始まり・外部入力のN番目）を一括管理。
    /// </summary>
    public sealed class SwitcherInputPort
    {
        public string Label { get; }
        public int InputIndex1Based { get; }

        public SwitcherInputPort(string label, int inputIndex1Based)
        {
            Label = label;
            InputIndex1Based = inputIndex1Based;
        }
    }

    /// <summary>
    /// 1つのマトリクス（スイッチャー画面 or 録画画面）の入出力定義。行＝出力、列＝入力。
    /// </summary>
    public sealed class SwitcherMatrixDef
    {
        public string MatrixId { get; }
        public IReadOnlyList<SwitcherOutputPort> Outputs { get; }
        public IReadOnlyList<SwitcherInputPort> Inputs { get; }

        public SwitcherMatrixDef(string matrixId, IReadOnlyList<SwitcherOutputPort> outputs, IReadOnlyList<SwitcherInputPort> inputs)
        {
            MatrixId = matrixId;
            Outputs = outputs;
            Inputs = inputs;
        }

        public int RowCount => Outputs.Count;
        public int ColumnCount => Inputs.Count;
    }
}
