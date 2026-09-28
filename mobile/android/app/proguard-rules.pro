# ML Kit text recognition ships optional per-script recognizer classes
# (Chinese/Devanagari/Japanese/Korean) as separate opt-in dependencies that
# this app does not include — R8 sees the reference in the plugin's Java
# glue code and treats it as missing. This app only uses the Latin script
# recognizer, so these classes are safe to not exist at runtime.
-dontwarn com.google.mlkit.vision.text.chinese.**
-dontwarn com.google.mlkit.vision.text.devanagari.**
-dontwarn com.google.mlkit.vision.text.japanese.**
-dontwarn com.google.mlkit.vision.text.korean.**
