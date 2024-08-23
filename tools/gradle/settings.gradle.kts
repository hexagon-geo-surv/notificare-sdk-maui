pluginManagement {
    repositories {
        google()
        mavenCentral()
        gradlePluginPortal()
    }
}
dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {
        google()
        mavenCentral()
        maven("https://maven.notifica.re/releases")
        maven("https://maven.notifica.re/prereleases")
    }
}

rootProject.name = "Project for Downloading Maven Dependencies"
include(":dependencies")
