# Retrofit / OkHttp
-dontwarn okhttp3.**
-dontwarn okio.**
-keepattributes Signature, InnerClasses, EnclosingMethod, RuntimeVisibleAnnotations

# kotlinx.serialization keeps its generated serializers
-keepclassmembers class ** {
    *** Companion;
}
-keepclasseswithmembers class ** {
    kotlinx.serialization.KSerializer serializer(...);
}

# ML Kit's pieces are discovered by name, not by reference: the registrar classes are listed
# as <meta-data> on MlKitComponentDiscoveryService in the merged manifest and instantiated
# reflectively at startup. firebase-components ships `-keep class * implements
# ComponentRegistrar`, which was written for ProGuard, where keeping a class implies keeping
# its default constructor. R8's full mode -- the default -- keeps only the type, sees no
# caller for the constructor and strips it. Discovery then fails silently, no barcode
# component is ever registered, and BarcodeScanning.getClient() dereferences the component
# that isn't there: an NPE the moment the scanner opens, in minified builds only.
-keep class * implements com.google.firebase.components.ComponentRegistrar {
    public <init>();
}
