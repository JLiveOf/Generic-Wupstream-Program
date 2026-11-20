/* SPDX-License-Identifier: GPL-3.0-or-later */
#define COBJMACROS
#define INITGUID
#include <windows.h>
#include <mmdeviceapi.h>
#include <audioclient.h>
#include <functiondiscoverykeys_devpkey.h>
#include <propsys.h>
#include <stdint.h>
#include <stdlib.h>

#define EXPORT __declspec(dllexport)

typedef struct {
    IAudioClient *client;
    IAudioCaptureClient *capture;
    IAudioRenderClient *render;
    UINT32 capacity;
    WCHAR *device_id;
    BOOL com;
} audio_stream;

static const WAVEFORMATEX pcm = { WAVE_FORMAT_PCM, 2, 48000, 192000, 4, 16, 0 };

EXPORT void audio_close(audio_stream *s) {
    if (!s) return;
    if (s->client) IAudioClient_Stop(s->client);
    if (s->capture) IAudioCaptureClient_Release(s->capture);
    if (s->render) IAudioRenderClient_Release(s->render);
    if (s->client) IAudioClient_Release(s->client);
    CoTaskMemFree(s->device_id);
    if (s->com) CoUninitialize();
    free(s);
}

static HRESULT cable_device(IMMDeviceEnumerator *enumerator, IMMDevice **device) {
    IMMDeviceCollection *collection;
    HRESULT hr = IMMDeviceEnumerator_EnumAudioEndpoints(enumerator, eRender, DEVICE_STATE_ACTIVE, &collection);
    if (FAILED(hr)) return hr;
    UINT count = 0;
    IMMDeviceCollection_GetCount(collection, &count);
    hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
    for (UINT i = 0; i < count && FAILED(hr); i++) {
        IMMDevice *candidate;
        IPropertyStore *properties;
        if (FAILED(IMMDeviceCollection_Item(collection, i, &candidate))) continue;
        if (SUCCEEDED(IMMDevice_OpenPropertyStore(candidate, STGM_READ, &properties))) {
            PROPVARIANT name;
            PropVariantInit(&name);
            if (SUCCEEDED(IPropertyStore_GetValue(properties, &PKEY_Device_FriendlyName, &name)) &&
                name.vt == VT_LPWSTR && wcsstr(name.pwszVal, L"CABLE Input") &&
                wcsstr(name.pwszVal, L"VB-Audio")) {
                *device = candidate;
                IMMDevice_AddRef(candidate);
                hr = S_OK;
            }
            PropVariantClear(&name);
            IPropertyStore_Release(properties);
        }
        IMMDevice_Release(candidate);
    }
    IMMDeviceCollection_Release(collection);
    return hr;
}

EXPORT audio_stream *audio_open(int cable, HRESULT *error) {
    *error = E_OUTOFMEMORY;
    audio_stream *s = calloc(1, sizeof *s);
    if (!s) return NULL;
    IMMDeviceEnumerator *enumerator = NULL;
    IMMDevice *device = NULL;
    HRESULT hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);
    s->com = SUCCEEDED(hr);
    if (FAILED(hr) && hr != RPC_E_CHANGED_MODE) goto done;
    hr = CoCreateInstance(&CLSID_MMDeviceEnumerator, NULL, CLSCTX_INPROC_SERVER,
                          &IID_IMMDeviceEnumerator, (void **)&enumerator);
    if (FAILED(hr)) goto done;
    hr = cable ? cable_device(enumerator, &device)
               : IMMDeviceEnumerator_GetDefaultAudioEndpoint(enumerator, eRender, eConsole, &device);
    if (FAILED(hr)) goto done;
    hr = IMMDevice_GetId(device, &s->device_id);
    if (FAILED(hr)) goto done;
    hr = IMMDevice_Activate(device, &IID_IAudioClient, CLSCTX_ALL, NULL, (void **)&s->client);
    if (FAILED(hr)) goto done;
    DWORD flags = AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY |
                  (cable ? 0 : AUDCLNT_STREAMFLAGS_LOOPBACK);
    hr = IAudioClient_Initialize(s->client, AUDCLNT_SHAREMODE_SHARED, flags, 400000, 0, &pcm, NULL);
    if (FAILED(hr)) goto done;
    hr = IAudioClient_GetBufferSize(s->client, &s->capacity);
    if (FAILED(hr)) goto done;
    hr = cable ? IAudioClient_GetService(s->client, &IID_IAudioRenderClient, (void **)&s->render)
               : IAudioClient_GetService(s->client, &IID_IAudioCaptureClient, (void **)&s->capture);
    if (SUCCEEDED(hr)) hr = IAudioClient_Start(s->client);
done:
    if (device) IMMDevice_Release(device);
    if (enumerator) IMMDeviceEnumerator_Release(enumerator);
    *error = hr;
    if (FAILED(hr)) { audio_close(s); return NULL; }
    return s;
}

EXPORT int audio_same_device(audio_stream *a, audio_stream *b) {
    return a && b && wcscmp(a->device_id, b->device_id) == 0;
}

/* Read one WASAPI packet. If a session stalls briefly, discard oversized packets
 * to keep the real-time path current rather than accumulate delay. */
EXPORT int audio_read(audio_stream *s, int16_t *out, UINT32 capacity) {
    UINT32 frames;
    if (FAILED(IAudioCaptureClient_GetNextPacketSize(s->capture, &frames))) return -1;
    if (frames == 0) return 0;
    BYTE *bytes;
    DWORD flags;
    if (FAILED(IAudioCaptureClient_GetBuffer(s->capture, &bytes, &frames, &flags, NULL, NULL))) return -1;
    UINT32 copied = frames <= capacity ? frames : 0;
    if (copied) {
        if (flags & AUDCLNT_BUFFERFLAGS_SILENT) memset(out, 0, copied * 4);
        else memcpy(out, bytes, copied * 4);
    }
    if (FAILED(IAudioCaptureClient_ReleaseBuffer(s->capture, frames))) return -1;
    return (int)copied;
}

EXPORT int audio_write_mono(audio_stream *s, const int16_t *samples, UINT32 frames) {
    UINT32 padding;
    if (FAILED(IAudioClient_GetCurrentPadding(s->client, &padding))) return -1;
    UINT32 available = s->capacity - padding;
    if (frames > available) { samples += frames - available; frames = available; }
    if (!frames) return 0;
    BYTE *bytes;
    if (FAILED(IAudioRenderClient_GetBuffer(s->render, frames, &bytes))) return -1;
    int16_t *out = (int16_t *)bytes;
    for (UINT32 i = 0; i < frames; i++) out[i * 2] = out[i * 2 + 1] = samples[i];
    return SUCCEEDED(IAudioRenderClient_ReleaseBuffer(s->render, frames, 0)) ? (int)frames : -1;
}
