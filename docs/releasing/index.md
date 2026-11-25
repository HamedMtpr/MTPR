# Release

- [Back to index](../../README.md)

This document describes how to cut a release for the project.

## Table of Contents

- [Unreleased](unreleased.md)
- [v0.1.0-alpha](v0.1.0-alpha.md)

## How to release

The tag is created by GitHub, not by hand.

1. Make sure `main` is green and `unreleased.md` is moved into a versioned notes file (for example `v0.1.0-alpha.md`).
2. On GitHub open **Releases -> Draft a new release**.
3. In **Choose a tag** type the new tag (for example `v0.1.0-alpha`) and select **Create new tag on publish**. Target `main`.
4. Paste the notes from the versioned file (or use **Generate release notes**, configured by `.github/release.yml`).
5. Tick **Set as a pre-release** for alpha/beta tags, then **Publish release**.
6. The `Release` workflow (`.github/workflows/release.yml`) builds and tests the tag, attaches the API zip, sample definitions and checksums to the release, and pushes the container image to GHCR.