/* SPDX-License-Identifier: GPL-3.0-or-later */
#define COBJMACROS
#include <windows.h>
#include <d3d11.h>
#include <dxgi1_2.h>
#include <stdint.h>
#include <stdlib.h>

#define EXPORT __declspec(dllexport)

struct desktop_capture {
    ID3D11DeviceContext *context;
    IDXGIOutputDuplication *output;
    ID3D11Texture2D *staging;
    unsigned width, height;
    BOOL pointer_visible;
};

EXPORT void host_desktop_close(struct desktop_capture *capture) {
    if (!capture) return;
    if (capture->staging) ID3D11Texture2D_Release(capture->staging);
    if (capture->output) IDXGIOutputDuplication_Release(capture->output);
    if (capture->context) ID3D11DeviceContext_Release(capture->context);
    free(capture);
}

static IDXGIOutput1 *find_output(IDXGIFactory1 *factory, HMONITOR monitor, IDXGIAdapter1 **owner) {
    for (UINT i = 0; SUCCEEDED(IDXGIFactory1_EnumAdapters1(factory, i, owner)); i++) {
        IDXGIOutput *output;
        for (UINT j = 0; SUCCEEDED(IDXGIAdapter1_EnumOutputs(*owner, j, &output)); j++) {
            DXGI_OUTPUT_DESC desc;
            IDXGIOutput1 *result = NULL;
            if (SUCCEEDED(IDXGIOutput_GetDesc(output, &desc)) && desc.Monitor == monitor &&
                desc.Rotation == DXGI_MODE_ROTATION_IDENTITY)
                IDXGIOutput_QueryInterface(output, &IID_IDXGIOutput1, (void **)&result);
            IDXGIOutput_Release(output);
            if (result) return result;
        }
        IDXGIAdapter1_Release(*owner);
    }
    return NULL;
}

EXPORT struct desktop_capture *host_desktop_open(HMONITOR monitor) {
    IDXGIFactory1 *factory;
    if (FAILED(CreateDXGIFactory1(&IID_IDXGIFactory1, (void **)&factory))) return NULL;
    IDXGIAdapter1 *adapter;
    IDXGIOutput1 *output = find_output(factory, monitor, &adapter);
    IDXGIFactory1_Release(factory);
    if (!output) return NULL;
    struct desktop_capture *capture = calloc(1, sizeof *capture);
    ID3D11Device *device = NULL;
    HRESULT hr = capture ? D3D11CreateDevice((IDXGIAdapter *)adapter, D3D_DRIVER_TYPE_UNKNOWN,
        NULL, D3D11_CREATE_DEVICE_BGRA_SUPPORT, NULL, 0, D3D11_SDK_VERSION, &device, NULL,
        &capture->context) : E_OUTOFMEMORY;
    if (SUCCEEDED(hr)) hr = IDXGIOutput1_DuplicateOutput(output, (IUnknown *)device, &capture->output);
    if (SUCCEEDED(hr)) {
        DXGI_OUTDUPL_DESC mode;
        IDXGIOutputDuplication_GetDesc(capture->output, &mode);
        capture->width = mode.ModeDesc.Width;
        capture->height = mode.ModeDesc.Height;
        D3D11_TEXTURE2D_DESC texture = {
            .Width = capture->width, .Height = capture->height, .MipLevels = 1, .ArraySize = 1,
            .Format = mode.ModeDesc.Format, .SampleDesc = {1, 0}, .Usage = D3D11_USAGE_STAGING,
            .CPUAccessFlags = D3D11_CPU_ACCESS_READ
        };
        hr = ID3D11Device_CreateTexture2D(device, &texture, NULL, &capture->staging);
    }
    if (device) ID3D11Device_Release(device);
    IDXGIOutput1_Release(output);
    IDXGIAdapter1_Release(adapter);
    if (FAILED(hr)) {
        host_desktop_close(capture);
        return NULL;
    }
    return capture;
}

/* The destination is the active rectangle of the BGRA capture bitmap. */
EXPORT int host_desktop_read(struct desktop_capture *capture, uint8_t *pixels, int stride,
    unsigned width, unsigned height) {
    DXGI_OUTDUPL_FRAME_INFO info;
    IDXGIResource *frame;
    HRESULT hr = IDXGIOutputDuplication_AcquireNextFrame(capture->output, 8, &info, &frame);
    if (hr == DXGI_ERROR_WAIT_TIMEOUT) return 0;
    if (FAILED(hr)) return -1;
    if (info.LastMouseUpdateTime.QuadPart) capture->pointer_visible = info.PointerPosition.Visible;
    ID3D11Texture2D *source;
    hr = IDXGIResource_QueryInterface(frame, &IID_ID3D11Texture2D, (void **)&source);
    IDXGIResource_Release(frame);
    if (SUCCEEDED(hr)) {
        ID3D11DeviceContext_CopyResource(capture->context, (ID3D11Resource *)capture->staging, (ID3D11Resource *)source);
        ID3D11Texture2D_Release(source);
    }
    D3D11_MAPPED_SUBRESOURCE map;
    if (SUCCEEDED(hr))
        hr = ID3D11DeviceContext_Map(capture->context, (ID3D11Resource *)capture->staging, 0, D3D11_MAP_READ, 0, &map);
    if (SUCCEEDED(hr)) {
        for (unsigned y = 0; y < height; y++) {
            const uint32_t *src = (const uint32_t *)((const uint8_t *)map.pData +
                (y * capture->height / height) * map.RowPitch);
            uint32_t *dst = (uint32_t *)pixels;
            for (unsigned x = 0; x < width; x++) dst[x] = src[x * capture->width / width];
            pixels += stride;
        }
        ID3D11DeviceContext_Unmap(capture->context, (ID3D11Resource *)capture->staging, 0);
    }
    HRESULT released = IDXGIOutputDuplication_ReleaseFrame(capture->output);
    return FAILED(hr) || FAILED(released) ? -1 : capture->pointer_visible ? 2 : 1;
}
