# TrustVault

TrustVault is a secure document-management application with an ASP.NET Core 8 API, Angular 18 UI, and SQL Server storage. It includes authentication, role-based authorization, private file uploads, document search, sharing permissions, audit logs, and document-access regression tests.

## Features

- JWT-based registration and login with Owner, Viewer, and Admin roles.
- Private PDF, DOCX, and XLSX uploads (maximum 10 MiB) with server-side validation.
- Document access checks, READ/EDIT grants, and immediate revocation.
- Search by document title or filename, owner, category, and upload date.
- Audit events for document actions, permission changes, and denied access.
- Admin activity view and automated API integration tests.

## Prerequisites

- .NET 8 SDK
- Node.js 20.11 or newer and npm
- SQL Server or SQL Server LocalDB

## Database

For a fresh SQL Server instance, run [`Database/schema.sql`](Database/schema.sql). To upgrade an existing Day 1 database, run [`Database/day2_security.sql`](Database/day2_security.sql); it adds the active-permission flag and creates any missing indexes/tables without dropping existing data. The default API connection string uses `(localdb)\\MSSQLLocalDB`; change it for your SQL Server instance with the `ConnectionStrings__TrustVault` environment variable.

## Run the API

From the repository root in PowerShell:

```powershell
$env:Jwt__Key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$env:ConnectionStrings__TrustVault = 'Server=(localdb)\MSSQLLocalDB;Database=TrustVaultDB;Trusted_Connection=True;TrustServerCertificate=True'
dotnet restore .\Backend\TrustVault.API\TrustVault.API.csproj
dotnet run --project .\Backend\TrustVault.API\TrustVault.API.csproj --urls http://localhost:5000
```

Keep the same JWT secret for the lifetime of sessions. Set `Storage__UploadDirectory` to an absolute private directory if you do not want files stored in `Backend/TrustVault.API/Uploads`. Uploads are never served as static files. For deployment, use HTTPS and a managed secret store.

## Run the Angular UI

In a second terminal:

```powershell
Set-Location .\Frontend\trustvault-ui
npm install
npm start
```

Open `http://localhost:4200`. The API listens at `http://localhost:5000`; its CORS policy allows the Angular dev server origin. Change the `API` constant in `src/app/core/auth.service.ts` and `src/app/core/document.service.ts` when deploying to another API origin.

## Roles and demo accounts

Public registration accepts only `Owner` and `Viewer` so a new user cannot grant themselves administrative access. Create the first account through the UI, then promote that account using a trusted SQL Server administrator connection:

```sql
UPDATE dbo.Users SET Role = 'Admin' WHERE Email = 'admin@example.com';
```

There are no built-in demo credentials. For a demo, register separate Owner and Viewer accounts through the UI, then promote a dedicated test account to Admin with the SQL statement above. Use throwaway accounts and non-sensitive documents; never publish real passwords or personal data.

Admins can list and manage all documents. Owners can upload, list, view, and delete their own files. Viewers can list and view their own files. Shared READ/EDIT permissions grant access to specific documents.

## API summary

- `POST /api/auth/register` creates an Owner or Viewer account.
- `POST /api/auth/login` returns a signed JWT and profile.
- `GET /api/document` lists the caller's documents (Admin: all).
- `GET /api/document/search?name=salary&category=HR&owner=John&date=2026-11-06` searches visible documents. Each filter is optional; the date is an exact UTC upload date.
- `POST /api/document/upload` accepts multipart fields `title`, `category`, and `file`; allowed extensions are PDF, DOCX, and XLSX, up to 10 MiB.
- `GET /api/document/{id}/view` streams an authorized file.
- `DELETE /api/document/{id}` deletes an authorized file (Owner/Admin policy; owners can delete their own).
- `GET /api/dashboard` returns document totals and five recent documents.
- `POST /api/permissions/grant` grants a named user `READ` or `EDIT` access (document owner/Admin only).
- `POST /api/permissions/revoke` revokes a user's active document permission (document owner/Admin only).
- `GET /api/permissions/document/{id}` lists permission history for a document (document owner/Admin only).
- `GET /api/permissions/users?documentId={id}` lists grantable users (document owner/Admin only).
- `GET /api/document/download/{id}` downloads a file after a READ/EDIT, owner, or Admin check.
- `PUT /api/document/{id}` edits document title/category after an EDIT, owner, or Admin check.
- `GET /api/admin/activity` returns recent uploads, downloads, permission changes, and denied attempts (Admin only).

