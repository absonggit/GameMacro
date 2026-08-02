# Pure Visual One-second GCD Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove local cooldown prediction and drive all skill selection from the captured original icon with only a one-second global lock.

**Architecture:** The visual scheduler ignores tracker state and scans all enabled rules. A ready-frame debouncer requests a key; successful input immediately starts a one-second scheduler lock. UI state is visual-only.

**Tech Stack:** .NET 8, WPF, xUnit, Win32 SendInput.

## Tasks

1. Add failing tests for tracker-independent visual selection, exact one-second lock, and `~` parsing/options.
2. Simplify release controller to ready debouncing only and scheduler to fixed global lock.
3. Remove timing fields from UI and replace cooldown labels with visual state labels.
4. Update README, run Release verification, publish, and smoke-test startup.

**Constraint:** Keep legacy JSON properties for compatibility and do not commit Git changes.
