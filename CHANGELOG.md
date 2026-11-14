# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> *History note:* All version sections were reformatted on 2026-07-05 for one-time
> [CONVENTIONS &sect;CHANGELOG](CONVENTIONS.md) conformance: forbidden sub-headers folded into
> the six Keep a Changelog headers, non-user-facing content removed (internal refactors, test
> counts, coverage numbers, CI and build hygiene, governance churn, roadmap notes), bullets
> kept in past-tense active voice with code-formatted API leads. The nuget.org Release Notes
> tab and the GitHub Release for each shipped version are unchanged. A CI `family-lint` gate
> keeps future sections conforming; each is frozen per Rule 7 once shipped.

## [Unreleased]

### Added

- **`GrpcCallBuilder.Faulted<TResponse>(Exception)`** fails a unary call with any exception in one line, for a fault outside gRPC such as a `TimeoutException` or an `ObjectDisposedException`. The response task throws the exception itself, the response headers are empty, the trailers accessor returns one empty `Metadata` on every call, and the status is `Unknown` with the exception message (`StatusCode.Cancelled` for an `OperationCanceledException`, the status a real client reports for a canceled call). The response task is faulted, never canceled, even for an `OperationCanceledException`, as with a real client that has `GrpcChannelOptions.ThrowOperationCanceledOnCancellation` set. An `RpcException` passed through this overload behaves exactly as in `Faulted<TResponse>(RpcException)`.
- **Target frameworks:** both packages now target `net10.0` and `net11.0`, following the family's target framework policy (every .NET version in Microsoft support, never an end-of-life one).

### Changed

- **`IsRpcException()`** now fails the assertion for a `null` exception instead of throwing `ArgumentNullException`, as `GrpcExceptions.IsRpcException(null)` already returned `false`. Its failure message now reads `Expected to be a gRPC RpcException` and `but it was InvalidOperationException`, naming the short type name like the other messages; before, it named the full type name.
- **Failure messages** now render a carriage return or line feed in a status detail, a thrown exception's message, or a trailer key or value (actual or expected) as the two characters backslash and `r` or `n`, so each clause of a message stays on one line as `GrpcOutcomeRendering.DescribeStatus` documents.
- **`GrpcOutcomeRendering.MaxDetailLength`** and the README now say the limit counts UTF-16 code units, not characters.
- **`ServerStreaming` and `ServerStreamingFaulted`** documentation and the README now say that the responses are materialized when the call is built, that status and trailers are available at once (a real client throws `InvalidOperationException` until the call completes), and that `MoveNext` with a canceled token returns a canceled task (a real client by default surfaces an `RpcException` with `StatusCode.Cancelled`). The README also says a second `Streams()` on one call finds the stream drained, and has a new section on how faithful the fakes are.
- **README roadmap** no longer promises dedicated deadline and cancellation assertions: `ThrowsGrpcException(StatusCode.DeadlineExceeded)` and the `IsDeadlineExceeded()` and `IsCancelled()` shorthands cover the observed use, and the roadmap now names a server-side call-context builder as next.
- **README, CONVENTIONS and SECURITY** describe the target framework policy, and every sample in the READMEs now compiles against the packed packages.
- **NuGet description** of the core package names the `Faulted` overload for any exception.

### Fixed

- **`WithDetailContaining`** threw `NullReferenceException` when the status detail was `null` (`default(Status)`, or a `Status` built with `null`); it now fails the assertion like any other mismatch. A `null` detail now counts as empty for `WithDetail` as well, as the rendered failure message and the builders already treated it.
- **Failure messages** no longer cut a status detail between the two halves of a surrogate pair, which left a lone surrogate in the output; a pair that straddles the limit is dropped whole.
- **`MoveNext` on a fake server stream** with an already-canceled token threw `OperationCanceledException` from the call itself; it now returns a canceled task, as the real client reader reports cancellation through the returned task.
- **`ServerStreaming`** returned a new trailers `Metadata` from each call of its trailers accessor; it now returns one stable instance, as `Success` does.

## [0.3.1] - 2026-11-05: worked example for a client wrapper

Patch release. No API or behavior change.

### Changed

