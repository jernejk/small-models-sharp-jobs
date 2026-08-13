# Organiser pack

Global AI Construct Brisbane — 31 August 2026.

## Title

**Small Models, Sharp Jobs: Build a Local Agent That Proves Its Work**

## What attendees actually do

> Attendees complete an evidence lookup tool, connect a typed extraction step, add one deterministic
> verification rule, then run the application to create a claim ledger, verification report and cited
> incident brief.

This is a build-along, not a talk. Everyone leaves with a running .NET application they wrote part
of, and can explain.

## Abstract

Everyone has seen an agent demo. Far fewer have seen one show exactly why its output should be
trusted. In this hands-on workshop you'll build a .NET application with Microsoft Agent Framework
that reads a fictional service-incident pack, uses a small open-weight model to produce a typed claim
ledger, and turns only verified claims into a cited Markdown brief. Deterministic code — not another
model — checks evidence IDs, dates, numbers and source references, making omissions and fabrications
visible. Run the measured 4B reference model locally where laptop preflight passes, or point the same
Chat Completions client at a controlled hosted fallback by changing configuration only. You'll leave with a runnable repo and a
reusable pattern: narrow jobs, constrained tools, explicit workflows and verification before trust.

*"Proves its work" refers to the declared checks, not proof of every semantic claim or real-world
truth.*

## The promise, and its edges

**We promise** every attendee leaves with three generated artifacts and can explain which parts a
model produced and which parts ordinary code decided.

**We do not promise** the local model runs on every laptop. A configuration-only recovery lane
exists for machines that cannot. This is stated in the setup guide rather than discovered on the day.

## Format

| | |
| --- | --- |
| Length | 120 minutes preferred; a credible 60-minute cut exists |
| Level | Intermediate. Comfortable reading and editing C#. No ML background needed. |
| Capacity | Scales with facilitators; aim for one per ~15 attendees |
| Language/stack | C#, .NET 10, Microsoft Agent Framework 1.17.0 |

In the 120-minute version the core is complete by minute 90 and the final 30 minutes are spent
breaking the pipeline and inspecting verification. The 60-minute version uses a staged starter repo
and cuts model comparison, routing theory, Harness and DevUI.

## Prerequisites (attendees)

Laptop with:

- .NET 10 SDK
- Ollama, with `nemotron-3-nano:4b` pulled **before arriving** (~2.8 GB)
- The repo cloned and `dotnet restore` run once on a real connection

Full instructions with a four-command verification: [SETUP.md](SETUP.md).

No API key, account or credit card is needed for the default path.

**A discrete GPU is not required.** On the reference machine Ollama placed 70% of the model on the
GPU and the full path finished in about 20 seconds. We quote the measured placement, not the card:
the GPU is not enumerable from our environment.

## Asks of the organiser

1. **Send the setup guide at least one week out**, and again 48 hours before. The single biggest
   risk to this workshop is 40 people downloading 2.8 GB on venue Wi-Fi.
2. **Room with power and desks.** People are typing for an hour, not watching.
3. **A screen we can put a terminal on**, legibly. Font size 18+.
4. **Confirm the recovery lane** — either an organiser-owned OpenAI-compatible deployment (Azure
   OpenAI's `/openai/v1` route with a key works unchanged) plus the key to hand out, or accept that
   machines failing preflight pair up. Needs a decision two weeks out; see *Outstanding* below.
5. **One extra facilitator per ~15 attendees** if possible. The five-minute recovery rule needs
   someone free to walk over.
6. **Confirm the slot length** (60 vs 120) two weeks out. Both agendas are written; they are
   genuinely different workshops.

## Outstanding decisions

| Decision | Owner | Needed by | Notes |
| --- | --- | --- | --- |
| Slot length: 60 or 120 minutes | organiser | 2 weeks out | both agendas ready |
| Recovery lane: hosted endpoint or pair-up | organiser | 2 weeks out | Needs an OpenAI-compatible endpoint, a deployment name and a key. No code change; the seam is unit-tested but **no live call has been made**. Entra/`az login` is not implemented |
| Attendee runtime blessed as Ollama | done | — | LM Studio remains a compatibility target, not verified against these gates |

## Honesty note for anyone quoting this workshop

Performance and reliability figures come from **one** reference machine, measured 14 August 2026.
The local lane is verified; LM Studio parity, the hosted lanes, other operating systems and other
hardware are **not**. **A non-author has not yet rehearsed the 60-minute path, so the one-hour
agenda is credible rather than proven.**

Every claim is itemised as measured, documented, inferred or unverified in
[CLAIMS-AND-LIMITS.md](CLAIMS-AND-LIMITS.md). Please read it before repeating a number in
marketing copy.
