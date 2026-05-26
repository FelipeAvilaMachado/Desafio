FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble

# Workaround: remove stale Yarn apt source inherited from base image.
# The docker-outside-of-docker feature runs apt-get update and fails when this
# source is present without its trusted key in some host environments.
RUN set -eux; \
    if [ -d /etc/apt/sources.list.d ]; then \
        find /etc/apt/sources.list.d -maxdepth 1 -type f -iname '*yarn*' -print -delete; \
    fi