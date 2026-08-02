# Compact No-overlay Half-second GCD Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans.

**Goal:** Use a 0.5-second global lock and compact the main UI while removing the runtime overlay.

**Tasks:**
1. Write a failing scheduler boundary test and change the global lock to exactly 500ms.
2. Delete overlay window files and remove its lifecycle/native-style integration.
3. Rebuild MainWindow XAML into compact queue, one-row skill editor, and bottom hotkey controls.
4. Update README, run Release tests/build, publish, and smoke-test startup.

**Constraint:** Preserve pure visual selection, drag priority, preset keys, and do not commit Git changes.
