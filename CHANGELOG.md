# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.0.1] - 2026-06-01: skeleton release

Skeleton release. Establishes the repository, the `GrpcAssertions` (core) and `GrpcAssertions.TUnit` (adapter) package identifiers on nuget.org, and the full family quality bar (AOT-clean with no runtime reflection in the assertion path, SBOM plus SLSA build provenance, public-API snapshot pinning). Ships a single discriminator, `IsRpcException()`, so the surface is real and exercised end-to-end rather than empty. The gRPC outcome-assertion surface (the `GrpcCallBuilder` test infrastructure plus the `ThrowsGrpcException` verbs and `StatusCode` shorthands) lands in v0.1.0.

### Added

- **`Assert.That(exception).IsRpcException()`** asserts that an `Exception` is a gRPC `RpcException`, the exception type a failed gRPC call surfaces. The failure message names the actual exception type. Source-generated via `[GenerateAssertion]`; the generated chain is AOT-clean.
- **`GrpcExceptions.IsRpcException(Exception?)`** framework-agnostic predicate in the `GrpcAssertions` core package. Returns `false` for `null` and for any non-`RpcException` type. The adapter assertion is a thin wrapper over this helper, so a future xUnit, NUnit, or MSTest adapter reuses the same logic.
- Single disclosed runtime dependency: `Grpc.Core.Api` (Apache-2.0), the package that defines the `RpcException` / `StatusCode` / `Status` types the assertions are about. It flows transitively to consumers through the core package.
- Repository scaffolding at the family quality bar: central package management, shared `Directory.Build.props` / `.targets`, `BannedSymbols.txt` no-reflection enforcement, CI (build / test / pack, CodeQL across `csharp` and `actions`, OpenSSF Scorecard, dependency-review, the zizmor workflow audit), Renovate dependency automation, SLSA build-provenance plus Sigstore-signed SBOM attestations on release, and a public-API snapshot test pinning both shipped assemblies.

[Unreleased]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/compare/v0.0.1...HEAD
[0.0.1]: https://github.com/JohnVerheij/GrpcAssertions.TUnit/releases/tag/v0.0.1
