# Porto ETA Notebook Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a starter Jupyter notebook that reads the Porto taxi `train.csv` dataset and explains each step with alternating markdown and code cells.

**Architecture:** The notebook will stay exploration-first. It will load a manageable sample from `train.csv`, inspect the schema, perform basic quality checks, and parse `POLYLINE` into trajectory features that can later be converted into ETA labels.

**Tech Stack:** Jupyter Notebook, Python, pandas, ast, pathlib

---

### Task 1: Plan and file structure

**Files:**
- Create: `docs/superpowers/plans/2026-05-13-porto-eta-notebook.md`
- Create: `notebooks/porto_eta_exploration.ipynb`

- [ ] **Step 1: Confirm the notebook structure**

Use alternating cells in this order:
- Markdown: notebook objective and dataset framing
- Code: import libraries, set dataset path, read sample rows from `train.csv`
- Markdown: explain schema inspection
- Code: show shape, columns, dtypes, and head
- Markdown: explain quality checks
- Code: inspect `MISSING_DATA`, null counts, and empty trajectories
- Markdown: explain trajectory parsing and ETA derivation
- Code: parse `POLYLINE` and compute simple trip-duration labels

- [ ] **Step 2: Keep scope intentionally small**

Do not implement training yet. The notebook should stop after producing features and a simple ETA target preview.

### Task 2: Create the notebook

**Files:**
- Create: `notebooks/porto_eta_exploration.ipynb`

- [ ] **Step 1: Add the opening markdown cell**

Include:
- notebook title
- dataset name
- project objective: derive ETA targets from raw GPS trajectories

- [ ] **Step 2: Add the first code cell**

Include:
- imports: `ast`, `pathlib.Path`, `pandas`
- `DATA_PATH = Path("../train.csv")`
- `SAMPLE_ROWS = 50000`
- `df = pd.read_csv(DATA_PATH, nrows=SAMPLE_ROWS)`
- print loaded row count and file path

- [ ] **Step 3: Add inspection cells**

Show:
- `df.shape`
- `df.columns.tolist()`
- `df.dtypes`
- `df.head(3)`

- [ ] **Step 4: Add quality-check cells**

Show:
- missing values per column
- `MISSING_DATA` counts
- count of empty `POLYLINE`

- [ ] **Step 5: Add trajectory parsing cells**

Implement:
- helper `parse_polyline`
- `num_points`
- `trip_duration_seconds = (num_points - 1) * 15`
- preview selected columns

### Task 3: Validate the notebook

**Files:**
- Test: `notebooks/porto_eta_exploration.ipynb`

- [ ] **Step 1: Validate JSON structure**

Run: `python -m json.tool notebooks/porto_eta_exploration.ipynb >/dev/null`
Expected: command exits successfully with no output

- [ ] **Step 2: Spot-check notebook metadata**

Run: `sed -n '1,120p' notebooks/porto_eta_exploration.ipynb`
Expected: notebook contains alternating markdown and code cells

### Self-review

- Spec coverage: the notebook starts with dataset loading, then explains schema, quality checks, and ETA-ready parsing.
- Placeholder scan: no TODO or TBD content remains in the plan.
- Type consistency: `POLYLINE` is treated as a string column before parsing into Python lists.
