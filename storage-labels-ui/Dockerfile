# Build stage
FROM node:20-alpine AS build

WORKDIR /app

# Declare build arguments
ARG API_URL=""
ARG REACT_APP_AUTH0_DOMAIN
ARG REACT_APP_AUTH0_CLIENT_ID
ARG REACT_APP_AUTH0_AUDIENCE

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Create .env file from build args
RUN echo "API_URL=${API_URL}" > .env && \
    echo "REACT_APP_AUTH0_DOMAIN=${REACT_APP_AUTH0_DOMAIN}" >> .env && \
    echo "REACT_APP_AUTH0_CLIENT_ID=${REACT_APP_AUTH0_CLIENT_ID}" >> .env && \
    echo "REACT_APP_AUTH0_AUDIENCE=${REACT_APP_AUTH0_AUDIENCE}" >> .env

# Display the .env file contents for debugging
RUN echo "=== .env file contents ===" && cat .env && echo "========================="

# Build the application (Parcel will read .env file)
RUN npm run build

# Production stage
FROM nginx:alpine

# Copy custom nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built files from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
