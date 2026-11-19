/* SPDX-License-Identifier: GPL-3.0-or-later */
#include <string.h>
#include "../wupstream.h"

#define EXPORT __declspec(dllexport)

EXPORT wupstream *host_open(const char *ip, int timeout, int (*cancel)(void *)) {
    return wupstream_open_cancel(ip, timeout, cancel, NULL);
}

EXPORT void host_close(wupstream *w) { wupstream_close(w); }
EXPORT int host_frame(wupstream *w, const void *pixels, int stride) {
    return wupstream_frame(w, pixels, stride, WUPSTREAM_BGRA);
}
EXPORT int host_audio(wupstream *w, const int16_t *samples, int frames) { return wupstream_audio(w, samples, frames); }
EXPORT int host_mic(wupstream *w, int16_t *samples, int frames) { return wupstream_mic(w, samples, frames); }
EXPORT void host_rumble(wupstream *w, int on) { wupstream_rumble(w, on); }
EXPORT void host_light(wupstream *w, int level) { wupstream_backlight(w, level, 0); }

EXPORT int host_input(wupstream *w, uint8_t *report, int *aux) {
    wupstream_input_t in;
    int result = wupstream_input(w, &in);
    if (result) return result;
    memcpy(report, in.report, sizeof in.report);
    aux[0] = in.touched;
    aux[1] = in.tx;
    aux[2] = in.ty;
    memcpy(aux + 3, in.accel, sizeof in.accel);
    memcpy(aux + 6, in.gyro, sizeof in.gyro);
    aux[9] = in.volume;
    aux[10] = in.backlight;
    return 0;
}
