#!/bin/bash

# Database Migration Script for Polymod
# Run this after SQL Server is up and running

echo "Starting database migrations for all modules..."

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to be ready..."
sleep 10

# Run migrations for each module
modules=("Auth" "User" "Address" "Schedule" "Service" "Recommendation" "StockPrediction" "Metrics")

for module in "${modules[@]}"; do
    echo "Running migrations for ${module}Module..."
    dotnet ef database update --context "${module}Context" --project "${module}Module"

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
dotnet run --project Polymod -- --seed

echo "Database setup complete!"
