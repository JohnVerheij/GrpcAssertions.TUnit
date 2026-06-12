# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-06-12: response and trailer metadata on the builder, trailer assertions

Minor release. Lets a faked call carry response headers and trailers, and adds trailer assertions (text and binary) to the `RpcException` chain. Purely additive.

### Added

- **`GrpcCallBuilder.Success<T>(T response, Metadata? responseHeaders, Metadata? trailers)`** and **`GrpcCallBuilder.Faulted<T>(StatusCode statusCode, string? detail, Metadata trailers)`** build calls that carry response metadata, so a wrapper that reads response headers or trailers can be tested against a fake. A null headers or trailers argument is treated as empty metadata.
- **`WithTrailer(string key, string value)`** and **`WithTrailer(string key, ReadOnlySpan<byte> value)`** on the `ThrowsGrpcException()` chain assert the exception's `Trailers` contain a text or binary (`-bin`) entry. Keys are matched case-insensitively (gRPC lowercases metadata keys). The text overload reads `Metadata.Entry.Value` and the binary overload reads `Metadata.Entry.ValueBytes`, each guarded by `Entry.IsBinary`, so the value of a binary entry is never read as a string. On failure the message renders the trailers, with binary entries shown as `(binary, N bytes)`.

### Fixed

- **`GrpcCallBuilder.Success<T>` now returns the same trailers `Metadata` instance on every call** to the trailers accessor, matching a real client; it previously returned a fresh empty `Metadata` each time, so an identity check or mutation did not behave as it would against a real call.

### Changed

- README adds an **"Await the call against a generated client"** note: a generated client's `XAsync` returns `AsyncUnaryCall<T>` whose fault lives in `ResponseAsync`, so a delegate that returns the call without awaiting it never surfaces the fault and the assertion reports no exception. Assert via `async () => await client.XAsync(...)` or `client.XAsync(...).ResponseAsync` when testing a raw generated client; wrapper tests, which return `Task`, are unaffected.
- Bumped `PackageValidationBaselineVersion` from `0.1.1` to `0.1.2` on both packages so ApiCompat strict-mode validates `0.2.0` against the most recently published baseline. The new overloads are recorded as additive differences in `CompatibilitySuppressions.xml`.

## [0.1.2] - 2026-06-05: documentation refresh

Documentation and release-tooling release. No API or behavior change.

### Changed

- Refreshed the README (plain-ASCII punctuation) and rewrote the shared `CONVENTIONS.md`: removed the version-history preamble so it reads as a conventions document, not a changelog.
- The release workflow now publishes the matching `CHANGELOG.md` section as the GitHub release body (`body_path`), so release notes carry the full hand-written detail instead of GitHub's auto-generated commit summary.

## [0.1.1] - 2026-06-04

Documentation patch. No public-surface change; the shipped assemblies are identical to v0.1.0.

### Changed

- README adds a **"Replacing hand-rolled `AsyncUnaryCall<T>` factories"** recipe as the lead Cookbook entry: a before/after that contrasts the five-parameter `AsyncUnaryCall<T>` constructor (response task, response-headers task, status accessor, trailers accessor, dispose callback) with `GrpcCallBuilder.Success(response)` and `GrpcCallBuilder.Faulted<T>(...)`, and spells out the type-inference rule (`Success<T>(T)` infers `T`; `Faulted<T>(RpcException)` needs the explicit `T`). This is the highest-value use of the builder: deleting fake-client constructor boilerplate.
- README adds a **"When *not* to use `ThrowsGrpcException`"** recipe: tests that assert the wrapper rethrows the *same* `RpcException` instance are a stronger contract than `ThrowsGrpcException(code)` and stay on `Throws<RpcException>()` plus `IsSameReferenceAs`, since matching only the status code would weaken an identity-propagation test.
- The two packed package READMEs link to the new Cookbook recipes and restate the `Success<T>` / `Faulted<T>` inference rule, kept consistent with the GitHub README.

## [0.1.0] - 2026-06-02: gRPC outcome assertions

Feature release. Lifts the package from skeleton to functional: the gRPC outcome-assertion surface ships. `ThrowsGrpcException()` asserts a delegate threw an `RpcException`, with `StatusCode` shorthands and `Status.Detail` refinements; `DoesNotThrowGrpcException()` covers the benign-error swallow tests; and `GrpcCallBuilder` removes the five-parameter `AsyncUnaryCall<T>` constructor that every hand-rolled gRPC fake repeats. The v0.0.1 `IsRpcException()` discriminator is preserved unchanged.

### Added

