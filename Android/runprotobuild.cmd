mkdir ..\OpenMetaverse\jni
xcopy /S ..\openjpeg-dotnet\dotnet ..\OpenMetaverse\jni\dotnet\
xcopy /S ..\openjpeg-dotnet\libopenjpeg ..\OpenMetaverse\jni\libopenjpeg\
xcopy /S ..\openjpeg-dotnet\*.mk ..\OpenMetaverse\jni\

cd ..\OpenMetaverse\jni
call %ANDROID_NDK_PATH%\ndk-build.cmd

cd ..\..\Android
mkdir bin
xcopy /S ..\OpenMetaverse\libs bin\libs\

Protobuild.exe --generate Android

rem set MSBuild="C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
set MSBuild="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
%MSBuild% OpenMetaverse.Android.sln /p:Configuration=Release

xcopy /S ..\OpenMetaverse\bin bin\
xcopy /S ..\OpenMetaverse.Rendering.Meshmerizer\bin bin\