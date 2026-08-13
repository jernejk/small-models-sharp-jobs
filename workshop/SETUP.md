# Setup — do this before the workshop

Attendees complete an evidence lookup tool, connect a typed extraction step, add one deterministic
verification rule, then run the application to create a claim ledger, verification report and cited
incident brief.

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

Run all four. Each one must finish before you travel.

```bash
# 1. the repo
git clone <workshop-repo-url> && cd global-ai-construct-offline-workshop

# 2. the model  (~2.8 GB)
ollama pull nemotron-3-nano:4b

# 3. the NuGet packages  (~40 MB, needs network exactly once)
dotnet restore

# 4. prove it all works
dotnet test                                    # expect 63 passed
dotnet run --project src/Workshop.App -- smoke # expect: smoke: PASS [JACKDAW_OK]
```

If step 4 prints `smoke: PASS [JACKDAW_OK]`, you are ready. Nothing else needs the internet.

## 3. Platform notes

### Windows

Two supported shapes:

- **Windows + WSL2 (Ubuntu)** — what the reference machine ran. Install the .NET SDK and Ollama
  *inside* WSL. This is the best-understood path.
- **Native Windows** — install both natively. Not run end-to-end by us; see
  [CLAIMS-AND-LIMITS.md](CLAIMS-AND-LIMITS.md).

A discrete GPU helps but is not required. The reference machine has a 4 GB RTX 3050 Ti and Ollama
placed the model 70% GPU / 30% CPU, still finishing the full path in under 25 seconds.

### Apple Silicon

Install the .NET 10 SDK (arm64) and Ollama for macOS, then run the same four prefetch commands.
Unified memory means a 4B model is comfortable on 16 GB. Not run end-to-end by us; see
[CLAIMS-AND-LIMITS.md](CLAIMS-AND-LIMITS.md).

### Low-spec machine or no GPU

The model still runs on CPU, just slower. If your `smoke` command takes more than about 90 seconds,
use the recovery lane on the day rather than fighting it. Tell a facilitator at the start.

### LM Studio

LM Studio is a **compatibility target, not a blessed runtime.** It has not passed the same
end-to-end gates as Ollama. If you already run LM Studio, bring Ollama as well. `.env.example` has
the LM Studio settings for anyone who wants to try after the core works.

## 4. Recovery lane (no local model)

If your machine cannot run the model, the same application points at an organiser-provided endpoint
by changing configuration only — no code changes:

```bash
export MAF_ENDPOINT=<organiser supplies>
export MAF_MODEL=<organiser supplies>
```

The organiser will hand out details on the day. **Only the fictional evidence pack in this repo may
be sent anywhere off your machine.** Never point this at real incident data, a private repository or
a credential.

## 5. Nothing to bring but the laptop

No API key, no account, no sign-up is required for the default path. If a step asks you for a
credit card, you are on the wrong path — stop and ask.
