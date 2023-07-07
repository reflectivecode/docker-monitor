#!/usr/bin/env sh
set -o errexit
set -o pipefail
set -o nounset

if [ -f "${HEALTHCHECK_PATH}" ]; then
    read -r line < "${HEALTHCHECK_PATH}"
    now=$(date +%s)
    now=$((now+10))
    if [ "${line}" -ge "${now}" ]; then
        exit 0
    fi
fi
exit 1
