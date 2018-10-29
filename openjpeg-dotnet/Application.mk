# Android makefile for OpenJPEG
APP_PROJECT_PATH := $(call my-dir)
APP_STL := c++_static
APP_CPPFLAGS += -fexceptions

APP_BUILD_SCRIPT := Android.mk
APP_ABI := armeabi-v7a arm64-v8a x86 x86_64

APP_OPTIM:=release