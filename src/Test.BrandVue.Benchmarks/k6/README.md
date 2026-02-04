# K6 Benchmark testing

https://github.com/grafana/k6

https://k6.io/docs/

These tests run by taking an array of requests to loop through in parallel and seeing how many they can achieve over a certain amount of time. This gives us a good overview into live-like behaviour - multiple separate users making different requests. By running over a longer time period we avoid anomalies in behaviour due to sql caching that we may get if we only benchmarked against one request.

If you're using local requests you'll need to make sure to have the necessary program running locally to allow the requests to succeed. The dashboard used is set in the script.

You'll need to install k6 - more details in the documentation link above
`winget install k6 --source winget`

## The script

Run using the following:

`./Run-K6.ps1 -Product 'retail'`

The script contains the time period setting - the default is 5 mins 30 secs. The reason it's set to so long is to allow the database cache to be saturated so we can test performance against a non-empty db cache. Change this in your local script if you want a different time. 

By default the script runs 10 threads in parallel, mimicking 10 users making different requests. You may need to reduce this (or reduce the number of requests) if you run into out of memory issues.

## The requests

They are defined in `GetRequests.txt`. These are a collection of requests (local or not) that k6 will run. K6 will loop through the requests for the time period and see how many it's managed to achieve.

By default the requests are varied against BV retail. Replace or remove requests locally, as required.

https://s1.stackify.com/Logs/Search?id=52 can be used as a starting point for requests providing the local db has enough relevant information

## Understanding the output

The output of the tests will look something like this:

```
  scenarios: (100.00%) 1 scenario, 10 max VUs, 6m0s max duration (incl. graceful stop):
           * default: 10 looping VUs for 5m30s (gracefulStop: 30s)

WARN[0287] Request Failed                                error="unexpected EOF"

     data_received..................: 407 MB 1.2 MB/s
     data_sent......................: 2.5 MB 7.2 kB/s
     http_req_blocked...............: avg=3.16ms   min=0s      med=1.18ms   max=98.11ms p(90)=7.77ms   p(95)=15.64ms
     http_req_connecting............: avg=2.96ms   min=0s      med=1.12ms   max=98.11ms p(90)=6.53ms   p(95)=15.62ms
     http_req_duration..............: avg=2.46s    min=44.92ms med=890.69ms max=33.49s  p(90)=5.6s     p(95)=6.59s
       { expected_response:true }...: avg=2.46s    min=44.92ms med=886.52ms max=33.49s  p(90)=5.61s    p(95)=6.59s
     http_req_failed................: 0.10%  ✓ 1        ✗ 959
     http_req_receiving.............: avg=29.24ms  min=0s      med=7.92ms   max=8.15s   p(90)=37.43ms  p(95)=69.63ms
     http_req_sending...............: avg=106.06µs min=0s      med=0s       max=5.02ms  p(90)=515.41µs p(95)=557.02µs
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s       max=0s      p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=2.43s    min=41.06ms med=890.69ms max=33.38s  p(90)=5.56s    p(95)=6.53s
     http_reqs......................: 960    2.822637/s
     iteration_duration.............: avg=17.42s   min=10.19s  med=15.32s   max=1m0s    p(90)=18.02s   p(95)=37.23s
     iterations.....................: 192    0.564527/s
     vus............................: 1      min=1      max=10
     vus_max........................: 10     min=10     max=10


running (5m40.1s), 00/10 VUs, 192 complete and 0 interrupted iterations
default ✓ [======================================] 10 VUs  5m30s
```

This shows the number of requests that were successfully made over the time period, plus other details.

### Areas of interest:

* You can see how many requests were completed at the bottom - in this case, 192 requests were completed over 5m30s with 10 users ("VUs")
* Make sure the requests have completed successfully. You can see any failed requests in http_req_failed and the reason above it - in this case only one request failed because of "unexpected EOF", likely to do with a memory issue. Usually you'd expect all runs to complete without any errors, but if it's just one anomaly it's usually ok
* A good overview of the run is to look at the average iteration_duration - this shows you roughly how long it took to complete a request and helps avoid anomalies from the db caching
* It's a good idea to check that the min and max of iteration_duration are significantly different - this means the db cache was fully saturated by the time it hit some of the min requests, giving a more realistic result
