# Deucarian Tweens

Version: 0.3.0 (development channel)

Shared enter/exit animation for Transform/GameObject and UI presentation.
This is a focused visibility engine, not a general-purpose DOTween replacement.
No LitMotion dependency is currently needed. Common owns easing; Tweens owns
scheduling, visibility sampling, lifecycle adapters and isolated previews.

## Authoring

Add **Tweened Visibility** to an object. Enter and Exit each have an animation
dropdown: Inherit, None, Scale, Slide, Fade, Scale And Fade. Timing and shape
overrides are optional. Prefer a visual child so animation does not affect the
object's logical pose or physics. Fade requires a CanvasGroup; mesh material
fading is the responsibility of the consuming renderer adapter.
Slide offsets use the target's local units. Generic visibility bindings do not
change physics colliders; consuming code must gate domain interaction when
needed. CanvasGroup interaction is gated immediately during hide.

Create a Tween Visibility Profile. Assign it to a Tween Visibility Scope for a
subtree, or to a specific object. A profile stored as
Resources/DeucarianTweenDefaults.asset supplies project defaults; code can also
call TweenVisibilityDefaults.Configure(profile).

Resolution order: project profile → nearest scope profile → object profile
(if assigned) → scope stable-ID override → per-object overrides. Missing profiles
use built-in scale defaults. Stable-ID overrides use exact ordinal identifiers.
Activity integration uses the full ModelElementId string (scheme:value).

The Inspector and Control Center Tweens tool preview the same runtime samples.
The workspace edits a selected profile with serialized Enter/Exit forms and Undo;
selecting or creating an asset never makes it the project default automatically.
Object Inspectors show resolved values and profile/scope/override provenance.
Effect, timing and shape overrides are independent and preserve inactive values.
Enter / Exit / Loop / scrub only affect an isolated preview, never scene objects.
Scrubbing follows the selected direction; leaving a page stops its preview.
Loop remains enabled while values change, including Undo/Redo. Active playback
uses edited duration, easing, shape and reduced-motion settings immediately;
changing duration preserves normalized time instead of restarting the cycle.
An edit that selects None, zero duration or reduced motion intentionally settles
that direction; Loop continues with the next direction after its usual pause.

Choose from 32 shared easing presets, or enable **Custom curve** for an editable
Unity AnimationCurve. The horizontal axis is normalized time (0–1), the vertical
axis is progress. Overshoot and nonmonotonic motion are supported; endpoints still
settle exactly. Null, empty and invalid curve samples fall back to the selected
preset. Per-object timing overrides include their own optional curve; disabling
an override or the custom-curve option preserves its authored keys.

Playback borrows the caller-owned curve and reads it live without copying keys
or allocating per tick. It never modifies the curve. For an immutable playback
snapshot, clone the profile or curve before starting playback, outside the hot
path. Previewing only samples the profile: it never changes scene objects.
The shared Editor package owns controls, colors and responsive layout. Inspectors
reuse those forms without embedding the Control Center sidebar or scale footer.
This native editor design requires Deucarian Editor 1.11.0 or newer and the expanded
preset list requires Common 0.3.0 or newer.

## Explicit lifecycle

Call Show(), Hide(), HideAndDestroy(), ShowImmediate() or HideImmediate() on the
component. For generated objects, reuse a TransformVisibilityBinding created by
TweenVisibilityBindings.Create(scheduler, target, stableId).
HideAndRelease(callback) returns an object to its pool after the exit completes.
A re-show cancels any pending destroy or pool callback, including during completion.

Direct SetActive(false), Destroy, scene unload, or disabling a parent cannot be
retroactively animated. They cancel playback without running destructive
completion callbacks. To show an exit, request it before disabling/destroying.
The package never intercepts every Unity object globally.

RestoreImmediate restores the authored pose and cancels pending completion work.
Dispose bindings you own when their lifetime ends. A TweenedVisibility component
owns its binding; consumers borrowing GetBinding() must not dispose it.
Configure transforms before creating a binding. Recreate the binding deliberately
if the authored resting pose changes.

