# Publishing a New Version to NuGet

This guide covers how to publish new versions of **ElBruno.PersonaPlex** to NuGet.org using GitHub Actions and NuGet Trusted Publishing (keyless, OIDC-based).

## Package

| Package | Project | Description |
|---------|---------|-------------|
| `ElBruno.PersonaPlex` | `src/ElBruno.PersonaPlex/` | Half-duplex audio round-trip using PersonaPlex (ONNX Runtime) |
| `ElBruno.PersonaPlex.BlazorComponents` | `src/ElBruno.PersonaPlex.BlazorComponents/` | Server-backed Blazor UI components for half-duplex, turn-based PersonaPlex sessions |

> **Auto-discovery:** the publish workflow discovers packable `.csproj` files under `src/` automatically (excluding samples and tests), then restores, builds, packs, and pushes them with one shared version.

> **Policy requirement:** if you add a new packable library under `src/`, you usually do **not** need to update `.github/workflows/publish.yml` for discovery. You **do** need to update this guide and add a matching NuGet Trusted Publishing policy for the new package ID before publishing.

## Prerequisites (One-Time Setup)

### 1. Configure NuGet.org Trusted Publishing Policy

1. Sign in to [nuget.org](https://www.nuget.org)
2. Click your username → **Trusted Publishing**
3. Add a policy for the package with these values:

| Setting | Value |
|---------|-------|
| **Repository Owner** | `elbruno` |
| **Repository** | `ElBruno.PersonaPlex` |
| **Workflow File** | `publish.yml` |
| **Environment** | `release` |

### 2. Configure GitHub Repository

1. Go to the repo **Settings** → **Environments**
2. Create an environment called **`release`**
   - Optionally add **required reviewers** for a manual approval gate
3. Go to **Settings** → **Secrets and variables** → **Actions**
4. Add a repository secret:
   - **Name:** `NUGET_USER`
   - **Value:** `elbruno` (your NuGet.org profile name)

## Publishing a New Version

### Option A: Create a GitHub Release (Recommended)

1. **Update the version** in `src/ElBruno.PersonaPlex/ElBruno.PersonaPlex.csproj`:
   ```xml
   <Version>1.1.0</Version>
   ```
2. **Commit and push** the version change to `main`
3. **Create a GitHub Release:**
   - Go to the repo → **Releases** → **Draft a new release**
   - Create a new tag: `v1.1.0` (must match the version in the csproj)
   - Fill in the release title and notes
   - Click **Publish release**
4. The **Publish to NuGet** workflow runs automatically

### Option B: Manual Dispatch

1. Go to the repo → **Actions** → **Publish to NuGet**
2. Click **Run workflow**
3. Optionally enter a version
4. Click **Run workflow**

## How It Works

```
GitHub Release created (e.g. v1.1.0)
  → GitHub Actions triggers publish.yml
    → Builds + tests the runtime + tests project
    → Packs every packable library under src/ with the same version
    → Requests an OIDC token from GitHub
    → Exchanges the token with NuGet.org for a temporary API key
    → Pushes all resulting packages to NuGet.org
    → Temp key expires automatically
```

## Version Resolution Priority

1. **Release tag** — if triggered by a GitHub Release (strips leading `v`)
2. **Manual input** — if triggered via workflow dispatch with a version specified
3. **Workflow default** — falls back to `1.1.0` when no version is supplied

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Workflow fails at "NuGet login" | Verify the Trusted Publishing policy matches repo owner, name, workflow file, and environment |
| `NUGET_USER` secret not found | Add the secret in GitHub repo Settings → Secrets → Actions |
| Package already exists | `--skip-duplicate` flag prevents failures; bump the version number |
| OIDC token errors | Ensure `id-token: write` permission is set in the workflow job |

## Reference Links

- [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
- [NuGet/login GitHub Action](https://github.com/NuGet/login)
- [OpenID Connect in GitHub Actions](https://docs.github.com/en/actions/security-for-github-actions/security-hardening-your-deployments/about-security-hardening-with-openid-connect)
- [NuGet Package Versioning](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning)
