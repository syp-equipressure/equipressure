# Multi-Stage Dockerfile für das Sattler-Frontend
# Stage 1: Build
FROM node:22-alpine AS build
WORKDIR /app

# package files zuerst für besseres Layer Caching
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# Stage 2: Runtime (nginx ohne root, Port 8080)
FROM nginxinc/nginx-unprivileged:1.29-alpine AS runtime

# API_URL und KEYCLOAK_ISSUER werden beim Containerstart in das Template eingesetzt
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist/sattler_frontend/browser /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget -q --spider http://127.0.0.1:8080/ || exit 1
