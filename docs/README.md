# Documentation Index

## Main Documentation
- **AGENTS.md** - Project architecture, migration steps, services pattern, lessons learned
- **PROGRESS.md** - Overall refactoring progress, phase status, current state

## Component Extraction
- **COMPONENT_EXTRACTION_PROGRESS.md** - Detailed progress tracking for component dependency extraction
- **COMPONENT_EXTRACTION_STRATEGY.md** - Strategy and patterns for extracting dependencies
- **ERROR_INVESTIGATION.md** - Explanation of error count fluctuations and compilation stages

## Error Investigation

### Understanding Error Count Fluctuations
When fixing compilation errors, you may observe error counts that appear to increase. This is **normal and expected behavior**:

- Fixing errors allows the compiler to proceed further
- New errors are revealed that were previously hidden
- Error types shift: syntax → type → semantic errors
- This progression indicates forward progress

See `ERROR_INVESTIGATION.md` for a detailed explanation.

### Current Build Status
- **Errors**: 0
- **Warnings**: present (mostly net6.0 TFM support + nullable warnings)

See `COMPONENT_EXTRACTION_PROGRESS.md` for current status and breakdown.

## Architecture
- **ARCHITECTURE_ANALYSIS.md** - Reference architecture from JobFinder-refactored
- **MAINLAYOUT_REFACTORING_SUMMARY.md** - Summary of MainLayout refactoring

## Services
- **SERVICES_IMPLEMENTATION_PLAN.md** - Implementation plan for dashboard services
