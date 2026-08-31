# Setup — do this before the workshop

Attendees complete two typed model steps in the crash-review pipeline — Extract and Analyse — behind
Gather's deterministic evidence pack and code-owned validation gates.

**Do this on the network you have at home or in the office, not on venue Wi-Fi.** You are
downloading roughly 3.5 GB. Once it is cached, the workshop runs offline.

Budget 20–30 minutes on a normal connection.

## 1. Prerequisites

| What | Version we tested | Check |
| --- | --- | --- |
| .NET SDK | 10.0.302 | `dotnet --version` |
| Ollama | 0.32.9 | `ollama --version` |
| Git | any | `git --version` |

.NET 10 SDK: <https://dotnet.microsoft.com/download/dotnet/10.0> · Ollama: <https://ollama.com/download>

Any .NET 10 SDK should work; 10.0.302 is what the reference machine ran.

## 2. Prefetch checklist

Run every command in this block. Each one must finish before you travel.

```bash
# 1. the repo
git clone https://github.com/jernejk/small-models-sharp-jobs && cd small-models-sharp-jobs

# 2. the model  (~2.8 GB)
ollama pull nemotron-3-nano:4b

# 3. the NuGet packages  (~40 MB, needs network exactly once)
#    Restore the final lab: it is the only one that references every package.
dotnet restore workshop/06-workflow/Workshop.slnx

# 4. prove it all works
cd workshop/01-getting-started && dotnet build
dotnet run --project src/Workshop.App                   # expect: WORKSHOP_OK once CP-01 is done
cd ../06-workflow && dotnet run --project src/Workshop.App -- ready --prompt "Show up to 5 intersection crashes from 2012." # expect: READY: model-backed supported path completed.
```

Labs 01 and 02 are one `Program.cs` each with no test project; tests start at lab 03. There the two
test projects report separately: `Workshop.Core.Tests` 9 passed, `Workshop.LocalModel.Tests` 22
passed with 5 skipped. The 5 skips need a running model — `WORKSHOP_LOCAL_MODEL=1 dotnet test` runs
all 27.

`Workshop.LocalModel.Tests` is 22 + 5 skipped in every lab. `Workshop.Core.Tests` grows as the
contract does: **9** in labs 01–02, **10** in 03–05 (the filter is validated and clamped from 03),
and **11** in 06 and the reference solution.

### Point the app at your model

`dotnet user-secrets` is the recommended way to set this: the values stay out of the repo and out of
your shell history. Run from the repo root.

```bash
# Ollama (the default — you only need this if you changed something)
dotnet user-secrets --project workshop/01-getting-started/src/Workshop.App set MAF_ENDPOINT http://localhost:11434/v1
dotnet user-secrets --project workshop/01-getting-started/src/Workshop.App set MAF_MODEL nemotron-3-nano:4b
dotnet user-secrets --project workshop/01-getting-started/src/Workshop.App set MAF_API_KEY ollama

# LM Studio
dotnet user-secrets --project workshop/01-getting-started/src/Workshop.App set MAF_ENDPOINT http://localhost:1234/v1
dotnet user-secrets --project workshop/01-getting-started/src/Workshop.App set MAF_MODEL nvidia-nemotron-3-nano-4b
dotnet user-secrets --project workshop/01-getting-started/src/Workshop.App set MAF_API_KEY lm-studio
```

`dotnet user-secrets --project workshop/01-getting-started/src/Workshop.App list` shows what is set; `remove MAF_ENDPOINT` or
`clear` undoes it. Keys: `MAF_ENDPOINT`, `MAF_MODEL`, `MAF_API_KEY`, `MAF_TIMEOUT_SECONDS`.

Precedence is **shell variables > user-secrets > `.env` in the lab root > built-in defaults**, so an
`export` in your terminal wins over everything and is the fastest way to try one value. Copying
`.env.example` to `.env` also works; it loses to a secret with the same key. The lab root is the
numbered folder you are working in — `workshop/03-gather/.env`, not one `.env` at the top of the
clone — because each lab is its own self-contained build. Labs 01 and 02 are stripped to one file
and read shell variables and user-secrets only, so they ship no `.env.example`.

