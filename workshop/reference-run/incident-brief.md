# Incident brief: INC-042

Rendered by deterministic code from verified claims only. The model did not write this file.

## Verified facts

| Claim | Kind | Value | Evidence |
| --- | --- | --- | --- |
| C001 | incident_id | INC-042 | `status.txt`: "INC-042 STATUS PAGE" |
| C002 | severity | SEV-2 | `status.txt`: "Severity: SEV-2" |
| C003 | affected_customers | 7 | `status.txt`: "Impact: 7 customers could not submit construction inspection forms." |
| C004 | timestamp | 2026-08-13T09:12:00+10:00 | `status.txt`: "Started: 2026-08-13T09:12:00+10:00" |
| C005 | timestamp | 2026-08-13T09:39:00+10:00 | `status.txt`: "Resolved: 2026-08-13T09:39:00+10:00" |

## Timeline

Parsed from `events.csv` by code, not by the model.

| Time | Event |
| --- | --- |
| 2026-08-13T09:12:00+10:00 | error rate alert fired |
| 2026-08-13T09:21:00+10:00 | stale routing rule identified |
| 2026-08-13T09:34:00+10:00 | routing rule replaced |
| 2026-08-13T09:39:00+10:00 | submissions verified healthy |

Duration from source parsing: 27 minutes (2026-08-13T09:12:00+10:00 to 2026-08-13T09:39:00+10:00).

## Shown but not verified

Deterministic checks cannot confirm these. They are reported, not asserted.

- **event**: cannot submit inspection forms — `customer-email.txt`: "Cannot submit inspection forms"
  - R11-EVENT-SUPPORTED: 'cannot submit inspection forms' is not one of the events code parsed from events.csv (error rate alert fired; stale routing rule identified; routing rule replaced; submissions verified healthy)
- **cause**: new billing system — `customer-email.txt`: "the new billing system"
  - R9-CAUSE-UNVERIFIED: cause cannot be established from the evidence pack by deterministic checks

## Excluded by verification

_No claim failed verification._

## Verification summary

- passed: 30
- failed: 0
- unverified: 2

Full detail is in `verification.json`.
