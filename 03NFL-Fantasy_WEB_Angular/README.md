# NFLFantasyWEBAngular

This is the Angular web application for the NFL Fantasy project. It uses the official [Angular CLI](https://angular.dev/tools/cli).

## Prerequisites

- Node.js LTS installed (recommend the latest LTS release)
- Package manager: npm (bundled with Node.js)
- Angular CLI available via npx (no global install required)

## Install dependencies

Run this once after cloning the repo:

```powershell
npm install
```

## Development server

Start the local dev server (hot-reload enabled):

```powershell
# Option 1: use the project script if defined
npm start

# Option 2: run via npx
npx ng serve
```

Then open `http://localhost:4200/` in your browser. The app reloads automatically when you change source files.

## Code scaffolding

Generate components, directives, pipes, etc.:

```powershell
npx ng generate component component-name
```

For all schematic options, run:

```powershell
npx ng generate --help
```

## Building

Create a production build:

```powershell
npx ng build --configuration production
```

Build outputs are written to the `dist/` folder. With recent Angular versions, the path is typically:

```
dist/<project-name>/browser
```

## Running unit tests

Run unit tests (the exact runner depends on the project setup, e.g., Karma, Jest, or Vitest):

```powershell
npx ng test
```

If tests aren’t configured yet, add a test runner first or remove this step from your workflow.

## Running end-to-end tests

E2E testing requires adding a tool like Cypress or Playwright. After configuring an e2e builder, you can run:

```powershell
npx ng e2e
```

## Additional resources

- Angular CLI docs: https://angular.dev/tools/cli
- Angular docs: https://angular.dev/