- **README:** a worked example that tests a client wrapper over a fake of a generated client, one test per outcome (success, a swallowed `NotFound`, a failure that passes through), with the reminder that a delegate over a raw `AsyncUnaryCall<T>` must await it; and a note for readers of the ASP.NET Core docs that `GrpcCallBuilder.Success` and `Faulted` replace the copied `CallHelpers` helper.
- **NuGet descriptions** of both packages now list the trailer and server-streaming surface: the core description names the response-header and trailer overloads of `Success` and `Faulted` and the server-streaming builders, and the TUnit description names `WithTrailer` and `Streams()`.
- **NuGet package metadata** of both packages now links `PackageProjectUrl` to https://grpcassertions.dev, and the `assertions` and `testing` tags now come before `grpc` (after `tunit` on the TUnit package).
- **README and core NuGet description** now say adapters for other test frameworks are not planned; the family targets TUnit.
- **`ThrowsGrpcException`** documentation now says to chain the status shorthands off the no-argument `ThrowsGrpcException()`, and that a status can be set once (through the argument or one shorthand): setting it a second time throws `InvalidOperationException`. Each of the 14 shorthands documents the exception.
- **`DoesNotThrowGrpcException`** documentation now says any thrown exception fails the assertion: an `RpcException` is rendered as its status code and detail, any other exception by type name and message. The README design note about a non-`RpcException` throw now applies to `ThrowsGrpcException` only.
- **README failure diagnostics** now say a failed `ThrowsGrpcException` or `DoesNotThrowGrpcException` that caught an `RpcException` renders its outcome, and that only the actual detail in that outcome is truncated at 200 characters (the expected detail and trailer values are not).
- **README namespace table** now lists the `IsUnavailable()` / `WithDetail()` chain as instance methods of `GrpcExceptionAssertion` in `GrpcAssertions.TUnit`, and the entry points (`ThrowsGrpcException`, `DoesNotThrowGrpcException`, `Streams`, `IsRpcException`) under `TUnit.Assertions.Extensions`.
- **`GrpcCallBuilder`** README design note now says the builder methods build both `AsyncUnaryCall<T>` and `AsyncServerStreamingCall<T>` and guard their reference arguments with `ArgumentNullException.ThrowIfNull`.
- **`GrpcCallBuilder.Faulted`** README sample for a pre-built `RpcException` now attaches trailers.
- **`GrpcCallBuilder.Faulted`** README now names the `Faulted<T>(StatusCode, string?, Metadata)` overload as the direct route to a faulted call with trailers.
- **`IsSameReferenceAs`** README recipe now uses a self-contained throwing fake.
- **README recipes** now sit under their own `Recipes` heading.
- **`ServerStreamAssertion<TResponse>`** documentation now says the `AndStreamItems` callback runs only when the count and predicate expectations pass, that only an `RpcException` from the reader becomes a failure (other exceptions propagate), and that calling `StreamsAtLeast`, `StreamsExactly` or `AndStreamItems` again replaces the earlier value.
- **`GrpcExceptions`** and **`RpcExceptionAssertions`** XML summaries now describe the current surface instead of the v0.0.1 stub.
- **`TUnit.Assertions`** and **`TUnit.Core`** minimum versions raised from 1.54.0 to 1.80.0, and **`Grpc.Core.Api`** from 2.80.0 to 2.85.0.

## [0.3.0] - 2026-06-14: server-streaming builders and assertions

Minor release. Extends the builder and the assertion surface from unary calls to server-streaming calls: `GrpcCallBuilder` now constructs `AsyncServerStreamingCall<T>` test doubles, and a `Streams()` chain asserts on the streamed responses. Purely additive; the `0.2.0` ApiCompat baseline is preserved.

### Added

- **`GrpcCallBuilder.ServerStreaming<TResponse>(IEnumerable<TResponse> responses)`** builds a successful server-streaming call whose response stream yields the responses in order and then ends cleanly with a terminal `StatusCode.OK`.
- **`GrpcCallBuilder.ServerStreamingFaulted<TResponse>(StatusCode statusCode, IEnumerable<TResponse> responses, string? detail = null)`** builds a server-streaming call that yields the responses and then throws an `RpcException` on the next read, so a wrapper's mid-stream fault handling can be exercised.
- **`Streams()`** on an `AsyncServerStreamingCall<TResponse>` begins a server-streaming assertion that reads the response stream once. Chain **`StreamsAtLeast(int)`** / **`StreamsExactly(int)`** to assert the response count, **`StreamContains(Func<TResponse, bool>)`** to assert at least one response matches (the predicate text is captured for the failure message), and **`AndStreamItems(Func<IReadOnlyList<TResponse>, Task>)`** to run follow-on assertions against the materialized responses without re-reading the stream. A stream that faults mid-read fails with the partial count and the rendered gRPC outcome.

