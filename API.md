# TrustVault API

The API is served by the ASP.NET Core application. Unless otherwise noted, endpoints require a bearer token:

```http
Authorization: Bearer <JWT>
```

Public registration creates only `Owner` or `Viewer` accounts. The first administrator must be provisioned by a trusted database administrator; public registration cannot create an Admin.

## Authentication

| Method and path | Allowed | Request | Success |
|---|---|---|---|
| `POST /api/auth/register` | Public | `{ "name": "Alex", "email": "alex@example.com", "password": "at-least-8-chars", "role": "Owner" }` | `201 Created` |
| `POST /api/auth/login` | Public | `{ "email": "alex@example.com", "password": "..." }` | `200 OK` with user details and JWT |

Invalid credentials return `401 Unauthorized`. Registration rejects unsupported roles and duplicate emails.

## Documents

| Method and path | Allowed | Description |
|---|---|---|
| `GET /api/document` | Authenticated users | Lists documents owned by the caller or shared with them; Admin sees all documents. |
| `GET /api/document/search` | Authenticated users | Searches only documents visible to the caller. Optional query parameters: `name`, `owner`, `category`, and `date` (`YYYY-MM-DD`, UTC upload date). Name matches title or original filename; owner matches name or email; text filters are partial matches. |
| `POST /api/document/upload` | Owner, Admin | Multipart form fields `title`, `category`, and `file`. Accepts PDF, DOCX, or XLSX up to 10 MiB. |
| `GET /api/document/{id}` or `GET /api/document/{id}/view` | Owner, Admin, or active READ/EDIT grantee | Streams an authorized document. |
| `GET /api/document/download/{id}` | Owner, Admin, or active READ/EDIT grantee | Downloads an authorized document. |
| `PUT /api/document/{id}` | Owner, Admin, or active EDIT grantee | JSON body: `{ "title": "New title", "category": "Finance" }`. |
| `DELETE /api/document/{id}` | Owner or Admin | Deletes document metadata and its stored file. |

An authenticated user who is not authorized to access a known document receives `403 Forbidden`; missing documents return `404 Not Found`. Invalid uploads return `400 Bad Request`.

## Permissions

| Method and path | Allowed | Request / description |
|---|---|---|
| `GET /api/permissions/users?documentId={id}` | Document owner or Admin | Lists users who can be granted access. |
| `GET /api/permissions/document/{id}` | Document owner or Admin | Lists permission records, including revoked records. |
| `POST /api/permissions/grant` | Document owner or Admin | JSON: `{ "documentId": 1, "userId": 2, "permission": "READ" }`; `permission` is `READ` or `EDIT`. |
| `POST /api/permissions/revoke` | Document owner or Admin | JSON: `{ "documentId": 1, "userId": 2 }`; returns `204 No Content` on success. |

Grant and revoke operations take effect on subsequent document requests and are audited.

## Dashboard and administration

| Method and path | Allowed | Description |
|---|---|---|
| `GET /api/dashboard` | Authenticated users | Returns document counts and recent documents. |
| `GET /api/admin/activity` | Admin | Returns recent uploads, downloads, permission changes, and denied attempts. |

## Status code summary

- `200 OK`: successful read or update.
- `201 Created`: successful registration or upload.
- `204 No Content`: successful permission revocation or document deletion.
- `400 Bad Request`: invalid fields, unsupported permission, or invalid upload.
- `401 Unauthorized`: missing/invalid authentication or invalid credentials.
- `403 Forbidden`: authenticated caller lacks the required role or document permission.
- `404 Not Found`: requested document, user, or active grant does not exist.
- `409 Conflict`: account email is already registered.

The API validates uploaded extension, size, and basic file signature; it does not perform malware scanning. See [THREATS.md](./THREATS.md) for security boundaries and deployment considerations.
