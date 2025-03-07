# Android Repository XML

## Generating C# from the XSD files

1. Clone https://android.googlesource.com/platform/tools/base
2. Default branch is likely fine
3. Collect XSD files from the following locations:
    - `repository/src/main/resources/xsd/*.xsd`
    - `repository/src/main/resources/xsd/sources/*.xsd`
    - `sdklib/src/main/resources/xsd/*.xsd`
    - `sdklib/src/main/resources/xsd/sources/*.xsd`
    - `sdklib/src/main/resources/xsd/legacy/*.xsd`
4. Using the `xscgen` dotnet tool (`dotnet tool install --global dotnet-xscgen`), run the command `xscgen --output="[path]" *.xsd --verbose` on all the xsd files you collected

## Updating the list of URLs
In the cloned `platform/tools/base` repo where the XSD files are found, the `sdklib/BUILD` file seems to list `generated_libs` as an array.  This looks to be roughly the actual 