## Playback and performance

TweenRuntime lazily creates one persistent runner independent of target objects.
It ticks a compact collection of active entries and disables its Update when
idle. Registration/cancellation uses generation-checked handles; new entries
created by callbacks wait until the next tick. Reuse bindings and pre-size
TweenScheduler for predictable allocations. Warmed-up tick allocation is tested.

Normal Unity Transform changes remain on the main thread. There is no per-object
Update or per-tween coroutine. No Jobs, Burst or managed worker threads are
required, which keeps the core suitable for WebGL; actual player performance
still needs measurement in the consuming build.

Use an explicit TweenScheduler in deterministic tests and editor previews.
Schedulers register sanitized active/peak/capacity/fault counters in Diagnostics
and unregister on Dispose. Exceptions from an active target are isolated and
counted, without retaining target data or exception messages.

Unscaled time defaults on for presentation. ReducedMotion on a profile/binding,
or TweenVisibilityDefaults.ReducedMotion, settles the next command immediately.
It is not a background OS accessibility preference detector.

## Integration boundaries

The separate, unpublished adoption workspace proposes these boundaries: UI keeps
its public transition/profile API; Notifications owns selection and row pooling;
Activity owns desired visibility; Report Viewer owns materials and selection.
Those consumer changes are not distributed by this package's editor UI rollout.
Only explicit visibility paths are intended for migration; unrelated icon/hover
motion stays with its existing owner.

## Validation and installation

Import the Visibility Playground sample and press Play. Runtime, Editor,
EditMode tests, PlayMode tests and sample assemblies are separate.
Run the shared Package Registry validator plus both Unity test platforms.
The initial implementation was tested on Unity 6000.3.5f1: 185 shared EditMode
tests and 14 core PlayMode tests passed. The later native editor rollout also
passed all 40 Tweens cases in its portfolio run. These are focused validation
results, not WebGL performance or Unity 2022.3 compatibility certification.

Development source is available at
`https://github.com/Deucarian/Tweens.git#develop`. There is no stable main channel,
tag or GitHub release yet. Installer catalog promotion and the separate consumer
adoption changes remain pending.

Until the live catalog includes Tweens, a disposable consumer can explicitly
declare these Git dependencies in its Packages/manifest.json:

```json
{
  "dependencies": {
    "com.deucarian.common": "https://github.com/Deucarian/Common.git#develop",
    "com.deucarian.diagnostics": "https://github.com/Deucarian/Diagnostics.git#develop",
    "com.deucarian.editor": "https://github.com/Deucarian/Editor.git#develop",
    "com.deucarian.logging": "https://github.com/Deucarian/Logging.git#develop",
    "com.deucarian.tweens": "https://github.com/Deucarian/Tweens.git#develop"
  }
}
```

Merge these entries into the existing dependency object; preserve the project's
other packages. Editor must resolve to 1.11.0 or newer and Common to 0.3.0 or newer. Git dependencies are
explicit because Unity cannot obtain Deucarian packages from npm/scoped registry.

CI runs the shared canonical validators against the pinned
[development-publication review](https://github.com/Deucarian/Package-Registry/blob/ff7a5c596358b0244a1d0d6df6f006aeaaae16d0/TWEENS_DEVELOPMENT_PUBLICATION.md).
This snapshot records ownership and bounded lifetime allowances without exposing
the planned stable URL in the live Installer catalog. Switch CI back to Registry
develop after approved stable/catalog promotion; no validation rule is disabled.

## Scale origin

Mesh visibility bindings capture the local bounds of child mesh/skinned renderers, including inactive children, once during warmup. Scale compensates the transform position so the visible centre stays fixed even when imported mesh coordinates are far from the transform origin. No meshes, materials or hierarchy are rewritten, and cancellation, completion and pooling restore the exact authored pose. Objects without mesh renderers retain their transform pivot. Select **Transform Origin** in Tweened Visibility when the authored pivot is intentional; the binding captures this setting when it is created.
