FROM node:24-alpine AS compilacao
WORKDIR /codigo

COPY package.json package-lock.json ./
RUN npm ci

COPY angular.json tsconfig*.json ./
COPY src/ src/
RUN npm run build

FROM nginx:stable-alpine AS execucao
RUN apk add --no-cache curl
COPY nginx.conf /etc/nginx/templates/nginx.conf.template
COPY --from=compilacao /codigo/dist/gestao-tarefas/browser/ /usr/share/nginx/html/
ENV NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1 \
    NGINX_ENVSUBST_OUTPUT_DIR=/tmp \
    NGINX_ENVSUBST_FILTER=NGINX_LOCAL_RESOLVERS
USER nginx
EXPOSE 8080
CMD ["nginx", "-c", "/tmp/nginx.conf", "-g", "daemon off;"]
