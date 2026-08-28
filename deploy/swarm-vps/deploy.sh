#!/usr/bin/env bash
# Deploy OpenMU no Swarm (carrega .env via source — compatível com Docker antigo)
set -euo pipefail
cd "$(dirname "$0")"
if [[ ! -f .env ]]; then
  echo "Crie .env a partir de env.example: cp env.example .env && nano .env"
  exit 1
fi
set -a
# shellcheck disable=SC1091
source .env
set +a
docker stack deploy -c docker-compose.stack.yml openmu
echo "OK. Verifique: docker service ls && docker service logs openmu_openmu --tail 40"
