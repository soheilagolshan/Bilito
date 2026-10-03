# ADR 0002: Passwordless mobile OTP authentication

## Context

Bilito needs a simple authentication flow for a separate React client, but there is no SMS provider yet. The initial ASP.NET Core Identity setup was only a persistence placeholder and the application has no password requirement.

## Decision

Use one passwordless mobile flow: request a six-digit OTP, verify it, create or authenticate the normalized mobile user, issue a short-lived JWT access token, and rotate a hashed refresh token stored behind an HttpOnly cookie. OTP and refresh-token secrets are never stored in raw form. Development OTP exposure is explicitly gated by both the Development environment and configuration.

Use a small domain user model rather than inheriting from `IdentityUser`. EF Core remains the runtime ORM, while FluentMigrator owns the schema.

## Consequences

- Mobile numbers have one canonical representation and a database uniqueness constraint.
- OTP challenges are expiring, single-use, rate-limited, and attempt-limited.
- Refresh-token rotation supports replay rejection without introducing a full token-family subsystem yet.
- A real SMS provider can replace the delivery implementation without changing the authentication use case.
- A signing key and database credentials must be supplied through user secrets or environment configuration.

## Alternatives considered

- Password authentication and full ASP.NET Core Identity: rejected because the current product flow is passwordless and the existing Identity setup was only a placeholder.
- Returning OTPs in all environments: rejected because it would create a production credential disclosure risk.
- Storing raw refresh tokens: rejected because a database compromise would immediately expose active sessions.
