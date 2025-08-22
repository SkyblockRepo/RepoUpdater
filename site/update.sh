#!/bin/bash
set -e # Exit immediately if a command fails

git pull

# Get the short hash of the latest commit
export COMMIT_HASH=$(git rev-parse --short HEAD)

echo "Deploying commit: $COMMIT_HASH"

docker compose up --build -d

# Clean up old, unused images and build cache
echo "Pruning old Docker images and build cache..."
docker image prune -f
docker builder prune -f

echo "Deployment finished and system pruned!"