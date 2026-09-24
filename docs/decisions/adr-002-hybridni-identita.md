# ADR-002: Hybridní Entra ID a lokální Identity

- **Status:** Accepted
- **Datum:** 2026-09-09
- **Rozhodující:** správce projektu

## Kontext a problém

Interní uživatelé se přihlašují přes Microsoft Entra ID, ale systém současně
potřebuje lokální Identity store, systémové role a stabilní číselné FK pro
auditní tabulky. Entra OID ani e-mail nejsou vhodné jako lokální databázový
primární klíč.

## Zvažované varianty

1. Pouze Entra claims bez lokálního uživatele.
2. Pouze lokální ASP.NET Core Identity.
3. Entra OIDC s provisioningem do společného lokálního Identity store.

## Rozhodnutí

Primární přihlášení používá Entra OIDC. `EntraClaimsTransformation` vyhledá
nebo vytvoří `identity.User` podle kombinace `EntraOid + EntraTenantId`,
synchronizuje profil a přidá claim `SportSysClaimTypes.UserId` s lokálním
číselným ID. Lokální Identity účty zůstávají fallbackem.

Registrace používá `AddIdentityCore<User>()` a `.AddSignInManager()`;
`AddIdentity<T>()` se nepoužívá, protože by přepsalo výchozí OIDC schéma.

## Důsledky

### Pozitivní

- Auditní FK vždy odkazují na jednotné `identity.User.Id`.
- Entra i lokální účty sdílejí role a user store.
- Změna e-mailu nerozbije identitu uživatele.

### Negativní

- Přihlášení přes Entra provádí provisioning a synchronizační zápis.
- Claims transformation musí odlišit Entra a lokální principal.
- Business permissions nejsou aktivní, dokud se jejich model znovu nezapojí.

## Reference

- `src/SportSys.Contract/Auth/EntraClaimsTransformation.cs`
- `src/SportSys.Contract/Auth/CurrentUserIdResolver.cs`
- `src/SportSys.Database/Models/identity/User.cs`
- `docs/modules/auth.md`
