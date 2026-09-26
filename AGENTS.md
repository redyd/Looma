# AGENTS.md

Guide pour les agents (et humains) qui modifient Looma. À lire avant toute modification.

## Projet

Application de bureau local-first (tricot / crochet) : **.NET 10**, **Avalonia 12**, **EF Core 10 + SQLite**, MVVM avec CommunityToolkit.Mvvm. Aucune donnée distante : tout vit dans le dossier de données de l'utilisateur.

## Architecture

```
src/
  Looma.Domain/          Entités, Result/ResultT, services métier, interfaces (repositories, IServices). Aucune dépendance UI/EF.
  Looma.Infrastructure/  EF Core (LoomaDbContext, Configurations, Migrations), repositories, stockage fichiers, backup, intégrité.
  Looma.Presentation/    ViewModels, navigation, notifications, TranslationService + Resources/Translations*.resx.
  Looma.Views/           Vues Avalonia (.axaml), styles, contrôles.
  Looma.App/             Point d'entrée, DI (DependencyInjection.cs), démarrage (App.axaml.cs), implémentations Avalonia (pickers, lifetime).
tests/                   Un projet xUnit par couche (Domain, Infrastructure, Presentation, App).
```

Dépendances : Domain ← Infrastructure, Presentation ← Views ← App. Le Domain ne référence jamais les autres couches.

## Commandes

```bash
dotnet build Looma.sln
dotnet test Looma.sln
dotnet run --project src/Looma.App -- --local                    # données dans ./Data au lieu d'AppData
dotnet run --project src/Looma.App -- --local --clear --seed     # base vide + données de démo
dotnet run --project src/Looma.App -- --local --seed-50          # démo avec 50 éléments
dotnet ef migrations add <Nom> --project src/Looma.Infrastructure  # nouvelle migration
```

Toujours utiliser `--local` pour tester à la main : ne jamais toucher aux données réelles de l'utilisateur (`~/.config/Looma`, `%AppData%\Looma`).

## Conventions obligatoires

- **En-tête de licence** sur chaque `.cs` et `.axaml` (vérifié en CI par `check-headers.yml`) :
  ```csharp
  // Copyright (c) 2026 SOEUR Timëo. All rights reserved.
  // This file is part of Looma, licensed under the AGPL-3.0.
  // See LICENSE in the project root for full license text.
  ```
- **Erreurs** : les repositories et services renvoient `Result` / `ResultT<T>` (`Ok`, `NotFound`, `Conflict`, `Failure`), pas d'exception vers l'UI. Les services passent par `DomainServiceBase.ExecuteAsync` (log + capture).
- **Traductions (dictionnaire)** : tout texte visible par l'utilisateur passe par `Translations*.resx` — 6 fichiers à mettre à jour ensemble (`Translations.resx` = anglais par défaut, `.en`, `.fr`, `.nl`, `.de`, `.es`).
  - Presentation / Views : `Translation["Clé"]`, `Translation.Format("Clé", args)`, `{loc:Loc Clé}` en XAML.
  - Domain / Infrastructure : `Localizer.Get("Clé")` / `Localizer.Format(...)` (`Looma.Domain.Localization`, branché sur `TranslationService` au démarrage).
  - Nommage des clés : `Section_Categorie_Nom` (ex. `Settings_Data_Export`, `Backup_Errors_InvalidArchive`).
  - Des messages FR codés en dur subsistent dans l'ancien code : ne pas en ajouter de nouveaux.
- **Styles** : réutiliser les classes existantes (`form-container`, `section-title`, `form-hint`, `form-submit`, `cancel-btn`, `danger-btn`, `alert-error`…) et les brushes `DynamicResource` du thème ; pas de couleurs en dur. Icônes : `LucideIcon Kind="…"` (vérifier que le nom existe, la compilation XAML échoue sinon).
- **Nouveaux services** : interface dans Domain (`IServices/` ou `Repositories/`), implémentation dans Infrastructure, enregistrement dans `src/Looma.App/DependencyInjection.cs`.
- **Sous-sections de Settings** : sous-ViewModel dédié (cf. `SettingsUpdaterViewModel`, `SettingsDataViewModel`) plutôt que gonfler `SettingsViewModel`.

