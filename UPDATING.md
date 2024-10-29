# Updating the Notificare SDKs  

this document covers the steps to manually upgrade the Notificare SDKs which are wrapped by this MAUI SDK.

## Android Library Upgrade

The `tools\gradle` subfolder contains an Artificial Android Project which allows downloading the Notificare Android SDKs. 

The `tools\gradle\dependencies\build.gradle.kts` file contains the list of libraries and versions to download and copy. 

To update all the Android libs go into this subfolder and execute

`gradlew dependencies:download`

### Inspecting Transitive Dependencies

When upgrading Notificare SDKs often also the transitive dependencies update. To get a list of the dependencies you can execute

`tools/gradle> gradlew -q dependencies:dependencies --configuration download`

This will show you the dependency tree. The MAUI SDK wrapper only generates bindings for Notificare itself. Any transitive dependencies are consumed through NuGet packages and matching or compatible packages have to be configured.

1. Find matching Binding Packages on nuget.org for the first level of dependencies from the Notificare. 
2. Add or Update referenced libraries via `PackageReference` tags in the projects. Consider removing any packages which are anyhow referenced through transitive dependencies. Or install them via NuGet CLI or UI.
3. Recompile the bindings and the test project.
4. Check any errors in conflicting packages and manually resolve them. Sometimes newer or older versions than the expected ones might be needed due to conflicting transitive dependencies.
5. Launch the Test App and ensure things are working without errors.

## iOS Framework Upgrade

The `tools\cocoapod` subfolder contains a .net project which downloads the configured cocoapod package and extracts the xcframeworks to the right MAUI binding projects. Execute the updater via 

`dotnet run`

### Dependency Upgrade

As of today, the Notificare SDK for iOS does not need any 3rd party libraries to be referenced which makes things a bit easier than on Android. Still we need to define for each Binding project which Frameworks and Kits from the Apple SDKs are needed. 

To find this out, search for [`import` on the iOS SDK project](https://github.com/search?q=repo%3ANotificare%2Fnotificare-sdk-ios+import&type=code) and then inspect which Frameworks are imported into the different projects. Filtering by paths can help to limit the search. 

Then in the respective MAUI csproj files add the Frameworks and Kits to the `Frameworks` tag.

### Updating C# bindings

To update the C# glue code for the iOS bindings we use Objective Sharpie. The easiest way to get the updated files is:

#### On a mac

Install Objective sharpie via `brew install --cask objectivesharpie` or https://learn.microsoft.com/en-us/previous-versions/xamarin/cross-platform/macios/binding/objective-sharpie/get-started?context=xamarin%2Fios

In `tools\cocoapod` execute `dotnet run --sharpie`.

#### Other platforms

Simply create a pull request on GitHub, GitHub Actions will execute Objective Sharpie and make the new C# files available as artifact on the build.
Download the artifacts and update the files before merging the updated packages.