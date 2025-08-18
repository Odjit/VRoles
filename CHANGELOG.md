# Changelog

## [Unreleased]

### Added
- Public readiness API for external integrations:
  - `RoleService.IsReady`: indicates when roles/config are fully loaded.
  - `RoleService.RolesLoaded`: event fired after roles/config finish loading.
  - `RoleService.EnsureRolesLoaded()`: forces loading if not already ready.
- Guard flag to prevent duplicate Harmony patches (`_hookedUp`).
- Thread-safe singleton: `RoleService.Instance` now uses `Lazy<RoleService>` with `LazyThreadSafetyMode.ExecutionAndPublication`.
- Concurrency controls:
  - `_loadLock` to synchronize `LoadSettings()` and `EnsureRolesLoaded()` with double?checked locking.
  - `_hookLock` to serialize Harmony hook installation.

### Changed
- `RoleService` is now a lazy singleton (`Instance` created on first use) to avoid early initialization before plugin/Harmony are ready and to guarantee thread?safety.
- Startup order improvements:
  - In `Plugin.Load`, Harmony is initialized and commands are registered first; then `RoleService.Instance` is created and `EnsureRolesLoaded()` is called.
  - In `Core.InitializeAfterLoaded`, the service now uses `RoleService.Instance` instead of `new RoleService()` and calls `EnsureRolesLoaded()`.
- Improved debuggability of VCF hook selection:
  - Replaced null?coalescing lookup with explicit resolution and logs indicating whether `CommandRegistry.ExecuteCommandWithArgs` or `CommandRegistry.Handle` is patched.

### Fixed
- Prevented `NullReferenceException` during startup by guarding Harmony patching until `Plugin.Harmony` is initialized.
- Compatibility with different VCF versions by patching either `CommandRegistry.ExecuteCommandWithArgs` or `CommandRegistry.Handle`.
- Replaced empty catch blocks around logging with proper error reporting; if the logger fails, fall back to `Console.Error` and include the original exception.
- Wrapped hook setup in try/catch and added locks to avoid unhandled exceptions, race conditions, and log spam.

### Impact
- Stability: eliminates startup crashes/log spam related to premature patching and reduces concurrency issues.
- Integration: other plugins can safely detect readiness, wait for `RolesLoaded`, or force?load with `EnsureRolesLoaded()`.
- Debugging: clearer logs show which VCF entry point was patched.
- Backward?compatible: existing behavior remains; only adds APIs, thread?safety, and safer initialization.
