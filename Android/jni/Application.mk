APP_PROJECT_PATH := $(call my-dir)
APP_STL := stlport_static

APP_BUILD_SCRIPT := Android.mk
STLPORT_FORCE_REBUILD := true
APP_ABI := armeabi armeabi-v7a arm64-v8a x86 x86_64

APP_OPTIM:=release