Every lab prints its endpoint and model on the first line, which is the 1-second check that the
right server answered. LM Studio serves whichever model is loaded no matter what `MAF_MODEL` says,
so a wrong name still succeeds — check the `model=` line in LM Studio's server log if results look
odd.

If step 4 prints `READY: model-backed supported path completed.`, you are ready. Nothing else needs
the internet. Run that final command inside `workshop/06-workflow`; the earlier numbered labs
intentionally stop at their one focused TODO.

`ready` is deliberately more than a smoke test: it runs the whole Gather → Extract → Analyse path
once against your loaded model and checks it reaches the supported outcome inside the 90-second
per-call budget. A model that merely answers is not enough — it has to clear every gate.

## 3. Platform notes

### Windows

Two supported shapes:

- **Windows + WSL2 (Ubuntu)** — what the reference machine ran. Install the .NET SDK and Ollama
  *inside* WSL. This is the best-understood path.
- **Native Windows** — install both natively. Not run end-to-end by us; see
  [CLAIMS-AND-LIMITS.md](../archive/retired-pre-restructure-material/CLAIMS-AND-LIMITS.md).

A discrete GPU helps but is not required. On the reference machine Ollama placed the model 70% GPU / 30% CPU
and the full path finished in about 20 seconds. We cannot enumerate that GPU from our environment,
so we quote the placement, not the card.

### Apple Silicon

Install the .NET 10 SDK (arm64) and Ollama for macOS, then run the same four prefetch commands.
Unified memory means a 4B model is comfortable on 16 GB. Not run end-to-end by us; see
[CLAIMS-AND-LIMITS.md](../archive/retired-pre-restructure-material/CLAIMS-AND-LIMITS.md).

### Low-spec machine or no GPU

The model still runs on CPU, just slower. If your `ready` command fails or takes more than about 90 seconds,
use the recovery lane on the day rather than fighting it. Tell a facilitator at the start.

### LM Studio

LM Studio is a first-class runtime: every presenter rehearsal and the seven-model benchmark ran on it.
Load `nvidia-nemotron-3-nano-4b`, start the server (port 1234), then set the LM Studio values from
[Point the app at your model](#point-the-app-at-your-model). For a one-off run, exporting works too:

```bash
export MAF_ENDPOINT=http://localhost:1234/v1
export MAF_MODEL=nvidia-nemotron-3-nano-4b
export MAF_API_KEY=lm-studio
```

`.env.example` has both sets; copy it to `.env` if you prefer a file over user-secrets.

## 4. Recovery lane (no local model)

If your machine cannot run the model, the same application points at an organiser-provided endpoint
by changing configuration only — no code changes:

```bash
dotnet user-secrets --project workshop/01-getting-started/src/Workshop.App set MAF_ENDPOINT <participant-owned endpoint>
dotnet user-secrets --project workshop/01-getting-started/src/Workshop.App set MAF_MODEL <participant-selected model>
dotnet user-secrets --project workshop/01-getting-started/src/Workshop.App set MAF_API_KEY <participant-owned key if required>
```

Keep the key in user-secrets rather than `.env` so it cannot be committed. The organiser will hand
out details on the day. On OpenRouter only `nvidia/nemotron-3-ultra-550b-a55b:free`
was confirmed working on 30 Aug (`qwen/qwen3-coder:free` and `openai/gpt-oss-120b:free` return 404
"unavailable for free"), and roughly 1 call in 3 fails with "provider returned no answer (upstream
overloaded)" — just run the command again. **Only the bundled Victorian crash sample in this
repo may be sent anywhere off your machine.** Never point this at real incident data, a private repository or
a credential.

## 5. Nothing to bring but the laptop

No API key, no account, no sign-up is required for the default path. If a step asks you for a
credit card, you are on the wrong path — stop and ask.
