# Two-row Drag, Hotkeys, and Overlay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add configurable network margins, preset keys, drag-priority two-row cards, configurable global hotkeys, and a topmost runtime overlay.

**Architecture:** Pure model helpers validate key assignments and reorder rules. WPF uses a two-row uniform layout and native drag/drop. A separate non-activating window binds to the same rules and toggles click-through using Win32 styles.

**Tech Stack:** .NET 8, WPF, Win32, xUnit.

## Global Constraints

- Preserve visual closed-loop semantics and 1-second global cooldown.
- Maximum 16 skills; no OCR or game-memory access.
- Do not commit changes to Git.

---

### Task 1: Models, Preset Keys, and Validation
- [ ] Add failing tests for network margin, preset keys, conflicts, and overlay persistence.
- [ ] Add `NetworkMarginMs`, `EmergencyHotkey`, overlay coordinates, preset-key provider, and conflict validator.
- [ ] Use per-rule margin in confirmation deadline and run focused tests.

### Task 2: Reorder and Two-row Queue
- [ ] Add failing reorder tests proving insert semantics and Priority 1～N.
- [ ] Implement reorder helper and native WPF drag/drop handlers.
- [ ] Use an 8-column, 2-row layout; remove name/priority and movement controls.

### Task 3: Preset Editors and Dynamic Hotkeys
- [ ] Replace skill key textbox with preset ComboBox and add network-margin field.
- [ ] Add profile-level toggle/emergency ComboBoxes and validate conflicts on save/start.
- [ ] Re-register global hotkeys and update button labels after profile changes.

### Task 4: Runtime Overlay
- [ ] Create topmost non-activating overlay bound to enabled rules.
- [ ] Add Ctrl-controlled click-through and drag-to-move behavior.
- [ ] Show/minimize on start and close/restore on stop; persist coordinates.

### Task 5: Verify and Publish
- [ ] Run complete Release tests and build.
- [ ] Publish to `artifacts/win-x64-auto`, verify no OCR files, and smoke-test startup.
