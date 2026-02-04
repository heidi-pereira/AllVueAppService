echo Running Live alerts
curl https://savanta.all-vue.com/openends/api/processalerts -X POST -H 'x-api-key: A95F81F3-1990-41D0-A501-E02A91782CD2'

echo Running Test alerts
curl https://savanta.test.all-vue.com/openends/api/processalerts -X POST -H 'x-api-key: A95F81F3-1990-41D0-A501-E02A91782CD2'
