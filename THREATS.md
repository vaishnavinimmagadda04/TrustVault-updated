# TrustVault Threat Model

This threat model describes the main document-security risks addressed by the application and the remaining production considerations.

## Threats and mitigations

| Threat | Example | Mitigation |
|---|---|---|
| Insecure direct object reference (IDOR) | A signed-in user changes a document ID to access another user's file. | Document reads, downloads, edits, deletes, and permission management check ownership or an active grant on the server for every request. Unauthorized attempts are recorded in the audit log. |
| Direct access to stored files | A user guesses a file URL or storage filename and bypasses API authorization. | Files are stored using generated GUID-based names outside the web root and are returned only through authorized API endpoints. Upload storage is not configured as static content. |
| Continued access after revocation | A former grantee reuses a saved link after the owner revokes access. | Each document request checks the current `IsActive` permission record in the database; revocation takes effect on subsequent requests. |
| Role escalation | A user submits `Admin` as their role during public registration. | Public registration accepts only `Owner` and `Viewer`. Admin access is provisioned out-of-band and protected by a role policy. |
| Malicious or oversized uploads | A user uploads an executable, disguises an unsupported file, or submits an oversized file. | The API accepts only PDF, DOCX, and XLSX extensions, enforces a 10 MiB limit, checks basic file signatures, and stores files under generated names. Client-side checks are only for usability; server-side validation is authoritative. |

## Residual risks and operational controls

- Signature checks are not malware scanning or full document-format validation. Production deployments should scan files before making them available and consider content-disarm-and-reconstruction for higher-risk environments.
- Store uploads on private persistent storage, not a public static directory. Protect and back up the storage independently from the database.
- Require HTTPS, use a unique secret of at least 32 bytes from the deployment platform's secret store, and restrict CORS to the deployed frontend origin.
- Restrict database credentials and deployment permissions to least privilege. Do not commit credentials or use real personal data in demo accounts.
- Monitor audit events and retain logs according to the organization's privacy and retention requirements.
