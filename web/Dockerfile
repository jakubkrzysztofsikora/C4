FROM node:22-alpine
WORKDIR /app
COPY web/package*.json ./
RUN npm ci
COPY web .
EXPOSE 3000
CMD ["npm", "run", "dev"]
