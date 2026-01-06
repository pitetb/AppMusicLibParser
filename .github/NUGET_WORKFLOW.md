# Configuration GitHub pour la publication du package NuGet

Ce projet utilise GitHub Actions pour automatiser la construction et la publication du package NuGet **AppMusicLibParser**.

## Workflows disponibles

### 1. CI Build (`.github/workflows/ci.yml`)

Workflow de build continu qui s'exécute sur :
- Push sur les branches `main` et `develop`
- Pull requests vers `main` et `develop`

**Actions :**
- Build sur Ubuntu, Windows et macOS
- Exécution des tests
- Création du package NuGet (artifacts)

### 2. NuGet Publish (`.github/workflows/nuget-publish.yml`)

Workflow de publication qui s'exécute sur :
- Tags de version (format `v*.*.*`, ex: `v1.0.0`)
- Déclenchement manuel via l'interface GitHub Actions

**Actions :**
- Build et test
- Création du package NuGet avec version depuis le tag
- Publication sur NuGet.org (si tag)
- Création d'une GitHub Release avec le package

## Configuration requise

### Secrets GitHub

Pour publier sur NuGet.org, vous devez configurer un secret dans votre repository GitHub :

1. Allez sur [NuGet.org](https://www.nuget.org/)
2. Connectez-vous et allez dans **Account Settings → API Keys**
3. Créez une nouvelle API Key avec les permissions :
   - **Push** : Activé
   - **Push new packages and package versions** : Activé
   - **Glob Pattern** : `AppMusicLibParser`
4. Dans GitHub, allez dans **Settings → Secrets and variables → Actions**
5. Créez un nouveau secret nommé `NUGET_API_KEY` avec votre clé API NuGet

## Publier une nouvelle version

### Méthode 1 : Via Git Tag (recommandé)

```bash
# Créer et pousser un tag de version
git tag v1.0.0
git push origin v1.0.0

# Le workflow se déclenchera automatiquement et publiera sur NuGet.org
```

### Méthode 2 : Déclenchement manuel

1. Allez dans l'onglet **Actions** de votre repository GitHub
2. Sélectionnez le workflow **Build and Publish NuGet Package**
3. Cliquez sur **Run workflow**
4. Entrez la version souhaitée (ex: `1.0.1`)
5. Cliquez sur **Run workflow**

> **Note** : Le déclenchement manuel ne publiera pas automatiquement sur NuGet.org. Le package sera disponible dans les artifacts du workflow.

## Tester localement

```bash
# Build
dotnet build MusicParser/MusicParser.csproj --configuration Release

# Créer le package
dotnet pack MusicParser/MusicParser.csproj --configuration Release --output ./nupkg

# Vérifier le contenu du package
dotnet nuget push ./nupkg/AppMusicLibParser.1.0.0.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY --skip-duplicate
```

## Versionning

Le projet suit le [Semantic Versioning](https://semver.org/) :

- **MAJOR** : Changements incompatibles avec l'API
- **MINOR** : Ajout de fonctionnalités rétro-compatibles
- **PATCH** : Corrections de bugs rétro-compatibles

Format : `v{MAJOR}.{MINOR}.{PATCH}` (ex: `v1.2.3`)

## Métadonnées du package

Les métadonnées sont définies dans `MusicParser/MusicParser.csproj` :

- **PackageId** : `AppMusicLibParser`
- **Version** : Définie via le tag git ou l'input manuel
- **License** : GPL-3.0-or-later
- **Repository** : Lien vers GitHub
- **Tags** : `apple-music`, `musicdb`, `parser`, `library`, `itunes`, `metadata`, `music`, `reverse-engineering`

## Artifacts

Chaque exécution du workflow génère des artifacts :
- **CI Build** : `build-artifacts` (package .nupkg)
- **NuGet Publish** : `nuget-packages` (package .nupkg)

Les artifacts sont conservés pendant 90 jours par défaut.
