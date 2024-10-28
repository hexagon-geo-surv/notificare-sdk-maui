import org.jetbrains.kotlin.incremental.deleteDirectoryContents

plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

val notificareVersion = "4.0.0"

android {
    namespace = "re.notifica"
    compileSdk = 33
}

configurations {
    create("download")
}

dependencies {
    "download"("re.notifica:notificare:$notificareVersion")
    "download"("re.notifica:notificare-inbox:$notificareVersion")
    "download"("re.notifica:notificare-in-app-messaging:$notificareVersion")
    "download"("re.notifica:notificare-push:$notificareVersion")
    "download"("re.notifica:notificare-push-ui:$notificareVersion")
}

task("download") {
    delete {
        files("./gradle-packages")
    }
    copy {
        from(configurations.getByName("download"))
        into("./gradle-packages")
    }

    val libs = mapOf(
        "notificare-utilities-$notificareVersion.aar" to "Notificare.Android.Bindings.Utilities",
        "notificare-$notificareVersion.aar" to "Notificare.Android.Bindings.Core",
        "notificare-inbox-$notificareVersion.aar" to "Notificare.Android.Bindings.Inbox",
        "notificare-in-app-messaging-$notificareVersion.aar" to "Notificare.Android.Bindings.InAppMessaging",
        "notificare-push-$notificareVersion.aar" to "Notificare.Android.Bindings.Push",
        "notificare-push-ui-$notificareVersion.aar" to "Notificare.Android.Bindings.Push.UI",
    )

    val repositoryRoot = rootProject.rootDir.resolve("../../")

    for(lib in libs) {
        val targetDir = repositoryRoot.resolve(lib.value + "/libs");
        println("Deleting files from $targetDir")
        targetDir.deleteRecursively()
        val sourceFile =  projectDir.resolve("./gradle-packages/" + lib.key);
        if(!sourceFile.exists()) {
            error("Source file ${sourceFile.absolutePath} missing")
        }
        println("Copy lib ${sourceFile.absolutePath} to ${targetDir.absolutePath}")
        targetDir.mkdirs()
        sourceFile.copyTo(targetDir.resolve(sourceFile.name))
    }

    delete {
        files("./gradle-packages")
    }
}
