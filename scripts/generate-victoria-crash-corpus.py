#!/usr/bin/env python3
"""Generate the de-identified workshop corpus from the official Data Vic CKAN API.

No API key is required. The query requests only the fields used to construct the workshop schema,
plus FATALITY and PEDESTRIAN solely to enforce the exclusion rule. Those two filter fields are never
written to the output file.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from urllib.parse import urlencode
from urllib.request import urlopen

RESOURCE_ID = "5df1f373-0c90-48f5-80e1-7b2a35507134"
ENDPOINT = "https://discover.data.vic.gov.au/api/3/action/datastore_search"
FIELDS = [
    "ACCIDENT_NO", "ACCIDENT_DATE", "ACCIDENT_TYPE", "DCA_CODE_DESCRIPTION", "SEVERITY",
    "LGA_NAME", "DTP_REGION", "NO_OF_VEHICLES", "FATALITY", "PEDESTRIAN",
]
DEFAULT_OUTPUT = Path("facilitator/workshop/data/victoria-road-crash-sample.json")


def readable(value: str) -> str:
    return " ".join(value.lower().replace("(", " (").replace("/", " / ").split())


def normalise(row: dict[str, str]) -> dict[str, str]:
    category = readable(row["DCA_CODE_DESCRIPTION"])
    crash_type = readable(row["ACCIDENT_TYPE"])
    severity = row["SEVERITY"].title()
    lga = row["LGA_NAME"].title()
    region = row["DTP_REGION"].title()
    vehicles = row["NO_OF_VEHICLES"]
    accident = row["ACCIDENT_NO"]
    return {
        "id": accident,
        "date": row["ACCIDENT_DATE"],
        "title": f"Road crash record: {category}",
        "summary": (
            f"Historic non-fatal Victorian road-crash record in {lga}, {region}. "
            f"Crash type: {crash_type}; category: {category}; reported severity: {severity}; "
            f"vehicles recorded: {vehicles}."
        ),
        "severity": severity,
        "sourceReference": f"Victoria Road Crash Data: {accident}",
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--limit", type=int, default=1000)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()
    if not 1 <= args.limit <= 1000:
        parser.error("--limit must be between 1 and 1000")

    query = urlencode({
        "resource_id": RESOURCE_ID,
        "limit": args.limit,
        "filters": json.dumps({"FATALITY": "0", "PEDESTRIAN": "0"}),
        "fields": ",".join(FIELDS),
    })
    with urlopen(f"{ENDPOINT}?{query}", timeout=30) as response:  # noqa: S310 - fixed public HTTPS endpoint
        payload = json.load(response)
    if not payload.get("success"):
        raise RuntimeError(f"Data Vic CKAN request failed: {payload}")

    rows = payload["result"]["records"]
    if len(rows) != args.limit:
        raise RuntimeError(f"expected {args.limit} filtered records, received {len(rows)}")
    if any(row["FATALITY"] != "0" or row["PEDESTRIAN"] != "0" for row in rows):
        raise RuntimeError("source filters were not honoured")

    output = [normalise(row) for row in rows]
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {len(output)} non-fatal, non-pedestrian records to {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
