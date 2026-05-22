# Terminal ETA Output Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show ETA predictions directly in `gps_eta_simulator.py` so the user can monitor ETA in the terminal without opening the web UI.

**Architecture:** Keep `GpsController` unchanged and let the simulator call the ETA API directly using the same observed GPS points and the same destination resolution rules from `appsettings.json`. Add small helper functions for destination lookup and ETA payload/formatting so the new behavior stays testable.

**Tech Stack:** Python, requests, JSON config parsing, unittest

---

### Task 1: Add failing tests for destination resolution and ETA status formatting

**Files:**
- Modify: `tests/test_gps_eta_simulator.py`
- Test: `python -m unittest tests.test_gps_eta_simulator -v`

- [ ] **Step 1: Add a failing test for per-vehicle destination override and fallback**
- [ ] **Step 2: Add a failing test for ETA status formatting**
- [ ] **Step 3: Run the tests and verify they fail before implementation**

### Task 2: Implement terminal ETA output in the simulator

**Files:**
- Modify: `gps_eta_simulator.py`
- Test: `python -m unittest tests.test_gps_eta_simulator -v`
- Test: `python -m py_compile gps_eta_simulator.py`

- [ ] **Step 1: Implement appsettings destination resolution helpers**
- [ ] **Step 2: Implement ETA status generation from observed points**
- [ ] **Step 3: Print ETA information after each successful GPS submission**
- [ ] **Step 4: Run tests and compile verification**

## Self-Review

- Spec coverage:
  - Terminal ETA output: covered by Task 2.
  - Reuse current destination config: covered by Task 1 and Task 2.
  - No web UI dependency: covered by Task 2.
- Placeholder scan:
  - No placeholders remain.
- Type consistency:
  - Simulator continues using `VehicleID`, `Latitude`, `Longitude`, `Speed`.
  - ETA requests continue using `gps_points` plus `destination.lat/lon`.
