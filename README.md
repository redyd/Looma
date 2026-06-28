<p align="center">
  <img src="src/Looma.App/Assets/logo.png" alt="Looma" width="60" />
  <h1 align="center">Looma</h1>
</p>

Looma est une application de bureau local-first pour organiser ses projets de tricot, crochet et crochet tunisien.

Elle centralise le stock de laine, les patrons, les documents, les images de projets et les statistiques d'utilisation, sans compte utilisateur ni service distant obligatoire. Les données restent sur la machine, dans une base SQLite et un dossier de documents local.

Looma est construit avec [Avalonia UI](https://avaloniaui.net/) et vise Windows, macOS et Linux.

---

## Fonctionnalités

### Projets

- Suivi des projets par statut : liste des souhaits, en cours, en pause ou fini.
- Association d'un projet à un patron et à une ou plusieurs laines du stock.
- Dates de début et de fin, notes, recherche et filtrage.
- Ajout d'images au projet, renommage au moment de l'import et consultation en détail.
- Actions rapides pour démarrer, mettre en pause, reprendre ou terminer un projet.
- Finalisation avec déduction de laine par pelote, poids ou longueur.

### Stock de laine

- Gestion des laines avec marque, nom, matière, couleurs, poids, longueur et quantité disponible.
- Ajustement du stock par pelote, par poids ou par longueur.
- Calcul automatique des quantités totales en grammes, mètres et pelotes.
- Sélection de la taille d'aiguilles via les plages de laine du domaine.
- Affichage d'une image de type de laine selon la plage d'aiguilles choisie.
- Recherche, pagination et fiche détaillée pour chaque laine.

### Patrons

- Création de patrons personnels ou externes.
- Types pris en charge : crochet, crochet tunisien et tricot.
- Notes, URL source, dates, documents associés et projets liés.
- Import, renommage et consultation de documents rattachés aux patrons.
- Navigation directe entre un patron et ses projets.

### Documents

- Import de documents dans le dossier local de Looma.
- Prise en charge des fichiers génériques pour les patrons et des images pour les projets.
- Recherche, pagination, renommage et suppression.
- Retour rapide vers le patron ou le projet lié à un document.

### Statistiques

- Graphique d'utilisation de laine basé sur les mouvements de stock.
- Filtres par période : tout, année en cours, six derniers mois, mois en cours ou semaine en cours.
- Filtre par type de patron.
- Affichage des quantités en pelotes, grammes ou mètres.

### Réglages

- Interface disponible en français et en anglais.
- Thèmes JSON importables, exportables, ouvrables et supprimables.
- Thèmes fournis au démarrage dans `src/Looma.App/Seed/Themes`.
- Vérification des mises à jour, notes de version et installation via Velopack.

### Stockage local

- Base de données SQLite.
- Documents importés copiés dans le dossier de données de l'application.
- Images de projets stockées comme documents locaux.
- Aucun compte, aucune synchronisation cloud imposée.

---

## Stack technique

- .NET 10
- Avalonia UI 12
- Entity Framework Core
- SQLite
- Velopack
- xUnit, FluentAssertions et NSubstitute

La solution est découpée en plusieurs projets :

- `src/Looma.Domain` : entités, services métier, recherches, statistiques et contrats.
- `src/Looma.Infrastructure` : SQLite, repositories, migrations EF Core et stockage local.
- `src/Looma.Presentation` : view models, navigation, traductions, notifications et thèmes.
- `src/Looma.Views` : vues Avalonia, styles, contrôles et converters.
- `src/Looma.App` : application de bureau, injection de dépendances, assets, seeds et mises à jour.

---

## Développement

### Prérequis

- .NET 10 SDK

### Lancer l'application

```bash
dotnet run --project src/Looma.App
```

### Tester

```bash
dotnet test
```

### Compiler

```bash
dotnet build
```

---

## Arguments de développement

Les arguments de démarrage sont gérés dans `src/Looma.App/App.axaml.cs`.

Passe les arguments après `--` avec `dotnet run` :

```bash
dotnet run --project src/Looma.App -- --local
```

### `--local`

Utilise un dossier de données local au projet :

```text
./Data
```

Sans `--local`, Looma stocke ses données dans le dossier applicatif du système, dans un répertoire `Looma`.

### `--clear`

Supprime la base SQLite et vide le dossier de documents avant le démarrage.

À utiliser avec attention :

```bash
dotnet run --project src/Looma.App -- --local --clear
```

### `--seed`

Remplit une base vide avec des données de démonstration :

- 10 laines
- 3 patrons
- 1 projet par statut
- documents de démonstration attachés aux patrons

Le seeder ne s'exécute que sur une base vide. Pour régénérer les données de démo, combine-le avec `--clear` :

```bash
dotnet run --project src/Looma.App -- --local --clear --seed
```

### `--seed-N`

Génère `N` éléments par collection principale, avec `N >= 0`.

Exemple avec 25 enregistrements générés :

```bash
dotnet run --project src/Looma.App -- --local --clear --seed-25
```

Les valeurs invalides déclenchent une erreur d'argument. Par exemple, `--seed--1` et `--seed-abc` sont rejetés.

### Commandes utiles

Utiliser une base locale isolée :

```bash
dotnet run --project src/Looma.App -- --local
```

Réinitialiser la base locale :

```bash
dotnet run --project src/Looma.App -- --local --clear
```

Réinitialiser avec les données de démonstration :

```bash
dotnet run --project src/Looma.App -- --local --clear --seed
```

Réinitialiser avec un jeu de données plus large :

```bash
dotnet run --project src/Looma.App -- --local --clear --seed-100
```

---

## Fichiers de données

Looma stocke :

- `looma.db` pour la base SQLite.
- `documents/` pour les documents importés et les images de projets.
- `themes/` pour les thèmes JSON importés ou exportés.

Avec `--local`, ces fichiers sont créés dans `./Data`.

---

## Site et téléchargements

Le site de Looma est disponible ici : [looma.redyd.dev](https://looma.redyd.dev).

---

## Licence

Ce projet est distribué sous licence [GNU Affero General Public License v3.0](./LICENSE). Le code est ouvert à la lecture, mais pas aux contributions ni à l'usage commercial.
