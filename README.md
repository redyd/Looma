<p align="center">
  <img src="src/Looma.App/Assets/logo.png" alt="Looma" width="60" />
  <h1 align="center">Looma</h1>
</p>

Looma is a local-first desktop app for knitters and crafters. It helps you manage wool stock, patterns, documents and projects without an account or remote service.

Built with [Avalonia UI](https://avaloniaui.net/), Looma targets Windows, macOS and Linux.

---

## Features

**Wool stock**

- Track brand, material, colors, weight, length and current stock.
- Adjust stock by ball, weight or length.
- Select needle size from the domain wool ranges instead of entering free-form min/max values.
- Display the matching wool type image from the selected needle range.
- Search and paginate the stock list.

**Patterns**

- Create personal or external patterns.
- Track pattern type: crochet, Tunisian crochet or knitting.
- Attach and rename documents such as PDFs, text files and images.
- Open linked URLs and keep notes on each pattern.

**Projects**

- Link projects to patterns and one or more wools.
- Track status: wishlist, in progress, paused or finished.
- Attach project images and browse them in the detail view.
- Search available patterns and wools while editing a project.
- Finish projects with wool deduction options.

**Documents**

- Store imported files in Looma's local document folder.
- Search, paginate, rename and delete documents.
- Jump from a document back to its linked pattern or project.

**Local storage**

- SQLite database.
- Imported documents are copied into the app data folder.
- No account, no cloud sync.

---

## Tech Stack

- .NET 10
- Avalonia UI 12
- Entity Framework Core
- SQLite
- Velopack
- xUnit, FluentAssertions and NSubstitute

The solution is split into domain, infrastructure, presentation, views and app projects:

- `src/Looma.Domain`
- `src/Looma.Infrastructure`
- `src/Looma.Presentation`
- `src/Looma.Views`
- `src/Looma.App`

---

## Development

### Prerequisites

- .NET 10 SDK

### Run

```bash
dotnet run --project src/Looma.App
```

### Test

```bash
dotnet test
```

### Build

```bash
dotnet build
```

---

## Development Arguments

Startup arguments are handled in `src/Looma.App/App.axaml.cs`.

Pass app arguments after `--` when using `dotnet run`:

```bash
dotnet run --project src/Looma.App --local
```

### `--local`

Uses a local development data folder at:

```text
./Data
```

Without `--local`, Looma stores data under the OS application data folder in a `Looma` directory.

### `--clear`

Deletes the SQLite database and clears the document storage folder before startup.

Use carefully:

```bash
dotnet run --project src/Looma.App --local --clear
```

### `--seed`

Seeds the database with the default demonstration data:

- 10 wool entries
- 3 patterns
- 1 project per project status
- demo documents attached to patterns

The seeder only runs on an empty database. To regenerate demo data, combine it with `--clear`:

```bash
dotnet run --project src/Looma.App --local --clear --seed
```

### `--seed-N`

Seeds `N` generated items per main collection, where `N >= 0`.

Example with 25 generated records:

```bash
dotnet run --project src/Looma.App --local --clear --seed-25
```

Invalid values throw an argument error. For example, `--seed--1` or `--seed-abc` are rejected.

### Common Development Commands

Use an isolated local database:

```bash
dotnet run --project src/Looma.App --local
```

Reset the local database:

```bash
dotnet run --project src/Looma.App --local --clear
```

Reset and seed default demo data:

```bash
dotnet run --project src/Looma.App --local --clear --seed
```

Reset and seed a larger dataset:

```bash
dotnet run --project src/Looma.App --local --clear --seed-100
```

---

## Data Files

Looma stores:

- `looma.db` for the SQLite database.
- `documents/` for imported documents and project images.

With `--local`, both are created inside `./Data`.

---

## Website & Downloads

Find Looma's website here: [looma.redyd.dev](https://looma.redyd.dev).

---

## License

This project is licensed under the [GNU Affero General Public License v3.0](./LICENSE). It is open to read, but not open for contributions or commercial use.