## Données et intégrité (à respecter absolument)

Dossier de données (`AppPaths`) : `looma.db` (+ `-wal`/`-shm`, EF Core active le WAL), `documents/{guid}.ext`, `themes/*.json`, `config.json`, `backups/`, `pending-restore/`.

- **config.json** : uniquement via `AppConfigStore` (écriture atomique + verrou ; un JSON corrompu est renommé `config.json.corrupt-*` et remplacé par les valeurs par défaut). Ne jamais écrire le fichier directement.
- **Écritures de fichiers** : `AtomicFile` (fichier temporaire + renommage) pour tout fichier qui ne doit jamais être à moitié écrit.
- **Suppression = base d'abord, fichier ensuite** : `SaveChangesAsync` puis `AppPaths.TryDeleteDocumentFile`. Un fichier orphelin est récupérable, une ligne pointant vers rien ne l'est pas.
- **Opérations multi-étapes** (stock, fin de projet, ajout de plusieurs documents) : envelopper dans `IUnitOfWork.ExecuteAsync` (transaction, rollback si le `Result` échoue). Les appels imbriqués rejoignent la transaction externe.
- **`LoomaDbContext`** vit toute la session (services résolus depuis la racine) : il rejette les nombres non finis et vide son suivi après un `SaveChanges` échoué hors transaction. Le travail en arrière-plan doit créer son propre contexte (cf. `DataIntegrityService`).
- **Jamais de suppression silencieuse de données utilisateur** : mettre de côté (`*.corrupt-*`, `documents/.orphans/`, `restore-previous-*`) plutôt que supprimer.
- **Copie de la base** : toujours `DatabaseHealth.Snapshot` (`VACUUM INTO`), jamais une copie brute du fichier.

### Démarrage (`StartupDataGuard`)

1. Applique un import en attente (`pending-restore/`) avant toute ouverture de la base, avec sauvegarde de sécurité `backups/pre-import-*.looma` et rollback en cas d'échec.
2. Vérifie la base (`DatabaseHealth` : en-tête + `PRAGMA quick_check`). Corrompue → `RecoveryWindow` (restaurer une sauvegarde, importer, repartir de zéro en conservant l'ancienne base).
3. Migrations en attente → sauvegarde automatique `backups/pre-migration-*.looma` (5 conservées) avant `Migrate()`.
4. Après ouverture : contrôle d'intégrité en arrière-plan (documents manquants/orphelins, thèmes invalides, config récupérée) → notification.

### Sauvegardes `.looma`

ZIP contenant `manifest.json` (version de format, version de l'app, dernière migration, SHA-256 de chaque fichier), `looma.db`, `documents/`, `themes/`, `config.json` (thème + langue). L'import valide tout avant de modifier quoi que ce soit : liste blanche des chemins, anti zip-slip, tailles, empreintes, intégrité SQLite, refus d'une sauvegarde issue d'une version plus récente. Le remplacement a lieu au redémarrage suivant.

Toute modification du schéma EF doit rester compatible avec la restauration de sauvegardes anciennes (elles sont migrées au démarrage).

## Tests

- xUnit + FluentAssertions ; NSubstitute dans Domain/Infrastructure ; fakes écrits à la main dans Presentation (`TestSupport/`).
- Infrastructure : `RepositoryTestFixture` (SQLite en mémoire + dossier temporaire) ; les tests backup/démarrage utilisent un vrai fichier SQLite (appeler `SqliteConnection.ClearAllPools()` avant de déplacer ou supprimer la base).
- Toute correction ou fonctionnalité touchant aux données s'accompagne d'un test ; `dotnet test Looma.sln` doit rester vert.
