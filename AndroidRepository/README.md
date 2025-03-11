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
In the cloned `platform/tools/base` repo where the XSD files are found, the `sdklib/BUILD` file seems to list `generated_libs` as an array.  This looks to be a comprehensive set of the actual xml urls built and all of the versions of them.

Looking further at the code, they use the the actual xsd schemas compiled into the build to know the upper bound version of each type of schema, so these urls are essentially hard coded into the client library, there is no overall manifest/file you can query from the server to understand which versions of repository/sys-img/addon-sites/etc schema files exist that I can find.

So in practice, if there's a new schema version built into a newer version of the android sdk manager libraries, we would need to update the generated code here to also include the new xsd in order to be able to parse out those new urls.