- **`Assert.That(() => call).ThrowsGrpcException()`** asserts the delegate throws a gRPC `RpcException` of any status; **`ThrowsGrpcException(StatusCode expected)`** asserts the status. Both return a `GrpcExceptionAssertion` chain, and failure messages render the actual `StatusCode` and `Status.Detail`. The entry points extend the delegate assertion source produced by `Assert.That(() => client.Method(...))`.
- **14 `StatusCode` shorthands** chaining off `ThrowsGrpcException()`: `IsOk`, `IsCancelled`, `IsInvalidArgument`, `IsDeadlineExceeded`, `IsNotFound`, `IsAlreadyExists`, `IsPermissionDenied`, `IsResourceExhausted`, `IsFailedPrecondition`, `IsAborted`, `IsUnimplemented`, `IsInternal`, `IsUnavailable`, `IsUnauthenticated`.
- **`WithDetail(string)`** (exact, ordinal) and **`WithDetailContaining(string, StringComparison)`** (substring, explicit comparison per the family convention) refine the assertion on `Status.Detail`.
- **`Assert.That(() => call).DoesNotThrowGrpcException()`** asserts the delegate completes without throwing an `RpcException`. A thrown `RpcException` fails with the gRPC outcome rendered; any other thrown exception fails naming its type and message.
- **`GrpcCallBuilder`** (core `GrpcAssertions` package): `Success<T>(T)`, `Faulted<T>(RpcException)`, and `Faulted<T>(StatusCode, string?)` build `AsyncUnaryCall<T>` instances for gRPC client test doubles, replacing the repeated five-parameter constructor. Both builders guard their arguments with `ArgumentNullException.ThrowIfNull`, a strict improvement over the hand-rolled fakes that would dereference-null later.
- **`GrpcOutcomeRendering`** (core): `Describe(RpcException)` and `DescribeStatus(StatusCode, string?)` render the status and detail (truncated at `MaxDetailLength`, 200 characters, with a horizontal-ellipsis suffix) for failure messages, shared so consumer-authored gRPC assertions produce identical diagnostics.

### Changed

- Pinned `PackageValidationBaselineVersion` to `0.0.1` on both packages; ApiCompat strict-mode now validates v0.1.0 against the v0.0.1 baseline at pack time. The v0.1.0 additions are purely additive (no breaking changes); strict-mode records the new public types as additive baseline suppressions (`CP0001`) in `CompatibilitySuppressions.xml`.
- Exempted `VSTHRD003` in test projects (`.editorconfig`). Tests routinely await Tasks created elsewhere (for example `AsyncUnaryCall<T>.ResponseAsync` from `GrpcCallBuilder`), which is safe under the no-`SynchronizationContext` test runner; the analyzer guards against a UI-thread deadlock that cannot occur here.

## [0.0.1] - 2026-06-01: skeleton release

Skeleton release. Establishes the repository, the `GrpcAssertions` (core) and `GrpcAssertions.TUnit` (adapter) package identifiers on nuget.org, and the full family quality bar (AOT-clean with no runtime reflection in the assertion path, SBOM plus SLSA build provenance, public-API snapshot pinning). Ships a single discriminator, `IsRpcException()`, so the surface is real and exercised end-to-end rather than empty. The gRPC outcome-assertion surface (the `GrpcCallBuilder` test infrastructure plus the `ThrowsGrpcException` verbs and `StatusCode` shorthands) lands in v0.1.0.

### Added

- **`Assert.That(exception).IsRpcException()`** asserts that an `Exception` is a gRPC `RpcException`, the exception type a failed gRPC call surfaces. The failure message names the actual exception type. Source-generated via `[GenerateAssertion]`; the generated chain is AOT-clean.
- **`GrpcExceptions.IsRpcException(Exception?)`** framework-agnostic predicate in the `GrpcAssertions` core package. Returns `false` for `null` and for any non-`RpcException` type. The adapter assertion is a thin wrapper over this helper, so a future xUnit, NUnit, or MSTest adapter reuses the same logic.
- Single disclosed runtime dependency: `Grpc.Core.Api` (Apache-2.0), the package that defines the `RpcException` / `StatusCode` / `Status` types the assertions are about. It flows transitively to consumers through the core package.
- Repository scaffolding at the family quality bar: central package management, shared `Directory.Build.props` / `.targets`, `BannedSymbols.txt` no-reflection enforcement, CI (build / test / pack, CodeQL across `csharp` and `actions`, OpenSSF Scorecard, dependency-review, the zizmor workflow audit), Renovate dependency automation, SLSA build-provenance plus Sigstore-signed SBOM attestations on release, and a public-API snapshot test pinning both shipped assemblies.

[unreleased]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.1.2...HEAD
[0.1.2]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.0.1...v0.1.0
[0.0.1]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/releases/tag/v0.0.1
