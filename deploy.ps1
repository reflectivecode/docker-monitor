& docker context use potato
& docker compose build --pull
& docker compose up --detach --wait --remove-orphans --pull always
Write-Output "Showing logs"
& docker compose logs --no-log-prefix --follow --since 5m
