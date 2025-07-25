#!/bin/bash
total_seconds=$((50 * 60))
interval=30
elapsed=0
while [ $elapsed -lt $total_seconds ]; do
  echo "Waiting... $(( (total_seconds - elapsed) / 60 )) minutes $(( (total_seconds - elapsed) % 60 )) seconds remaining."
  sleep $interval
  elapsed=$((elapsed + interval))
done