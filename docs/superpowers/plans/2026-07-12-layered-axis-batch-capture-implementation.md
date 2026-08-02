# Layered Axis and Batch Capture Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace role checkboxes with draggable shared skill groups and remove multi-skill capture latency.

**Architecture:** Persist ordered skill IDs for burst and base groups while keeping one `MacroRule` instance per skill. Schedule from these explicit groups and capture one bounding frame per tick for all icon classifications.

**Tech Stack:** .NET 8, WPF, xUnit, Win32 GDI

## Global Constraints

- PNG availability remains authoritative.
- Base group scans left-to-right from the beginning every tick.
- Burst group starts only when every member is ready.
- No fixed public cooldown, cast delay, or network delay.
- Do not commit changes.

---

### Task 1: Explicit group model and scheduler

- [ ] Add ordered burst/base rule ID lists to `MacroProfile`.
- [ ] Write failing scheduler tests for all-ready burst and left-priority base selection.
- [ ] Update `RotationAxisScheduler` and migration behavior.
- [ ] Run focused tests.

### Task 2: Single-frame multi-skill capture

- [ ] Add crop tests for BGRA buffers.
- [ ] Implement one bounding BitBlt and per-rule signature cropping.
- [ ] Integrate batch signatures into the automation tick.

### Task 3: Layered drag-and-drop UI

- [ ] Replace role checkboxes with skill library, burst group, and base priority group.
- [ ] Support library-to-group copy, group reorder, and group-reference removal.
- [ ] Keep skill-library deletion cascading to both ID lists.

### Task 4: Debounce, docs, and verification

- [ ] Reduce ready confirmation to two frames.
- [ ] Update README with layered configuration and base priority semantics.
- [ ] Run full tests, build, publish, and startup smoke test.
