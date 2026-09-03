#!/bin/bash
set -e

ACTIVE_FILE="/root/active_color"
UPSTREAM_FILE="/etc/nginx/conf.d/api_upstream.conf"
BLUE_PORT=5000
GREEN_PORT=5001
HEALTH_RETRIES=30
HEALTH_INTERVAL=2

# Read current active color
if [ -f "$ACTIVE_FILE" ]; then
    ACTIVE=$(cat "$ACTIVE_FILE")
else
    ACTIVE="blue"
fi

# Determine new color and ports
if [ "$ACTIVE" = "blue" ]; then
    NEW="green"
    NEW_PORT=$GREEN_PORT
    OLD="blue"
else
    NEW="blue"
    NEW_PORT=$BLUE_PORT
    OLD="green"
fi

echo "=== Blue-Green Deploy ==="
echo "Active: $ACTIVE → deploying: $NEW (port $NEW_PORT)"

cd /root/parrotsAPI2

# Build and start new container
echo "Building api-$NEW..."
docker compose up --build -d api-$NEW

# Wait for health check
echo "Waiting for api-$NEW to be healthy..."
for i in $(seq 1 $HEALTH_RETRIES); do
    if curl -sf http://localhost:$NEW_PORT/api/Health > /dev/null 2>&1; then
        echo "api-$NEW is healthy"
        break
    fi
    if [ $i -eq $HEALTH_RETRIES ]; then
        echo "ERROR: api-$NEW failed to become healthy. Aborting."
        docker compose stop api-$NEW
        exit 1
    fi
    echo "  attempt $i/$HEALTH_RETRIES..."
    sleep $HEALTH_INTERVAL
done

# Switch nginx upstream (near-zero downtime)
echo "Switching nginx to port $NEW_PORT..."
echo "upstream api_backend { server localhost:$NEW_PORT; }" > $UPSTREAM_FILE
nginx -s reload

# Record new active color
echo "$NEW" > "$ACTIVE_FILE"

# Wait for in-flight requests to finish on the old container
echo "Waiting 15 seconds for in-flight requests to complete on api-$OLD..."
sleep 15

# Stop old container
echo "Stopping api-$OLD..."
docker compose stop api-$OLD

echo "=== Deploy complete. Active: $NEW on port $NEW_PORT ==="
