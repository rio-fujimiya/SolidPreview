# SolidPreview

**SolidCraftの描画エンジンでWindowsエクスプローラーにCADプレビュー・サムネイルを表示する非公式拡張。**
![demo_animation](animation_f30.0.gif)

[English](README.md) / [変更履歴](CHANGELOG.md) / [技術構成](docs/architecture.md)

> 実験的なソフトウェアであり、SolidCraftやCADベンダーの公式製品ではありません。

## 機能

- STL、STEP、OBJ、3MF、SolidCraft形式のプレビューペイン・サムネイル表示。IGESはサンプル指定で追加可能。
- ViewCubeの面・辺・頂点から26方向の視点へ移動。面視点では上下左右の矢印で90°ずつ回転。
- 左右ドラッグで自由回転、ホイールでズーム、中ボタンドラッグでパン。自由回転ではズームとパン位置を維持。ViewCubeの各ボタンで標準倍率・モデル中心に復帰し、ホームはISO視点へ戻ります。モデルのダブルクリックでもISOへ復帰。
- ペインに合わせた可変解像度（長辺最大2048px、ドラッグ中960px）。
- PNGキャッシュ、対応形式では常駐MCPセッション、非対応時はCLIのスナップショットにフォールバック。

## 必要環境

Windows 11 x64、64bit Windows PowerShell 5.1、.NET Framework 4.xのC#コンパイラ、および別途インストールしたSolidCraft CLI。初期開発ではCLI 0.45.0が使用されました。

## インストール

ZIPを展開し、SolidCraftを起動しておきます。展開先で通常権限のPowerShellを実行します。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1
```

CLIの自動検出に失敗する場合は `-Cli` で実行ファイルを指定できます。管理者権限は通常不要です。インストールは対応形式ごとの描画テストを行い、成功した形式だけを登録します。`PASS live` は常駐描画対応、`PASS orbit` は回転描画対応を示します。

Explorerで `Alt+P` を押してプレビューを表示します。大アイコン・特大アイコンでサムネイルが確認できます。ドラッグ時のFPSは保証されません。

## 更新・アンインストール

新しいZIPの `Update-SolidPreview.bat` をダブルクリックすれば、登録解除・再インストール・Explorer再起動をまとめて実行します。実行前にファイル操作を終了してください。古いDLLが残る場合はサインアウトが必要になることがあります。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Uninstall.ps1
```

キャッシュも消す場合は `-ClearCache` を追加します。既定アプリは変更せず、記録済みのレジストリ値を可能な範囲で復元します。

## トラブルシューティング

画像が出ない場合は、Explorerのプレビュー・サムネイル設定、インストール時のPASS表示、CLIの実行可否を確認してください。`Collect-Diagnostics.ps1` でログを採取できます。共有前にユーザー名、ローカルパス、機密情報を伏せてください。

詳細は [Troubleshooting](docs/troubleshooting.md) へ。ハングが起きる場合は `-PreviewOnly` / `-ThumbnailOnly` で切り分けできます。切り替え前には登録解除が必要です。

## 開発・公開状況

C#の実装とPowerShell/BATを同梱し、バイナリは含みません。CIはWindows上でのコンパイル・静的確認を対象とし、実機Explorer操作は含みません。

## ライセンス

本拡張は [MITライセンス](LICENSE) で提供します。SolidCraft本体には別のライセンスが適用されます。詳細は [ライセンス](docs/licensing.md) と [サードパーティ表記](THIRD_PARTY_NOTICES.md) を参照してください。
