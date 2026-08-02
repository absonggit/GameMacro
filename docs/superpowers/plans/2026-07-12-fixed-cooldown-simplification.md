# Fixed Cooldown Simplification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Remove all OCR and screen detection and ship a fixed-CD tracker driven only by physical skill key presses.

**Architecture:** `HybridCooldownTracker` becomes a local-only fixed timer. `PhysicalKeyboardHook` starts each rule's cooldown and cast timestamp while the selected game is foreground. WPF renders the timer over existing card icons and no longer initializes image capture or native OCR libraries.

**Tech Stack:** C# 12, .NET 8 WPF, Win32 read-only keyboard hook, xUnit 2.x

## Global Constraints

- Remove Tesseract, tessdata, image capture, OCR, brightness detection, region selection and calibration UI.
- Keep fixed base CD, cast time and physical key listening.
- Never send, suppress or modify input.
- Publish self-contained win-x64 folder without OCR native files.
- Do not commit Git changes.

### Task 1: Make Timer Local-Only

- [ ] Replace the interrupted OCR-ready test with tests proving local countdown continues until its configured end.
- [ ] Remove OCR correction methods from `HybridCooldownTracker`.
- [ ] Run timing tests and require PASS.

### Task 2: Remove OCR and Capture Stack

- [ ] Remove Tesseract and System.Drawing package references and tessdata content.
- [ ] Remove OCR, detection, capture, region selector and their tests.
- [ ] Remove obsolete screen-region and threshold fields from active UI while retaining JSON compatibility in models.
- [ ] Run the full test suite and repair only compile references caused by removal.

### Task 3: Simplify WPF Runtime

- [ ] Remove OCR monitor initialization, callbacks, diagnostics and capture buttons.
- [ ] Make F8 start only the physical keyboard hook and 100ms card refresh timer.
- [ ] Keep base CD, cast time, key listening, icon overlay and foreground-window guard.
- [ ] Update README for fixed-CD behavior.

### Task 4: Verify and Publish

- [ ] Run Release tests with zero failures and build with zero warnings/errors.
- [ ] Publish to `artifacts/win-x64-fixed` with `PublishSingleFile=false`.
- [ ] Verify the output has no Tesseract, Leptonica or tessdata files.
- [ ] Launch for four seconds, verify responsive, and close normally.
