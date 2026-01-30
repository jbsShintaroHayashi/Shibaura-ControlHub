ATEM Switcher API の参照用フォルダです。

プロジェクトはこの lib\ATEM フォルダの BMDSwitcherAPI64.dll（COM 型ライブラリ）を
COMFileReference で参照し、ビルド時に tlbimp でラッパーを生成します。

必要なファイル:
- BMDSwitcherAPI64.dll … Blackmagic ATEM Switchers SDK の 64bit 用 DLL をこのフォルダにコピー
  例: SDK インストール先\Blackmagic ATEM Switchers SDK 10.1\Windows\BMDSwitcherAPI64.dll

■ 別のPCにアプリを入れた場合（重要）
  そのPCで ATEM 接続を使うには、そのPCで BMDSwitcherAPI64.dll を COM 登録する必要があります。
  - ビルド出力フォルダに「ATEM_COM登録.bat」がコピーされています。
  - アプリを別PCにコピーしたら、exe と同じフォルダで「ATEM_COM登録.bat」を
    **右クリック → 管理者として実行** してください。
  - または 管理者のコマンドプロンプトで:
    cd アプリのフォルダ
    regsvr32 BMDSwitcherAPI64.dll
