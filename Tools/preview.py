#!/usr/bin/env python3
"""Serve the real Unity WebGL build from a stable local origin."""
from pathlib import Path
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
import argparse, functools, webbrowser
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--port',type=int,default=8918);p.add_argument('--no-open',action='store_true');args=p.parse_args()
if not 1024<=args.port<=65535:p.error('port must be 1024..65535')
folder=root/'Build/WebGL'
if not (folder/'index.html').exists():raise SystemExit('请先在 Unity 菜单「土豆子的港硕生活 / 构建 WebGL」生成游戏。')
class Handler(SimpleHTTPRequestHandler):
    extensions_map={**SimpleHTTPRequestHandler.extensions_map,'.wasm':'application/wasm','.data':'application/octet-stream'}
    def end_headers(self):
        self.send_header('Cache-Control','no-cache');super().end_headers()
server=ThreadingHTTPServer(('127.0.0.1',args.port),functools.partial(Handler,directory=str(folder)))
url=f'http://127.0.0.1:{args.port}'
print(f'土豆子的港硕生活：{url}\n保持此终端打开。按 Ctrl+C 停止。',flush=True)
if not args.no_open:webbrowser.open(url)
try:server.serve_forever()
except KeyboardInterrupt:pass
finally:server.server_close()
