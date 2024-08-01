param (
    [switch]$Run
)

$ErrorActionPreference = "Stop"

# BUILD 
javac -sourcepath . -d obj *.java
jar --create --file EchoApp.jar --main-class com.androidsdk.EchoApp -C obj com/androidsdk/EchoApp.class
Move-Item EchoApp.jar ../ -Force

# RUN
if ($Run) {
    java -jar ../EchoApp.jar
    Write-Host "Exit code: $LastExitCode"
}
