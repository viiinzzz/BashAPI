@echo off
docker run -it -p7580:8080 -p7581:8081 -e ASPNETCORE_HTTPS_PORT=7581 --rm lyapi:debug
