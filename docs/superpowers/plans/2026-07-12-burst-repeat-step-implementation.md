# Burst Repeat Step Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reliably send a no-cooldown skill five times during a five-second state and make every layered card fully visible.

**Architecture:** Persist repeat timing on the shared skill but apply it only while that skill is an active burst step. Extend the axis scheduler with a monotonic due-time and repeat counter; keep PNG readiness as a one-frame gate for explicitly configured no-cooldown repeats.

**Tech Stack:** .NET 8, WPF, xUnit

## Global Constraints

- No public cooldown or global delay.
- Repeat timing applies only inside the burst group.
- Normal skills retain two-frame confirmation and PNG transition blocking.
- Do not commit changes.

---

### Task 1: Timed repeat scheduler

- [ ] Write failing tests for 200ms first delay and five 1000ms-spaced sends.
- [ ] Add repeat properties to `MacroRule` and timed burst state to `RotationAxisScheduler`.
- [ ] Run focused tests to green.

### Task 2: Runtime fast repeat path

- [ ] Pass current time to scheduler selection and release recording.
- [ ] Bypass two-frame debounce only for an active no-cooldown repeat step.
- [ ] Keep one-frame PNG-ready validation before every repeat send.

### Task 3: Auto-enable and visible cards

- [ ] Enable a skill when dropped into either group.
- [ ] Add repeat controls to skill settings.
- [ ] Compact library cards and enlarge group content so no card is clipped.
- [ ] Mark disabled group references visibly and validate them at startup.

### Task 4: Verify and publish

- [ ] Update README with the F4/4 example.
- [ ] Run all tests and Release build.
- [ ] Publish and perform a startup smoke test.
