# Workshop incident data

## Victorian road-crash sample

`victoria-road-crash-sample.json` is a compact, attributed teaching subset of the Victorian
Government's **Victoria Road Crash Data**. The source is owned and published by the Victorian
Department of Transport and Planning (DTP), and was retrieved on 29 August 2026 from the official
[dataset page](https://discover.data.vic.gov.au/en_AU/dataset/victoria-road-crash-data) using its
[direct, queryable CSV resource](https://opendata.transport.vic.gov.au/dataset/bb77800e-1857-4edc-bf9e-e188437a1c8e/resource/5df1f373-0c90-48f5-80e1-7b2a35507134/download/victorian_road_crash_data.csv).

The corpus is generated reproducibly from Data Vic's public CKAN endpoint, not from a full CSV
download. Run `python3 scripts/generate-victoria-crash-corpus.py` from the repository root. It calls
`https://discover.data.vic.gov.au/api/3/action/datastore_search` with resource ID
`5df1f373-0c90-48f5-80e1-7b2a35507134`, limit `1000`, explicit fields, and CKAN filters
`FATALITY=0` and `PEDESTRIAN=0`. The script fetches the two filter fields only to enforce exclusion;
it never writes them to the workshop JSON.

The official metadata identifies the resource as **Creative Commons Attribution 4.0 International**.
This local adaptation is licensed under the same terms. Attribution: **© State of Victoria,
Department of Transport and Planning, Victoria Road Crash Data, licensed under CC BY 4.0**. It has
been adapted for an educational workshop; the source and this adaptation are not endorsements of
the workshop. The licence requires attribution, a link to the licence, and an indication of changes:
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

### Curation and sensitivity

The file contains 1,000 historic, crash-level records. It is a bounded, non-exhaustive subset
selected from the official resource's `ACCIDENT`-level fields through its public data API. It excludes
fatal crashes, pedestrian crashes, road names, street-level locations, coordinates, times of day,
person counts and categories, and all person or vehicle-table fields. It does not identify any
individual. Road trauma is still sensitive subject matter: present the records respectfully and do
not infer causes, blame, or personal outcomes from this compact sample.

The source fields retained or transformed are `ACCIDENT_NO`, `ACCIDENT_DATE`, `ACCIDENT_TYPE`,
`DCA_CODE_DESCRIPTION`, `SEVERITY`, `LGA_NAME`, `DTP_REGION`, and `NO_OF_VEHICLES`. In the workshop
schema these become `id`, `date`, `title`, `summary`, `severity`, and `sourceReference`. Those six
fields are the whole record: the pack sent to Extract and Analyse carries nothing else.

Designed deterministic Gather queries:

- supported/bounded: `--term intersection --max 8` returns a mixed capped evidence pack;
- selective: `--term "rear end" --max 3` returns a smaller set;
- no result: `--term cyclist` returns no evidence.

Use it explicitly until the workshop's default dataset is changed and rehearsed:

```bash
cd workshop/06-workflow
dotnet run --project src/Workshop.App -- gather --term intersection --max 8
```

## Synthetic fallback

`synthetic-incident-records.json` is fictional training data created for this workshop on 29 August
2026. The Victorian sample is the default (`Program.cs` resolves it unless `--dataset` is given); the
synthetic file is the recovery fallback via `--dataset workshop/data/synthetic-incident-records.json`.
It is not a government report or a record of an actual incident.

The Gather step reads one approved local file in ordinary C# and filters it in memory. It does not
accept a filename from the model or user and it does not expose arbitrary filesystem access.

Alternative candidates, their evidence status, and the verification needed before use are recorded
in [SOURCE-ASSESSMENT.md](SOURCE-ASSESSMENT.md).
