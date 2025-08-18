# Changelog

## [Unreleased]

### Added
- Public readiness API for external integrations:
  - `RoleService.IsReady`: indicates when roles/config are fully loaded.
  - `RoleService.RolesLoaded`: event fired after roles/config finish loading.
  - `RoleService.EnsureRolesLoaded()`: forces loading if not already ready.
- Guard flag to prevent duplicate Harmony patches (`_hookedUp`).

### Changed
- `RoleService` is now a lazy singleton (`Instance` created on first use) to avoid early initialization before plugin/Harmony are ready.
- Startup order improvements:
  - In `Plugin.Load`, Harmony is initialized and commands are registered first; then `RoleService.Instance` is created and `EnsureRolesLoaded()` is called.
  - In `Core.InitializeAfterLoaded`, the service now uses `RoleService.Instance` instead of `new RoleService()` and calls `EnsureRolesLoaded()`.

### Fixed
- Prevented `NullReferenceException` during startup by guarding Harmony patching until `Plugin.Harmony` is initialized.
- Compatibility with different VCF versions by patching either `CommandRegistry.ExecuteCommandWithArgs` or `CommandRegistry.Handle`.
- Wrapped hook setup in try/catch to avoid unhandled exceptions and log spam.

### Impact
- Stability: eliminates startup crashes/log spam related to premature patching.
- Integration: other plugins can safely detect readiness, wait for `RolesLoaded`, or force-load with `EnsureRolesLoaded()`.
- Backward-compatible: existing behavior remains; only adds APIs and safer initialization.
