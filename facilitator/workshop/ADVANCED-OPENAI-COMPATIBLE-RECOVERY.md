# Advanced recovery: an OpenAI-compatible endpoint

This optional guide is for a facilitator or an advanced participant whose local 4B model is not
ready. It is not part of the official attendee path: the workshop is designed around a local 4B
model, saved checkpoints, and the deterministic Gather stage.

Microsoft Agent Framework can use any endpoint that genuinely implements the OpenAI-compatible chat
surface. The application already reads these settings without printing their values:

```bash
export MAF_ENDPOINT=https://<provider-host>/v1
export MAF_API_KEY=<provider-issued-key>
export MAF_MODEL=<provider-model-name>
export MAF_TIMEOUT_SECONDS=90
```

The endpoint, key and model must come from the participant's own compatible provider or approved
organisation service. Validate in this order, stopping at the first failure:

```bash
dotnet run --project src/Workshop.App -- smoke                  # harmless hello
dotnet run --project src/Workshop.App -- typed                  # no-tool typed output
dotnet run --project src/Workshop.App -- gather --term intersection  # no endpoint involved
dotnet run --project src/Workshop.App -- run --prompt "Show up to 5 intersection crashes from 2012." # Query → Gather → Extract → Analyse
```

Every model-backed command prints `HOSTED` once the endpoint is not loopback; if it still prints
`LOCAL`, the environment variables did not reach `dotnet run`. Do not treat a successful request as
proof that the provider supports tools plus schema-constrained JSON in the same call.

### Optional participant-owned OpenRouter route

[OpenRouter's official quickstart](https://openrouter.ai/docs/quickstart) documents its
OpenAI-compatible chat-completions endpoint. A participant may choose to use their own account and
key as an optional recovery route, configuring the endpoint, key and model through the shape above.
Costs, quotas, availability, data handling and model choice remain the participant's responsibility.
This repository has not live-authenticated that integration. Rehearse the exact selected model for
hello, typed output, tool calls and the combined tool/structured-output case before presenting it.

## What llmyard is — and is not

`llmyard` is JK's local control plane/proxy. It can manage virtual keys, budgets and routing for
services behind it. It is **not** a free-model provider, public attendee service, or a dependency of
this workshop. Do not configure this workshop to use JK's proxy, request its private endpoint, or
copy anyone else's credentials.

A free OpenCode route, including Big Pickle, is likewise not assumed to be directly usable from
Microsoft Agent Framework. Before relying on one, separately verify its provider, OpenAI-compatible
endpoint, authentication method, model identifier, tool behaviour, typed-output behaviour, quota,
and workshop suitability. Until that verification exists, use it only as a future option—not a
recovery promise.

## Recovery decision tree

```text
4B local model healthy?
  yes -> use the official local path.
  no  -> can the participant use an approved, independently verified compatible endpoint?
           yes -> run hello, typed no-tool, then split Gather -> Extract checkpoints.
           no  -> use saved outputs / observer path; continue the deterministic Gather and validation lesson.
```

If the alternate endpoint fails at any checkpoint, stop switching providers during the session. Use
the saved output for that stage and record the exact provider/model/endpoint class for later
rehearsal. Never expose keys, private URLs, raw logs containing credentials, or real incident data.

## Verified and pending

Verified in this repository: the configuration seam exists, defaults are loopback-local, and the
deterministic Gather checkpoint needs no endpoint. Pending separate runs: compatibility of any
specific non-local provider, including an OpenCode/Big Pickle route; tool plus typed-output support;
latency; quotas; and suitability for the workshop.
