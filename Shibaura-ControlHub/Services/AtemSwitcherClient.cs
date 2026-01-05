using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Interop.BMDSwitcherAPI;

namespace Shibaura_ControlHub.Services;

public sealed class AtemSwitcherClient : IAtemSwitcherClient, IDisposable
{
    private readonly object _syncRoot = new();

    private IBMDSwitcher? _switcher;
    private readonly List<long> _externalInputIds = new();

    public Task ConnectAsync(string ipAddress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("IP アドレスが空です。", nameof(ipAddress));
        }

        try
        {
            var discovery = new CBMDSwitcherDiscovery();
            _BMDSwitcherConnectToFailure failureReason = _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureNoResponse;

            discovery.ConnectTo(ipAddress, out var switcher, out failureReason);

            if (switcher is null)
            {
                throw new InvalidOperationException(GetFailureMessage(failureReason));
            }

            lock (_syncRoot)
            {
                ReleaseCurrentSwitcher();
                _switcher = switcher;
                CacheExternalInputsNoLock();
            }
        }
        catch (COMException ex)
        {
            throw new InvalidOperationException("ATEM への接続に失敗しました。Blackmagic ATEM Switchers SDK が正しくインストールされているか確認してください。", ex);
        }

        return Task.CompletedTask;
    }

    public Task RouteAuxAsync(int auxIndex, int inputIndex, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        long inputId;

        lock (_syncRoot)
        {
            if (_externalInputIds.Count == 0)
            {
                throw new InvalidOperationException("ATEM に接続されていません。先に ConnectAsync を呼び出してください。");
            }

            if (inputIndex < 1 || inputIndex > _externalInputIds.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(inputIndex), $"入力 {inputIndex} は利用できません (検出 {_externalInputIds.Count} 系統)。");
            }

            inputId = _externalInputIds[inputIndex - 1];
        }

        RouteAuxInternal(auxIndex, inputId);
        return Task.CompletedTask;
    }

    private void RouteAuxInternal(int auxIndex, long inputId)
    {
        var switcher = GetSwitcherOrThrow();

        IntPtr iteratorPtr = IntPtr.Zero;
        IBMDSwitcherInputIterator? iterator = null;
        try
        {
            switcher.CreateIterator(typeof(IBMDSwitcherInputIterator).GUID, out iteratorPtr);
            if (iteratorPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("AUX 出力の列挙に失敗しました。");
            }

            iterator = (IBMDSwitcherInputIterator)Marshal.GetObjectForIUnknown(iteratorPtr);
            Marshal.Release(iteratorPtr);
            iteratorPtr = IntPtr.Zero;
            try
            {
                IBMDSwitcherInput? input = null;
                int currentAuxIndex = 0;

                while (true)
                {
                    iterator.Next(out input);
                    if (input == null)
                    {
                        break;
                    }

                    try
                    {
                        input.GetPortType(out _BMDSwitcherPortType portType);
                        if (portType != _BMDSwitcherPortType.bmdSwitcherPortTypeAuxOutput)
                        {
                            continue;
                        }

                        if (input is IBMDSwitcherInputAux aux)
                        {
                            try
                            {
                                if (currentAuxIndex == auxIndex)
                                {
                                    aux.SetInputSource(inputId);
                                    return;
                                }

                                currentAuxIndex++;
                            }
                            finally
                            {
                                Marshal.ReleaseComObject(aux);
                            }
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(input);
                    }
                }
            }
            finally
            {
                if (iterator is not null)
                {
                    Marshal.ReleaseComObject(iterator);
                }

                if (iteratorPtr != IntPtr.Zero)
                {
                    Marshal.Release(iteratorPtr);
                }
            }
        }
        catch (COMException ex)
        {
            throw new InvalidOperationException("AUX 出力の切り替えに失敗しました。", ex);
        }

        throw new ArgumentOutOfRangeException(nameof(auxIndex), $"AUX {auxIndex + 1} は利用できません。");
    }

    private void CacheExternalInputsNoLock()
    {
        _externalInputIds.Clear();

        if (_switcher is null)
        {
            return;
        }

        IntPtr iteratorPtr = IntPtr.Zero;
        IBMDSwitcherInputIterator? iterator = null;
        try
        {
            _switcher.CreateIterator(typeof(IBMDSwitcherInputIterator).GUID, out iteratorPtr);
            if (iteratorPtr == IntPtr.Zero)
            {
                return;
            }

            iterator = (IBMDSwitcherInputIterator)Marshal.GetObjectForIUnknown(iteratorPtr);
            Marshal.Release(iteratorPtr);
            iteratorPtr = IntPtr.Zero;
            try
            {
                IBMDSwitcherInput? input = null;
                while (true)
                {
                    iterator.Next(out input);
                    if (input == null)
                    {
                        break;
                    }

                    try
                    {
                        input.GetPortType(out _BMDSwitcherPortType portType);
                        if (portType == _BMDSwitcherPortType.bmdSwitcherPortTypeExternal)
                        {
                            input.GetInputId(out long inputId);
                            _externalInputIds.Add(inputId);
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(input);
                    }
                }
            }
            finally
            {
                if (iterator is not null)
                {
                    Marshal.ReleaseComObject(iterator);
                }

                if (iteratorPtr != IntPtr.Zero)
                {
                    Marshal.Release(iteratorPtr);
                }
            }
        }
        catch (COMException ex)
        {
            throw new InvalidOperationException("ATEM から入力一覧を取得できませんでした。", ex);
        }

        _externalInputIds.Sort();
    }

    private IBMDSwitcher GetSwitcherOrThrow()
    {
        lock (_syncRoot)
        {
            return _switcher ?? throw new InvalidOperationException("ATEM に接続されていません。");
        }
    }

    private void ReleaseCurrentSwitcher()
    {
        if (_switcher is not null)
        {
            Marshal.ReleaseComObject(_switcher);
            _switcher = null;
        }

        _externalInputIds.Clear();
    }

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        lock (_syncRoot)
        {
            ReleaseCurrentSwitcher();
        }

        _disposed = true;
    }

    ~AtemSwitcherClient()
    {
        Dispose(false);
    }

    #endregion

    private static string GetFailureMessage(_BMDSwitcherConnectToFailure failure) =>
        failure switch
        {
            _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureIncompatibleFirmware =>
                "Switcher のファームウェアが SDK と互換性がありません。",
            _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureCorruptData =>
                "Switcher から不正なデータが返されました。",
            _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureStateSync =>
                "Switcher との状態同期に失敗しました。",
            _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureStateSyncTimedOut =>
                "Switcher との状態同期がタイムアウトしました。",
            _ => "Switcher から応答がありませんでした。IP アドレスとネットワーク接続を確認してください。"
        };
}

