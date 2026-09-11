"""用游戏自带的 FMOD 库渲染事件；只写 WAV，不使用服务器声卡。"""
import ctypes as c
import json
import os
from pathlib import Path
import sys
import uuid
import wave
import array


def run(request):
    if sys.platform != 'win32' or c.sizeof(c.c_void_p) != 8:
        raise ValueError('试听需要 Windows 和 64 位 Python 3。')
    dll_dir = Path(request['dllDir']).resolve()
    dll_cookie = os.add_dll_directory(str(dll_dir))
    core = c.CDLL(str(dll_dir / 'fmod.dll'))
    studio = c.CDLL(str(dll_dir / 'fmodstudio.dll'))

    def call(lib, name, *args):
        result = getattr(lib, name)(*args)
        if result:
            raise RuntimeError(f'{name} 失败（FMOD 错误码 {result}）。请检查 bank 版本、样本依赖和插件。')

    system, mixer = c.c_void_p(), c.c_void_p()
    call(studio, 'FMOD_Studio_System_Create', c.byref(system), 0x00020300)
    output = request.get('output')
    try:
        call(studio, 'FMOD_Studio_System_GetCoreSystem', system, c.byref(mixer))
        version = c.c_uint()
        call(core, 'FMOD_System_GetVersion', mixer, c.byref(version))
        call(core, 'FMOD_System_SetOutput', mixer, 5 if output else 4)
        call(core, 'FMOD_System_SetSoftwareFormat', mixer, 48000, 3, 0)
        call(core, 'FMOD_System_SetDSPBufferSize', mixer, 1024, 4)
        filename = c.create_string_buffer(os.fsencode(output)) if output else None
        # 同步 Studio 更新；流解码随 update 推进，防止离线渲染跳音。
        call(studio, 'FMOD_Studio_System_Initialize', system, 128, 4, 1, filename)
        banks = []
        for filename in request['banks']:
            bank = c.c_void_p()
            call(studio, 'FMOD_Studio_System_LoadBankFile', system, str(filename).encode('utf-8'), 0, c.byref(bank))
            banks.append(bank)
        names = request.get('guidNames', {})
        events = []
        for bank, bank_file in zip(banks, request['banks']):
            count = c.c_int()
            call(studio, 'FMOD_Studio_Bank_GetEventCount', bank, c.byref(count))
            descriptions = (c.c_void_p * count.value)()
            call(studio, 'FMOD_Studio_Bank_GetEventList', bank, descriptions, count, c.byref(count))
            for pointer in descriptions:
                desc = c.c_void_p(pointer)
                guid = (c.c_ubyte * 16)()
                call(studio, 'FMOD_Studio_EventDescription_GetID', desc, c.byref(guid))
                key = str(uuid.UUID(bytes_le=bytes(guid)))
                name = names.get(key)
                if not name:
                    buf = c.create_string_buffer(2048)
                    length = c.c_int()
                    if studio.FMOD_Studio_EventDescription_GetPath(desc, buf, 2048, c.byref(length)) == 0:
                        name = buf.value.decode('utf-8')
                if name:
                    events.append({'path': name, 'guid': key, 'bank': str(bank_file)})
        if not output:
            return {'version': f'{version.value:08x}', 'events': events}
        desc = c.c_void_p()
        if request.get('guid'):
            guid = (c.c_ubyte * 16).from_buffer_copy(uuid.UUID(request['guid']).bytes_le)
            call(studio, 'FMOD_Studio_System_GetEventByID', system, c.byref(guid), c.byref(desc))
        else:
            call(studio, 'FMOD_Studio_System_GetEvent', system, request['event'].encode('utf-8'), c.byref(desc))
        call(studio, 'FMOD_Studio_EventDescription_LoadSampleData', desc)
        call(studio, 'FMOD_Studio_System_FlushSampleLoading', system)
        instance = c.c_void_p()
        call(studio, 'FMOD_Studio_EventDescription_CreateInstance', desc, c.byref(instance))
        call(studio, 'FMOD_Studio_EventInstance_Start', instance)
        call(studio, 'FMOD_Studio_System_FlushCommands', system)
        # 最长八秒；保留尾音。循环事件不会无限占用进程。
        stopped_at = None
        for step in range(375):
            call(studio, 'FMOD_Studio_System_Update', system)
            state = c.c_int()
            call(studio, 'FMOD_Studio_EventInstance_GetPlaybackState', instance, c.byref(state))
            if state.value == 2:
                stopped_at = step if stopped_at is None else stopped_at
                if step - stopped_at >= 24:
                    break
        call(studio, 'FMOD_Studio_EventInstance_Stop', instance, 1)
        call(studio, 'FMOD_Studio_EventInstance_Release', instance)
    finally:
        call(studio, 'FMOD_Studio_System_Release', system)
        dll_cookie.close()
    # WAVWRITER 输出 PCM16。检查非静音，避免把缺样本伪装为试听成功。
    with wave.open(output, 'rb') as wav:
        samples = array.array('h', wav.readframes(wav.getnframes()))
        peak = max((abs(value) for value in samples), default=0)
        duration = wav.getnframes() / wav.getframerate()
    if peak < 2:
        raise ValueError('事件渲染结果为静音，请检查默认参数、样本、混音总线或所需插件。')
    return {'version': f'{version.value:08x}', 'duration': duration, 'peak': peak}


if __name__ == '__main__':
    try:
        print(json.dumps(run(json.load(sys.stdin)), ensure_ascii=False))
    except Exception as error:
        print(json.dumps({'error': str(error)}, ensure_ascii=False))
        sys.exit(1)