## [0.2.0] - 2026-06-12: response and trailer metadata on the builder, trailer assertions

Minor release. Lets a faked call carry response headers and trailers, and adds trailer assertions (text and binary) to the `RpcException` chain. Purely additive.

### Added

- **`GrpcCallBuilder.Success<T>(T response, Metadata? responseHeaders, Metadata? trailers)`** and **`GrpcCallBuilder.Faulted<T>(StatusCode statusCode, string? detail, Metadata trailers)`** build calls that carry response metadata, so a wrapper that reads response headers or trailers can be tested against a fake. A null headers or trailers argument is treated as empty metadata.
- **`WithTrailer(string key, string value)`** and **`WithTrailer(string key, ReadOnlySpan<byte> value)`** on the `ThrowsGrpcException()` chain assert the exception's `Trailers` contain a text or binary (`-bin`) entry. Keys are matched case-insensitively (gRPC lowercases metadata keys). The text overload reads `Metadata.Entry.Value` and the binary overload reads `Metadata.Entry.ValueBytes`, each guarded by `Entry.IsBinary`, so the value of a binary entry is never read as a string. On failure the message renders the trailers, with binary entries shown as `(binary, N bytes)`.

### Changed

- README adds an **"Await the call against a generated client"** note: a generated client's `XAsync` returns `AsyncUnaryCall<T>` whose fault lives in `ResponseAsync`, so a delegate that returns the call without awaiting it never surfaces the fault and the assertion reports no exception. Assert via `async () => await client.XAsync(...)` or `client.XAsync(...).ResponseAsync` when testing a raw generated client; wrapper tests, which return `Task`, are unaffected.

### Fixed

- **`GrpcCallBuilder.Success<T>` now returns the same trailers `Metadata` instance on every call** to the trailers accessor, matching a real client; it previously returned a fresh empty `Metadata` each time, so an identity check or mutation did not behave as it would against a real call.

## [0.1.2] - 2026-06-05: documentation refresh

Documentation and release-tooling release. No API or behavior change.

## [0.1.1] - 2026-06-04: README cookbook recipes

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

## [0.0.1] - 2026-06-01: skeleton release

Skeleton release. Establishes the repository, the `GrpcAssertions` (core) and `GrpcAssertions.TUnit` (adapter) package identifiers on nuget.org, and the full family quality bar (AOT-clean with no runtime reflection in the assertion path, SBOM plus SLSA build provenance, public-API snapshot pinning). Ships a single discriminator, `IsRpcException()`, so the surface is real and exercised end-to-end rather than empty. The gRPC outcome-assertion surface (the `GrpcCallBuilder` test infrastructure plus the `ThrowsGrpcException` verbs and `StatusCode` shorthands) lands in v0.1.0.

### Added

- **`Assert.That(exception).IsRpcException()`** asserts that an `Exception` is a gRPC `RpcException`, the exception type a failed gRPC call surfaces. The failure message names the actual exception type. Source-generated via `[GenerateAssertion]`; the generated chain is AOT-clean.
- **`GrpcExceptions.IsRpcException(Exception?)`** framework-agnostic predicate in the `GrpcAssertions` core package. Returns `false` for `null` and for any non-`RpcException` type. The adapter assertion is a thin wrapper over this helper, so a future xUnit, NUnit, or MSTest adapter reuses the same logic.
- Single disclosed runtime dependency: `Grpc.Core.Api` (Apache-2.0), the package that defines the `RpcException` / `StatusCode` / `Status` types the assertions are about. It flows transitively to consumers through the core package.

[unreleased]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.3.1...HEAD
[0.3.1]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.1.2...v0.2.0
[0.1.2]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.0.1...v0.1.0
[0.0.1]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/releases/tag/v0.0.1
