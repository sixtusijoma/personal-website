# sixtus.dev

My personal site, built to look like a desktop. The resume and a Q&A console open as draggable windows (the console answers questions about me using Gemini), and the GitHub and LinkedIn icons link out.

- `frontend/`: React + TypeScript (Vite), hosted on Cloudflare Pages.
- `backend/`: ASP.NET Core minimal API, hosted on Render. It answers console questions and logs them to Supabase Postgres.

It's mostly written by hand, with some AI assistance.

## Running locally

```sh
cd backend
dotnet run        # http://localhost:5035

cd frontend
npm install
npm run dev       # http://localhost:5173, proxies /api to the backend
```

The backend reads `GEMINI_API_KEY`, `SUPABASE_DB_CONNECTION`, `IP_HASH_SALT`, `PROFILE_JSON` and `BIRTH_DATE` from configuration (environment variables work). It exits at startup and names any that are missing.

See [DEPLOY.md](DEPLOY.md) for hosting.

## License

[MIT](LICENSE)
