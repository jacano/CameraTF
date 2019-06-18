LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)

LOCAL_MODULE := yuv
LOCAL_SRC_FILES := yuv2rgb.cc
LOCAL_LDLIBS := -llog
include $(BUILD_SHARED_LIBRARY)
