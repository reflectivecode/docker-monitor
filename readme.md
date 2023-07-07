[![Pulls from DockerHub](https://img.shields.io/docker/pulls/reflectivecode/docker-monitor.svg)](https://hub.docker.com/r/reflectivecode/docker-monitor)

Monitor your docker containers and get Slack alerts when they change state (e.g. fail)

*Important:* All monitored containers must have the following label
`com.reflectivecode.dockermonitor.enable=true`

# Environment variables

## SLACK_WEBHOOK_URL

_Required_

Example: `https://hooks.slack.com/services/TXXXXXXXXXX/BXXXXXXXXXX/XXXXXXXXXXXXXXXXXXXXXXXX`

## SCHEDULE

_Optional_

Six-part cron expression that determines when to check containers.

Default value: `0 * * * * *`

```
* * * * * *
- - - - - -
| | | | | |
| | | | | +--- day of week (0 - 6) (Sunday=0)
| | | | +----- month (1 - 12)
| | | +------- day of month (1 - 31)
| | +--------- hour (0 - 23)
| +----------- min (0 - 59)
+------------- sec (0 - 59)
```

## HOST

_Optional_

Name of the docker host used in Slack messages.

Default value: container hostname

## HEARTBEAT_URL

_Optional_

Make a GET request to this url each time after checking containers. Can include the milliseconds elapsed making the check.

Example: `https://push.statuscake.com/?PK=XXXXXXXXXXXXXXX&TestID=XXXXXXX&time={milliseconds}`

Default value: _none_

## LOG_LEVEL

_Optional_

Valid values: `Error`, `Warn`, `Info`, `Debug`

Default value: `Info`

## DOCKER_SOCKET

_Optional_

Default value: `/var/run/docker.sock`

## TIMEOUT

_Optional_

Number of seconds to wait for an HTTP response.

Default value: `10`

## HEALTHCHECK_PATH

_Optional_

Where to write the heathcheck file for monitoring container health.

Default value: `/tmp/health`
