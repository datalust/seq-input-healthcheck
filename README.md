# Seq Health Check [![Build status](https://ci.appveyor.com/api/projects/status/hjmlyd6j94uigycg?svg=true)](https://ci.appveyor.com/project/datalust/seq-input-healthcheck) [![NuGet package](https://img.shields.io/nuget/vpre/seq.input.healthcheck.svg)](https://nuget.org/packages/Seq.Input.HealthCheck)

Periodically GET an HTTP resource and write response metrics to Seq. These can then be used as a basis for [alerting](https://docs.getseq.net/docs/alerts) and diagnostics.

![Events in Seq](https://raw.githubusercontent.com/datalust/seq-input-healthcheck/dev/asset/screenshot.png)

### Getting started

1. The app requires Seq 5.1 or newer
2. Navigate to _Settings_ > _Apps_ and select _Install from NuGet_
3. Install the app with package id _Seq.Input.HealthCheck_
4. Back on the _Apps_ screen, choose _Add Instance_
5. Enter a title for the health check; events raised by the health check will be tagged with this
6. Enter a URL to probe
   - the URL must respond to `GET` requests
   - if the URL is an HTTPS URL, the Seq server must trust the SSL certificate used by the server
   - the response will be fully downloaded on every check, so ideally the resource won't be more than a few kB
7. Enter a probing interval in seconds; each event is stored internally in Seq, so be aware that shorter intervals will consume more space

