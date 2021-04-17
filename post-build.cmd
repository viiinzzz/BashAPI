rem Dockerize...
@echo off
if not exist "$(ProjectDir)\featured" exit 0
cd "$(ProjectDir)\featured"

echo Reading featured image name...
for /f "tokens=*" %%v in ('powershell Get-Content featured.json ^^^| ConvertFrom-Json ^^^| Select -expandproperty dockerImageName') do set dockerImageName=%%v
echo image name: %dockerImageName%
if "%dockerImageName%" == "" exit -1

echo Reading featured ports...
for /f "tokens=*" %%v in ('powershell Get-Content featured.json ^^^| ConvertFrom-Json ^^^| Select -expandproperty dockerPublishHttpPort') do set dockerPublishHttpPort=%%v
for /f "tokens=*" %%v in ('powershell Get-Content featured.json ^^^| ConvertFrom-Json ^^^| Select -expandproperty dockerPublishSslPort') do set dockerPublishSslPort=%%v
echo http: %dockerPublishHttpPort% https: %dockerPublishSslPort%
if "%dockerPublishHttpPort%" == "" exit -1
if "%dockerPublishSslPort%" == "" exit -1

echo Reading build secrets...
cd "$(ProjectDir)"
for /f "tokens=*" %%v in ('dotnet user-secrets list ^| find "dockerRootPassword"') do set %%v
for /f "tokens=*" %%v in ('dotnet user-secrets list ^| find "dockerSslPassword"') do set %%v
set buildsecrets=--build-arg rootPassword=%dockerRootPassword :~1% --build-arg sslPassword=%dockerSslPassword :~1%
rem echo build secrets: %buildsecrets%
if "%dockerRootPassword :~1%" == "" exit -1
if "%dockerSslPassword :~1%" == "" exit -1

cd "$(ProjectDir)bin\Docker"
echo Copying build...
xcopy /Q /Y /I /E "$(TargetDir)*.*" .
echo Copying wwwroot...
xcopy /Q /Y /I /E "$(ProjectDir)wwwroot" .\wwwroot
echo Copying featured...
xcopy /Q /Y /I /E "$(ProjectDir)\featured\*.*" .

echo Building image...
set DOCKER_BUILDKIT=true
if "$(ConfigurationName)" == "Debug" (
  set buildoptions=--tag=%dockerImageName%:debug --build-arg imageName=%dockerImageName% --build-arg CONFIG=debug --build-arg NETENV=Development
  set CONFIG=debug
) else (
  set buildoptions=--tag=%dockerImageName%:release --build-arg imageName=%dockerImageName% --build-arg CONFIG=release --build-arg NETENV=Production
  set CONFIG=release
)

echo Removing old running builds of %dockerImageName%:%CONFIG%...
for /f "tokens=*" %%v in ('docker ps -a -q --filter ancestor^=%dockerImageName%:%CONFIG% --format^="{{.ID}}"') do (
  echo Stopping %%v...
  docker stop %%v
)
rem docker ps -a --filter ancestor=%dockerImageName%:%CONFIG%
echo Removing running builds  on ports %dockerPublishHttpPort%,%dockerPublishSslPort%...
for /f "tokens=*" %%v in ('docker container ls -a --filter publish^=%dockerPublishHttpPort% --format "{{.ID}}"') do (
  echo Stopping %%v...
  docker stop %%v
)
for /f "tokens=*" %%v in ('docker container ls -a --filter publish^=%dockerPublishSslPort% --format "{{.ID}}"') do (
  echo Stopping %%v...
  docker stop %%v
)
rem docker container ls -a --filter publish=%PORTS% --format "{{.ID}}"
echo Pruning containers...
docker system prune --force

echo Building new version of %dockerImageName%:%CONFIG%...
rem echo docker build . %buildoptions% %buildsecrets%
docker build . %buildoptions% %buildsecrets%
docker images %dockerImageName%:%CONFIG% --format="{{.ID}}"

rem launch container
echo Running new build of %dockerImageName%:%CONFIG%...
docker run -d -p%dockerPublishHttpPort%:8080 -p%dockerPublishSslPort%:8081 -e ASPNETCORE_HTTPS_PORT=%dockerPublishSslPort% --rm %dockerImageName%:%CONFIG%