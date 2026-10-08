FROM node:22-alpine AS build
WORKDIR /src
RUN npm i -g pnpm@11

COPY TodoSystem.WebClient/package.json TodoSystem.WebClient/pnpm-lock.yaml TodoSystem.WebClient/pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile

COPY TodoSystem.WebClient/ ./
RUN pnpm build

FROM nginx:1.27-alpine AS final
COPY --from=build /src/dist /usr/share/nginx/html
COPY TodoSystem.WebClient/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
