# Use the offical nginx (based on debian)
FROM nginx:stable

ENV STI_SCRIPTS_PATH=/usr/libexec/s2i
ENV USE_SELF_SIGNED_SSL='usss'
ENV WEB_BASE_HREF='/scjscv/'
ENV API_URL='http://host.docker.internal:5000/api/'
ENV RealIpFrom='172.17.0.1'
ENV NODE_ENV='development'
ENV SHELL=/bin/bash

# Required for HTTP Basic feature
RUN apt-get update -y && \
    apt-get install -y openssl ca-certificates procps gpgconf net-tools && \
    rm -rf /var/lib/apt/lists/*

RUN mkdir /var/www && mkdir /var/www/.vscode-server && mkdir /var/www/.gnupg && mkdir /var/www/.devcontainer && mkdir /workspaces

# Copy our OpenShift s2i scripts over to default location
COPY ./fix-base-url /usr/libexec/s2i/
COPY ./run /usr/libexec/s2i/

# Expose this variable to OpenShift
LABEL io.openshift.s2i.scripts-url=image:///usr/libexec/s2i

# Copy config from source to container
COPY nginx.conf.template /tmp/

# =================================================================================
# Fix up permissions
# ref: https://torstenwalter.de/openshift/nginx/2017/08/04/nginx-on-openshift.html
# - S2I scripts must be executable
# - Make sure nginx can read and write it's working directories.
# - The container dynamically configures nginx on startup
# - The application artifacts live in /tmp
# ---------------------------------------------------------------------------------
RUN chmod -R g+rwx $STI_SCRIPTS_PATH
RUN chmod og+rw /var/cache/nginx \ 
    /var/run \
    /etc/nginx/nginx.conf \
    /tmp
RUN chmod og+rw /var/www/.vscode-server \
    /var/www \
    /var/www/.gnupg \
    /var/cache/nginx \
    /var/www/.devcontainer \
    /workspaces

# =================================================================================

# Work-around for issues with S2I builds on Windows
WORKDIR /tmp

# Nginx runs on port 8080 by default
EXPOSE 8080

# Switch to usermode
USER www-data
