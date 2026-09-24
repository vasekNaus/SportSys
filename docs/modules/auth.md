# Autentizace a autorizace

## Účel

Modul sjednocuje přihlášení přes Microsoft Entra ID a lokální ASP.NET Core
Identity nad jedním uživatelským úložištěm. Poskytuje stabilní lokální ID
uživatele pro auditní vazby a systémové role pro autorizaci.

## Odpovědnosti

- OIDC přihlášení interních uživatelů přes Microsoft Entra ID.
- Lokální Identity účty jako fallback.
- Automatický provisioning a synchronizace uživatele z Entra claims.
- Doplnění lokálního `identity.User.Id` do principalu.
- Registrace systémových rolí a fallback politiky vyžadující přihlášení.

## Datový model

`SportSys.Database.Models.identity.User` dědí z `IdentityUser<int>` a doplňuje:

| Vlastnost | Význam |
|---|---|
| `EntraOid` | Object ID uživatele v Entra ID |
| `EntraTenantId` | Tenant ID vydavatele |
| `DisplayName` | Zobrazované jméno synchronizované z Entra |
| `IsLocalAccount` | Rozlišení lokálního a Entra účtu |
| `LastLoginUtc` | Poslední synchronizované přihlášení |

Identity tabulky jsou ve schématu `identity` bez prefixu `AspNet`.
Kombinace `EntraOid + EntraTenantId` je identitou Entra uživatele; e-mail ani
UPN nesmí být použit jako stabilní klíč.

`hr.Coach` je TPT potomek `identity.User`. Trenér proto používá stejné
`User.Id`, Identity údaje a profilová pole jako základní uživatel. Běžný
uživatel derived řádek nemá. Entra provisioning vytváří pouze základního
uživatele; trenérský profil vzniká samostatnou personální operací.

Business role a permission entity jsou v aktuálním projektu vyřazeny z
kompilace a jejich DbSety i transformace claims jsou zakomentované. Nejde tedy
o aktivní autorizační mechanismus.

## Tok zpracování

1. `SportSys.Razor` zahájí OIDC přihlášení přes Microsoft Identity Web.
2. `EntraClaimsTransformation` načte claims `oid` a `tid`.
3. Uživatele vyhledá v `identity.User`, případně jej vytvoří přes
   `UserManager<User>`.
4. Synchronizuje jméno, e-mail a `LastLoginUtc`.
5. Přidá claim `SportSysClaimTypes.UserId` s lokálním číselným ID.
6. Auditní operace používají `CurrentUserIdResolver`, nikoli Entra OID.

Lokální Identity principal může jako fallback použít kladný číselný
`NameIdentifier`.

## Klíčové komponenty

| Komponenta | Cesta | Odpovědnost |
|---|---|---|
| Registrace | `src/SportSys.Contract/ServiceCollectionExtensions.cs` | DbContext, Identity, cookies, transformace a policies |
| Entra transformace | `src/SportSys.Contract/Auth/EntraClaimsTransformation.cs` | Provisioning, synchronizace a lokální ID claim |
| Typ claimu | `src/SportSys.Contract/Auth/SportSysClaimTypes.cs` | Stabilní název lokálního user ID |
| Resolver | `src/SportSys.Contract/Auth/CurrentUserIdResolver.cs` | Bezpečné získání lokálního ID |
| User model | `src/SportSys.Database/Models/identity/User.cs` | Jednotný user store |
| HTTP konfigurace | `src/SportSys.Razor/Program.cs` | OIDC a middleware pipeline |

## Rozhraní

- Policy `SystemAdmin`, `Support` a `InternalUser` vyžadují stejnojmenné
  Identity role.
- Fallback policy odpovídá default policy, takže všechny nezpřístupněné
  endpointy vyžadují přihlášení.
- `SportSysClaimTypes.UserId` slouží pouze jako interní lokální identifikátor,
  ne jako business oprávnění.

## Integrační vazby

- Microsoft Entra ID poskytuje OIDC token a claims.
- ASP.NET Core Identity ukládá uživatele a role do SQL Serveru.
- HR docházka používá lokální user ID pro povinný audit uploadu.

## Závislosti

`SportSys.Razor` registruje OIDC, ale DbContext, Identity store, cookie schémata
a policies registruje výhradně `AddSportSysServices()` v Contract vrstvě.

## Omezení a pravidla

- Používej `AddIdentityCore<User>()` a `.AddSignInManager()`.
- Nepoužívej `AddIdentity<T>()`; přepsalo by výchozí OIDC schéma.
- Nepoužívej e-mail ani UPN jako klíč Entra uživatele.
- Po Identity scaffoldingu proveď cleanup podle příslušného skillu.
- Do dokumentace neuváděj business permissions jako aktivní, dokud se jejich
  entity a DbSety nevrátí do kompilace.
- TPT `User -> Coach` nemění přihlašovací tok ani typ Identity store.

## Příklady

Auditní služba získá lokální ID pomocí:

```csharp
var userId = CurrentUserIdResolver.GetRequiredUserId(principal);
```

## Odkazovaná dokumentace

- `docs/architecture.md`
- `.github/skills/identity-scaffold-cleanup/SKILL.md`
- `docs/decisions/adr-002-hybridni-identita.md`
- `docs/decisions/adr-004-coach-jako-tpt-potomek-user.md`
