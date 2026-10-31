# SattlerFrontend

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 20.3.6.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Docker

The image builds the app and serves it with nginx on port 8080. The URLs are set when the container starts, so the same image works for every environment:

```bash
docker build -t sattler-frontend .
docker run -p 8080:8080 \
  -e API_URL=https://api.example.at/api \
  -e KEYCLOAK_ISSUER=https://auth.example.at/realms/equipressure \
  sattler-frontend
```

Both variables are required, the container does not start without them. Keycloak needs HTTPS unless it runs on localhost.

The URL of the frontend also has to be added to:

- the client `sattler-web` in Keycloak (`redirectUris`, `webOrigins`)
- `General:ClientOrigin` of the backend (CORS)

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
