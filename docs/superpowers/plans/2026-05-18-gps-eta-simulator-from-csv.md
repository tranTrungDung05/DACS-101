# GPS ETA Simulator From CSV Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a dedicated GPS-only simulator for `1404157147620000079_gps.csv` and align the Python ETA API contract with the existing C# caller so each new GPS point can trigger ETA updates.

**Architecture:** Keep the existing `simulator.py` untouched. Add a new Python script with small helper functions for CSV loading and payload building, and update the ETA FastAPI app to accept the `gps_points`/`destination.{lat,lon}` contract that `ETAService` already sends.

**Tech Stack:** Python, FastAPI, requests, unittest

---

### Task 1: Add failing regression tests for simulator input parsing and ETA API contract

**Files:**
- Create: `tests/test_gps_eta_simulator.py`
- Create: `tests/test_eta_api_contract.py`
- Test: `python -m unittest tests.test_gps_eta_simulator tests.test_eta_api_contract -v`

- [ ] **Step 1: Write failing tests for the simulator helpers**
- [ ] **Step 2: Run the tests and verify import/behavior failures**
- [ ] **Step 3: Write failing tests for the ETA API request model contract**
- [ ] **Step 4: Run the tests and verify the contract mismatch fails**

### Task 2: Implement the GPS-only simulator and align the ETA API contract

**Files:**
- Create: `gps_eta_simulator.py`
- Modify: `Services/app.py`
- Test: `python -m unittest tests.test_gps_eta_simulator tests.test_eta_api_contract -v`
- Test: `python -m py_compile gps_eta_simulator.py Services/app.py`

- [ ] **Step 1: Implement CSV loading that accepts the repo CSV header**
- [ ] **Step 2: Implement payload building and request playback every 15 seconds**
- [ ] **Step 3: Update the ETA FastAPI request models to match the C# payload**
- [ ] **Step 4: Run tests and compile checks**

### Task 3: Smoke-check the simulator CLI defaults

**Files:**
- Modify: `gps_eta_simulator.py`
- Test: `python gps_eta_simulator.py --help`

- [ ] **Step 1: Add CLI defaults for the provided CSV and localhost controller URL**
- [ ] **Step 2: Run the help command to confirm the script is discoverable**

## Self-Review

- Spec coverage:
  - Separate simulator file: covered by Task 2.
  - CSV source `1404157147620000079_gps.csv`: covered by Task 2 and Task 3.
  - 15-second cadence: covered by Task 2.
  - GPS-only payload: covered by Task 2.
  - ETA trigger compatibility with current controller: covered by Task 1 and Task 2.
- Placeholder scan:
  - No placeholders remain.
- Type consistency:
  - Simulator payload uses `VehicleID`, `Latitude`, `Longitude`, `Speed`.
  - ETA API accepts `gps_points` plus destination `lat`/`lon`.
