# ZPantry Authentication Module

## Flow
1. `POST /api/auth/register`
2. `POST /api/auth/verify-otp`
3. `POST /api/auth/login`
4. `POST /api/auth/logout`

## Login
Request:
```json
{
  "email": "email@example.com",
  "password": "password"
}
```

Response includes:
```json
{
  "accessToken": "...",
  "expiresAt": "2026-06-16T...",
  "fullName": "Name",
  "email": "email@example.com"
}
```

The user id is stored in the access token `ClaimTypes.NameIdentifier` claim and is not returned in the login response.

## Logout
- Send the JWT in `Authorization: Bearer <token>`.
- The current token is revoked in memory until it expires.
- If the app restarts, the in-memory blacklist is cleared.

## JWT config
Configure in `authenticationconfig.json`:
```json
{
  "Jwt": {
    "Issuer": "ZPantry",
    "Audience": "ZPantryClient",
    "SecretKey": "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET",
    "AccessTokenMinutes": 60
  }
}
```
