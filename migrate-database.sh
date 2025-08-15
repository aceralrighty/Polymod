#!/bin/bash

# Database Migration Script for Polymod
# Run this after SQL Server is up and running

echo "Starting database migrations for all modules..."
# Determine script directory to resolve paths regardless of where it's run from
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
STARTUP_PROJ="$SCRIPT_DIR/TBD.csproj"
if [ ! -f "$STARTUP_PROJ" ]; then
  STARTUP_PROJ=""
fi

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to be ready..."
sleep 10

# Run migrations for each module
modules=("Auth" "User" "Address" "Schedule" "Service" "Recommendation" "StockPrediction" "Metrics")

for module in "${modules[@]}"; do
    echo "Running migrations for ${module}Module..."

    # Find the module project file anywhere under the repo/script directory
    PROJECT_PATH="$(find "$SCRIPT_DIR" -type f -name "${module}Module.csproj" -print -quit)"
    if [ -z "$PROJECT_PATH" ]; then
        echo "❌ Could not find ${module}Module.csproj under $SCRIPT_DIR"
        exit 1
    fi

    # Build EF command
    CMD=(dotnet ef database update --context "${module}Context" --project "$PROJECT_PATH")
    if [ -n "$STARTUP_PROJ" ]; then
        CMD+=(--startup-project "$STARTUP_PROJ")
    fi

    "${CMD[@]}"

    if [ $? -eq 0 ]; then
        echo "✅ ${module}Module migrations completed successfully"
    else
        echo "❌ ${module}Module migrations failed"
        exit 1
    fi
done

echo "All migrations completed successfully!"

# Run seeding
echo "Running data seeding..."
if [ -n "$STARTUP_PROJ" ]; then
  dotnet run --project "$STARTUP_PROJ" -- --seed
else
  dotnet run --project Polymod -- --seed
fi

echo "Database setup complete!"
