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

Page en deux onglets, avec filtres par période (tout, année en cours, six derniers mois, mois en cours, semaine en cours) et par type de patron. Le découpage des graphiques s'adapte à la période (jour, semaine, mois ou année).

- **Laine** : chiffres clés (laine utilisée avec tendance vs période précédente, laine ajoutée, stock actuel, projets alimentés), colonnes « utilisée / ajoutée » dans le temps, répartitions par matière et par grosseur, stock par grosseur, top 5 des laines et des projets les plus gourmands. Quantités en pelotes, grammes ou mètres.
- **Global** : projets terminés (avec tendance), commencés, durée moyenne, total ; projets commencés / terminés dans le temps ; répartitions par statut et par technique ; projets les plus longs ; nombre de patrons, laines et documents.
- L'historique de consommation est conservé même après la suppression d'une laine ou d'un projet : chaque mouvement de stock garde une copie des caractéristiques de la laine et du projet au moment où il a été enregistré.

### Réglages

- Interface disponible en français, anglais, néerlandais, allemand et espagnol.
- Thèmes JSON importables, exportables, ouvrables et supprimables.
- Thèmes fournis au démarrage dans `src/Looma.App/Seed/Themes`.
- Vérification des mises à jour, notes de version et installation via Velopack.
- Section « Données et sauvegardes » : export, import, vérification et réparation des données.
- Réinitialisation complète de l'application, avec double confirmation et sauvegarde automatique préalable (`backups/pre-reset-*.looma`).

### Sauvegardes et intégrité des données

- Export complet dans un fichier `.looma` : base de données, documents, images, thèmes et préférences.
- Import d'une sauvegarde en remplacement total des données : l'archive est entièrement vérifiée (empreintes SHA-256, chemins, intégrité SQLite, version) avant toute modification, une sauvegarde de sécurité des données actuelles est créée, puis Looma redémarre pour l'appliquer. En cas d'échec, les données précédentes sont remises en place.
- Sauvegarde automatique avant chaque migration de la base (5 conservées dans le dossier `backups`).
- Écran de récupération au démarrage si la base est endommagée : restaurer une sauvegarde, importer un fichier ou repartir de zéro en conservant l'ancienne base.
- Vérification des données : documents dont le fichier manque (signalés dans les listes), fichiers orphelins (mis en quarantaine dans `documents/.orphans`), thèmes invalides, préférences corrompues (réinitialisées, copie conservée).
- Écritures atomiques et transactions : une erreur ne laisse jamais de modification à moitié appliquée.

### Stockage local

- Base de données SQLite.
- Préférences dans `config.json`, sauvegardes automatiques dans `backups`.
- Documents importés copiés dans le dossier de données de l'application.
- Images de projets stockées comme documents locaux.
- Aucun compte, aucune synchronisation cloud imposée.

---

## Stack technique

- .NET 10
- Avalonia UI 12.1
- Entity Framework Core 10
- SQLite
- Velopack 1.2
- xUnit, FluentAssertions et NSubstitute

La solution est découpée en plusieurs projets :

- `src/Looma.Domain` : entités, services métier, recherches, statistiques et contrats.
- `src/Looma.Infrastructure` : SQLite, repositories, migrations EF Core, stockage local, sauvegardes et contrôle d'intégrité.
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

### Conventions

Les règles à respecter pour contribuer (architecture, traductions, intégrité des données, tests) sont décrites dans [`AGENTS.md`](AGENTS.md).

### Langues

Les traductions de l'application sont dans `src/Looma.Presentation/Resources` :

- `Translations.resx` : ressources neutres.
- `Translations.fr.resx` : français.
- `Translations.en.resx` : anglais.
- `Translations.nl.resx` : néerlandais.
- `Translations.de.resx` : allemand.
- `Translations.es.resx` : espagnol.

La liste des langues affichées dans les réglages est déclarée dans `TranslationService.SupportedLanguage`.

Tout texte visible par l'utilisateur doit être ajouté dans les six fichiers. Les couches Domain et Infrastructure y accèdent via `Localizer` (`Looma.Domain.Localization`).

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

Supprime la base SQLite et vide le dossier de documents avant le démarrage. Les sauvegardes du dossier `backups` ne sont pas supprimées.

À utiliser avec attention :

```bash
dotnet run --project src/Looma.App -- --local --clear
```

### `--seed`

Remplit une base vide avec des données de démonstration :

- 10 laines
- 3 patrons
- 12 projets (3 par statut) avec dates de début et de fin
- documents de démonstration attachés aux patrons
- environ 18 mois d'historique de stock (achats, consommations réparties sur la durée des projets, ajustements récents) pour alimenter les statistiques
- une laine utilisée puis supprimée, pour illustrer la conservation de l'historique

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
