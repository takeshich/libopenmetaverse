mkdir ..\OpenMetaverse\jni
xcopy /S ..\openjpeg-dotnet\dotnet ..\OpenMetaverse\jni\dotnet\
xcopy /S ..\openjpeg-dotnet\libopenjpeg ..\OpenMetaverse\jni\libopenjpeg\
xcopy jni\*.* ..\OpenMetaverse\jni\

cd ..\OpenMetaverse\jni
call %ANDROID_NDK_PATH%\ndk-build.cmd

cd ..\..\bin
mkdir Android
xcopy /S ..\OpenMetaverse\libs Android\libs\

cd ..\Android

Protobuild.exe

set MSBuild="C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
echo %MSBuild% OpenMetaverse.Android.sln /p:Configuration=Release /property:OutputPath=..\bin\Android >> compile.cmd

compile.cmd