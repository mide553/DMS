# Use the official nginx base image
FROM nginx:1.25-alpine

# Remove default nginx configuration
RUN rm /etc/nginx/conf.d/default.conf

# Copy our nginx configuration
COPY nginx.conf /etc/nginx/conf.d/

# Cache busting - this ensures the COPY layer below is never cached
ARG CACHEBUST=1
RUN echo "Cache bust: $CACHEBUST"

# Copy the web application files to nginx html directory
COPY src/ /usr/share/nginx/html/

# Set proper permissions
RUN chown -R nginx:nginx /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]