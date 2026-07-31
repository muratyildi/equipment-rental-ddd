#!/usr/bin/env bash

set -euo pipefail

failures=0

candidate_files() {
  git ls-files --cached --others --exclude-standard
}

for required_file in \
  README.md \
  LICENSE \
  SECURITY.md \
  CONTRIBUTING.md \
  CHANGELOG.md; do
  if [[ ! -f "${required_file}" ]]; then
    echo "Required repository file is missing: ${required_file}"
    failures=$((failures + 1))
  fi
done

for release_script in scripts/*.sh; do
  if [[ ! -x "${release_script}" ]]; then
    echo "Release script is not executable: ${release_script}"
    failures=$((failures + 1))
  fi
done

while IFS= read -r candidate_file; do
  case "/${candidate_file}/" in
    */bin/*|*/obj/*|*/TestResults/*|*/.DS_Store/*|*/.env/*)
      echo "Forbidden generated or local file would be published: ${candidate_file}"
      failures=$((failures + 1))
      ;;
  esac
done < <(candidate_files)

secret_pattern='BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY|ghp_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|AKIA[0-9A-Z]{16}|sk-[A-Za-z0-9]{20,}'
while IFS= read -r candidate_file; do
  if [[ "${candidate_file}" == "scripts/verify-repository.sh" ]]; then
    continue
  fi

  if grep -IEn "${secret_pattern}" "${candidate_file}"; then
    echo "Potential secret in release candidate: ${candidate_file}"
    failures=$((failures + 1))
  fi
done < <(candidate_files)

while IFS= read -r markdown_file; do
  while IFS= read -r markdown_link; do
    target="${markdown_link#](}"
    target="${target%)}"
    target="${target#<}"
    target="${target%>}"
    target="${target%%#*}"

    case "${target}" in
      ""|http://*|https://*|mailto:*)
        continue
        ;;
    esac

    resolved_path="$(dirname "${markdown_file}")/${target}"
    if [[ ! -e "${resolved_path}" ]]; then
      echo "Broken Markdown link: ${markdown_file} -> ${target}"
      failures=$((failures + 1))
    fi
  done < <(grep -Eo '\]\([^)]+\)' "${markdown_file}" || true)
done < <(find . \
  -path './.git' -prune -o \
  -path '*/bin' -prune -o \
  -path '*/obj' -prune -o \
  -name '*.md' -type f -print)

if ((failures > 0)); then
  echo "Repository verification failed with ${failures} issue(s)."
  exit 1
fi

echo "Repository hygiene and local Markdown links are valid."
