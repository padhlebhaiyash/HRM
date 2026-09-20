# Branching Model & Contribution Guidelines

This repository follows a structured branching model designed for continuous deployment and safe feature integration for the **HRM System**.

---

## 1. Branch Hierarchy

```
main (Production)
  │
  ▲ (Release PRs / Hotfixes)
  │
develop (Integration & Staging)
  │
  ▲ (Feature PRs)
  │
  ├── feature/<feature-name>
  ├── bugfix/<bug-description>
  └── hotfix/<critical-patch>
```

### Primary Branches

| Branch | Description | Protection Level |
|---|---|---|
| **`main`** | **Production**. Reflects the live production state deployed to Azure App Service (`https://munrohr.inovexa.solutions`). Every merge to `main` should be tagged with a release version (e.g., `v1.0.0`). | Protected: Requires PR & passing checks. |
| **`develop`** | **Integration**. Active branch where feature branches merge. Used for testing and QA before rolling out to `main`. | Protected: Default branch for daily development. |

---

## 2. Supporting Branches

### Feature Branches (`feature/*`)
- **Source**: `develop`
- **Merge Target**: `develop`
- **Naming**: `feature/<short-descriptive-name>` (e.g., `feature/attendance-biometrics`, `feature/payslip-pdf-export`)
- **Workflow**:
  ```bash
  git checkout develop
  git pull origin develop
  git checkout -b feature/my-new-feature
  # ... write code & commit ...
  git push -u origin feature/my-new-feature
  # Open Pull Request targeting 'develop'
  ```

### Bugfix Branches (`bugfix/*`)
- **Source**: `develop`
- **Merge Target**: `develop`
- **Naming**: `bugfix/<issue-summary>` (e.g., `bugfix/clockout-radius-validation`)

### Hotfix Branches (`hotfix/*`)
- **Source**: `main`
- **Merge Target**: `main` **and** `develop`
- **Naming**: `hotfix/<patch-summary>` (e.g., `hotfix/session-expiry-500`)
- **Purpose**: Urgent production fixes that cannot wait for the next regular release cycle.

---

## 3. Commit Message Standards

Use Conventional Commits syntax:
- `feat: <description>` - A new user-facing feature
- `fix: <description>` - A bug fix
- `refactor: <description>` - Code refactoring without behavioral change
- `chore: <description>` - Build, configuration, or dependency updates
- `docs: <description>` - Documentation changes

---

## 4. Environment Configuration & Secrets

- **Never commit live connection strings or passwords** to Git.
- Use `appsettings.Production.template.json` as a baseline.
- For production, set connection strings and SMTP credentials via Azure App Service Application Settings or Key Vault.
