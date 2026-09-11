# Changelog

## [Unreleased]

- Include the previously documented VisibilityPlayground scene, ready to open and play.
- Choose a shader appropriate to the active built-in/URP renderer and allow an Inspector-assigned preview material without taking ownership of it.

## [0.2.0] - 2026-09-11

- Keep preview loops running while profile/override values change and during Undo/Redo.
- Refresh active playback at its existing normalized time, using the latest shape, easing, timing and reduced-motion values.
- Add optional custom AnimationCurve authoring for profile and per-object timing, with safe invalid/empty fallback and exact endpoints.
- Reuse Common 0.3.0's 32 serialized-compatible easing presets and shared Editor 1.11.0 controls.
- Cover live retiming, curve ownership/overshoot/fallback, allocation-free refresh and editor loop persistence.

## [0.1.0] - 2026-09-10

- Introduce active-only, generation-safe tween scheduling with sanitized diagnostics.
- Add reusable visibility progress, profiles and per-object/stable-ID overrides.
- Add explicit animated show/hide/destroy/pool handling with reversal and cancellation.
- Add shared editor preview and playable visibility sample.
- Refine the Control Center profile workspace and composed object/profile Inspectors with shared Editor controls, serialized Undo, resolved provenance and independent overrides.
- Require Editor 1.10.8 for the shared native design and compatibility fixes.
- Preserve preview direction during scrubbing and stop loops on page detach while supporting a later return.
- Add scheduler allocation, lifecycle, preview and Unity integration tests.
- Publish the initial develop Git channel with shared validation pinned to its development-only governance review; stable/catalog promotion remains separate.
