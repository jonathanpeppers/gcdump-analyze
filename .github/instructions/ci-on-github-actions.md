---
applyTo: ".github/workflows/**"
---

# CI on GitHub Actions

This document describes the main GitHub Actions workflow for this repo. The workflow builds and tests on Ubuntu, runs on pull requests and the main branch, packs the solution, captures binlogs and test results under `bin/`, and uploads them as artifacts even when failures occur.

## Goals

- OS: `ubuntu-latest`
- Triggers: `push` to `main` and `pull_request`
- Install .NET 9 SDK via `actions/setup-dotnet`
- Build and pack the solution (`gcdump-analyze.sln`) while writing MSBuild binlogs to `bin/`
- Run tests and save test results (TRX/attachments) to `bin/`
- Upload everything in `bin/` as an artifact regardless of success/failure

## Example workflow

Below is an example workflow that implements the above. Adapt as needed.

```yaml
name: CI

on:
	push:
		branches: [ main ]
	pull_request:
		branches: [ main ]

jobs:
	build:
		runs-on: ubuntu-latest

		steps:
			- name: Checkout
				uses: actions/checkout@v4

			- name: Setup .NET 9 SDK
				uses: actions/setup-dotnet@v4
				with:
					dotnet-version: '9.0.x'

			- name: Build (with binlogs)
				run: |
					dotnet build gcdump-analyze.sln -c Release -bl:bin/build.binlog

			- name: Test (collect results)
				run: |
					dotnet test gcdump-analyze.sln -c Release \
						--logger "trx;LogFileName=TestResults.trx" \
						--results-directory bin \
						-bl:bin/test.binlog

			- name: Upload bin artifacts (always)
				if: always()
				uses: actions/upload-artifact@v4
				with:
					name: bin-${{ github.run_number }}
					path: bin
					if-no-files-found: warn
```

## Notes

- The `/bl:bin/*.binlog` switches capture MSBuild binary logs for troubleshooting.
- `--results-directory bin` ensures TRX and attachments are placed under `bin/`.
- The final artifact step uses `if: always()` so logs are preserved even on failures.
- See a similar pipeline for reference: https://github.com/jonathanpeppers/dotnes/blob/main/.github/workflows/dotnes.yml

