#!/usr/bin/env bash
# Copie les problèmes ouverts de SonarQube Cloud en tickets GitHub (ignore ceux déjà créés).
# Nécessite : gh (connecté avec `gh auth login`), jq, curl.
# Utilisation : SONAR_TOKEN=xxx ./sonar-to-github.sh [--dry-run]
set -euo pipefail

SONAR_PROJECT="Velskudd_peroche.now"
GH_REPO="Velskudd/peroche.now"
LABEL="sonarcloud"
DRY_RUN=false; [[ "${1:-}" == "--dry-run" ]] && DRY_RUN=true

AUTH=(); [[ -n "${SONAR_TOKEN:-}" ]] && AUTH=(-u "${SONAR_TOKEN}:")

# Étiquette (l'erreur est ignorée si elle existe déjà)
$DRY_RUN || gh label create "$LABEL" --repo "$GH_REPO" --color "F3702A" \
  --description "Imported from SonarQube Cloud" 2>/dev/null || true

# Clés Sonar déjà présentes dans les tickets GitHub (pour éviter les doublons)
EXISTING=$(gh issue list --repo "$GH_REPO" --label "$LABEL" --state all --limit 1000 \
  --json body --jq '.[].body' | grep -oE 'sonar-key: [A-Za-z0-9_-]+' || true)

page=1
while :; do
  json=$(curl -sf "${AUTH[@]}" \
    "https://sonarcloud.io/api/issues/search?componentKeys=${SONAR_PROJECT}&resolved=false&ps=100&p=${page}")
  count=$(jq '.issues | length' <<<"$json")
  [[ "$count" -eq 0 ]] && break

  jq -c '.issues[]' <<<"$json" | while read -r issue; do
    key=$(jq -r '.key' <<<"$issue")
    grep -q "sonar-key: $key" <<<"$EXISTING" && { echo "skip  $key (already exists)"; continue; }

    msg=$(jq -r '.message' <<<"$issue")
    rule=$(jq -r '.rule' <<<"$issue")
    type=$(jq -r '.type' <<<"$issue")
    sev=$(jq -r 'if (.impacts|length) > 0 then [.impacts[].severity] | join(", ") else .severity end' <<<"$issue")
    file=$(jq -r '.component | sub("^[^:]*:"; "")' <<<"$issue")
    line=$(jq -r '.line // "?"' <<<"$issue")

    title="[Sonar] ${msg:0:200}"
    body=$(cat <<EOF
**Type:** ${type}
**Severity:** ${sev}
**Rule:** \`${rule}\`
**Location:** \`${file}:${line}\`

${msg}

[Open in SonarQube Cloud](https://sonarcloud.io/project/issues?id=${SONAR_PROJECT}&open=${key})

<!-- sonar-key: ${key} -->
EOF
)
    if $DRY_RUN; then
      echo "would create: $title"
    else
      gh issue create --repo "$GH_REPO" --title "$title" --body "$body" --label "$LABEL"
    fi
  done
  page=$((page + 1))
done
