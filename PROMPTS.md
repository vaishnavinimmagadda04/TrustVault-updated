# AI Usage Log

Entries below record the Day 3 requirements supplied in the project plan and the corresponding work performed for this submission. No historical prompts or commits are fabricated.

## 1. Document search

- **Date:** 2026-11-06
- **Prompt:** "Search by name, owner, category and date."
- **AI Output:** Added an access-scoped `GET /api/document/search` endpoint and document-library filters for name, owner, category, and upload date.
- **Issue:** Search was not available, and search results needed to preserve the same document visibility rules as the library.
- **Fix:** Reused the accessible-document query for both listing and searching; added URL-encoded query parameters in the UI.
- **Commit:** Day 3 code submission commit.

## 2. Upload validation tests

- **Date:** 2026-11-06
- **Prompt:** "Allowed pdf docx xlsx ... Reject exe bat js zip ... 10 MB."
- **AI Output:** Added API integration tests for disallowed extensions, files larger than 10 MiB, and a valid small PDF. Existing server-side extension, signature, and size checks remain authoritative.
- **Issue:** Upload checks existed but had no automated regression tests.
- **Fix:** Added tests that submit multipart uploads through the API.
- **Commit:** Day 3 code submission commit.

## 3. Authorization and audit tests

- **Date:** 2026-11-06
- **Prompt:** "Automated tests proving one user cannot access another user's documents" and "Unauthorized attempt logged."
- **AI Output:** Added integration coverage for denied access before a grant, successful access while READ permission is active, denial after revocation, audit records for denied requests, and Viewer denial on the Admin Activity endpoint.
- **Issue:** Cross-user access, revocation behavior, audit logging, and role enforcement needed executable proof.
- **Fix:** Exercised the real HTTP pipeline with isolated in-memory test databases and signed test JWTs.
- **Commit:** Day 3 code submission commit.

## 4. Threat model

- **Date:** 2026-11-06
- **Prompt:** "Include: IDOR, Direct file access, Revoked user still accessing, Role escalation, Malicious uploads."
- **AI Output:** Added `THREATS.md` with examples, implemented mitigations, and explicit residual risks.
- **Issue:** Security controls and remaining production risks were not documented in one place.
- **Fix:** Documented controls and cautioned that signature checks do not replace malware scanning.
- **Commit:** Day 3 code submission commit.

## 5. API documentation

- **Date:** 2026-11-06
- **Prompt:** "API Documentation ... POST /api/auth/login ... POST /api/permissions/grant ... GET /api/audit."
- **AI Output:** Added `API.md` covering authentication, role requirements, routes, parameters, request bodies, and important status codes. The audit route is documented at its implemented path, `GET /api/admin/activity`.
- **Issue:** The API route list, authorization expectations, and request shapes were not documented comprehensively.
- **Fix:** Documented the implemented endpoints and corrected the audit route to match the code.
- **Commit:** Day 3 code submission commit.
