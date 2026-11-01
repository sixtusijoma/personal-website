# Deploying

- Frontend: Cloudflare Pages at `https://sixtus.dev` and `https://www.sixtus.dev`.
- Backend: Render free web service built from `backend/Dockerfile`.
- Database: the existing Supabase Postgres.

Deploy the backend first so you have its URL for the frontend build.

## Backend on Render

Either create a Blueprint from `render.yaml` (Render prompts for each secret), or create a Web Service by hand with these settings:

| Setting | Value |
| --- | --- |
| Language / runtime | Docker |
| Branch | the branch that has the site code (`main` only holds a starter commit) |
| Root directory | blank |
| Dockerfile path | `./backend/Dockerfile` |
| Docker build context | `./backend` |
| Instance type | Free |
| Health check path | `/api/health` |
| Build filters (included paths) | `backend/**` |
| Region | the one closest to the Supabase project (it can't be changed later; for the Blueprint, add `region:` to `render.yaml` before the first sync, otherwise it defaults to `oregon`) |

Environment variables:

| Name | Notes |
| --- | --- |
| `GEMINI_API_KEY` | secret |
| `SUPABASE_DB_CONNECTION` | secret. Npgsql keyword format (`Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require`). Use Supabase's **Session pooler** host: Render has no outbound IPv6 and the direct `db.<ref>.supabase.co` host is IPv6-only. |
| `IP_HASH_SALT` | secret. Reuse the value that produced existing `ip_hash` rows if hashes should stay comparable. |
| `PROFILE_JSON` | the profile JSON (same shape as `backend/profile.example.json`); multi-line values are fine. |
| `BIRTH_DATE` | `yyyy-MM-dd` |
| `CORS_ALLOWED_ORIGINS` | optional, comma-separated. Defaults to `https://sixtus.dev,https://www.sixtus.dev` (from `appsettings.json`). Setting it replaces the default list. |

Don't set `PORT`. Render sets it, and the container listens on it (falling back to 8080). The app exits at startup and names any missing setting.

The service URL is `https://<service-name>.onrender.com`. To use `api.sixtus.dev` instead, add it as a custom domain in Render and create a CNAME in Cloudflare DNS pointing at the onrender.com host.

## Frontend on Cloudflare Pages

Connect the repo (Workers & Pages > Create > Pages > Connect to Git):

| Setting | Value |
| --- | --- |
| Production branch | the branch that has the site code |
| Framework preset | None |
| Root directory | `frontend` |
| Build command | `npm run build` |
| Build output directory | `dist` |

Environment variables (Production):

| Name | Notes |
| --- | --- |
| `VITE_API_BASE_URL` | the backend's public origin, e.g. the onrender.com URL. Baked in at build time, so redeploy after changing it. Leave it unset locally: the app then calls `/api` on its own origin, which the Vite dev server proxies to `localhost:5035`. |
| `NODE_VERSION` | `22` (Vite 8 needs Node 20.19+ or 22.12+) |

Then add `sixtus.dev` and `www.sixtus.dev` under Custom domains. Both are in the backend's CORS list.

No `_redirects` file is needed. The site has no client-side routes, and Pages already serves `index.html` for unknown paths when there is no `404.html`. Preview deployments (`*.pages.dev`) aren't in the CORS list, so their console will show the error message unless you add their origin to `CORS_ALLOWED_ORIGINS`.

## Database migrations

Migrations are **not** applied on startup. The production database already has the current schema plus the hand-applied `keepalive()` function. A failing migration at startup would crash-loop the service, and Render's free tier has no pre-deploy step. When you add a migration, apply it yourself before deploying the code that needs it:

```sh
cd backend
dotnet tool install --global dotnet-ef --version 10.0.13
dotnet ef migrations script --idempotent -o ../migrate.sql
```

Review `migrate.sql`, run it in the Supabase SQL editor (or `psql`), and delete the file. The idempotent script checks `__EFMigrationsHistory` and only applies missing migrations. It doesn't touch `keepalive()`, and running it against an up-to-date database does nothing. Alternatively, export `SUPABASE_DB_CONNECTION` in your shell and run `dotnet ef database update`.

## Visitor IPs

Render puts Cloudflare and then its own internal proxy in front of the container, and each appends to `X-Forwarded-For`. The backend walks that header right to left and stops at the first address that isn't in `TrustedProxyNetworks` (`backend/appsettings.json`: private ranges plus [Cloudflare's published ranges](https://www.cloudflare.com/ips/)). That address is the visitor, and it's what gets HMAC-hashed into `chat_logs.ip_hash`. A client can't spoof it by sending its own `X-Forwarded-For`, and requests from untrusted peers ignore the header entirely. If Cloudflare publishes new ranges, add them to that list. This also works if `api.sixtus.dev` is proxied (orange cloud) through your own Cloudflare zone.

## Cold starts

Render's free instance sleeps after about 15 minutes idle and takes up to a minute to wake. Loading the page sends a request to `/api/health` once, so the backend starts waking before the visitor opens the console. If an answer takes longer than 5 seconds, the console shows "Waking up the server…". Requests time out after 90 seconds.

## Running the container locally

```sh
docker build -t sixtus-api backend
docker run --rm -p 8080:8080 --env-file backend/.env.docker sixtus-api
```

`backend/.env.docker` is git-ignored (by `.env.*`) and needs the variables listed above. Npgsql may log `Cannot load library libgssapi_krb5.so.2` when it connects. That's harmless (Supabase doesn't use Kerberos), and you can silence it by adding `Gss Encryption Mode=Disable` to the connection string.
