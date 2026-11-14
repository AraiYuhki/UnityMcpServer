# Changelog

このプロジェクトに対するすべての重要な変更はこのファイルに記録されます。

フォーマットは [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に基づいています。

## [Unreleased]

## [2.8.0] - 2026-11-14

### Added
- `Tools/Randomize MCP Server Port` メニューを追加
  - 他のプロセス（別のUnityエディタ等）が使用中のポートを避けてランダムにポートを選択し、
    `McpServerSetting` へ保存してサーバーを自動で再起動する
  - プロジェクト直下の `.mcp.json` に `unity-mcp` のエントリがあれば、URLのポートも自動更新する
    （クライアント側は再接続が必要）


## [2.7.0] - 2026-11-09

### Changed
- Unity 6000.6.8f1 対応を確認

## [2.6.0] - 2026-10-25

### Changed
- Unity 6000.6.7f1 対応を確認

## [2.5.1] - 2026-10-20

### Fixed
- プロジェクト起動・ドメインリロード後、コンパイル完了済みにもかかわらず `compileState` が `pending` のまま残る問題を修正
  - ドメインリロード直後の `OnPostprocessAllAssets`（`didDomainReload == true`）をPending化の対象から除外
  - リロードを跨いで残ったPendingを、リロード後の復元時に完了へ解消


## [2.5.0] - 2026-10-18

### Changed
- Unity 6000.6.6f1 対応を確認

## [2.4.0] - 2026-10-12

### Changed
- Unity 6000.6.5f1 対応を確認

## [2.3.0] - 2026-10-07

### Changed
- Unity 6000.6.4f1 対応を確認
