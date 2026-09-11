# Deucarian Tweens

Package ID: `com.deucarian.tweens`.
Follow the canonical Package Registry ARCHITECTURE.md and TWEENS_EXTRACTION_REVIEW.md.

Owns reusable tween scheduling, visibility profiles, explicit animated lifecycle
adapters and isolated editor previews. Does not own authoritative application
visibility, notification selection, model loading, materials or physics.

Use Common easing and safe object cleanup. Register sanitized Diagnostics for
live schedulers. Editor surfaces compose the shared Deucarian Editor shell.
No direct Debug calls, runtime reflection, per-object Update or per-tween
coroutines. Keep hot playback allocation-free after capacity/target warmup.
Tests must cover cancellation, reentrant callbacks, pooling, external destruction,
disabled parents, reduced motion and baseline restoration.

Work from develop. No publishing, releases or deployment without user approval.
Do not modify Library/PackageCache. Runtime, Editor and tests use separate asmdefs.