See [`API.md`](API.md) for request bodies, role requirements, and status codes.

The API validates file extensions and basic PDF/ZIP signatures, generates GUID-based storage names, and keeps original filenames only as display metadata. Do not treat file-signature checks as malware scanning; add an antivirus/content-scanning step before production use.

Every document view, download, edit, delete, permission change, upload, and login is written to `AuditLogs`. Authenticated permission denials are recorded as `ACCESS_DENIED`. Document access is limited to the owner, Admin, or a grantee with active READ/EDIT permission; only the owner/Admin can delete or manage grants.

## Automated tests

Run the API integration tests from the repository root:

```powershell
dotnet test .\Backend\TrustVault.API.Tests\TrustVault.API.Tests.csproj --configuration Release -p:UseAppHost=false
```

The tests cover cross-user denial, successful access while a READ grant is active, denial after revocation, audit records for denied requests, Viewer denial on the Admin Activity endpoint, and upload type/size validation.

## Manual smoke checks

1. Register an Owner and a Viewer, then sign in and confirm a JWT is returned.
2. Registering with the `Admin` role should be rejected; promote a test account through SQL for admin checks.
3. As Owner, upload a small valid PDF, DOCX, and XLSX. Confirm the document list shows the title, category, and upload date.
4. Upload an EXE, a file with a mismatched extension/signature, and a file larger than 10 MiB; each should be rejected.
5. As Viewer, confirm listing and viewing work but upload and delete are denied.
6. Confirm one user cannot view or delete another user's document, while Admin can manage all documents.

## Permission and audit checks

For an existing Day 1 database, run `Database/day2_security.sql` before starting the API. It adds `DocumentPermissions.IsActive` without dropping permission history and creates missing indexes/tables idempotently.

- As the owner, grant a user `READ`; verify that user can view and download but cannot edit or delete.
- Upgrade the grant to `EDIT`; verify the grantee can update document title/category.
- Revoke access; verify the former grantee receives `403` for view, download, and edit.
- Verify an unrelated user receives `403` and the attempt appears under Admin Activity as `ACCESS_DENIED`.
- As Admin, open `/admin/activity` and check uploads, downloads, permission changes, and failed attempts.

## Submission documents

- [`THREATS.md`](THREATS.md) documents the threat model, mitigations, and residual risks.
- [`PROMPTS.md`](PROMPTS.md) records the Day 3 AI-assisted work; its commit fields remain pending until changes are committed.
- [`API.md`](API.md) documents the implemented API.

## Deployment and demo

### Deploy to Render

The root [`render.yaml`](render.yaml) defines separate Render services for the Angular static site and .NET API. It builds the API from [`Backend/TrustVault.API/Dockerfile`](Backend/TrustVault.API/Dockerfile) and configures a persistent disk for uploaded documents. The API's persistent disk requires a paid Render web-service plan.

1. Push this repository to a Git provider, then create a Render Blueprint from the repository and select `render.yaml`.
2. Provide `ConnectionStrings__TrustVault` for an externally hosted SQL Server and `Jwt__Key` as a random secret of at least 32 bytes. Keep both values in Render's environment settings, not in source control.
3. Before first use, initialize the SQL Server database with `Database/schema.sql`; for an existing Day 1 database, run `Database/day2_security.sql` instead.
4. Set the API service's `FrontendOrigin` to the deployed static-site origin (for example, `https://trustvault-ui.onrender.com`).
5. Set the static site's `TRUSTVAULT_API_URL` to the deployed API URL ending in `/api` (for example, `https://trustvault-api.onrender.com/api`) and redeploy it.
6. Confirm the API responds at `/health`. The Render disk is mounted at `/app/Uploads`; do not remove it or uploaded files will not persist across deploys.

The API uses `AllowedHosts=*` behind Render's proxy, and the Angular build reads its API URL from generated runtime configuration. The regular local Angular build continues to target the backend's `wwwroot` for the existing combined local setup. Ensure the SQL Server provider permits connections from the deployed API, and do not put database credentials or JWT secrets in this repository.

The 5-minute demo flow is: sign in as Owner, upload a valid document, grant a Viewer READ access, open it as Viewer, revoke access as Owner, confirm the Viewer gets `403`, inspect audit activity as Admin, and demonstrate search. A demo recording must be captured separately in the configured environment.
