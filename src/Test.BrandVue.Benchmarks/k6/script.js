import http from 'k6/http';
import { SharedArray } from 'k6/data';
import { randomSeed } from 'k6';
import exec from 'k6/execution';
import { textSummary } from './lib/k6-summary.js'

const minLogRequestSeconds = 3;
const maxLogRequestSeconds = 600;
const maxUniqueRequestCount = 40;

export const options = {
  vus: 40,
  iterations: 40,  // You might want to specify this in order to focus on first load performance, or to time how long a set number of iterations takes
  duration: '30m'
};

const requests = new SharedArray('RequestToUse', function () {
  // All heavy work (opening and processing big files for example) should be done inside here.
  // This way it will happen only once and the result will be shared between all VUs, saving time and memory.
  const productShortCode = __ENV.PRODUCT != null ? __ENV.PRODUCT : "retail";
  const stackifyLogLinePrefix = "10.42.30.10 GET /" + productShortCode;
  const rawRows = open('./GetRequests.txt').split("\n").filter(x => x.includes(stackifyLogLinePrefix) && x.indexOf(" 200 ") > -1);
  const f = rawRows.map(x => {
    const parts = x.substring(x.indexOf(stackifyLogLinePrefix) + stackifyLogLinePrefix.length).split(" ");
    const relativeRequest = parts[0] + "?" + parts[1];
    const milliseconds = parseInt(parts.pop());
    const requestObj = {Url: relativeRequest, milliseconds: milliseconds};

    return requestObj;
  }).sort((a, b) => b.milliseconds - a.milliseconds) // Slowest requests first
  .filter(x => minLogRequestSeconds * 1000 < x.milliseconds && x.milliseconds < maxLogRequestSeconds * 1000)
  .slice(0, maxUniqueRequestCount);

  console.log(`Testing ${f.length} requests that took between ${f[f.length - 1].milliseconds} and ${f[0].milliseconds} milliseconds in log`);

  if (f.length === 0) {
    throw new Error(`No requests found, check product short code of GetRequests.txt matches ${productShortCode} variable in script.js`);
  }
  return f.map(x => x.Url); // f must be an array
});

const shuffle = (array) => { 
  randomSeed(exec.vu.idInInstance); // Seed the generator to get consistent results but different for each VU
  for (let i = array.length - 1; i > 0; i--) { 
    const j = Math.floor(Math.random() * (i + 1)); 
    [array[i], array[j]] = [array[j], array[i]]; 
  } 
  return array; 
};

// TODO: Before this runs, get vue started with the launch url as a page you don't care about the first load of (e.g. 1 month of an experience metric from 3 years back)
export default function() {
  const shuffledRequests = shuffle(requests.map(x => x));
  shuffledRequests.forEach(r => {
    const params = {
      timeout: '10m'
    };
    http.get('http://localhost:8082' + r, params);
  });
}

export function handleSummary(data) {
  const fileNameWithoutExtension = __ENV.RESULT_FILENAME != null ? __ENV.RESULT_FILENAME : 'last';
  const fileName = `./results/${fileNameWithoutExtension}.txt`;
  const obj = {};
  obj[fileName] = textSummary(data, { indent: ' ', enableColors: false, summaryTimeUnit: 'ms' });
  obj['stdout'] = textSummary(data, { indent: ' ', enableColors: true });
  return obj;
}